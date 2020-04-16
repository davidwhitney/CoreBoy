using CoreBoy.cpu;

namespace CoreBoy.Test.Unit.Integration.Support
{
    public interface ITracer
    {
        void Collect(Registers state);
        void Save();
    }
}