namespace CoreBoy.sound
{


    public class FrequencySweep
    {

        private static readonly int DIVIDER = Gameboy.TicksPerSec / 128;

        // sweep parameters
        private int period;

        private bool negate;

        private int shift;

        // current process variables
        private int timer;

        private int shadowFreq;

        private int nr13, nr14;

        private int i;

        private bool overflow;

        private bool counterEnabled;

        private bool negging;

        public void start()
        {
            counterEnabled = false;
            i = 8192;
        }

        public void trigger()
        {
            negging = false;
            overflow = false;

            shadowFreq = nr13 | ((nr14 & 0b111) << 8);
            timer = period == 0 ? 8 : period;
            counterEnabled = period != 0 || shift != 0;

            if (shift > 0)
            {
                calculate();
            }
        }

        public void setNr10(int value)
        {
            period = (value >> 4) & 0b111;
            negate = (value & (1 << 3)) != 0;
            shift = value & 0b111;
            if (negging && !negate)
            {
                overflow = true;
            }
        }

        public void setNr13(int value)
        {
            nr13 = value;
        }

        public void setNr14(int value)
        {
            nr14 = value;
            if ((value & (1 << 7)) != 0)
            {
                trigger();
            }
        }

        public int getNr13()
        {
            return nr13;
        }

        public int getNr14()
        {
            return nr14;
        }

        public void tick()
        {
            if (++i == DIVIDER)
            {
                i = 0;
                if (!counterEnabled)
                {
                    return;
                }

                if (--timer == 0)
                {
                    timer = period == 0 ? 8 : period;
                    if (period != 0)
                    {
                        int newFreq = calculate();
                        if (!overflow && shift != 0)
                        {
                            shadowFreq = newFreq;
                            nr13 = shadowFreq & 0xff;
                            nr14 = (shadowFreq & 0x700) >> 8;
                            calculate();
                        }
                    }
                }
            }
        }

        private int calculate()
        {
            int freq = shadowFreq >> shift;
            if (negate)
            {
                freq = shadowFreq - freq;
                negging = true;
            }
            else
            {
                freq = shadowFreq + freq;
            }

            if (freq > 2047)
            {
                overflow = true;
            }

            return freq;
        }

        public bool isEnabled()
        {
            return !overflow;
        }
    }

}