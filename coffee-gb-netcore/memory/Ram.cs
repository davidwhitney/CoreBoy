using System;

namespace eu.rekawek.coffeegb.memory
{
    public class Ram : AddressSpace
    {

        private int[] space;

        private int length;

        private int offset;

        public Ram(int offset, int length)
        {
            this.space = new int[length];
            this.length = length;
            this.offset = offset;
        }

        private Ram(int offset, int length, Ram ram)
        {
            this.offset = offset;
            this.length = length;
            this.space = ram.space;
        }

        public static Ram createShadow(int offset, int length, Ram ram)
        {
            return new Ram(offset, length, ram);
        }


        public bool accepts(int address)
        {
            return address >= offset && address < offset + length;
        }


        public void setByte(int address, int value)
        {
            space[address - offset] = value;
        }

        public int getByte(int address)
        {
            int index = address - offset;
            if (index < 0 || index >= space.Length)
            {
                throw new IndexOutOfRangeException("Address: " + address);
            }

            return space[index];
        }
    }
}