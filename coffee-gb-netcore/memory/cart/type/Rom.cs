using eu.rekawek.coffeegb.memory.cart;

namespace eu.rekawek.coffeegb.memory.cart.type
{
    public class Rom : AddressSpace
    {
        private int[] rom;

        public Rom(int[] rom, CartridgeType type, int romBanks, int ramBanks)
        {
            this.rom = rom;
        }

        public bool accepts(int address)
        {
            return (address >= 0x0000 && address < 0x8000) ||
                   (address >= 0xa000 && address < 0xc000);
        }

        public void setByte(int address, int value)
        {
        }

        public int getByte(int address)
        {
            if (address >= 0x0000 && address < 0x8000)
            {
                return rom[address];
            }
            else
            {
                return 0;
            }
        }
    }
}