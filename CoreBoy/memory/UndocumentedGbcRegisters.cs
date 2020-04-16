using System;

namespace CoreBoy.memory
{
    public class UndocumentedGbcRegisters : IAddressSpace
    {
        private readonly Ram _ram = new Ram(0xff72, 6);
        private int _xff6C;

        public UndocumentedGbcRegisters()
        {
            _xff6C = 0xfe;
            _ram.SetByte(0xff74, 0xff);
            _ram.SetByte(0xff75, 0x8f);
        }

        public bool Accepts(int address) => address == 0xff6c || _ram.Accepts(address);

        public void SetByte(int address, int value)
        {
            switch (address)
            {
                case 0xff6c:
                    _xff6C = 0xfe | (value & 1);
                    break;

                case 0xff72:
                case 0xff73:
                case 0xff74:
                    _ram.SetByte(address, value);
                    break;

                case 0xff75:
                    _ram.SetByte(address, 0x8f | (value & 0b01110000));
                    break;
            }
        }

        public int GetByte(int address)
        {
            if (address == 0xff6c)
            {
                return _xff6C;
            }

            if (!_ram.Accepts(address))
            {
                throw new ArgumentException();
            }

            return _ram.GetByte(address);
        }
    }
}