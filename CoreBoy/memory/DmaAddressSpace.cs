using System;

namespace CoreBoy.memory
{
    public class DmaAddressSpace : AddressSpace
    {
        private readonly AddressSpace _addressSpace;

        public DmaAddressSpace(AddressSpace addressSpace) => _addressSpace = addressSpace;
        public bool accepts(int address) => true;
        public void setByte(int address, int value) => throw new NotImplementedException("Unsupported");

        public int getByte(int address) =>
            address < 0xe000
                ? _addressSpace.getByte(address)
                : _addressSpace.getByte(address - 0x2000);
    }
}