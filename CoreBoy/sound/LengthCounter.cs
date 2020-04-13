using System;

namespace CoreBoy.sound
{

    //using static eu.rekawek.coffeegb.Gameboy.TICKS_PER_SEC;

    public class LengthCounter
    {
        // Replace with eu.rekawek.coffeegb.Gameboy.TICKS_PER_SEC...
        public static readonly int TICKS_PER_SEC = 4_194_304;

        private int DIVIDER = TICKS_PER_SEC / 256;

        private int fullLength;

        private int length;

        private long i;

        private bool enabled;

        public LengthCounter(int fullLength)
        {
            this.fullLength = fullLength;
        }

        public void start()
        {
            i = 8192;
        }

        public void tick()
        {
            if (++i == DIVIDER)
            {
                i = 0;
                if (enabled && length > 0)
                {
                    length--;
                }
            }
        }

        public void setLength(int length)
        {
            if (length == 0)
            {
                this.length = fullLength;
            }
            else
            {
                this.length = length;
            }
        }

        public void setNr4(int value)
        {
            bool enable = (value & (1 << 6)) != 0;
            bool trigger = (value & (1 << 7)) != 0;

            if (enabled)
            {
                if (length == 0 && trigger)
                {
                    if (enable && i < DIVIDER / 2)
                    {
                        setLength(fullLength - 1);
                    }
                    else
                    {
                        setLength(fullLength);
                    }
                }
            }
            else if (enable)
            {
                if (length > 0 && i < DIVIDER / 2)
                {
                    length--;
                }

                if (length == 0 && trigger && i < DIVIDER / 2)
                {
                    setLength(fullLength - 1);
                }
            }
            else
            {
                if (length == 0 && trigger)
                {
                    setLength(fullLength);
                }
            }

            this.enabled = enable;
        }

        public int getValue()
        {
            return length;
        }

        public bool isEnabled()
        {
            return enabled;
        }


        public String toString()
        {
            return String.Format("LengthCounter[l=%d,f=%d,c=%d,%s]", length, fullLength, i,
                enabled ? "enabled" : "disabled");
        }

        public void reset()
        {
            this.enabled = true;
            this.i = 0;
            this.length = 0;
        }
    }

}