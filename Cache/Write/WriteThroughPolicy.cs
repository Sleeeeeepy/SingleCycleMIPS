using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS.Cache.Write
{
    public class WriteThroughPolicy : IWritePolicy
    {
        public ICacheWrite This { get; set; }
        public IMemoryComponent Next { get; set; }

        public CacheWriteMissType Write(int address, int value)
        {
            var ret = This.WriteReplace(address, value, out var cacheline);
            cacheline.Dirty = false;
            Next.SetValue(address, value);
            return ret;
        }
    }
}
