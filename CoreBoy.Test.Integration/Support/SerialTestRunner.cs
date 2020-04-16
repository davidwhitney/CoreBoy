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
    public class SerialTestRunner : SerialEndpoint
    {

        private readonly Gameboy _gb;
        private readonly StringBuilder _text;
        private readonly TextWriter _os;
        private readonly ITracer _tracer;

        public SerialTestRunner(FileInfo romFileInfo, TextWriter os, bool trace)
        {
            _tracer = trace ? (ITracer)new Tracer(romFileInfo.Name) : new NullTracer();

            var options = new GameboyOptions(romFileInfo);
            var cart = new Cartridge(options);
            _gb = new Gameboy(options, cart, new NullDisplay(), new NullController(), new NullSoundOutput(), this);
            _text = new StringBuilder();
            _os = os;
        }

        public string RunTest()
        {
            _tracer.Collect(_gb.Cpu.Registers);

            int divider = 0;
            while (true)
            {
                _gb.Tick();
                if (++divider == 4)
                {
                    if (IsInfiniteLoop(_gb))
                    {
                        break;
                    }

                    divider = 0;
                }

                _tracer.Collect(_gb.Cpu.Registers);
            }

            return _text.ToString();
        }

        public int transfer(int outgoing)
        {
            _text.Append((char) outgoing);
            _os.Write(outgoing);
            _os.Flush();
            return 0;
        }

        public static bool IsInfiniteLoop(Gameboy gb)
        {
            Cpu cpu = gb.Cpu;
            if (cpu.State != State.OPCODE)
            {
                return false;
            }

            Registers regs = cpu.Registers;
            AddressSpace mem = gb.Mmu;

            int i = regs.PC;
            bool found = true;
            foreach (int v in new int[] {0x18, 0xfe})
            {
                // jr fe
                if (mem.getByte(i++) != v)
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                return true;
            }

            i = regs.PC;
            foreach (int v in new int[] {0xc3, BitUtils.GetLsb(i), BitUtils.GetMsb(i)})
            {
                // jp pc
                if (mem.getByte(i++) != v)
                {
                    return false;
                }
            }

            return true;
        }
    }
}