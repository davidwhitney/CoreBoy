using System.Collections.Concurrent;
using System.Collections.Generic;
using CoreBoy.cpu;

namespace CoreBoy.controller
{
    public class Joypad : AddressSpace
    {
        private ConcurrentDictionary<Button, Button> buttons = new ConcurrentDictionary<Button, Button>();
        private int p1;

        public Joypad(InterruptManager interruptManager, IController controller)
        {
            controller.SetButtonListener(new JoyPadButtonListener(interruptManager, buttons));
        }

        public bool accepts(int address)
        {
            return address == 0xff00;
        }


        public void setByte(int address, int value)
        {
            p1 = value & 0b00110000;
        }

        public int getByte(int address)
        {
            int result = p1 | 0b11001111;
            foreach (var b in buttons.Keys)
            {
                if ((b.getLine() & p1) == 0)
                {
                    result &= 0xff & ~b.getMask();
                }
            }

            return result;
        }
    }
}