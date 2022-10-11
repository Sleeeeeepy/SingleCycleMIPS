namespace SingleCycleMIPS.Cache
{
    public partial class CacheFactory
    {
        public enum WritePolicyType
        {
            WriteBack,
            WriteThrough,
            WriteThroughNoAllocate
        }
    }
}
