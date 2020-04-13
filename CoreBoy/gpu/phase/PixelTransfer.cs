using CoreBoy.memory;

namespace CoreBoy.gpu.phase
{
    public class PixelTransfer : GpuPhase
    {

        private readonly PixelFifo fifo;

        private readonly Fetcher fetcher;

        private readonly Display display;

        private readonly MemoryRegisters r;

        private readonly Lcdc lcdc;

        private readonly bool gbc;

        private OamSearch.SpritePosition[] sprites;

        private int droppedPixels;

        private int x;

        private bool window;

        public PixelTransfer(AddressSpace videoRam0, AddressSpace videoRam1, AddressSpace oemRam, Display display,
            Lcdc lcdc, MemoryRegisters r, bool gbc, ColorPalette bgPalette, ColorPalette oamPalette)
        {
            this.r = r;
            this.lcdc = lcdc;
            this.gbc = gbc;
            if (gbc)
            {
                this.fifo = new ColorPixelFifo(lcdc, display, bgPalette, oamPalette);
            }
            else
            {
                this.fifo = new DmgPixelFifo(display, lcdc, r);
            }

            this.fetcher = new Fetcher(fifo, videoRam0, videoRam1, oemRam, lcdc, r, gbc);
            this.display = display;

        }

        public PixelTransfer start(OamSearch.SpritePosition[] sprites)
        {
            this.sprites = sprites;
            droppedPixels = 0;
            x = 0;
            window = false;

            fetcher.init();
            if (gbc || lcdc.isBgAndWindowDisplay())
            {
                startFetchingBackground();
            }
            else
            {
                fetcher.fetchingDisabled();
            }

            return this;
        }

        public bool tick()
        {
            fetcher.tick();
            if (lcdc.isBgAndWindowDisplay() || gbc)
            {
                if (fifo.getLength() <= 8)
                {
                    return true;
                }

                if (droppedPixels < r.get(GpuRegister.SCX) % 8)
                {
                    fifo.dropPixel();
                    droppedPixels++;
                    return true;
                }

                if (!window && lcdc.isWindowDisplay() && r.get(GpuRegister.LY) >= r.get(GpuRegister.WY) &&
                    x == r.get(GpuRegister.WX) - 7)
                {
                    window = true;
                    startFetchingWindow();
                    return true;
                }
            }

            if (lcdc.isObjDisplay())
            {
                if (fetcher.spriteInProgress())
                {
                    return true;
                }

                bool spriteAdded = false;
                for (int i = 0; i < sprites.Length; i++)
                {
                    OamSearch.SpritePosition s = sprites[i];
                    if (s == null)
                    {
                        continue;
                    }

                    if (x == 0 && s.getX() < 8)
                    {
                        if (!spriteAdded)
                        {
                            fetcher.addSprite(s, 8 - s.getX(), i);
                            spriteAdded = true;
                        }

                        sprites[i] = null;
                    }
                    else if (s.getX() - 8 == x)
                    {
                        if (!spriteAdded)
                        {
                            fetcher.addSprite(s, 0, i);
                            spriteAdded = true;
                        }

                        sprites[i] = null;
                    }

                    if (spriteAdded)
                    {
                        return true;
                    }
                }
            }

            fifo.putPixelToScreen();
            if (++x == 160)
            {
                return false;
            }

            return true;
        }

        private void startFetchingBackground()
        {
            int bgX = r.get(GpuRegister.SCX) / 0x08;
            int bgY = (r.get(GpuRegister.SCY) + r.get(GpuRegister.LY)) % 0x100;

            fetcher.startFetching(lcdc.getBgTileMapDisplay() + (bgY / 0x08) * 0x20, lcdc.getBgWindowTileData(), bgX,
                lcdc.isBgWindowTileDataSigned(), bgY % 0x08);
        }

        private void startFetchingWindow()
        {
            int winX = (this.x - r.get(GpuRegister.WX) + 7) / 0x08;
            int winY = r.get(GpuRegister.LY) - r.get(GpuRegister.WY);

            fetcher.startFetching(lcdc.getWindowTileMapDisplay() + (winY / 0x08) * 0x20, lcdc.getBgWindowTileData(),
                winX, lcdc.isBgWindowTileDataSigned(), winY % 0x08);
        }

    }
}