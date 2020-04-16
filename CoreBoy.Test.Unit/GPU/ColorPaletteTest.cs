using System.Collections.Generic;
using System.Text;
using CoreBoy.gpu;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.GPU
{
    class ColorPaletteTest
    {
        [Test]
        public void TestAutoIncrement()
        {
            var p = new ColorPalette(0xff68);
            p.SetByte(0xff68, 0x80);
            p.SetByte(0xff69, 0x00);
            p.SetByte(0xff69, 0xaa);
            p.SetByte(0xff69, 0x11);
            p.SetByte(0xff69, 0xbb);
            p.SetByte(0xff69, 0x22);
            p.SetByte(0xff69, 0xcc);
            p.SetByte(0xff69, 0x33);
            p.SetByte(0xff69, 0xdd);
            p.SetByte(0xff69, 0x44);
            p.SetByte(0xff69, 0xee);
            p.SetByte(0xff69, 0x55);
            p.SetByte(0xff69, 0xff);

            AssertArrayEquals(new[] {0xaa00, 0xbb11, 0xcc22, 0xdd33}, p.GetPalette(0));
            AssertArrayEquals(new[] {0xee44, 0xff55, 0x0000, 0x0000}, p.GetPalette(1));
        }

        private static void AssertArrayEquals(IReadOnlyList<int> expected, IReadOnlyList<int> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);

            for (var i = 0; i < expected.Count; i++)
            {
                if (expected[i] == actual[i]) continue;

                var msg = new StringBuilder();
                msg.AppendLine($"arrays first differed at element [{i}]");
                msg.AppendLine($"Expected :{expected[i]:X4}");
                msg.Append($"Actual   :{actual[i]:X4}");
                Assert.Fail(msg.ToString());
            }
        }
    }
}
