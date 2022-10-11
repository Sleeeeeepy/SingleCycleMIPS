namespace SingleCycleMIPS.Cache
{
    public class CacheLine
    {
        public bool Valid { get; set; } = false;
        public bool Dirty { get; set; } = false;
        public int Tag { get; set; }
        public int[] Line { get; set; }

        public CacheLine(int cacheLineSize)
        {
            this.Line = new int[cacheLineSize / sizeof(int)];
        }
    }
}
