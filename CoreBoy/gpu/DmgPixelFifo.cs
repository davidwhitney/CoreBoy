using CoreBoy.memory;

namespace CoreBoy.gpu
{
    public class DmgPixelFifo : IPixelFifo
    {
        public IntQueue Pixels { get; } = new IntQueue(16);
        private readonly IntQueue _palettes = new IntQueue(16);
        private readonly IntQueue _pixelType = new IntQueue(16); // 0 - bg, 1 - sprite

        private readonly IDisplay _display;
        private readonly MemoryRegisters _registers;

        public DmgPixelFifo(IDisplay display, MemoryRegisters registers)
        {
            _display = display;
            _registers = registers;
        }

        public int GetLength() => Pixels.Size();
        public void PutPixelToScreen() => _display.PutDmgPixel(DequeuePixel());
        public void DropPixel() => DequeuePixel();

        public int DequeuePixel()
        {
            _pixelType.Dequeue();
            return GetColor(_palettes.Dequeue(), Pixels.Dequeue());
        }

        public void Enqueue8Pixels(int[] pixelLine, TileAttributes tileAttributes)
        {
            foreach (var p in pixelLine)
            {
                Pixels.Enqueue(p);
                _palettes.Enqueue(_registers.Get(GpuRegister.Bgp));
                _pixelType.Enqueue(0);
            }
        }

        public void SetOverlay(int[] pixelLine, int offset, TileAttributes flags, int oamIndex)
        {
            var priority = flags.IsPriority();
            var overlayPalette = _registers.Get(flags.GetDmgPalette());

            for (var j = offset; j < pixelLine.Length; j++)
            {
                var p = pixelLine[j];
                var i = j - offset;

                if (_pixelType.Get(i) == 1)
                {
                    continue;
                }

                if (priority && Pixels.Get(i) == 0 || !priority && p != 0)
                {
                    Pixels.Set(i, p);
                    _palettes.Set(i, overlayPalette);
                    _pixelType.Set(i, 1);
                }
            }
        }
        
        private static int GetColor(int palette, int colorIndex) => 0b11 & (palette >> (colorIndex * 2));

        public void Clear()
        {
            Pixels.Clear();
            _palettes.Clear();
            _pixelType.Clear();
        }
    }
}