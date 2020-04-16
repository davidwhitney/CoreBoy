using System.IO;
using CoreBoy.Test.Unit.Integration.Support;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.Integration.Blargg.Individual
{
    [TestFixture, Timeout(1000 * 60)]
    public class CpuInstrsTest
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("blargg/cpu_instrs");

        [Test, Parallelizable(ParallelScope.All)]
        [TestCaseSource(nameof(RomsFrom))]
        public void Execute(string filePath)
        {
            var rom = new FileInfo(filePath);
            RomTestUtils.testRomWithSerial(rom);
        }
    }
}
