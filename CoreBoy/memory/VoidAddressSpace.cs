using System;

namespace CoreBoy.memory
{
    public class VoidAddressSpace : IAddressSpace
    {
        public bool Accepts(int address) => true;

        public void SetByte(int address, int value)
        {
            if (address < 0 || address > 0xffff)
            {
                throw new ArgumentException("Invalid address: " + Integer.ToHexString(address));
            }

            //LOG.debug("Writing value {} to void address {}", Integer.toHexString(value), int.ToHexString(address));
        }

        public int GetByte(int address)
        {
            if (address < 0 || address > 0xffff)
            {
                throw new ArgumentException("Invalid address: " + Integer.ToHexString(address));
            }

            //LOG.debug("Reading value from void address {}", Integer.toHexString(address));
            return 0xff;
        }
    }
}