namespace CoreBoy.gpu
{
    public class TileAttributes
    {
        public static TileAttributes EMPTY;

        private static TileAttributes[] ATTRIBUTES;

        static TileAttributes()
        {
            ATTRIBUTES = new TileAttributes[256];
            for (int i = 0; i < 256; i++)
            {
                ATTRIBUTES[i] = new TileAttributes(i);
            }

            EMPTY = ATTRIBUTES[0];
        }

        private int value;

        private TileAttributes(int value)
        {
            this.value = value;
        }

        public static TileAttributes valueOf(int value)
        {
            return ATTRIBUTES[value];
        }

        public bool isPriority()
        {
            return (value & (1 << 7)) != 0;
        }

        public bool isYflip()
        {
            return (value & (1 << 6)) != 0;
        }

        public bool isXflip()
        {
            return (value & (1 << 5)) != 0;
        }

        public GpuRegister getDmgPalette()
        {
            return (value & (1 << 4)) == 0 ? GpuRegister.OBP0 : GpuRegister.OBP1;
        }

        public int getBank()
        {
            return (value & (1 << 3)) == 0 ? 0 : 1;
        }

        public int getColorPaletteIndex()
        {
            return value & 0x07;
        }
    }
}