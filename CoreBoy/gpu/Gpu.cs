using System.Linq;
using CoreBoy.cpu;
using CoreBoy.gpu.phase;
using CoreBoy.memory;

namespace CoreBoy.gpu
{
    public class Gpu : IAddressSpace
    {
        public enum Mode
        {
            HBlank,
            VBlank,
            OamSearch,
            PixelTransfer
        }

        private readonly IAddressSpace _videoRam0;
        private readonly IAddressSpace _videoRam1;
        private readonly IAddressSpace _oamRam;
        private readonly IDisplay _display;
        private readonly InterruptManager _interruptManager;
        private readonly Dma _dma;
        private readonly Lcdc _lcdc;
        private readonly bool _gbc;
        private readonly ColorPalette _bgPalette;
        private readonly ColorPalette _oamPalette;
        private readonly HBlankPhase _hBlankPhase;
        private readonly OamSearch _oamSearchPhase;
        private readonly PixelTransfer _pixelTransferPhase;
        private readonly VBlankPhase _vBlankPhase;
        private readonly MemoryRegisters _r;

        private bool _lcdEnabled = true;
        private int _lcdEnabledDelay;
        private int _ticksInLine;
        private Mode _mode;
        private IGpuPhase _phase;

        public Gpu(IDisplay display, InterruptManager interruptManager, Dma dma, Ram oamRam, bool gbc)
        {
            _r = new MemoryRegisters(GpuRegister.Values().ToArray());
            _lcdc = new Lcdc();
            _interruptManager = interruptManager;
            _gbc = gbc;
            _videoRam0 = new Ram(0x8000, 0x2000);
            _videoRam1 = gbc ? new Ram(0x8000, 0x2000) : null;
            _oamRam = oamRam;
            _dma = dma;

            _bgPalette = new ColorPalette(0xff68);
            _oamPalette = new ColorPalette(0xff6a);
            _oamPalette.FillWithFf();

            _oamSearchPhase = new OamSearch(oamRam, _lcdc, _r);
            _pixelTransferPhase = new PixelTransfer(_videoRam0, _videoRam1, oamRam, display, _lcdc, _r, gbc, _bgPalette,
                _oamPalette);
            _hBlankPhase = new HBlankPhase();
            _vBlankPhase = new VBlankPhase();

            _mode = Mode.OamSearch;
            _phase = _oamSearchPhase.Start();

            _display = display;
        }

        private IAddressSpace GetAddressSpace(int address)
        {
            if (_videoRam0.Accepts(address) /* && mode != Mode.PixelTransfer*/)
            {
                return GetVideoRam();
            }

            if (_oamRam.Accepts(address) &&
                !_dma.IsOamBlocked() /* && mode != Mode.OamSearch && mode != Mode.PixelTransfer*/)
            {
                return _oamRam;
            }

            if (_lcdc.Accepts(address))
            {
                return _lcdc;
            }

            if (_r.Accepts(address))
            {
                return _r;
            }

            if (_gbc && _bgPalette.Accepts(address))
            {
                return _bgPalette;
            }

            if (_gbc && _oamPalette.Accepts(address))
            {
                return _oamPalette;
            }

            return null;
        }

        private IAddressSpace GetVideoRam()
        {
            if (_gbc && (_r.Get(GpuRegister.Vbk) & 1) == 1)
            {
                return _videoRam1;
            }

            return _videoRam0;
        }

        public bool Accepts(int address) => GetAddressSpace(address) != null;

        public void SetByte(int address, int value)
        {
            if (address == GpuRegister.Stat.Address)
            {
                SetStat(value);
                return;
            }

            var space = GetAddressSpace(address);
            if (space == _lcdc)
            {
                SetLcdc(value);
                return;
            }

            space?.SetByte(address, value);
        }

        public int GetByte(int address)
        {
            if (address == GpuRegister.Stat.Address)
            {
                return GetStat();
            }

            var space = GetAddressSpace(address);
            if (space == null)
            {
                return 0xff;
            }

            if (address == GpuRegister.Vbk.Address)
            {
                return _gbc ? 0xfe : 0xff;
            }

            return space.GetByte(address);
        }

