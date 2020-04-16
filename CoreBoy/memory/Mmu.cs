using System;
using System.Collections.Generic;

namespace CoreBoy.memory
{
    public class Mmu : IAddressSpace
    {
        private static readonly IAddressSpace VOID = new VoidAddressSpace();
        private List<IAddressSpace> spaces = new List<IAddressSpace>();

        public void addAddressSpace(IAddressSpace space)
        {
            spaces.Add(space);
        }

        public bool Accepts(int address)
        {
            return true;
        }

        public void SetByte(int address, int value)
        {
            getSpace(address).SetByte(address, value);
        }

        public int GetByte(int address)
        {
            return getSpace(address).GetByte(address);
        }

        private IAddressSpace getSpace(int address)
        {
            foreach (var s in spaces)
            {
                if (s.Accepts(address))
                {
                    return s;
                }
            }

            return VOID;
        }

    }

    public class VoidAddressSpace : IAddressSpace
    {
        public bool Accepts(int address)
        {
            return true;
        }

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