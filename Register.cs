using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS
{
    public class Register
    {
        private readonly int[] reg;
        public Action<int, int>? OnWrite { get; set; }
        public Action<int, int>? OnRead { get; set; }
        
        public Register()
        {
            this.reg = new int[32];
            this.reg[29] = 0x1000000;
            this.reg[31] = -1;
        }

        public Register(int sp)
        {
            this.reg = new int[32];
            this.reg[29] = sp;
            this.reg[31] = -1;
        }

        public int GetValue(int register)
        {
            var value = reg[register];
            OnRead?.Invoke(register, value);
            return value;
        }

        public void SetValue(int register, int value, ControlLogic control)
        {
            if (control.RegWrite == 1)
            {
                reg[register] = value;
                OnWrite?.Invoke(register, value);
            }
        }

        public void ForceSetValue(int register, int value)
        {
            reg[register] = value;
        }
    }
}
