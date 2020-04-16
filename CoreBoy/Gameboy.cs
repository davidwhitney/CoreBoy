using System.Diagnostics;
using System.Threading;
using CoreBoy.controller;
using CoreBoy.cpu;
using CoreBoy.gpu;
using CoreBoy.gui;
using CoreBoy.memory;
using CoreBoy.memory.cart;
using CoreBoy.serial;
using CoreBoy.sound;
using Timer = CoreBoy.timer.Timer;

namespace CoreBoy
{
    public class Gameboy : IRunnable
    {
        public static readonly int TicksPerSec = 4_194_304;

        public Mmu Mmu { get; }
        public Cpu Cpu { get; }
        public SpeedMode SpeedMode { get; }

        private readonly Gpu _gpu;
        private readonly Timer _timer;
        private readonly Dma _dma;
        private readonly Hdma _hdma;
        private readonly IDisplay _display;
        private readonly Sound _sound;
        private readonly SerialPort _serialPort;
        private readonly InterruptManager _interruptManager;

        private readonly bool _gbc;

        public Gameboy(
            GameboyOptions options, 
            Cartridge rom, 
            IDisplay display, 
            IController controller,
            SoundOutput soundOutput,
            SerialEndpoint serialEndpoint)
        {
            _display = display;
            _gbc = rom.Gbc;
            SpeedMode = new SpeedMode();

            _interruptManager = new InterruptManager(_gbc);
            _timer = new Timer(_interruptManager, SpeedMode);
            Mmu = new Mmu();

            var oamRam = new Ram(0xfe00, 0x00a0);

            _dma = new Dma(Mmu, oamRam, SpeedMode);
            _gpu = new Gpu(display, _interruptManager, _dma, oamRam, _gbc);
            _hdma = new Hdma(Mmu);
            _sound = new Sound(soundOutput, _gbc);
            _serialPort = new SerialPort(_interruptManager, serialEndpoint, SpeedMode);

            Mmu.addAddressSpace(rom);
            Mmu.addAddressSpace(_gpu);
            Mmu.addAddressSpace(new Joypad(_interruptManager, controller));
            Mmu.addAddressSpace(_interruptManager);
            Mmu.addAddressSpace(_serialPort);
            Mmu.addAddressSpace(_timer);
            Mmu.addAddressSpace(_dma);
            Mmu.addAddressSpace(_sound);

            Mmu.addAddressSpace(new Ram(0xc000, 0x1000));
            if (_gbc)
            {
                Mmu.addAddressSpace(SpeedMode);
                Mmu.addAddressSpace(_hdma);
                Mmu.addAddressSpace(new GbcRam());
                Mmu.addAddressSpace(new UndocumentedGbcRegisters());
            }
            else
            {
                Mmu.addAddressSpace(new Ram(0xd000, 0x1000));
            }

            Mmu.addAddressSpace(new Ram(0xff80, 0x7f));
            Mmu.addAddressSpace(new ShadowAddressSpace(Mmu, 0xe000, 0xc000, 0x1e00));

            Cpu = new Cpu(Mmu, _interruptManager, _gpu, display, SpeedMode);

            _interruptManager.DisableInterrupts(false);
            
            if (!options.UseBootstrap)
            {
                InitiliseRegisters();
            }
        }

        private void InitiliseRegisters()
        {
            var registers = Cpu.GetRegisters();

            registers.SetAf(0x01b0);
            if (_gbc)
            {
                registers.A = 0x11;
            }

            registers.SetBc(0x0013);
            registers.SetDe(0x00d8);
            registers.SetHl(0x014d);
            registers.SP = 0xfffe;
            registers.PC = 0x0100;
        }

        public void Run(CancellationToken token)
        {
            var requestedScreenRefresh = false;
            var lcdDisabled = false;
            
            while (!token.IsCancellationRequested)
            {
                var newMode = Tick();
                if (newMode.HasValue)
                {
                    _hdma.OnGpuUpdate(newMode.Value);
                }

                if (!lcdDisabled && !_gpu.IsLcdEnabled())
                {
                    lcdDisabled = true;
                    _display.RequestRefresh();
                    _hdma.OnLcdSwitch(false);
                }
                else if (newMode == Gpu.Mode.VBlank)
                {
                    requestedScreenRefresh = true;
                    _display.RequestRefresh();
                }

                if (lcdDisabled && _gpu.IsLcdEnabled())
                {
                    lcdDisabled = false;
                    _display.WaitForRefresh();
                    _hdma.OnLcdSwitch(true);
                }
                else if (requestedScreenRefresh && newMode == Gpu.Mode.OamSearch)
                {
                    requestedScreenRefresh = false;
                    _display.WaitForRefresh();
                }
            }
        }

        public Gpu.Mode? Tick()
        {
            _timer.tick();
            if (_hdma.IsTransferInProgress())
            {
                _hdma.Tick();
            }
            else
            {
                Cpu.Tick();
            }

            _dma.Tick();
            _sound.tick();
            _serialPort.Tick();
            return _gpu.Tick();
        }
    }
}