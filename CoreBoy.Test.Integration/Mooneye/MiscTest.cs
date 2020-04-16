using System.IO;
using CoreBoy.Test.Integration.Support;
using NUnit.Framework;

namespace CoreBoy.Test.Integration.Mooneye
{
    [Ignore("JVMFailed")]
    [TestFixture, Timeout(1000 * 60)]
    public class MiscTest
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("mooneye/misc");

        [Test, Parallelizable(ParallelScope.All)]
        [TestCaseSource(nameof(RomsFrom))]
        public void Execute(string filePath)
        {
            // Three of these tests fail in the JVM original.

            var rom = new FileInfo(filePath);
            RomTestUtils.testMooneyeRom(rom);
        }
    }
}