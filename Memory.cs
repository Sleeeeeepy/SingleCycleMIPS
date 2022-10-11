using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS
{
    public class Memory
    {
        private int[] mem;
        public Action<int, int>? OnWrite { get; set; }
        public Action<int, int>? OnRead { get; set; }
        private ExecutionStatistics Stats = ExecutionStatistics.GetInstance();

        public Memory(int size = 0x4000000)
        {
            // stack memory
            mem = new int[size];
        }
        
        public void SetValue(int address, int value, ControlLogic control)
        {
            if (control.MemWrite == 1)
            {
                mem[address / 4] = value;
                Stats.MemoryWrite += 1;
                OnWrite?.Invoke(address, value);
            }
            return;
        }

        public int GetValue(int address, ControlLogic control)
        {
            if (control.MemRead == 1)
            {
                var value = mem[address / 4];
                OnRead?.Invoke(address, value);
                Stats.MemoryRead += 1;
                return value;
            }
            return 0;
        }

        public void ForceSetValue(int address, int value)
        {
            mem[address / 4] = value;
        }

        public int ForceGetValue(int address)
        {
            return mem[address / 4];
        }
    }
}
