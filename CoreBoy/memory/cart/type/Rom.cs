namespace CoreBoy.memory.cart.type
{
    public class Rom : IAddressSpace
    {
        private readonly int[] _rom;

        public Rom(int[] rom, CartridgeType type, int romBanks, int ramBanks)
        {
            _rom = rom;
        }

        public bool Accepts(int address) => address >= 0x0000 && address < 0x8000 || address >= 0xa000 && address < 0xc000;

        public void SetByte(int address, int value)
        {
        }

        public int GetByte(int address)
        {
            if (address >= 0x0000 && address < 0x8000)
            {
                return _rom[address];
            }

            return 0;
        }
    }
}