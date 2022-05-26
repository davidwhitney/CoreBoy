namespace CoreBoy
{
    public static class Integer
    {
        public static string ToHexString(int address)
        {
            return $"{address} - THIS SHOULD BE A HEX ADDRESS";
        }

        public static bool GetBit(this int byteValue, int position) => (byteValue & (1 << position)) != 0;

        public static int SetBit(this int byteValue, int position, bool value) => value ? SetBit(byteValue, position) : ClearBit(byteValue, position);
        public static int SetBit(this int byteValue, int position) => (byteValue | (1 << position)) & 0xff;
        public static int ClearBit(this int byteValue, int position) => ~(1 << position) & byteValue & 0xff;
    }
}