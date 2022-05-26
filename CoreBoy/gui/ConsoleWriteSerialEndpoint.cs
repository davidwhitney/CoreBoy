using System;
using CoreBoy.serial;

namespace CoreBoy.gui
{
    public class ConsoleWriteSerialEndpoint : SerialEndpoint
    {
        public bool externalClockPulsed() => false;

        public int transfer(int b)
        {
            Console.Write((char) b);
            return (b << 1) & 0xFF;
        }
    }
}