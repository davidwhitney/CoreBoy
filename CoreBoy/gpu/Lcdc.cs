namespace CoreBoy.gpu
{
    public class Lcdc : AddressSpace
    {
        private int value = 0x91;

        public bool isBgAndWindowDisplay()
        {
            return (value & 0x01) != 0;
        }

        public bool isObjDisplay()
        {
            return (value & 0x02) != 0;
        }

        public int getSpriteHeight()
        {
            return (value & 0x04) == 0 ? 8 : 16;
        }

        public int getBgTileMapDisplay()
        {
            return (value & 0x08) == 0 ? 0x9800 : 0x9c00;
        }

        public int getBgWindowTileData()
        {
            return (value & 0x10) == 0 ? 0x9000 : 0x8000;
        }

        public bool isBgWindowTileDataSigned()
        {
            return (value & 0x10) == 0;
        }

        public bool isWindowDisplay()
        {
            return (value & 0x20) != 0;
        }

        public int getWindowTileMapDisplay()
        {
            return (value & 0x40) == 0 ? 0x9800 : 0x9c00;
        }

        public bool isLcdEnabled()
        {
            return (value & 0x80) != 0;
        }

        public bool accepts(int address)
        {
            return address == 0xff40;
        }

        public void setByte(int address, int value) => this.value = value;
        public int getByte(int address) => value;
        public void set(int value) => this.value = value;
        public int get() => value;
    }
}