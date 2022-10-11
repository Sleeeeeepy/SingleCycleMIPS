using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SingleCycleMIPS.Cache;
using SingleCycleMIPS.Cache.Replacement;
using SingleCycleMIPS.Cache.Write;

namespace SingleCycleMIPS
{
    public class CacheArgument
    {
        //--cache <cacheType> <cacheSize> <cacheLineSize> <writePolicy> <replacePolicy> -[way]\tUse cache.
        public string CacheType { get; private set; }
        public int CacheSize { get; private set; }
        public int CacheLineSize { get; private set; }
        public CacheFactory.WritePolicyType WritePolicy { get; private set; }
        public CacheFactory.ReplacementPolicyType Replacement { get; private set; }
        public int Way { get; private set; }

        public CacheArgument(string cacheType, string cacheSize, string cacheLineSize, string writePolicy, string replacementPolicy, string way)
        {
            try
            {
                this.CacheSize = Int32.Parse(cacheSize);
                this.CacheLineSize = Int32.Parse(cacheLineSize);
            } 
            catch (Exception)
            {
                Console.Error.WriteLine("fatal error: fail to parse cacheSize and cacheLineSize");
                Environment.Exit(-1);
            }

            switch (writePolicy)
            {
                case "WB":
                    WritePolicy = CacheFactory.WritePolicyType.WriteBack;
                    break;
                case "WT":
                    WritePolicy = CacheFactory.WritePolicyType.WriteThrough;
                    break;
                case "WT_NO":
                    WritePolicy = CacheFactory.WritePolicyType.WriteThroughNoAllocate;
                    break;
                default:
                    Console.Error.WriteLine("fatal error: invalid writePolicy");
                    Environment.Exit(-1);
                    break;
            }

            switch (replacementPolicy)
            {
                case "LRU":
                    Replacement = CacheFactory.ReplacementPolicyType.LRU;
                    break;
                case "RAND":
                    Replacement = CacheFactory.ReplacementPolicyType.Random;
                    break;
                case "SCA":
                    Replacement = CacheFactory.ReplacementPolicyType.SecondChance;
                    break;
                default:
                    Console.Error.WriteLine("fatal error: invalid replacementPolicy");
                    Environment.Exit(-1);
                    break;
            }

            try
            {
                if (way == null)
                {
                    this.Way = 1;
                }
                else
                {
                    this.Way = Int32.Parse(way);
                }

            } 
            catch (Exception)
            {
                this.Way = 1;
            }

            switch (cacheType)
            {
                case "SA":
                case "FA":
                    this.CacheType = cacheType;
                    break;
                default:
                    Console.Error.WriteLine("fatal error: invalid cacheType");
                    Environment.Exit(-1);
                    break;
            }
        }
    }
}
