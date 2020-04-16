using System;
using CoreBoy.cpu;

namespace CoreBoy.timer
{
    public class Timer : IAddressSpace
    {

        private readonly SpeedMode speedMode;

        private readonly InterruptManager interruptManager;

        private static readonly int[] FREQ_TO_BIT = {9, 3, 5, 7};

        private int div, tac, tma, tima;

        private bool previousBit;

        private bool overflow;

        private int ticksSinceOverflow;

        public Timer(InterruptManager interruptManager, SpeedMode speedMode)
        {
            this.speedMode = speedMode;
            this.interruptManager = interruptManager;
        }

        public void tick()
        {
            updateDiv((div + 1) & 0xffff);
            if (overflow)
            {
                ticksSinceOverflow++;
                if (ticksSinceOverflow == 4)
                {
                    interruptManager.RequestInterrupt(InterruptManager.InterruptType.Timer);
                }

                if (ticksSinceOverflow == 5)
                {
                    tima = tma;
                }

                if (ticksSinceOverflow == 6)
                {
                    tima = tma;
                    overflow = false;
                    ticksSinceOverflow = 0;
                }
            }
        }

        private void incTima()
        {
            tima++;
            tima %= 0x100;
            if (tima == 0)
            {
                overflow = true;
                ticksSinceOverflow = 0;
            }
        }

        private void updateDiv(int newDiv)
        {
            div = newDiv;
            int bitPos = FREQ_TO_BIT[tac & 0b11];
            bitPos <<= speedMode.GetSpeedMode() - 1;
            bool bit = (div & (1 << bitPos)) != 0;
            bit &= (tac & (1 << 2)) != 0;
            if (!bit && previousBit)
            {
                incTima();
            }

            previousBit = bit;
        }

        public bool Accepts(int address)
        {
            return address >= 0xff04 && address <= 0xff07;
        }

        public void SetByte(int address, int value)
        {
            switch (address)
            {
                case 0xff04:
                    updateDiv(0);
                    break;

                case 0xff05:
                    if (ticksSinceOverflow < 5)
                    {
                        tima = value;
                        overflow = false;
                        ticksSinceOverflow = 0;
                    }

                    break;

                case 0xff06:
                    tma = value;
                    break;

                case 0xff07:
                    tac = value;
                    break;
            }
        }

        public int GetByte(int address)
        {
            switch (address)
            {
                case 0xff04:
                    return div >> 8;

                case 0xff05:
                    return tima;

                case 0xff06:
                    return tma;

                case 0xff07:
                    return tac | 0b11111000;
            }

            throw new ArgumentException();
        }
    }
}