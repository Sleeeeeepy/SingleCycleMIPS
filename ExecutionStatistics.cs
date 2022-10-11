namespace SingleCycleMIPS
{
    public class ExecutionStatistics
    {
        public int IType { get; set; } = 0;
        public int RType { get; set; } = 0;
        public int JType { get; set; } = 0;
        public int MemoryWrite { get; set; } = 0;
        public int MemoryRead { get; set; } = 0;
        public int ExecutedInstruction { get; set; } = 0;
        public int Branch { get; set; } = 0;

        private static ExecutionStatistics? instance;
        private static readonly object lockobj = new();
        private ExecutionStatistics() { }

        public static ExecutionStatistics GetInstance()
        {
            lock (lockobj)
            {
                if (instance == null)
                    instance = new ExecutionStatistics();
            }
            return instance;
        }
    }
}
