using System.Collections.Generic;
using eu.rekawek.coffeegb.cpu;

namespace eu.rekawek.coffeegb.controller
{
    public class Joypad : AddressSpace
    {
        private HashSet<Button> buttons = new HashSet<Button>();
        private int p1;

        public Joypad(InterruptManager interruptManager, Controller controller)
        {
            controller.setButtonListener(new JoyPadButtonListener(interruptManager, buttons));
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
            foreach (var b in buttons)
            {
                if ((b.getLine() & p1) == 0)
                {
                    result &= 0xff & ~b.getMask();
                }
            }

            return result;
        }
        
        private class JoyPadButtonListener : ButtonListener
        {
            private readonly InterruptManager _interruptManager;
            private readonly HashSet<Button> _buttons;

            public JoyPadButtonListener(InterruptManager interruptManager, HashSet<Button> buttons)
            {
                _interruptManager = interruptManager;
                _buttons = buttons;
            }

            public void onButtonPress(Button button)
            {
                _interruptManager.requestInterrupt(InterruptManager.InterruptType.P10_13);
                _buttons.Add(button);
            }

            public void onButtonRelease(Button button)
            {
                _buttons.Remove(button);
            }
        }
    }
}