using CoreBoy.memory;

namespace CoreBoy.gpu
{
    public class DmgPixelFifo : PixelFifo
    {

        private readonly IntQueue pixels = new IntQueue(16);

        private readonly IntQueue palettes = new IntQueue(16);

        private readonly IntQueue pixelType = new IntQueue(16); // 0 - bg, 1 - sprite

        private readonly Display display;

        private readonly Lcdc lcdc;

        private readonly MemoryRegisters registers;

        public DmgPixelFifo(Display display, Lcdc lcdc, MemoryRegisters registers)
        {
            this.lcdc = lcdc;
            this.display = display;
            this.registers = registers;
        }

        public int getLength()
        {
            return pixels.size();
        }

        public void putPixelToScreen()
        {
            display.putDmgPixel(dequeuePixel());
        }

        public void dropPixel()
        {
            dequeuePixel();
        }

        int dequeuePixel()
        {
            pixelType.dequeue();
            return getColor(palettes.dequeue(), pixels.dequeue());
        }

        public void enqueue8Pixels(int[] pixelLine, TileAttributes tileAttributes)
        {
            foreach (int p in pixelLine)
            {
                pixels.enqueue(p);
                palettes.enqueue(registers.get(GpuRegister.BGP));
                pixelType.enqueue(0);
            }
        }

        public void setOverlay(int[] pixelLine, int offset, TileAttributes flags, int oamIndex)
        {
            bool priority = flags.isPriority();
            int overlayPalette = registers.get(flags.getDmgPalette());

            for (int j = offset; j < pixelLine.Length; j++)
            {
                int p = pixelLine[j];
                int i = j - offset;
                if (pixelType.get(i) == 1)
                {
                    continue;
                }

                if ((priority && pixels.get(i) == 0) || !priority && p != 0)
                {
                    pixels.set(i, p);
                    palettes.set(i, overlayPalette);
                    pixelType.set(i, 1);
                }
            }
        }

        IntQueue getPixels()
        {
            return pixels;
        }

        private static int getColor(int palette, int colorIndex)
        {
            return 0b11 & (palette >> (colorIndex * 2));
        }

        public void clear()
        {
            pixels.clear();
            palettes.clear();
            pixelType.clear();
        }
    }
}