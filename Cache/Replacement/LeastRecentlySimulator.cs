using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS.Cache.Replacement
{
    public class LeastRecentlySimulator : IReplacementSimulator
    {
        private readonly int capacity;
        private readonly LinkedList<int> deque;
        private readonly HashSet<int> hashSet;

        public LeastRecentlySimulator(int capacity)
        {
            this.capacity = capacity;
            this.deque = new();
            this.hashSet = new HashSet<int>(capacity);
        }

        // from geeksforgeeks
        public (int signature, int victim, bool needReplace) DoSimulateReplace(int signature)
        {
            bool needReplace = false;
            int victim = 0;
            if (!hashSet.Contains(signature))
            {
                if (deque.Count == capacity)
                {
                    int last = deque.Last();
                    victim = last;
                    deque.RemoveLast();
                    hashSet.Remove(last);
                    needReplace = true;
                }
            }
            else
            {
                deque.Remove(signature);
            }
            deque.AddFirst(signature);
            hashSet.Add(signature);
            return (signature, victim, needReplace);
        }

        public IReplacementSimulator GetNewInstance(int capacity)
        {
            return new LeastRecentlySimulator(capacity);
        }
    }
}
