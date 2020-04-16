using System;
using System.IO;
using System.Text;
using CoreBoy.controller;
using CoreBoy.cpu;
using CoreBoy.gpu;
using CoreBoy.memory.cart;
using CoreBoy.serial;
using CoreBoy.sound;

namespace CoreBoy.Test.Integration.Support
{
    public class MemoryTestRunner
    {
        private readonly Gameboy gb;
        private readonly StringBuilder text;
        private readonly TextWriter os;
        private bool testStarted;

        public MemoryTestRunner(FileInfo romFileInfo, TextWriter os)
        {
            var options = new GameboyOptions(romFileInfo);
            var cart = new Cartridge(options);
            gb = new Gameboy(options, cart, new NullDisplay(), new NullController(), new NullSoundOutput(),
                new NullSerialEndpoint());
            text = new StringBuilder();
            this.os = os;
        }

        public TestResult runTest()
        {
            int status = 0x80;
            int divider = 0;
            while (status == 0x80 && !SerialTestRunner.IsInfiniteLoop(gb))
            {
                gb.Tick();
                if (++divider >= (gb.SpeedMode.GetSpeedMode() == 2 ? 1 : 4))
                {
                    status = getTestResult(gb);
                    divider = 0;
                }
            }

            return new TestResult(status, text.ToString());
        }

        private int getTestResult(Gameboy gb)
        {
            AddressSpace mem = gb.Mmu;
            if (!testStarted)
            {
                var i = 0xa000;
                foreach (var v in new[] {0x80, 0xde, 0xb0, 0x61})
                {
                    if (mem.getByte(i++) != v)
                    {
                        return 0x80;
                    }
                }

                testStarted = true;
            }

            int status = mem.getByte(0xa000);

            if (gb.Cpu.State != State.OPCODE)
            {
                return status;
            }

            var reg = gb.Cpu.Registers;

            int ii = reg.PC;
            foreach (int v in new int[] {0xe5, 0xf5, 0xfa, 0x83, 0xd8})
            {
                if (mem.getByte(ii++) != v)
                {
                    return status;
                }
            }

            var c = (char) reg.A;
            text.Append(c);
            os?.Write(c);

            reg.PC += 0x19;
            return status;
        }

        public class TestResult
        {

            private readonly int status;

            private readonly String text;

            public TestResult(int status, String text)
            {
                this.status = status;
                this.text = text;
            }

            public int getStatus()
            {
                return status;
            }

            public String getText()
            {
                return text;
            }
        }
    }
}