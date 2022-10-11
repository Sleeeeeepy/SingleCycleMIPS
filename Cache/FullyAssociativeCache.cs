using SingleCycleMIPS.Cache.Replacement;
using SingleCycleMIPS.Cache.Write;
using SingleCycleMIPS.Exceptions;
using SingleCycleMIPS.Util;
using System.Collections.Concurrent;

namespace SingleCycleMIPS.Cache
{
    internal class FullyAssociativeCache : IMemoryComponent, ICacheWrite
    {

        public int CacheLineSize { get; private set; }
        public int TagSize { get; private set; }
        public int NumberOfCacheLine { get; private set; }
        private List<CacheLine> memory;
        private int tag_size = 32;
        private int offset_size = 0;
        private IMemoryComponent next;
        private IReplacementSimulator replacementStrategy;
        private IWritePolicy writePolicy;
        private int capacity;
        private int tag_and;
        private int offset_and;

        public FullyAssociativeCache(int size, int cacheLineSize, IMemoryComponent next, IWritePolicy writePolicy, IReplacementSimulator replacementStrategy)
        {
            memory = new();
            this.next = next;
            this.writePolicy = writePolicy;
            this.CacheLineSize = cacheLineSize;
            this.offset_size = cacheLineSize.Lg();
            this.tag_size -= this.offset_size;
            this.capacity = size / cacheLineSize;
            this.replacementStrategy = replacementStrategy.GetNewInstance(capacity);
            this.tag_and = (int)(0xFFFFFFFF >> this.offset_size);
            this.offset_and = (int)(0xFFFFFFFF >> this.tag_size);

            if (writePolicy != null)
            {
                writePolicy.This = this;
                writePolicy.Next = next;
            }
        }

        public IMemoryComponent? GetNext()
        {
            return next;
        }

        public CacheReadMissType GetValue(int address, out int value)
        {
            // 병렬 탐색
            (int tag, int offset) = ResolveAddress(address);
            ConcurrentBag<CacheLine> bag = new();
            (int signature, int victim, bool needReplace) = replacementStrategy.DoSimulateReplace(tag);
            Parallel.ForEach(memory, cacheline =>
            {
                if (cacheline.Valid && cacheline.Tag == tag)
                {
                    bag.Add(cacheline);
                }
            });

            // multiple hit
            if (bag.Count != 1 && !bag.IsEmpty)
                throw new UndefinedBehaviorException("mapping failure.");

            // hit
            if (bag.Count == 1)
            {
                value = bag.First().Line[offset / sizeof(int)];
                return CacheReadMissType.HIT;
            }

            // cold miss
            if (memory.Count < this.capacity)
            {
                CacheLine cacheline = new(CacheLineSize);
                DoAllocate(address, tag, cacheline);
                memory.Add(cacheline);
                value = cacheline.Line[offset / sizeof(int)];
                return CacheReadMissType.COLD;
            }

            // capacity miss
            if (!needReplace)
                throw new UndefinedBehaviorException("capacity max, no need replace, but not hit. please check replacement algorithm.");

            // 다시 한 번 병렬탐색
            bag.Clear();
            var item = Parallel.ForEach(memory, cacheline =>
            {
                if (cacheline.Tag == victim)
                {
                    bag.Add(cacheline);
                }
            });

            if (bag.Count != 1)
                throw new UndefinedBehaviorException("mapping failure.");

            var victimLine = bag.First();
            DoAllocate(address, victim, victimLine);
            value = victimLine.Line[offset / sizeof(int)];
            return CacheReadMissType.CAPACITY;
        }

