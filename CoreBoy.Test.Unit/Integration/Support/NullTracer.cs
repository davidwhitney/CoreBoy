using System.Text;
using CoreBoy.cpu;

namespace CoreBoy.Test.Unit.Integration.Support
{
    public class NullTracer : ITracer
    {
        public void Collect(Registers state)
        {
        
        }
        public void Save()
        {
        }
    }
}