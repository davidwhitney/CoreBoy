using System.IO;
using CoreBoy.Test.Integration.Support;
using NUnit.Framework;

namespace CoreBoy.Test.Integration.Mooneye
{
    [Ignore("JVMFailed")]
    [TestFixture, Timeout(1000 * 60)]
    public class InterruptsTest
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("mooneye/acceptance/interrupts");

        [Test, Parallelizable(ParallelScope.All)]
        [TestCaseSource(nameof(RomsFrom))]
        public void Execute(string filePath)
        {
            var rom = new FileInfo(filePath);
            RomTestUtils.testMooneyeRom(rom);
        }
    }
}