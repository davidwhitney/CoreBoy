using System;
using System.Collections.Generic;
using CoreBoy.cpu;

namespace CoreBoy.memory
{
    public class Mmu : AddressSpace
    {
        private static readonly AddressSpace VOID = new VoidAddressSpace();
        private List<AddressSpace> spaces = new List<AddressSpace>();

        public void addAddressSpace(AddressSpace space)
        {
            spaces.Add(space);
        }

        public bool accepts(int address)
        {
            return true;
        }

        public void setByte(int address, int value)
        {
            BitUtils.checkByteArgument("value", value);
            BitUtils.checkWordArgument("address", address);
            getSpace(address).setByte(address, value);
        }

        public int getByte(int address)
        {
            BitUtils.checkWordArgument("address", address);
            return getSpace(address).getByte(address);
        }

        private AddressSpace getSpace(int address)
        {
            foreach (var s in spaces)
            {
                if (s.accepts(address))
                {
                    return s;
                }
            }

            return VOID;
        }

    }

    public class VoidAddressSpace : AddressSpace
    {
        public bool accepts(int address)
        {
            return true;
        }

        public void setByte(int address, int value)
        {
            if (address < 0 || address > 0xffff)
            {
                throw new ArgumentException("Invalid address: " + Integer.toHexString(address));
            }

            //LOG.debug("Writing value {} to void address {}", Integer.toHexString(value), int.ToHexString(address));
        }

        public int getByte(int address)
        {
            if (address < 0 || address > 0xffff)
            {
                throw new ArgumentException("Invalid address: " + Integer.toHexString(address));
            }

            //LOG.debug("Reading value from void address {}", Integer.toHexString(address));
            return 0xff;
        }
    }
}