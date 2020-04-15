using System.IO;
using System.Text;
using CoreBoy.cpu;

namespace CoreBoy.Test.Unit.Integration.Support
{
    public class Tracer : ITracer
    {
        private readonly string _filename;
        private readonly StringBuilder _log;

        public Tracer(string filename)
        {
            _filename = filename;
            _log = new StringBuilder();
        }

        public void Collect(Registers state) => _log.AppendLine(state.ToString());
        public void Save() => File.WriteAllText(_filename, _log.ToString());
    }
}