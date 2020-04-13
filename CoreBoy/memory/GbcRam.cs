using System;

namespace CoreBoy.memory
{
    public class GbcRam : AddressSpace
    {
        private readonly int[] _ram = new int[7 * 0x1000];
        private int _svbk;

        public bool accepts(int address) => address == 0xff70 || (address >= 0xd000 && address < 0xe000);

        public void setByte(int address, int value)
        {
            if (address == 0xff70)
            {
                _svbk = value;
            }
            else
            {
                _ram[Translate(address)] = value;
            }
        }

        public int getByte(int address) => address == 0xff70 ? _svbk : _ram[Translate(address)];

        private int Translate(int address)
        {
            var ramBank = _svbk & 0x7;
            if (ramBank == 0)
            {
                ramBank = 1;
            }

            var result = address - 0xd000 + (ramBank - 1) * 0x1000;
            if (result < 0 || result >= _ram.Length)
            {
                throw new ArgumentException();
            }

            return result;
        }
    }
}