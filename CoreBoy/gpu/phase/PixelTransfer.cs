using CoreBoy.memory;

namespace CoreBoy.gpu.phase
{
    public class PixelTransfer : IGpuPhase
    {
        private readonly IPixelFifo _fifo;
        private readonly Fetcher _fetcher;
        private readonly MemoryRegisters _r;
        private readonly Lcdc _lcdc;
        private readonly bool _gbc;
        private OamSearch.SpritePosition[] _sprites;
        private int _droppedPixels;
        private int _x;
        private bool _window;

        public PixelTransfer(IAddressSpace videoRam0, IAddressSpace videoRam1, IAddressSpace oemRam, IDisplay display,
            Lcdc lcdc, MemoryRegisters r, bool gbc, ColorPalette bgPalette, ColorPalette oamPalette)
        {
            _r = r;
            _lcdc = lcdc;
            _gbc = gbc;
            
            _fifo = gbc
                ? (IPixelFifo) new ColorPixelFifo(lcdc, display, bgPalette, oamPalette)
                : new DmgPixelFifo(display, r);

            _fetcher = new Fetcher(_fifo, videoRam0, videoRam1, oemRam, lcdc, r, gbc);
        }

        public PixelTransfer Start(OamSearch.SpritePosition[] sprites)
        {
            _sprites = sprites;
            _droppedPixels = 0;
            _x = 0;
            _window = false;

            _fetcher.Init();
            if (_gbc || _lcdc.IsBgAndWindowDisplay())
            {
                StartFetchingBackground();
            }
            else
            {
                _fetcher.FetchingDisabled();
            }

            return this;
        }

        public bool Tick()
        {
            _fetcher.Tick();
            if (_lcdc.IsBgAndWindowDisplay() || _gbc)
            {
                if (_fifo.GetLength() <= 8)
                {
                    return true;
                }

                if (_droppedPixels < _r.Get(GpuRegister.Scx) % 8)
                {
                    _fifo.DropPixel();
                    _droppedPixels++;
                    return true;
                }

                if (!_window && _lcdc.IsWindowDisplay() && _r.Get(GpuRegister.Ly) >= _r.Get(GpuRegister.Wy) &&
                    _x == _r.Get(GpuRegister.Wx) - 7)
                {
                    _window = true;
                    StartFetchingWindow();
                    return true;
                }
            }

            if (_lcdc.IsObjDisplay())
            {
                if (_fetcher.SpriteInProgress())
                {
                    return true;
                }

                var spriteAdded = false;
                for (var i = 0; i < _sprites.Length; i++)
                {
                    var s = _sprites[i];
                    if (s == null)
                    {
                        continue;
                    }

                    if (_x == 0 && s.GetX() < 8)
                    {
                        _fetcher.AddSprite(s, 8 - s.GetX(), i);
                        spriteAdded = true;

                        _sprites[i] = null;
                    }
                    else if (s.GetX() - 8 == _x)
                    {
                        _fetcher.AddSprite(s, 0, i);
                        spriteAdded = true;

                        _sprites[i] = null;
                    }

                    if (spriteAdded)
                    {
                        return true;
                    }
                }
            }

            _fifo.PutPixelToScreen();
            if (++_x == 160)
            {
                return false;
            }

            return true;
        }

        private void StartFetchingBackground()
        {
            var bgX = _r.Get(GpuRegister.Scx) / 0x08;
            var bgY = (_r.Get(GpuRegister.Scy) + _r.Get(GpuRegister.Ly)) % 0x100;

            _fetcher.StartFetching(_lcdc.GetBgTileMapDisplay() + (bgY / 0x08) * 0x20, _lcdc.GetBgWindowTileData(), bgX,
                _lcdc.IsBgWindowTileDataSigned(), bgY % 0x08);
        }

        private void StartFetchingWindow()
        {
            var winX = (_x - _r.Get(GpuRegister.Wx) + 7) / 0x08;
            var winY = _r.Get(GpuRegister.Ly) - _r.Get(GpuRegister.Wy);

            _fetcher.StartFetching(_lcdc.GetWindowTileMapDisplay() + (winY / 0x08) * 0x20, _lcdc.GetBgWindowTileData(),
                winX, _lcdc.IsBgWindowTileDataSigned(), winY % 0x08);
        }

    }
}