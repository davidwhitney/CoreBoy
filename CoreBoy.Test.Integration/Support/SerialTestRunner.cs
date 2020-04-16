using System.IO;
using System.Text;
using CoreBoy.controller;
using CoreBoy.cpu;
using CoreBoy.gpu;
using CoreBoy.memory.cart;
using CoreBoy.serial;
using CoreBoy.sound;

namespace CoreBoy.Test.Unit.Integration.Support
{
    public class SerialTestRunner : SerialEndpoint
    {

        private readonly Gameboy gb;
        private readonly StringBuilder text;
        private readonly TextWriter os;

        public SerialTestRunner(FileInfo romFileInfo, TextWriter os)
        {
            var options = new GameboyOptions(romFileInfo);
            var cart = new Cartridge(options);
            gb = new Gameboy(options, cart, new NullDisplay(), new NullController(), new NullSoundOutput(), this);
            text = new StringBuilder();
            this.os = os;
        }

        public string runTest()
        {
            int divider = 0;
            while (true)
            {
                gb.Tick();
                if (++divider == 4)
                {
                    if (isInfiniteLoop(gb))
                    {
                        break;
                    }

                    divider = 0;
                }
            }

            return text.ToString();
        }

        public int transfer(int outgoing)
        {
            text.Append((char) outgoing);
            os.Write(outgoing);
            os.Flush();
            return 0;
        }

        public static bool isInfiniteLoop(Gameboy gb)
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