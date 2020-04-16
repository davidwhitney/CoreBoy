using System.IO;
using CoreBoy.Test.Unit.Integration.Support;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.Integration.Mooneye
{
    [TestFixture, /*Timeout(1000 * 60)*/]
    public class EmulatorOnlyTest
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("mooneye/emulator-only", SearchOption.AllDirectories);

        [Test, Parallelizable(ParallelScope.All)]
        [TestCaseSource(nameof(RomsFrom))]
        public void Execute(string filePath)
        {
            var rom = new FileInfo(filePath);
            RomTestUtils.testMooneyeRom(rom);
        }
    }
}