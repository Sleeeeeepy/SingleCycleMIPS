namespace SingleCycleMIPS.Cache.Replacement
{
    public class RandomSimulator : IReplacementSimulator
    {
        private readonly int capacity;
        private readonly List<int> list;
        private readonly Random rand;

        public RandomSimulator(int capacity)
        {
            this.capacity = capacity;
            this.list = new(capacity);
            this.rand = new Random(DateTime.Now.Millisecond);
        }

        public (int signature, int victim, bool needReplace) DoSimulateReplace(int signature)
        {
            var needReplace = false;
            var count = list.Count;
            int victim = 0;
            if (list.Contains(signature))
            {
                return (signature, victim, needReplace); // false
            }

            if (count == capacity)
            {
                var random_value = rand.Next(0, count - 1);
                victim = list.ElementAt(random_value);
                list.RemoveAt(random_value);
                needReplace = true;
                list.Add(signature);
                return (signature, victim, needReplace); // true
            } 
            else
            {
                list.Add(signature);
                //int sig = list.ElementAt(signature);
                return (signature, victim, needReplace); //false
            }
        }

        public IReplacementSimulator GetNewInstance(int capacity)
        {
            return new RandomSimulator(capacity);
        }
    }
}
