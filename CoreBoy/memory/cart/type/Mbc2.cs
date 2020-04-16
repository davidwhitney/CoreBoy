using CoreBoy.memory.cart.battery;

namespace CoreBoy.memory.cart.type
{


    public class Mbc2 : IAddressSpace
    {

        private readonly CartridgeType type;

        private readonly int romBanks;

        private readonly int[] cartridge;

        private readonly int[] ram;

        private readonly Battery battery;

        private int selectedRomBank = 1;

        private bool ramWriteEnabled;

        public Mbc2(int[] cartridge, CartridgeType type, Battery battery, int romBanks)
        {
            this.cartridge = cartridge;
            this.romBanks = romBanks;
            ram = new int[0x0200];
            for (var i = 0; i < ram.Length; i++)
            {
                ram[i] = 0xff;
            }

            this.type = type;
            this.battery = battery;
            battery.loadRam(ram);
        }

        

        public bool Accepts(int address)
        {
            return (address >= 0x0000 && address < 0x8000) ||
                   (address >= 0xa000 && address < 0xc000);
        }

        

        public void SetByte(int address, int value)
        {
            if (address >= 0x0000 && address < 0x2000)
            {
                if ((address & 0x0100) == 0)
                {
                    ramWriteEnabled = (value & 0b1010) != 0;
                    if (!ramWriteEnabled)
                    {
                        battery.saveRam(ram);
                    }
                }
            }
            else if (address >= 0x2000 && address < 0x4000)
            {
                if ((address & 0x0100) != 0)
                {
                    selectedRomBank = value & 0b00001111;
                }
            }
            else if (address >= 0xa000 && address < 0xc000 && ramWriteEnabled)
            {
                var ramAddress = getRamAddress(address);
                if (ramAddress < ram.Length)
                {
                    ram[ramAddress] = value & 0x0f;
                }
            }
        }

        

        public int GetByte(int address)
        {
            if (address >= 0x0000 && address < 0x4000)
            {
                return getRomByte(0, address);
            }
            else if (address >= 0x4000 && address < 0x8000)
            {
                return getRomByte(selectedRomBank, address - 0x4000);
            }
            else if (address >= 0xa000 && address < 0xb000)
            {
                var ramAddress = getRamAddress(address);
                if (ramAddress < ram.Length)
                {
                    return ram[ramAddress];
                }
                else
                {
                    return 0xff;
                }
            }
            else
            {
                return 0xff;
            }
        }

        private int getRomByte(int bank, int address)
        {
            var cartOffset = bank * 0x4000 + address;
            if (cartOffset < cartridge.Length)
            {
                return cartridge[cartOffset];
            }
            else
            {
                return 0xff;
            }
        }

        private int getRamAddress(int address)
        {
            return address - 0xa000;
        }
    }
}