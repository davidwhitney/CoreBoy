using System.Collections.Concurrent;
using System.Collections.Generic;
using CoreBoy.cpu;

namespace CoreBoy.controller
{
    public class JoyPadButtonListener : IButtonListener
    {
        private readonly InterruptManager _interruptManager;
        private readonly ConcurrentDictionary<Button, Button> _buttons;

        public JoyPadButtonListener(InterruptManager interruptManager, ConcurrentDictionary<Button, Button> buttons)
        {
            _interruptManager = interruptManager;
            _buttons = buttons;
        }

        public void OnButtonPress(Button button)
        {
            _interruptManager.RequestInterrupt(InterruptManager.InterruptType.P1013);
            _buttons.TryAdd(button, button);
        }

        public void OnButtonRelease(Button button)
        {
            _buttons.TryRemove(button, out _);
        }
    }
}