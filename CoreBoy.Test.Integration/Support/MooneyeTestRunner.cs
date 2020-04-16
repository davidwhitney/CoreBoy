using System;
using System.Collections.Generic;
using System.IO;
using CoreBoy.controller;
using CoreBoy.cpu;
using CoreBoy.gpu;
using CoreBoy.memory.cart;
using CoreBoy.serial;
using CoreBoy.sound;

namespace CoreBoy.Test.Integration.Support
{
    public class MooneyeTestRunner
    {
        private readonly Gameboy _gb;
        private readonly Cpu _cpu;
        private readonly AddressSpace _mem;
        private readonly Registers _registers;
        private readonly TextWriter _os;
        private readonly ITracer _tracer;

        public MooneyeTestRunner(FileInfo romFileInfo, TextWriter os, bool trace)
        {
            _tracer = trace ? (ITracer) new Tracer(romFileInfo.Name) : new NullTracer();

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
            _gb = new Gameboy(options, cart, new NullDisplay(), new NullController(), new NullSoundOutput(), 
                new NullSerialEndpoint());
            Console.WriteLine("System type: " + (cart.Gbc ? "CGB" : "DMG"));
            Console.WriteLine("Bootstrap: " + (options.UseBootstrap ? "enabled" : "disabled"));
            _cpu = _gb.Cpu;
            _registers = _cpu.Registers;
            _mem = _gb.Mmu;
            _os = os;
        }

        public bool RunTest()
        {
            _tracer.Collect(_gb.Cpu.Registers);

            int divider = 0;
            while (!IsByteSequenceAtPc(0x00, 0x18, 0xfd))
            {
                _gb.Tick();
                if (++divider >= (_gb.SpeedMode.GetSpeedMode() == 2 ? 1 : 4))
                {
                    DisplayProgress();
                    divider = 0;
                }
                
                _tracer.Collect(_gb.Cpu.Registers);
            }

            _tracer.Save();

            return _registers.A == 0 
                   && _registers.B == 3 
                   && _registers.C == 5 
                   && _registers.D == 8 
                   && _registers.E == 13 
                   && _registers.H == 21 
                   && _registers.L == 34;
        }

        private void DisplayProgress()
        {
            if (_cpu.State == State.OPCODE && _mem.getByte(_registers.PC) == 0x22 && _registers.HL >= 0x9800 &&
                _registers.HL < 0x9c00)
            {
                if (_registers.A != 0)
                {
                    _os.Write(_registers.A);
                }
            }
            else if (IsByteSequenceAtPc(0x7d, 0xe6, 0x1f, 0xee, 0x1f))
            {
                _os.Write('\n');
            }
        }

        private bool IsByteSequenceAtPc(params int[] seq)
        {
            if (_cpu.State != State.OPCODE)
            {
                return false;
            }

            int i = _registers.PC;
            bool found = true;
            foreach (int v in seq)
            {
                if (_mem.getByte(i++) != v)
                {
                    found = false;
                    break;
                }
            }

            return found;
        }

    }
}