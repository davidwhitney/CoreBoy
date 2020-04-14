using System;

namespace CoreBoy.memory
{
    public class ShadowAddressSpace : AddressSpace
    {
		private readonly AddressSpace _addressSpace;
        private readonly int _echoStart;
        private readonly int _targetStart;
        private readonly int _length;

        public ShadowAddressSpace(AddressSpace addressSpace, int echoStart, int targetStart, int length)
        {
            _addressSpace = addressSpace;
            _echoStart = echoStart;
            _targetStart = targetStart;
            _length = length;
        }

        public bool accepts(int address) => address >= _echoStart && address < _echoStart + _length;
        public void setByte(int address, int value) => _addressSpace.setByte(Translate(address), value);
        public int getByte(int address) => _addressSpace.getByte(Translate(address));
        
        private int Translate(int address) => GetRelative(address) + _targetStart;

        private int GetRelative(int address)
        {
            var i = address - _echoStart;
            if (i < 0 || i >= _length)
            {
                throw new ArgumentException();
            }

            return i;
        }
    }
}