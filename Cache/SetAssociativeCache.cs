using SingleCycleMIPS.Cache.Write;
using SingleCycleMIPS.Cache.Replacement;
using SingleCycleMIPS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SingleCycleMIPS.Exceptions;

namespace SingleCycleMIPS.Cache
{
    public partial class SetAssociativeCache : IMemoryComponent, ICacheWrite
    {
        
        public int CacheLineSize { get; private set; }
        public int CacheLineSizePerSet { get; private set; }
        public int TagSize { get; private set; }
        public int NumberOfCacheLine { get; private set; }
        public int NumberOfCacheLinePerSet { get; private set; }
        public int Way { get; private set; }
        public List<IReplacementSimulator> replacements;
        public Action? OnHit { get; set; }
        public Action? OnMiss { get; set; }
        public Action? OnColdMiss { get; set; }

        private List<Dictionary<int, CacheLine>> memory;
        private readonly int tag_size = 32;
        private readonly int index_size = 0;
        private readonly int offset_size = 0;
        private readonly IMemoryComponent next;
        private readonly IWritePolicy writePolicy;
        private readonly int tag_and;
        private readonly int index_and;
        private readonly int offset_and;

        public SetAssociativeCache(int size, int cacheLineSize, int way, IMemoryComponent next, IWritePolicy writePolicy, IReplacementSimulator replacement)
        {
            this.memory = new();
            this.CacheLineSize = cacheLineSize;
            this.CacheLineSizePerSet = cacheLineSize / way;
            this.Way = way.IsPowerOfTwo() ? way : way.ClosetPowerOfTwo();
            this.NumberOfCacheLine = size / cacheLineSize;
            this.NumberOfCacheLinePerSet = this.NumberOfCacheLine / this.Way;
            this.next = next;
            this.writePolicy = writePolicy;
            this.replacements = new();
            this.index_size = this.NumberOfCacheLinePerSet.Lg();
            this.offset_size = this.CacheLineSize.Lg();
            this.tag_size -= (this.index_size + this.offset_size);
            this.tag_and = (int)(0xFFFFFFFF >> (this.index_size + this.offset_size));
            this.index_and = (int)(0xFFFFFFFF << this.tag_size >> this.tag_size >> this.offset_size);
            this.offset_and = (int)(0xFFFFFFFF >> (this.tag_size + this.index_size));
            this.TagSize = (sizeof(bool) * 2 + sizeof(int)) * NumberOfCacheLine;

            for (int i = 0; i < way; i++)
            {
                this.memory.Add(new Dictionary<int, CacheLine>());
                for (int j = 0; j < this.NumberOfCacheLinePerSet; j++)
                {
                    this.memory[i].Add(j, new CacheLine(this.CacheLineSize));
                }
            }
            
            for (int i = 0; i < this.NumberOfCacheLinePerSet; i++)
            {
                replacements.Add(replacement.GetNewInstance(way));
            }

            if (writePolicy != null)
            {
                writePolicy.This = this;
                writePolicy.Next = next;
            }
        }

        private (int tag, int index, int offset) ResolveAddress(int address)
        {
            int tag = (address >> (this.index_size + this.offset_size)) & tag_and;
            int index = (address >> this.offset_size) & index_and;
            int offset = address & offset_and;
            return (tag, index, offset);
        }

        private int GetAddress(int tag, int index, int offset)
        {
            int tag_shift = index_size + offset_size;
            int index_shift = offset_size;
            return tag << tag_shift | index << index_shift | offset;
        }

        public IMemoryComponent? GetNext()
        {
            return next;
        }

