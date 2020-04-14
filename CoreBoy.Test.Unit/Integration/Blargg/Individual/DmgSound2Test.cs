using System.IO;
using CoreBoy.Test.Unit.Integration.Support;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.Integration.Blargg.Individual
{
    public class DmgSound2Test
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("blargg/dmg_sound-2");

        [TestCaseSource(nameof(RomsFrom))]
        public void Test(string filePath)
        {
            var rom = new FileInfo(filePath);
            RomTestUtils.testRomWithMemory(rom);
        }
    }
}