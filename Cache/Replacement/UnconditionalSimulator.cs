using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS.Cache.Replacement
{
    // for test
    public class UnconditionalSimulator : IReplacementSimulator
    {
        private int memory = 0;
        private int count = 0;
        public (int signature, int victim, bool needReplace) DoSimulateReplace(int signature)
        {
            int victim = memory;
            bool needReplace = false;
            if (count == 1)
            {
                memory = signature;
                needReplace = true;
            }
            else
            {
                memory = signature;
                count++;
            }
            return (signature, victim, needReplace);
        }

        public IReplacementSimulator GetNewInstance(int capacity)
        {
            return new UnconditionalSimulator();
        }
    }
}
