using SingleCycleMIPS.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS
{
    public class Memory : IMemoryComponent
    {
        private readonly int[] mem;
        public Action<int, int>? OnWrite { get; set; }
        public Action<int, int>? OnRead { get; set; }

        public Memory(int size = 0x4000000)
        {
            // stack memory
            mem = new int[size];
        }
        
        public CacheWriteMissType SetValue(int address, int value)
        {
            mem[address / sizeof(int)] = value;
            OnWrite?.Invoke(address, value);
            return CacheWriteMissType.HIT;
        }

        public CacheReadMissType GetValue(int address, out int value)
        {
            value = mem[address / sizeof(int)];
            OnRead?.Invoke(address, value);
            return CacheReadMissType.HIT;
        }

        public void ForceSetValue(int address, int value)
        {
            mem[address / sizeof(int)] = value;
        }

        public int ForceGetValue(int address)
        {
            return mem[address / sizeof(int)];
        }

        public IMemoryComponent? GetNext()
        {
            return null;
        }
    }
}
