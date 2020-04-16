using System;
using CoreBoy.sound;

namespace CoreBoy.Test.Unit.Sound
{
    public abstract class AbstractLengthCounterTest
    {
        protected readonly int maxlen;

        protected readonly LengthCounter lengthCounter;

        protected AbstractLengthCounterTest() : this(256)
        {
        }

        protected AbstractLengthCounterTest(int maxlen)
        {
            this.maxlen = maxlen;
            lengthCounter = new LengthCounter(maxlen);
        }

        protected void wchn(int register, int value)
        {
            if (register == 1)
            {
                lengthCounter.setLength(0 - value);
            }
            else if (register == 4)
            {
                lengthCounter.setNr4(value);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        protected void delayClocks(int clocks)
        {
            for (int i = 0; i < clocks; i++)
            {
                lengthCounter.tick();
            }
        }

        protected void delayApu(int apuUnit)
        {
            delayClocks(apuUnit * (Gameboy.TicksPerSec / 256));
        }

        protected void syncApu()
        {
            lengthCounter.reset();
        }
    }
}