        public CacheWriteMissType WriteReplace(int address, int value, out CacheLine cacheLine)
        {
            // 병렬 탐색
            (int tag, int offset) = ResolveAddress(address);
            ConcurrentBag<CacheLine> bag = new();
            (int signature, int victim, bool needReplace) = replacementStrategy.DoSimulateReplace(tag);
            Parallel.ForEach(memory, cacheline =>
            {
                if (cacheline.Valid && cacheline.Tag == tag)
                {
                    bag.Add(cacheline);
                }
            });

            // multiple hit
            if (bag.Count != 1 && !bag.IsEmpty)
                throw new UndefinedBehaviorException("mapping failure.");

            // hit
            if (bag.Count == 1)
            {
                cacheLine = bag.First();
                cacheLine.Line[offset / sizeof(int)] = value;
                //cacheLine.Dirty = true;
                return CacheWriteMissType.HIT;
            }

            // cold miss
            if (memory.Count < this.capacity)
            {
                // Do Allocate
                CacheLine cacheline = new(CacheLineSize);
                int memStart = GetAddress(tag, 0);
                DoAllocate(address, tag, cacheline);
                memory.Add(cacheline);
                cacheLine = cacheline;
                cacheLine.Line[offset / sizeof(int)] = value;
                return CacheWriteMissType.HIT;
            }

            // capacity miss
            if (!needReplace)
                throw new UndefinedBehaviorException("capacity max, no need replace, but not hit. please check replacement algorithm.");

            // 다시 한 번 병렬탐색
            bag.Clear();
            var item = Parallel.ForEach(memory, cacheline =>
            {
                if (cacheline.Tag == victim)
                {
                    bag.Add(cacheline);
                }
            });

            if (bag.Count != 1)
                throw new UndefinedBehaviorException("mapping failure.");

            var victimLine = bag.First();
            DoAllocate(address, victim, victimLine);
            victimLine.Line[offset / sizeof(int)] = value;
            cacheLine = victimLine;
            return CacheWriteMissType.ALLOCATE;
        }

        public CacheWriteMissType WriteDirectly(int address, int value)
        {
            // 병렬 탐색
            (int tag, int offset) = ResolveAddress(address);
            ConcurrentBag<CacheLine> bag = new();
            Parallel.ForEach(memory, cacheline =>
            {
                if (cacheline.Valid && cacheline.Tag == tag)
                {
                    bag.Add(cacheline);
                }
            });

            // multiple hit
            if (bag.Count != 1 && !bag.IsEmpty)
                throw new UndefinedBehaviorException("mapping failure.");

            // hit
            if (bag.Count == 1)
            {
                //replacementStrategy.DoSimulateReplace(tag);
                bag.First().Line[offset / sizeof(int)] = value;
                next.SetValue(address, value);
                return CacheWriteMissType.HIT;
            }

            next.SetValue(address, value);
            return CacheWriteMissType.NO_ALLOCATE;
        }

        private void DoAllocate(int address, int victim, CacheLine cacheLine)
        {
            (int tag, _) = ResolveAddress(address);
            int memStart = GetAddress(tag, 0);
            int victimStart = memStart;
            if (victim != tag)
            {
                victimStart = GetAddress(victim, 0);
            }
            for (int i = 0; i < CacheLineSize / sizeof(int); i++)
            {
                next.GetValue(memStart + (sizeof(int) * i), out int val);
                if (cacheLine.Dirty)
                {
                    next.SetValue(victimStart + (sizeof(int) * i), cacheLine.Line[i]);
                }
                cacheLine.Line[i] = val;
            }
            cacheLine.Tag = tag;
            cacheLine.Valid = true;
            cacheLine.Dirty = false;
        }

        public CacheWriteMissType SetValue(int address, int value)
        {
            if (writePolicy == null)
            {
                return WriteDirectly(address, value);
            }
            return writePolicy.Write(address, value);
        }

        private (int tag, int offset) ResolveAddress(int address)
        {
            int tag = (address >> (this.offset_size)) & tag_and;
            int offset = address & offset_and;
            return (tag, offset);
        }

        private int GetAddress(int tag, int offset)
        {
            int tag_shift = this.offset_size;
            return tag << tag_shift | offset;
        }
    }
}
