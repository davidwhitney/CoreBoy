using System.IO;
using CoreBoy.Test.Unit.Integration.Support;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.Integration.Blargg.Individual
{
    [TestFixture, Timeout(1000 * 60)]
    public class CgbSoundTest
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("blargg\\cgb_sound");

        [Test]
        [TestCaseSource(nameof(RomsFrom))]
        public void Execute(string filePath)
        {
            var rom = new FileInfo(filePath);
            RomTestUtils.testRomWithMemory(rom);
        }
    }
}