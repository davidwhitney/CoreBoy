using CoreBoy.sound;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.Sound
{
    [TestFixture]
    public class LfsrTest
    {

        [Test]
        public void testLfsr()
        {
            Lfsr lfsr = new Lfsr();
            int previousValue = 0;
            for (int i = 0; i < 100; i++)
            {
                lfsr.NextBit(false);
                Assert.AreNotEqual(previousValue, lfsr.Value);
                previousValue = lfsr.Value;
            }
        }

        [Test]
        public void testLfsrWidth7()
        {
            Lfsr lfsr = new Lfsr();
            int previousValue = 0;
            for (int i = 0; i < 100; i++)
            {
                lfsr.NextBit(true);
                Assert.AreNotEqual(previousValue, lfsr.Value);
                previousValue = lfsr.Value;
            }
        }
    }
}