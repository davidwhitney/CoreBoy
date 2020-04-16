using System.IO;
using System.Text;
using CoreBoy.cpu;

namespace CoreBoy.Test.Integration.Support
{
    public class Tracer : ITracer
    {
        private int Counter { set; get; }
        private readonly StringBuilder _log;
        private readonly StreamWriter _outputFile;

        public Tracer(string filename)
        {
            _log = new StringBuilder();
            _outputFile = new StreamWriter($"{filename}.csharp.log") {AutoFlush = true};
        }

        public void Collect(Registers state)
        {
            _outputFile.WriteLine(state.ToString());

            if (Counter % 10000 == 0)
            {
                _outputFile.Flush();
            }

            Counter++;
        }

        public void Save() => _outputFile.Flush();
    }
}