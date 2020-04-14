using System.IO;
using CoreBoy.Test.Unit.Integration.Support;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.Integration.Blargg.Individual
{
    public class CgbSoundTest
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("blargg/cgb_sound");

        [TestCaseSource(nameof(RomsFrom))]
        public void Test(string filePath)
        {
            var rom = new FileInfo(filePath);
            RomTestUtils.testRomWithMemory(rom);
        }
    }
}