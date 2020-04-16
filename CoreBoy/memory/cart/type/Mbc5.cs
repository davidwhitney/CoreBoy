using System;
using CoreBoy.memory.cart.battery;

namespace CoreBoy.memory.cart.type
{
    public class Mbc5 : IAddressSpace
    {
        private readonly int _ramBanks;
        private readonly int[] _cartridge;
        private readonly int[] _ram;
        private readonly IBattery _battery;
        private int _selectedRamBank;
        private int _selectedRomBank = 1;
        private bool _ramWriteEnabled;

        public Mbc5(int[] cartridge, CartridgeType type, IBattery battery, int romBanks, int ramBanks)
        {
            _cartridge = cartridge;
            _ramBanks = ramBanks;
            _ram = new int[0x2000 * Math.Max(_ramBanks, 1)];
            for (var i = 0; i < _ram.Length; i++)
            {
                _ram[i] = 0xff;
            }

            _battery = battery;
            battery.LoadRam(_ram);
        }

        public bool Accepts(int address) => address >= 0x0000 && address < 0x8000 || address >= 0xa000 && address < 0xc000;
        
        public void SetByte(int address, int value)
        {
            if (address >= 0x0000 && address < 0x2000)
            {
                _ramWriteEnabled = (value & 0b1010) != 0;
                if (!_ramWriteEnabled)
                {
                    _battery.SaveRam(_ram);
                }
            }
            else if (address >= 0x2000 && address < 0x3000)
            {
                _selectedRomBank = (_selectedRomBank & 0x100) | value;
            }
            else if (address >= 0x3000 && address < 0x4000)
            {
                _selectedRomBank = (_selectedRomBank & 0x0ff) | ((value & 1) << 8);
            }
            else if (address >= 0x4000 && address < 0x6000)
            {
                var bank = value & 0x0f;
                if (bank < _ramBanks)
                {
                    _selectedRamBank = bank;
                }
            }
            else if (address >= 0xa000 && address < 0xc000 && _ramWriteEnabled)
            {
                var ramAddress = GetRamAddress(address);
                if (ramAddress < _ram.Length)
                {
                    _ram[ramAddress] = value;
                }
            }
        }

        public int GetByte(int address)
        {
            if (address >= 0x0000 && address < 0x4000)
            {
                return GetRomByte(0, address);
            }

            if (address >= 0x4000 && address < 0x8000)
            {
                return GetRomByte(_selectedRomBank, address - 0x4000);
            }

            if (address >= 0xa000 && address < 0xc000)
            {
                var ramAddress = GetRamAddress(address);
                if (ramAddress < _ram.Length)
                {
                    return _ram[ramAddress];
                }

                return 0xff;
            }

            throw new ArgumentException(Integer.ToHexString(address));
        }

        private int GetRomByte(int bank, int address)
        {
            var cartOffset = bank * 0x4000 + address;
            if (cartOffset < _cartridge.Length)
            {
                return _cartridge[cartOffset];
            }

            return 0xff;
        }

        private int GetRamAddress(int address) => _selectedRamBank * 0x2000 + (address - 0xa000);
    }
}