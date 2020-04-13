namespace CoreBoy.gpu
{
    public class ColorPixelFifo : IPixelFifo
    {
        private readonly IntQueue pixels = new IntQueue(16);
        private readonly IntQueue palettes = new IntQueue(16);
        private readonly IntQueue priorities = new IntQueue(16);
        private readonly Lcdc lcdc;
        private readonly IDisplay display;
        private readonly ColorPalette bgPalette;
        private readonly ColorPalette oamPalette;

        public ColorPixelFifo(Lcdc lcdc, IDisplay display, ColorPalette bgPalette, ColorPalette oamPalette)
        {
            this.lcdc = lcdc;
            this.display = display;
            this.bgPalette = bgPalette;
            this.oamPalette = oamPalette;
        }

        public int GetLength()
        {
            return pixels.size();
        }

        public void PutPixelToScreen()
        {
            display.PutColorPixel(dequeuePixel());
        }

        private int dequeuePixel()
        {
            return getColor(priorities.dequeue(), palettes.dequeue(), pixels.dequeue());
        }

        public void DropPixel()
        {
            dequeuePixel();
        }

        public void Enqueue8Pixels(int[] pixelLine, TileAttributes tileAttributes)
        {
            foreach (int p in pixelLine)
            {
                pixels.enqueue(p);
                palettes.enqueue(tileAttributes.getColorPaletteIndex());
                priorities.enqueue(tileAttributes.isPriority() ? 100 : -1);
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
            for (int j = offset; j < pixelLine.Length; j++)
            {
                int p = pixelLine[j];
                int i = j - offset;
                if (p == 0)
                {
                    continue; // color 0 is always transparent
                }

                int oldPriority = priorities.get(i);

                bool put = false;
                if ((oldPriority == -1 || oldPriority == 100) && !lcdc.isBgAndWindowDisplay())
                {
                    // this one takes precedence
                    put = true;
                }
                else if (oldPriority == 100)
                {
                    // bg with priority
                    put = pixels.get(i) == 0;
                }
                else if (oldPriority == -1 && !spriteAttr.isPriority())
                {
                    // bg without priority
                    put = true;
                }
                else if (oldPriority == -1 && spriteAttr.isPriority() && pixels.get(i) == 0)
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
                    pixels.set(i, p);
                    palettes.set(i, spriteAttr.getColorPaletteIndex());
                    priorities.set(i, oamIndex);
                }
            }
        }


        public void Clear()
        {
            pixels.clear();
            palettes.clear();
            priorities.clear();
        }

        private int getColor(int priority, int palette, int color)
        {
            if (priority >= 0 && priority < 10)
            {
                return oamPalette.GetPalette(palette)[color];
            }
            else
            {
                return bgPalette.GetPalette(palette)[color];
            }
        }
    }
}