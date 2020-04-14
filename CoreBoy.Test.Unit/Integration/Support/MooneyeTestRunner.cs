using System;
using System.Collections.Generic;
using System.IO;
using CoreBoy.controller;
using CoreBoy.cpu;
using CoreBoy.gpu;
using CoreBoy.memory.cart;
using CoreBoy.serial;
using CoreBoy.sound;

namespace CoreBoy.Test.Unit.Integration.Support
{
    public class MooneyeTestRunner
    {

        private readonly Gameboy gb;

        private readonly Cpu cpu;

        private readonly AddressSpace mem;

        private readonly Registers regs;

        private readonly TextWriter os;

        public MooneyeTestRunner(FileInfo romFileInfo, TextWriter os)
        {
            var opts = new List<string>();
            if (romFileInfo.ToString().EndsWith("-C.gb") || romFileInfo.ToString().EndsWith("-cgb.gb"))
            {
                opts.Add("c");
            }

            if (romFileInfo.Name.StartsWith("boot_"))
            {
                opts.Add("b");
            }

            opts.Add("db");
            var options = new GameboyOptions(romFileInfo, new List<string>(), opts);
            var cart = new Cartridge(options);
            gb = new Gameboy(options, cart, new NullDisplay(), new NullController(), new NullSoundOutput(), 
                new NullSerialEndpoint());
            Console.WriteLine("System type: " + (cart.Gbc ? "CGB" : "DMG"));
            Console.WriteLine("Bootstrap: " + (options.UseBootstrap ? "enabled" : "disabled"));
            cpu = gb.Cpu;
            regs = cpu.Registers;
            mem = gb.Mmu;
            this.os = os;
        }

        public bool runTest()
        {
            int divider = 0;
            while (!isByteSequenceAtPc(0x00, 0x18, 0xfd))
            {
                // infinite loop
                gb.Tick();
                if (++divider >= (gb.SpeedMode.GetSpeedMode() == 2 ? 1 : 4))
                {
                    displayProgress();
                    divider = 0;
                }
            }

            return regs.A == 0 
                   && regs.B == 3 
                   && regs.C == 5 
                   && regs.D == 8 
                   && regs.E == 13 
                   && regs.H == 21 
                   && regs.L == 34;
        }

        private void displayProgress()
        {
            if (cpu.State == State.OPCODE && mem.getByte(regs.PC) == 0x22 && regs.HL >= 0x9800 &&
                regs.HL < 0x9c00)
            {
                if (regs.A != 0)
                {
                    os.Write(regs.A);
                }
            }
            else if (isByteSequenceAtPc(0x7d, 0xe6, 0x1f, 0xee, 0x1f))
            {
                os.Write('\n');
            }
        }

        private bool isByteSequenceAtPc(params int[] seq)
        {
            if (cpu.State != State.OPCODE)
            {
                return false;
            }

            int i = regs.PC;
            bool found = true;
            foreach (int v in seq)
            {
                if (mem.getByte(i++) != v)
                {
                    found = false;
                    break;
                }
            }

            return found;
        }

    }
}