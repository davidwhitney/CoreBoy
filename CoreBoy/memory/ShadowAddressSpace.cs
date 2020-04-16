using System;

namespace CoreBoy.memory
{
    public class ShadowAddressSpace : IAddressSpace
    {
		private readonly IAddressSpace _addressSpace;
        private readonly int _echoStart;
        private readonly int _targetStart;
        private readonly int _length;

        public ShadowAddressSpace(IAddressSpace addressSpace, int echoStart, int targetStart, int length)
        {
            _addressSpace = addressSpace;
            _echoStart = echoStart;
            _targetStart = targetStart;
            _length = length;
        }

        public bool Accepts(int address) => address >= _echoStart && address < _echoStart + _length;
        public void SetByte(int address, int value) => _addressSpace.SetByte(Translate(address), value);
        public int GetByte(int address) => _addressSpace.GetByte(Translate(address));
        
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