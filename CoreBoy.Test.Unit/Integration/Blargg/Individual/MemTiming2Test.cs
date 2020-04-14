using System.IO;
using CoreBoy.Test.Unit.Integration.Support;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.Integration.Blargg.Individual
{
    public class MemTiming2Test
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("blargg/mem_timing-2");

        [TestCaseSource(nameof(RomsFrom))]
        public void Test(string filePath)
        {
            var rom = new FileInfo(filePath);
            RomTestUtils.testRomWithMemory(rom);
        }
    }
}