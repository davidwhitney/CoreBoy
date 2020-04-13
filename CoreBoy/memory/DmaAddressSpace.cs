using System;

namespace CoreBoy.memory
{
    public class DmaAddressSpace : AddressSpace
    {

        private readonly AddressSpace addressSpace;

        public DmaAddressSpace(AddressSpace addressSpace)
        {
            this.addressSpace = addressSpace;
        }

        public bool accepts(int address)
        {
            return true;
        }

        public void setByte(int address, int value)
        {
            throw new NotImplementedException("Unsupported");
        }

        public int getByte(int address)
        {
            if (address < 0xe000)
            {
                return addressSpace.getByte(address);
            }
            else
            {
                return addressSpace.getByte(address - 0x2000);
            }
        }
    }
}