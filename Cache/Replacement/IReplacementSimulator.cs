using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS.Cache.Replacement
{
    public interface IReplacementSimulator
    {
        public (int signature, int victim, bool needReplace) DoSimulateReplace(int signature);
        public IReplacementSimulator GetNewInstance(int capacity);
    }
}
