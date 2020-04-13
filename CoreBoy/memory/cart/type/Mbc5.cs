using System;
using CoreBoy.memory.cart.battery;

namespace CoreBoy.memory.cart.type
{


    public class Mbc5 : AddressSpace {

    private readonly CartridgeType type;

    private readonly int romBanks;

    private readonly int ramBanks;

    private readonly int[] cartridge;

    private readonly int[] ram;

    private readonly Battery battery;

    private int selectedRamBank;

    private int selectedRomBank = 1;

    private bool ramWriteEnabled;

    public Mbc5(int[] cartridge, CartridgeType type, Battery battery, int romBanks, int ramBanks)
    {
        this.cartridge = cartridge;
        this.ramBanks = ramBanks;
        this.romBanks = romBanks;
        this.ram = new int[0x2000 * Math.Max(this.ramBanks, 1)];
        for (int i = 0; i < ram.Length; i++)
        {
            ram[i] = 0xff;
        }

        this.type = type;
        this.battery = battery;
        battery.loadRam(ram);
    }

    

    public bool accepts(int address)
    {
        return (address >= 0x0000 && address < 0x8000) ||
               (address >= 0xa000 && address < 0xc000);
    }

    

    public void setByte(int address, int value)
    {
        if (address >= 0x0000 && address < 0x2000)
        {
            ramWriteEnabled = (value & 0b1010) != 0;
            if (!ramWriteEnabled)
            {
                battery.saveRam(ram);
            }
        }
        else if (address >= 0x2000 && address < 0x3000)
        {
            selectedRomBank = (selectedRomBank & 0x100) | value;
        }
        else if (address >= 0x3000 && address < 0x4000)
        {
            selectedRomBank = (selectedRomBank & 0x0ff) | ((value & 1) << 8);
        }
        else if (address >= 0x4000 && address < 0x6000)
        {
            int bank = value & 0x0f;
            if (bank < ramBanks)
            {
                selectedRamBank = bank;
            }
        }
        else if (address >= 0xa000 && address < 0xc000 && ramWriteEnabled)
        {
            int ramAddress = getRamAddress(address);
            if (ramAddress < ram.Length)
            {
                ram[ramAddress] = value;
            }
        }
    }

    

    public int getByte(int address)
    {
        if (address >= 0x0000 && address < 0x4000)
        {
            return getRomByte(0, address);
        }
        else if (address >= 0x4000 && address < 0x8000)
        {
            return getRomByte(selectedRomBank, address - 0x4000);
        }
        else if (address >= 0xa000 && address < 0xc000)
        {
            int ramAddress = getRamAddress(address);
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
            throw new ArgumentException(Integer.toHexString(address));
        }
    }

    private int getRomByte(int bank, int address)
    {
        int cartOffset = bank * 0x4000 + address;
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
        return selectedRamBank * 0x2000 + (address - 0xa000);
    }
    }
}