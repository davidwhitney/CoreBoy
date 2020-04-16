using System.Linq;
using CoreBoy.cpu;
using CoreBoy.gpu.phase;
using CoreBoy.memory;

namespace CoreBoy.gpu
{
    public class Gpu : AddressSpace
    {
        public enum Mode
        {
            HBlank,
            VBlank,
            OamSearch,
            PixelTransfer
        }

        private readonly AddressSpace _videoRam0;
        private readonly AddressSpace _videoRam1;
        private readonly AddressSpace _oamRam;
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
        private GpuPhase _phase;

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
            _phase = _oamSearchPhase.start();

            _display = display;
        }

        private AddressSpace GetAddressSpace(int address)
        {
            if (_videoRam0.accepts(address) /* && mode != Mode.PixelTransfer*/)
            {
                return GetVideoRam();
            }

            if (_oamRam.accepts(address) &&
                !_dma.IsOamBlocked() /* && mode != Mode.OamSearch && mode != Mode.PixelTransfer*/)
            {
                return _oamRam;
            }

            if (_lcdc.accepts(address))
            {
                return _lcdc;
            }

            if (_r.accepts(address))
            {
                return _r;
            }

            if (_gbc && _bgPalette.accepts(address))
            {
                return _bgPalette;
            }

            if (_gbc && _oamPalette.accepts(address))
            {
                return _oamPalette;
            }

            return null;
        }

        private AddressSpace GetVideoRam()
        {
            if (_gbc && (_r.Get(GpuRegister.VBK) & 1) == 1)
            {
                return _videoRam1;
            }

            return _videoRam0;
        }

        public bool accepts(int address) => GetAddressSpace(address) != null;

        public void setByte(int address, int value)
        {
            if (address == GpuRegister.STAT.Address)
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

            space?.setByte(address, value);
        }

        public int getByte(int address)
        {
            if (address == GpuRegister.STAT.Address)
            {
                return GetStat();
            }

            var space = GetAddressSpace(address);
            if (space == null)
            {
                return 0xff;
            }

            if (address == GpuRegister.VBK.Address)
            {
                return _gbc ? 0xfe : 0xff;
            }

            return space.getByte(address);
        }

        public Mode? Tick()
        {
            if (!_lcdEnabled)
            {
                if (_lcdEnabledDelay != -1)
                {
                    if (--_lcdEnabledDelay == 0)
                    {
                        _display.EnableLcd();
                        _lcdEnabled = true;
                    }
                }
            }

            if (!_lcdEnabled)
            {
                return null;
            }

            Mode oldMode = _mode;
            _ticksInLine++;
            if (_phase.tick())
            {
                // switch line 153 to 0
                if (_ticksInLine == 4 && _mode == Mode.VBlank && _r.Get(GpuRegister.LY) == 153)
                {
                    _r.Put(GpuRegister.LY, 0);
                    RequestLycEqualsLyInterrupt();
                }
            }
            else
            {
                switch (oldMode)
                {
                    case Mode.OamSearch:
                        _mode = Mode.PixelTransfer;
                        _phase = _pixelTransferPhase.start(_oamSearchPhase.getSprites());
                        break;

                    case Mode.PixelTransfer:
                        _mode = Mode.HBlank;
                        _phase = _hBlankPhase.start(_ticksInLine);
                        RequestLcdcInterrupt(3);
                        break;

                    case Mode.HBlank:
                        _ticksInLine = 0;
                        if (_r.PreIncrement(GpuRegister.LY) == 144)
                        {
                            _mode = Mode.VBlank;
                            _phase = _vBlankPhase.start();
                            _interruptManager.RequestInterrupt(InterruptManager.InterruptType.VBlank);
                            RequestLcdcInterrupt(4);
                        }
                        else
                        {
                            _mode = Mode.OamSearch;
                            _phase = _oamSearchPhase.start();
                        }

                        RequestLcdcInterrupt(5);
                        RequestLycEqualsLyInterrupt();
                        break;

                    case Mode.VBlank:
                        _ticksInLine = 0;
                        if (_r.PreIncrement(GpuRegister.LY) == 1)
                        {
                            _mode = Mode.OamSearch;
                            _r.Put(GpuRegister.LY, 0);
                            _phase = _oamSearchPhase.start();
                            RequestLcdcInterrupt(5);
                        }
                        else
                        {
                            _phase = _vBlankPhase.start();
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
            if ((_r.Get(GpuRegister.STAT) & (1 << statBit)) != 0)
            {
                _interruptManager.RequestInterrupt(InterruptManager.InterruptType.Lcdc);
            }
        }

        private void RequestLycEqualsLyInterrupt()
        {
            if (_r.Get(GpuRegister.LYC) == _r.Get(GpuRegister.LY))
            {
                RequestLcdcInterrupt(6);
            }
        }

        private int GetStat()
        {
            return _r.Get(GpuRegister.STAT) | (int) _mode |
                   (_r.Get(GpuRegister.LYC) == _r.Get(GpuRegister.LY) ? (1 << 2) : 0) | 0x80;
        }

        private void SetStat(int value)
        {
            _r.Put(GpuRegister.STAT, value & 0b11111000); // last three bits are read-only
        }

        private void SetLcdc(int value)
        {
            _lcdc.set(value);
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
            _r.Put(GpuRegister.LY, 0);
            _ticksInLine = 0;
            _phase = _hBlankPhase.start(250);
            _mode = Mode.HBlank;
            _lcdEnabled = false;
            _lcdEnabledDelay = -1;
            _display.DisableLcd();
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