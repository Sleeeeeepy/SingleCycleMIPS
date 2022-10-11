using SingleCycleMIPS.Cache;

namespace SingleCycleMIPS
{
    public interface IMemoryComponent
    {
        CacheWriteMissType SetValue(int address, int value);
        CacheReadMissType GetValue(int address, out int value);
        public IMemoryComponent? GetNext();
    }
}
