using System.Collections.Generic;

namespace CoreBoy.cpu
{
    public class InterruptManager : AddressSpace
    {

        public class InterruptType
        {
            public static InterruptType VBlank = new InterruptType(0x0040, 0);
            public static InterruptType LCDC = new InterruptType(0x0048, 1);
            public static InterruptType Timer = new InterruptType(0x0050, 2);
            public static InterruptType Serial = new InterruptType(0x0058, 3);
            public static InterruptType P10_13 = new InterruptType(0x0060, 4);

            public int Ordinal { get; }

            private int handler;

            InterruptType(int handler, int ordinal)
            {
                Ordinal = ordinal;
                this.handler = handler;
            }

            public int ordinal() => Ordinal;

            public int getHandler()
            {
                return handler;
            }

            public static IEnumerable<InterruptType> values()
            {
                yield return VBlank;
                yield return LCDC;
                yield return Timer;
                yield return Serial;
                yield return P10_13;
            }
        }

        private bool gbc;

        private bool ime;

        private int interruptFlag = 0xe1;

        private int interruptEnabled;

        private int pendingEnableInterrupts = -1;

        private int pendingDisableInterrupts = -1;

        public InterruptManager(bool gbc)
        {
            this.gbc = gbc;
        }

        public void enableInterrupts(bool withDelay)
        {
            pendingDisableInterrupts = -1;
            if (withDelay)
            {
                if (pendingEnableInterrupts == -1)
                {
                    pendingEnableInterrupts = 1;
                }
            }
            else
            {
                pendingEnableInterrupts = -1;
                ime = true;
            }
        }

        public void disableInterrupts(bool withDelay)
        {
            pendingEnableInterrupts = -1;
            if (withDelay && gbc)
            {
                if (pendingDisableInterrupts == -1)
                {
                    pendingDisableInterrupts = 1;
                }
            }
            else
            {
                pendingDisableInterrupts = -1;
                ime = false;
            }
        }

        public void requestInterrupt(InterruptType type)
        {
            interruptFlag = interruptFlag | (1 << type.Ordinal);
        }

        public void clearInterrupt(InterruptType type)
        {
            interruptFlag = interruptFlag & ~(1 << type.Ordinal);
        }

        public void onInstructionFinished()
        {
            if (pendingEnableInterrupts != -1)
            {
                if (pendingEnableInterrupts-- == 0)
                {
                    enableInterrupts(false);
                }
            }

            if (pendingDisableInterrupts != -1)
            {
                if (pendingDisableInterrupts-- == 0)
                {
                    disableInterrupts(false);
                }
            }
        }

        public bool isIme()
        {
            return ime;
        }

        public bool isInterruptRequested()
        {
            return (interruptFlag & interruptEnabled) != 0;
        }

        public bool isHaltBug()
        {
            return (interruptFlag & interruptEnabled & 0x1f) != 0 && !ime;
        }

        public bool accepts(int address)
        {
            return address == 0xff0f || address == 0xffff;
        }

        public void setByte(int address, int value)
        {
            switch (address)
            {
                case 0xff0f:
                    interruptFlag = value | 0xe0;
                    break;

                case 0xffff:
                    interruptEnabled = value;
                    break;
            }
        }

        public int getByte(int address)
        {
            switch (address)
            {
                case 0xff0f:
                    return interruptFlag;

                case 0xffff:
                    return interruptEnabled;

                default:
                    return 0xff;
            }
        }
    }
}