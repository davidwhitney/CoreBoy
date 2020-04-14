using System;

namespace CoreBoy.memory
{
    public class Ram : AddressSpace
    {
        private readonly int[] _space;
        private readonly int _length;
        private readonly int _offset;

        public Ram(int offset, int length)
        {
            _space = new int[length];
            _length = length;
            _offset = offset;
        }

        public bool accepts(int address) => address >= _offset && address < _offset + _length;
        public void setByte(int address, int value) => _space[address - _offset] = value;

        public int getByte(int address)
        {
            var index = address - _offset;
            if (index < 0 || index >= _space.Length)
            {
                throw new IndexOutOfRangeException("Address: " + address);
            }

            return _space[index];
        }
    }
}