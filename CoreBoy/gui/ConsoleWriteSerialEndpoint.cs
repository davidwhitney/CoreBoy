using System;
using CoreBoy.serial;

namespace CoreBoy.gui
{
    public class ConsoleWriteSerialEndpoint : SerialEndpoint
    {
        public int transfer(int b)
        {
            Console.Write((char) b);
            return 0;
        }
    }
}