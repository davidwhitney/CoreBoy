using System.Collections.Generic;
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

        private readonly InterruptManager _interruptManager;

        private readonly Gpu _gpu;
        private readonly Mmu _mmu;
        private readonly Cpu _cpu;
        private readonly Timer _timer;
        private readonly Dma _dma;
        private readonly Hdma _hdma;
        private readonly IDisplay _display;
        private readonly Sound _sound;
        private readonly SerialPort _serialPort;

        private readonly bool _gbc;
        private readonly SpeedMode _speedMode;

        private volatile bool _doStop;

        private readonly List<Thread> _tickListeners = new List<Thread>();

        public Gameboy(GameboyOptions options, Cartridge rom, IDisplay display, Controller controller,
            SoundOutput soundOutput, SerialEndpoint serialEndpoint)
        {
            _display = display;
            _gbc = rom.isGbc();
            _speedMode = new SpeedMode();
            _interruptManager = new InterruptManager(_gbc);
            _timer = new Timer(_interruptManager, _speedMode);
            _mmu = new Mmu();

            var oamRam = new Ram(0xfe00, 0x00a0);

            _dma = new Dma(_mmu, oamRam, _speedMode);
            _gpu = new Gpu(display, _interruptManager, _dma, oamRam, _gbc);
            _hdma = new Hdma(_mmu);
            _sound = new Sound(soundOutput, _gbc);
            _serialPort = new SerialPort(_interruptManager, serialEndpoint, _speedMode);
            _mmu.addAddressSpace(rom);
            _mmu.addAddressSpace(_gpu);
            _mmu.addAddressSpace(new Joypad(_interruptManager, controller));
            _mmu.addAddressSpace(_interruptManager);
            _mmu.addAddressSpace(_serialPort);
            _mmu.addAddressSpace(_timer);
            _mmu.addAddressSpace(_dma);
            _mmu.addAddressSpace(_sound);

            _mmu.addAddressSpace(new Ram(0xc000, 0x1000));
            if (_gbc)
            {
                _mmu.addAddressSpace(_speedMode);
                _mmu.addAddressSpace(_hdma);
                _mmu.addAddressSpace(new GbcRam());
                _mmu.addAddressSpace(new UndocumentedGbcRegisters());
            }
            else
            {
                _mmu.addAddressSpace(new Ram(0xd000, 0x1000));
            }

            _mmu.addAddressSpace(new Ram(0xff80, 0x7f));
            _mmu.addAddressSpace(new ShadowAddressSpace(_mmu, 0xe000, 0xc000, 0x1e00));

            _cpu = new Cpu(_mmu, _interruptManager, _gpu, display, _speedMode);

            _interruptManager.disableInterrupts(false);
            
            if (!options.UseBootstrap)
            {
                InitiliseRegisters();
            }
        }

        private void InitiliseRegisters()
        {
            var registers = _cpu.getRegisters();

            registers.setAF(0x01b0);
            if (_gbc)
            {
                registers.setA(0x11);
            }

            registers.setBC(0x0013);
            registers.setDE(0x00d8);
            registers.setHL(0x014d);
            registers.setSP(0xfffe);
            registers.setPC(0x0100);
        }

        public void Run()
        {
            var requestedScreenRefresh = false;
            var lcdDisabled = false;
            _doStop = false;
            while (!_doStop)
            {
                var newMode = Tick();
                if (newMode.HasValue)
                {
                    _hdma.onGpuUpdate(newMode.Value);
                }

                if (!lcdDisabled && !_gpu.IsLcdEnabled())
                {
                    lcdDisabled = true;
                    _display.RequestRefresh();
                    _hdma.onLcdSwitch(false);
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
                    _hdma.onLcdSwitch(true);
                }
                else if (requestedScreenRefresh && newMode == Gpu.Mode.OamSearch)
                {
                    requestedScreenRefresh = false;
                    _display.WaitForRefresh();
                }

                _tickListeners.ForEach(thread => thread.Start());
            }
        }

        public void Stop()
        {
            _doStop = true;
        }

        public Gpu.Mode? Tick()
        {
            _timer.tick();
            if (_hdma.isTransferInProgress())
            {
                _hdma.tick();
            }
            else
            {
                _cpu.tick();
            }

            _dma.Tick();
            _sound.tick();
            _serialPort.tick();
            return _gpu.Tick();
        }
    }
}