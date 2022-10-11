namespace SingleCycleMIPS.Cache
{
    public partial class CacheFactory
    {
        public enum ReplacementPolicyType
        {
            Random,
            LRU,
            SecondChance,
            Uncondition
        }
    }
}
