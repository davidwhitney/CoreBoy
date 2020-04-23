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

        public bool Pause { get; set; }

        private readonly Gpu _gpu;
        private readonly Timer _timer;
        private readonly Dma _dma;
        private readonly Hdma _hdma;
        private readonly IDisplay _display;
        private readonly Sound _sound;
        private readonly SerialPort _serialPort;

        private readonly bool _gbc;

        public Gameboy(
            GameboyOptions options, 
            Cartridge rom, 
            IDisplay display, 
            IController controller,
            ISoundOutput soundOutput,
            SerialEndpoint serialEndpoint)
        {
            _display = display;
            _gbc = rom.Gbc;
            SpeedMode = new SpeedMode();

            var interruptManager = new InterruptManager(_gbc);

            _timer = new Timer(interruptManager, SpeedMode);
            Mmu = new Mmu();

            var oamRam = new Ram(0xfe00, 0x00a0);

            _dma = new Dma(Mmu, oamRam, SpeedMode);
            _gpu = new Gpu(display, interruptManager, _dma, oamRam, _gbc);
            _hdma = new Hdma(Mmu);
            _sound = new Sound(soundOutput, _gbc);
            _serialPort = new SerialPort(interruptManager, serialEndpoint, SpeedMode);

            Mmu.AddAddressSpace(rom);
            Mmu.AddAddressSpace(_gpu);
            Mmu.AddAddressSpace(new Joypad(interruptManager, controller));
            Mmu.AddAddressSpace(interruptManager);
            Mmu.AddAddressSpace(_serialPort);
            Mmu.AddAddressSpace(_timer);
            Mmu.AddAddressSpace(_dma);
            Mmu.AddAddressSpace(_sound);

            Mmu.AddAddressSpace(new Ram(0xc000, 0x1000));
            if (_gbc)
            {
                Mmu.AddAddressSpace(SpeedMode);
                Mmu.AddAddressSpace(_hdma);
                Mmu.AddAddressSpace(new GbcRam());
                Mmu.AddAddressSpace(new UndocumentedGbcRegisters());
            }
            else
            {
                Mmu.AddAddressSpace(new Ram(0xd000, 0x1000));
            }

            Mmu.AddAddressSpace(new Ram(0xff80, 0x7f));
            Mmu.AddAddressSpace(new ShadowAddressSpace(Mmu, 0xe000, 0xc000, 0x1e00));

            Cpu = new Cpu(Mmu, interruptManager, _gpu, display, SpeedMode);

            interruptManager.DisableInterrupts(false);
            
            if (!options.UseBootstrap)
            {
                InitiliseRegisters();
            }
        }

        private void InitiliseRegisters()
        {
            var registers = Cpu.Registers;

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
                if (Pause)
                {
                    Thread.Sleep(1000);
                    continue;
                }

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
            _timer.Tick();
            if (_hdma.IsTransferInProgress())
            {
                _hdma.Tick();
            }
            else
            {
                Cpu.Tick();
            }

            _dma.Tick();
            _sound.Tick();
            _serialPort.Tick();
            return _gpu.Tick();
        }
    }
}