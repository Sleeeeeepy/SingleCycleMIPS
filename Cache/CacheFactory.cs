using SingleCycleMIPS.Cache.Write;
using SingleCycleMIPS.Cache.Replacement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SingleCycleMIPS.Exceptions;

namespace SingleCycleMIPS.Cache
{
    public partial class CacheFactory
    {
        public static IMemoryComponent GetFullyAssociativeCacheInstance(int size, int cacheLineSize, IMemoryComponent next, WritePolicyType writeType, ReplacementPolicyType replacementType)
        {
            IWritePolicy? writePolicy;
            IReplacementSimulator? replacementPolicy = null;
            
            if (writeType == WritePolicyType.WriteBack)
            {
                writePolicy = new WriteBackPolicy();
            }
            else
            {
                writePolicy = new WriteThroughPolicy();
            }
            
            switch (replacementType)
            {
                case ReplacementPolicyType.LRU:
                    replacementPolicy = new LeastRecentlySimulator(size / cacheLineSize);
                    break;
                case ReplacementPolicyType.Random:
                    replacementPolicy = new RandomSimulator(size / cacheLineSize);
                    break;
                case ReplacementPolicyType.SecondChance:
                    replacementPolicy = new SecondChanceSimulator(size / cacheLineSize);
                    break;
                case ReplacementPolicyType.Uncondition:
                    replacementPolicy = new UnconditionalSimulator();
                    break;
            }

            if (replacementPolicy == null)
            {
                throw new UndefinedBehaviorException("replacementPolicy");
            }

            var ret = new FullyAssociativeCache(size, cacheLineSize, next, writePolicy, replacementPolicy);
            return ret;
        }

        public static IMemoryComponent GetSetAssociativeCacheInstance(int size, int cacheLineSize, int way, IMemoryComponent next, WritePolicyType writeType, ReplacementPolicyType replacementType)
        {
            IWritePolicy? writePolicy;
            IReplacementSimulator? replacementPolicy = null;

            if (writeType == WritePolicyType.WriteBack)
            {
                writePolicy = new WriteBackPolicy();
            }
            else if (writeType == WritePolicyType.WriteThrough)
            {
                writePolicy = new WriteThroughPolicy();
            } 
            else
            {
                writePolicy = new WriteThroughNoAllocatePolicy();
            }

            switch (replacementType)
            {
                case ReplacementPolicyType.LRU:
                    replacementPolicy = new LeastRecentlySimulator(size / cacheLineSize / way);
                    break;
                case ReplacementPolicyType.Random:
                    replacementPolicy = new RandomSimulator(size / cacheLineSize / way);
                    break;
                case ReplacementPolicyType.SecondChance:
                    replacementPolicy = new SecondChanceSimulator(size / cacheLineSize / way);
                    break;
                case ReplacementPolicyType.Uncondition:
                    replacementPolicy = new UnconditionalSimulator();
                    break;
            }

            if (replacementPolicy == null)
            {
                throw new UndefinedBehaviorException("replacementPolicy");
            }

            var ret = new SetAssociativeCache(size, cacheLineSize, way, next, writePolicy, replacementPolicy);
            return ret;
        }
    }
}
