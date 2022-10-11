using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS.Cache
{
    public interface ICacheWrite
    {
        public CacheWriteMissType WriteDirectly(int address, int value);
        public CacheWriteMissType WriteReplace(int address, int value, out CacheLine cacheLine);
    }
}
