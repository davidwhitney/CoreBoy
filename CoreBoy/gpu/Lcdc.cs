namespace CoreBoy.gpu
{
    public class Lcdc : IAddressSpace
    {
        private int _value = 0x91;

        public bool IsBgAndWindowDisplay() => (_value & 0x01) != 0;
        public bool IsObjDisplay() => (_value & 0x02) != 0;
        public int GetSpriteHeight() => (_value & 0x04) == 0 ? 8 : 16;
        public int GetBgTileMapDisplay() => (_value & 0x08) == 0 ? 0x9800 : 0x9c00;
        public int GetBgWindowTileData() => (_value & 0x10) == 0 ? 0x9000 : 0x8000;
        public bool IsBgWindowTileDataSigned() => (_value & 0x10) == 0;
        public bool IsWindowDisplay() => (_value & 0x20) != 0;
        public int GetWindowTileMapDisplay() => (_value & 0x40) == 0 ? 0x9800 : 0x9c00;
        public bool IsLcdEnabled() => (_value & 0x80) != 0;
        public bool Accepts(int address) => address == 0xff40;
        
        public void SetByte(int address, int val) => _value = val;
        public int GetByte(int address) => _value;
        public void Set(int val) => _value = val;
        public int Get() => _value;
    }
}