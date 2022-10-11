namespace SingleCycleMIPS.Cache.Replacement
{
    public class SecondChanceSimulator : IReplacementSimulator
    {
        private readonly int capacity;
        private readonly LinkedList<int> list; //main
        private readonly Dictionary<int, bool> secondChance;
        private int count;
        private int oldest = 0;
        public SecondChanceSimulator(int capacity)
        {
            this.capacity = capacity;
            this.list = new();
            this.count = 0;
            this.secondChance = new Dictionary<int, bool>();
        }

        public (int signature, int victim, bool needReplace) DoSimulateReplace(int signature)
        {
            int victim = 0;
            bool needReplace = false;
            // full
            if (count == capacity)
            {
                if (secondChance.ContainsKey(signature))
                {
                    secondChance[signature] = true;
                    return (signature, victim, needReplace);
                }

                while (true)
                {
                    int i = list.ElementAt(oldest);
                    bool sca = secondChance[i];
                    if (sca)
                    {
                        secondChance[i] = false;
                        oldest = (oldest + 1) % capacity;
                    }
                    else
                    {
                        victim = i;
                        var victimNode = list.Find(victim);
                        list.AddAfter(victimNode, signature);
                        list.Remove(victim);
                        secondChance.Remove(victim);
                        secondChance.Add(signature, false);
                        needReplace = true;
                        oldest = (oldest + 1) % capacity;
                        break;
                    }
                    
                }

            }
            else
            {
                if (secondChance.ContainsKey(signature))
                {
                    secondChance[signature] = true;
                }
                else
                {
                    list.AddLast(signature);
                    secondChance.Add(signature, false);
                    count++;
                }
            }
            return (signature, victim, needReplace);
        }

        public IReplacementSimulator GetNewInstance(int capacity)
        {
            return new SecondChanceSimulator(capacity);
        }
    }
}
