using System;
using CoreBoy.memory.cart.battery;
using CoreBoy.memory.cart.rtc;

namespace CoreBoy.memory.cart.type
{
    public class Mbc3 : IAddressSpace
    {
        private readonly int[] _cartridge;
        private readonly int[] _ram;
        private readonly RealTimeClock _clock;
        private readonly Battery _battery;
        private int _selectedRamBank;
        private int _selectedRomBank = 1;
        private bool _ramWriteEnabled;
        private int _latchClockReg = 0xff;
        private bool _clockLatched;

        public Mbc3(int[] cartridge, CartridgeType type, Battery battery, int romBanks, int ramBanks)
        {
            _cartridge = cartridge;
            _ram = new int[0x2000 * Math.Max(ramBanks, 1)];
            for (var i = 0; i < _ram.Length; i++)
            {
                _ram[i] = 0xff;
            }

            _clock = new RealTimeClock(Clock.SYSTEM_CLOCK);
            _battery = battery;

            var clockData = new long[12];
            battery.loadRamWithClock(_ram, clockData);
            _clock.deserialize(clockData);
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
                _ramWriteEnabled = (value & 0b1010) != 0;
                if (!_ramWriteEnabled)
                {
                    _battery.saveRamWithClock(_ram, _clock.serialize());
                }
            }
            else if (address >= 0x2000 && address < 0x4000)
            {
                var bank = value & 0b01111111;
                SelectRomBank(bank);
            }
            else if (address >= 0x4000 && address < 0x6000)
            {
                _selectedRamBank = value;
            }
            else if (address >= 0x6000 && address < 0x8000)
            {
                if (value == 0x01 && _latchClockReg == 0x00)
                {
                    if (_clockLatched)
                    {
                        _clock.unlatch();
                        _clockLatched = false;
                    }
                    else
                    {
                        _clock.latch();
                        _clockLatched = true;
                    }
                }

                _latchClockReg = value;
            }
            else if (address >= 0xa000 && address < 0xc000 && _ramWriteEnabled && _selectedRamBank < 4)
            {
                var ramAddress = GetRamAddress(address);
                if (ramAddress < _ram.Length)
                {
                    _ram[ramAddress] = value;
                }
            }
            else if (address >= 0xa000 && address < 0xc000 && _ramWriteEnabled && _selectedRamBank >= 4)
            {
                SetTimer(value);
            }
        }

        private void SelectRomBank(int bank)
        {
            if (bank == 0)
            {
                bank = 1;
            }

            _selectedRomBank = bank;
        }


        public int GetByte(int address)
        {
            if (address >= 0x0000 && address < 0x4000)
            {
                return GetRomByte(0, address);
            }
            else if (address >= 0x4000 && address < 0x8000)
            {
                return GetRomByte(_selectedRomBank, address - 0x4000);
            }
            else if (address >= 0xa000 && address < 0xc000 && _selectedRamBank < 4)
            {
                var ramAddress = GetRamAddress(address);
                if (ramAddress < _ram.Length)
                {
                    return _ram[ramAddress];
                }
                else
                {
                    return 0xff;
                }
            }
            else if (address >= 0xa000 && address < 0xc000 && _selectedRamBank >= 4)
            {
                return GetTimer();
            }
            else
            {
                throw new ArgumentException(Integer.ToHexString(address));
            }
        }

        private int GetRomByte(int bank, int address)
        {
            var cartOffset = bank * 0x4000 + address;
            if (cartOffset < _cartridge.Length)
            {
                return _cartridge[cartOffset];
            }
            else
            {
                return 0xff;
            }
        }

        private int GetRamAddress(int address)
        {
            return _selectedRamBank * 0x2000 + (address - 0xa000);
        }

        private int GetTimer()
        {
            switch (_selectedRamBank)
            {
                case 0x08:
                    return _clock.getSeconds();

                case 0x09:
                    return _clock.getMinutes();

                case 0x0a:
                    return _clock.getHours();

                case 0x0b:
                    return _clock.getDayCounter() & 0xff;

                case 0x0c:
                    var result = ((_clock.getDayCounter() & 0x100) >> 8);
                    result |= _clock.isHalt() ? (1 << 6) : 0;
                    result |= _clock.isCounterOverflow() ? (1 << 7) : 0;
                    return result;
            }

            return 0xff;
        }

        private void SetTimer(int value)
        {
            var dayCounter = _clock.getDayCounter();
            switch (_selectedRamBank)
            {
                case 0x08:
                    _clock.setSeconds(value);
                    break;

                case 0x09:
                    _clock.setMinutes(value);
                    break;

                case 0x0a:
                    _clock.setHours(value);
                    break;

                case 0x0b:
                    _clock.setDayCounter((dayCounter & 0x100) | (value & 0xff));
                    break;

                case 0x0c:
                    _clock.setDayCounter((dayCounter & 0xff) | ((value & 1) << 8));
                    _clock.setHalt((value & (1 << 6)) != 0);
                    if ((value & (1 << 7)) == 0)
                    {
                        _clock.clearCounterOverflow();
                    }

                    break;
            }
        }
    }
}