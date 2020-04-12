using System;

namespace eu.rekawek.coffeegb.memory
{
    public class ShadowAddressSpace : AddressSpace
    {
		private readonly AddressSpace addressSpace;
        private readonly int echoStart;
        private readonly int targetStart;
        private readonly int length;

        public ShadowAddressSpace(AddressSpace addressSpace, int echoStart, int targetStart, int length)
        {
            this.addressSpace = addressSpace;
            this.echoStart = echoStart;
            this.targetStart = targetStart;
            this.length = length;
        }

        public bool accepts(int address)
        {
            return address >= echoStart && address < echoStart + length;
        }


        public void setByte(int address, int value)
        {
            addressSpace.setByte(translate(address), value);
        }


        public int getByte(int address)
        {
            return addressSpace.getByte(translate(address));
        }

        private int translate(int address)
        {
            return getRelative(address) + targetStart;
        }

        private int getRelative(int address)
        {
            int i = address - echoStart;
            if (i < 0 || i >= length)
            {
                throw new ArgumentException();
            }

            return i;
        }
    }
}