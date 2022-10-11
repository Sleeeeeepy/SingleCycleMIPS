using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS.Cache
{
    public enum CacheReadMissType
    {
        HIT,
        COLD,
        CONFLICT,
        CAPACITY,
        NONE
    }

    public enum CacheWriteMissType
    {
        HIT,
        ALLOCATE,
        NO_ALLOCATE,
        NONE
    }
}
