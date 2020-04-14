using System;

namespace CoreBoy.cpu
{
    public static class BitUtils
    {
        public static int GetMsb(int word)
        {
            CheckWordArgument("word", word);
            return word >> 8;
        }

        public static int GetLsb(int word)
        {
            CheckWordArgument("word", word);
            return word & 0xff;
        }

        public static int ToWord(int[] bytes)
        {
            return ToWord(bytes[1], bytes[0]);
        }

        public static int ToWord(int msb, int lsb)
        {
            CheckByteArgument("msb", msb);
            CheckByteArgument("lsb", lsb);
            return (msb << 8) | lsb;
        }

        public static bool GetBit(int byteValue, int position)
        {
            return (byteValue & (1 << position)) != 0;
        }

        public static int SetBit(int byteValue, int position, bool value)
        {
            return value ? SetBit(byteValue, position) : ClearBit(byteValue, position);
        }

        public static int SetBit(int byteValue, int position)
        {
            CheckByteArgument("byteValue", byteValue);
            return (byteValue | (1 << position)) & 0xff;
        }

        public static int ClearBit(int byteValue, int position)
        {
            CheckByteArgument("byteValue", byteValue);
            return ~(1 << position) & byteValue & 0xff;
        }

        public static int ToSigned(int byteValue)
        {
            if ((byteValue & (1 << 7)) == 0)
            {
                return byteValue;
            }
            else
            {
                return byteValue - 0x100;
            }
        }

        public static void CheckByteArgument(String argumentName, int argument)
        {
            //checkArgument(argument >= 0 && argument <= 0xff, "Argument {} should be a byte", argumentName);
        }

        public static void CheckWordArgument(String argumentName, int argument)
        {
            //checkArgument(argument >= 0 && argument <= 0xffff, "Argument {} should be a word", argumentName);
        }

    }
}