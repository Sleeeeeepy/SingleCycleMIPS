using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS.Cache.Write
{
    public class WriteThroughNoAllocatePolicy : IWritePolicy
    {
        public ICacheWrite This { get; set; }
        public IMemoryComponent Next { get; set; }

        public CacheWriteMissType Write(int address, int value)
        {
            var ret = This.WriteDirectly(address, value);
            return ret;
        }
    }
}
