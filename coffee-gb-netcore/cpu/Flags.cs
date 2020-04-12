using System;
using System.Text;
using static eu.rekawek.coffeegb.cpu.BitUtils;

namespace eu.rekawek.coffeegb.cpu
{
    public class Flags
    {
        private static int Z_POS = 7;
        private static int N_POS = 6;
        private static int H_POS = 5;
        private static int C_POS = 4;
        private int flags;

        public int getFlagsByte()
        {
            return flags;
        }

        public bool isZ()
        {
            return getBit(flags, Z_POS);
        }

        public bool isN()
        {
            return getBit(flags, N_POS);
        }

        public bool isH()
        {
            return getBit(flags, H_POS);
        }

        public bool isC()
        {
            return getBit(flags, C_POS);
        }

        public void setZ(bool z)
        {
            flags = setBit(flags, Z_POS, z);
        }

        public void setN(bool n)
        {
            flags = setBit(flags, N_POS, n);
        }

        public void setH(bool h)
        {
            flags = setBit(flags, H_POS, h);
        }

        public void setC(bool c)
        {
            flags = setBit(flags, C_POS, c);
        }

        public void setFlagsByte(int flags)
        {
            checkByteArgument("flags", flags);
            this.flags = flags & 0xf0;
        }

        public string toString() => ToString();

        public override String ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append(isZ() ? 'Z' : '-');
            result.Append(isN() ? 'N' : '-');
            result.Append(isH() ? 'H' : '-');
            result.Append(isC() ? 'C' : '-');
            result.Append("----");
            return result.ToString();
        }
    }
}