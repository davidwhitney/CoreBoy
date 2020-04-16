using CoreBoy.cpu;

namespace CoreBoy.Test.Integration.Support
{
    public interface ITracer
    {
        void Collect(Registers state);
        void Save();
    }
}