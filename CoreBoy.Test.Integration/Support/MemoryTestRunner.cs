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
        private readonly Gameboy _gb;
        private readonly StringBuilder _text;
        private readonly TextWriter _os;
        private bool _testStarted;
        private ITracer _tracer;

        public MemoryTestRunner(FileInfo romFileInfo, TextWriter os, bool trace = false)
        {
            _tracer = trace ? (ITracer)new Tracer(romFileInfo.Name) : new NullTracer();

            var options = new GameboyOptions(romFileInfo);
            var cart = new Cartridge(options);
            _gb = new Gameboy(options, cart, new NullDisplay(), new NullController(), new NullSoundOutput(),
                new NullSerialEndpoint());
            _text = new StringBuilder();
            this._os = os;
        }

        public TestResult RunTest()
        {
            _tracer.Collect(_gb.Cpu.Registers);

            int status = 0x80;
            int divider = 0;
            while (status == 0x80 && !SerialTestRunner.IsInfiniteLoop(_gb))
            {
                _gb.Tick();
                if (++divider >= (_gb.SpeedMode.GetSpeedMode() == 2 ? 1 : 4))
                {
                    status = GetTestResult(_gb);
                    divider = 0;
                }
                
                _tracer.Collect(_gb.Cpu.Registers);
            }

            _tracer.Save();

            return new TestResult(status, _text.ToString());
        }

        private int GetTestResult(Gameboy gb)
        {
            IAddressSpace mem = gb.Mmu;
            if (!_testStarted)
            {
                var i = 0xa000;
                foreach (var v in new[] {0x80, 0xde, 0xb0, 0x61})
                {
                    if (mem.GetByte(i++) != v)
                    {
                        return 0x80;
                    }
                }

                _testStarted = true;
            }

            int status = mem.GetByte(0xa000);

            if (gb.Cpu.State != State.OPCODE)
            {
                return status;
            }

            var reg = gb.Cpu.Registers;

            int ii = reg.PC;
            foreach (int v in new int[] {0xe5, 0xf5, 0xfa, 0x83, 0xd8})
            {
                if (mem.GetByte(ii++) != v)
                {
                    return status;
                }
            }

            var c = (char) reg.A;
            _text.Append(c);
            _os?.Write(c);

            reg.PC += 0x19;
            return status;
        }

        public class TestResult
        {

            private readonly int _status;

            private readonly string _text;

            public TestResult(int status, string text)
            {
                this._status = status;
                this._text = text;
            }

            public int GetStatus()
            {
                return _status;
            }

            public string GetText()
            {
                return _text;
            }
        }
    }
}