        public CacheReadMissType GetValue(int address, out int value)
        {
            (int tag, int index, int offset) = ResolveAddress(address);
            (int signature, int victim, bool needReplace) = replacements[index].DoSimulateReplace(tag);
            // hit
            for (int i = 0; i < Way; i++)
            {
                var cacheline = memory[i][index];
                if (cacheline.Valid && cacheline.Tag == tag)
                {
                    value = cacheline.Line[offset / 4];
                    OnHit?.Invoke();
                    return CacheReadMissType.HIT;
                }
            }

            // cold miss
            var memStart = GetAddress(tag, index, 0);
            for (int i = 0; i < Way; i++)
            {
                var cacheline = memory[i][index];
                if (!cacheline.Valid)
                {
                    DoAllocate(memStart, tag, cacheline);
                    value = cacheline.Line[offset / 4];
                    OnColdMiss?.Invoke();
                    return CacheReadMissType.COLD;
                }
            }

            // capacity miss
            if (!needReplace)
                throw new UndefinedBehaviorException("no hit, no cold miss but no need replacement. please check replacement algorithm.");
            
            for (int i = 0; i < Way; i++)
            {
                var cacheline = memory[i][index];
                if (cacheline.Tag == victim)
                {
                    DoAllocate(address, victim, cacheline);
                    value = cacheline.Line[offset / 4];
                    OnMiss?.Invoke();
                    return CacheReadMissType.CONFLICT;
                }
            }
            throw new UndefinedBehaviorException("no hit, no cold miss, need replacement but no same tag. please check replacement algorithm.");
        }

        private void DoAllocate(int address, int victimTag, CacheLine cacheline)
        {
            (int tag, int index, _) = ResolveAddress(address);
            var memStart = GetAddress(tag, index, 0);
            var victimStart = memStart;
            if (tag != victimTag)
            {
                victimStart = GetAddress(victimTag, index, 0);
            }
            for (int j = 0; j < CacheLineSize / sizeof(int); j++)
            {
                next.GetValue(memStart + (sizeof(int) * j), out int val);
                if (cacheline.Dirty)
                {
                    next.SetValue(victimStart + (sizeof(int) * j), cacheline.Line[j]);
                }
                cacheline.Line[j] = val;
            }
            cacheline.Tag = tag;
            cacheline.Valid = true;
            cacheline.Dirty = false;
        }

        // write back, write through with allocate
        public CacheWriteMissType WriteReplace(int address, int value, out CacheLine cacheLine)
        {
            (int tag, int index, int offset) = ResolveAddress(address);
            (int signature, int victim, bool needReplace) = replacements[index].DoSimulateReplace(tag);
            // hit
            for (int i = 0; i < Way; i++)
            {
                var cacheline = memory[i][index];
                if (cacheline.Valid && cacheline.Tag == tag)
                {
                    cacheline.Line[offset / sizeof(int)] = value;
                    //next.SetValue(address, value);
                    //cacheline.Dirty = true;
                    cacheLine = cacheline;
                    return CacheWriteMissType.HIT;
                }
            }

            // cold miss
            for (int i = 0; i < Way; i++)
            {
                var cacheline = memory[i][index];
                if (!cacheline.Valid)
                {
                    DoAllocate(address, tag, cacheline);
                    cacheline.Line[offset / sizeof(int)] = value;
                    //cacheline.Dirty = true;
                    cacheLine = cacheline;
                    return CacheWriteMissType.ALLOCATE;
                }
            }

            // capacity miss
            if (!needReplace)
                throw new UndefinedBehaviorException("no hit, no cold miss but no need replacement");

            for (int i = 0; i < Way; i++)
            {
                var cacheline = memory[i][index];
                if (cacheline.Tag == victim)
                {
                    DoAllocate(address, victim, cacheline);
                    cacheline.Line[offset / 4] = value;
                    //cacheline.Dirty = true;
                    cacheLine = cacheline;
                    return CacheWriteMissType.ALLOCATE;
                }
            }
            throw new UndefinedBehaviorException("no hit, no cold miss, need replacement but no same tag.");
        }

        public CacheWriteMissType SetValue(int address, int value)
        {
            if (writePolicy == null)
            {
                return WriteDirectly(address, value);
            }
            return writePolicy.Write(address, value);
        }

        // write thorugh no allocate
        public CacheWriteMissType WriteDirectly(int address, int value)
        {
            (int tag, int index, int offset) = ResolveAddress(address);
            for (int i = 0; i < Way; i++)
            {
                var cacheline = memory[i][index];
                if (cacheline.Valid && cacheline.Tag == tag)
                {
                    cacheline.Line[offset / 4] = value;
                    next.SetValue(address, value);
                    return CacheWriteMissType.HIT;
                }
            }

            next.SetValue(address, value);
            return CacheWriteMissType.NO_ALLOCATE;
        }
    }
}