        public Mode? Tick()
        {
            if (!_lcdEnabled)
            {
                if (_lcdEnabledDelay != -1)
                {
                    if (--_lcdEnabledDelay == 0)
                    {
                        _display.Enabled = true;
                        _lcdEnabled = true;
                    }
                }
            }

            if (!_lcdEnabled)
            {
                return null;
            }

            var oldMode = _mode;
            _ticksInLine++;
            if (_phase.Tick())
            {
                // switch line 153 to 0
                if (_ticksInLine == 4 && _mode == Mode.VBlank && _r.Get(GpuRegister.Ly) == 153)
                {
                    _r.Put(GpuRegister.Ly, 0);
                    RequestLycEqualsLyInterrupt();
                }
            }
            else
            {
                switch (oldMode)
                {
                    case Mode.OamSearch:
                        _mode = Mode.PixelTransfer;
                        _phase = _pixelTransferPhase.Start(_oamSearchPhase.GetSprites());
                        break;

                    case Mode.PixelTransfer:
                        _mode = Mode.HBlank;
                        _phase = _hBlankPhase.Start(_ticksInLine);
                        RequestLcdcInterrupt(3);
                        break;

                    case Mode.HBlank:
                        _ticksInLine = 0;
                        if (_r.PreIncrement(GpuRegister.Ly) == 144)
                        {
                            _mode = Mode.VBlank;
                            _phase = _vBlankPhase.Start();
                            _interruptManager.RequestInterrupt(InterruptManager.InterruptType.VBlank);
                            RequestLcdcInterrupt(4);
                        }
                        else
                        {
                            _mode = Mode.OamSearch;
                            _phase = _oamSearchPhase.Start();
                        }

                        RequestLcdcInterrupt(5);
                        RequestLycEqualsLyInterrupt();
                        break;

                    case Mode.VBlank:
                        _ticksInLine = 0;
                        if (_r.PreIncrement(GpuRegister.Ly) == 1)
                        {
                            _mode = Mode.OamSearch;
                            _r.Put(GpuRegister.Ly, 0);
                            _phase = _oamSearchPhase.Start();
                            RequestLcdcInterrupt(5);
                        }
                        else
                        {
                            _phase = _vBlankPhase.Start();
                        }

                        RequestLycEqualsLyInterrupt();
                        break;
                }
            }

            if (oldMode == _mode)
            {
                return null;
            }

            return _mode;
        }

        public int GetTicksInLine()
        {
            return _ticksInLine;
        }

        private void RequestLcdcInterrupt(int statBit)
        {
            if ((_r.Get(GpuRegister.Stat) & (1 << statBit)) != 0)
            {
                _interruptManager.RequestInterrupt(InterruptManager.InterruptType.Lcdc);
            }
        }

        private void RequestLycEqualsLyInterrupt()
        {
            if (_r.Get(GpuRegister.Lyc) == _r.Get(GpuRegister.Ly))
            {
                RequestLcdcInterrupt(6);
            }
        }

        private int GetStat()
        {
            return _r.Get(GpuRegister.Stat) | (int) _mode |
                   (_r.Get(GpuRegister.Lyc) == _r.Get(GpuRegister.Ly) ? (1 << 2) : 0) | 0x80;
        }

        private void SetStat(int value)
        {
            _r.Put(GpuRegister.Stat, value & 0b11111000); // last three bits are read-only
        }

        private void SetLcdc(int value)
        {
            _lcdc.Set(value);
            if ((value & (1 << 7)) == 0)
            {
                DisableLcd();
            }
            else
            {
                EnableLcd();
            }
        }

        private void DisableLcd()
        {
            _r.Put(GpuRegister.Ly, 0);
            _ticksInLine = 0;
            _phase = _hBlankPhase.Start(250);
            _mode = Mode.HBlank;
            _lcdEnabled = false;
            _lcdEnabledDelay = -1;
            _display.Enabled = false;
        }

        private void EnableLcd()
        {
            _lcdEnabledDelay = 244;
        }

        public bool IsLcdEnabled()
        {
            return _lcdEnabled;
        }

        public Lcdc GetLcdc()
        {
            return _lcdc;
        }
    }
}