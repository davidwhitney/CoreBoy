using System;
using System.IO;
using NUnit.Framework;

using static CoreBoy.Test.Unit.Integration.Support.RomTestUtils;

namespace CoreBoy.Test.Unit.Integration.Blargg
{
    [TestFixture]
    public class BlarggRomTest
    {
        [Test]
        public void testCgbSound()
        {
            testRomWithMemory(getPath("cgb_sound.gb"));
        }

        [Test]
        public void testCpuInstrs()
        {
            testRomWithSerial(getPath("cpu_instrs.gb"));
        }

        [Test]
        public void testDmgSound2()
        {
            testRomWithMemory(getPath("dmg_sound-2.gb"));
        }

        [Test]
        public void testHaltBug()
        {
            testRomWithMemory(getPath("halt_bug.gb"));
        }

        [Test]
        public void testInstrTiming()
        {
            testRomWithSerial(getPath("instr_timing.gb"));
        }

        [Test]
        [Ignore("Copying the java impl")]
        public void testInterruptTime()
        {
            testRomWithMemory(getPath("interrupt_time.gb"));
        }

        [Test]
        public void testMemTiming2()
        {
            testRomWithMemory(getPath("mem_timing-2.gb"));
        }

        [Test]
        public void testOamBug2()
        {
            testRomWithMemory(getPath("oam_bug-2.gb"));
        }

        private static FileInfo getPath(String name)
        {
            var root = "C:\\Users\\David Whitney\\OneDrive\\Desktop\\coffee-gb-netcore\\CoreBoy.Test.Integration\\roms";
            return new FileInfo(Path.Combine(root, "blargg", name));
        }
    }
}