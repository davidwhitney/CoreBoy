namespace CoreBoy.gpu
{
    public class ColorPixelFifo : IPixelFifo
    {
        private readonly IntQueue _pixels = new IntQueue(16);
        private readonly IntQueue _palettes = new IntQueue(16);
        private readonly IntQueue _priorities = new IntQueue(16);
        private readonly Lcdc _lcdc;
        private readonly IDisplay _display;
        private readonly ColorPalette _bgPalette;
        private readonly ColorPalette _oamPalette;

        public ColorPixelFifo(Lcdc lcdc, IDisplay display, ColorPalette bgPalette, ColorPalette oamPalette)
        {
            _lcdc = lcdc;
            _display = display;
            _bgPalette = bgPalette;
            _oamPalette = oamPalette;
        }

        public int GetLength() => _pixels.Size();
        public void PutPixelToScreen() => _display.PutColorPixel(DequeuePixel());

        private int DequeuePixel()
        {
            return GetColor(_priorities.Dequeue(), _palettes.Dequeue(), _pixels.Dequeue());
        }

        public void DropPixel() => DequeuePixel();

        public void Enqueue8Pixels(int[] pixelLine, TileAttributes tileAttributes)
        {
            foreach (var p in pixelLine)
            {
                _pixels.Enqueue(p);
                _palettes.Enqueue(tileAttributes.GetColorPaletteIndex());
                _priorities.Enqueue(tileAttributes.IsPriority() ? 100 : -1);
            }
        }

        /*
        lcdc.0
    
        when 0 => sprites are always displayed on top of the bg
    
        bg tile attribute.7
    
        when 0 => use oam priority bit
        when 1 => bg priority
    
        sprite attribute.7
    
        when 0 => sprite above bg
        when 1 => sprite above bg color 0
         */

        public void SetOverlay(int[] pixelLine, int offset, TileAttributes spriteAttr, int oamIndex)
        {
            for (var j = offset; j < pixelLine.Length; j++)
            {
                var p = pixelLine[j];
                var i = j - offset;
                if (p == 0)
                {
                    continue; // color 0 is always transparent
                }

                var oldPriority = _priorities.Get(i);

                var put = false;
                if ((oldPriority == -1 || oldPriority == 100) && !_lcdc.IsBgAndWindowDisplay())
                {
                    // this one takes precedence
                    put = true;
                }
                else if (oldPriority == 100)
                {
                    // bg with priority
                    put = _pixels.Get(i) == 0;
                }
                else if (oldPriority == -1 && !spriteAttr.IsPriority())
                {
                    // bg without priority
                    put = true;
                }
                else if (oldPriority == -1 && spriteAttr.IsPriority() && _pixels.Get(i) == 0)
                {
                    // bg without priority
                    put = true;
                }
                else if (oldPriority >= 0 && oldPriority < 10)
                {
                    // other sprite
                    put = oldPriority > oamIndex;
                }

                if (put)
                {
                    _pixels.Set(i, p);
                    _palettes.Set(i, spriteAttr.GetColorPaletteIndex());
                    _priorities.Set(i, oamIndex);
                }
            }
        }
        
        public void Clear()
        {
            _pixels.Clear();
            _palettes.Clear();
            _priorities.Clear();
        }

        private int GetColor(int priority, int palette, int color)
        {
            return priority >= 0 && priority < 10
                ? _oamPalette.GetPalette(palette)[color]
                : _bgPalette.GetPalette(palette)[color];
        }
    }
}