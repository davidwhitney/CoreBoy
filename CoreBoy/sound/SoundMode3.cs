using System;
using CoreBoy.memory;

namespace CoreBoy.sound
{
    public class SoundMode3 : AbstractSoundMode
    {

        private static readonly int[] DMG_WAVE = new int[]
        {
            0x84, 0x40, 0x43, 0xaa, 0x2d, 0x78, 0x92, 0x3c,
            0x60, 0x59, 0x59, 0xb0, 0x34, 0xb8, 0x2e, 0xda
        };

        private static readonly int[] CGB_WAVE = new int[]
        {
            0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff,
            0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff
        };

        private readonly Ram waveRam = new Ram(0xff30, 0x10);

        private int freqDivider;

        private int lastOutput;

        private int i;

        private int ticksSinceRead = 65536;

        private int lastReadAddr;

        private int buffer;

        private bool triggered;

        public SoundMode3(bool gbc) : base(0xff1a, 256, gbc)
        {
            foreach (int v in gbc ? CGB_WAVE : DMG_WAVE)
            {
                waveRam.setByte(0xff30, v);
            }
        }


        public override bool accepts(int address)
        {
            return waveRam.accepts(address) || base.accepts(address);
        }


        public override int getByte(int address)
        {
            if (!waveRam.accepts(address))
            {
                return base.getByte(address);
            }

            if (!isEnabled())
            {
                return waveRam.getByte(address);
            }
            else if (waveRam.accepts(lastReadAddr) && (gbc || ticksSinceRead < 2))
            {
                return waveRam.getByte(lastReadAddr);
            }
            else
            {
                return 0xff;
            }
        }


        public override void setByte(int address, int value)
        {
            if (!waveRam.accepts(address))
            {
                base.setByte(address, value);
                return;
            }

            if (!isEnabled())
            {
                waveRam.setByte(address, value);
            }
            else if (waveRam.accepts(lastReadAddr) && (gbc || ticksSinceRead < 2))
            {
                waveRam.setByte(lastReadAddr, value);
            }
        }


        protected override void setNr0(int value)
        {
            base.setNr0(value);
            dacEnabled = (value & (1 << 7)) != 0;
            channelEnabled &= dacEnabled;
        }


        protected override void setNr1(int value)
        {
            base.setNr1(value);
            length.setLength(256 - value);
        }


        protected override void setNr3(int value)
        {
            base.setNr3(value);
        }


        protected override void setNr4(int value)
        {
            if (!gbc && (value & (1 << 7)) != 0)
            {
                if (isEnabled() && freqDivider == 2)
                {
                    int pos = i / 2;
                    if (pos < 4)
                    {
                        waveRam.setByte(0xff30, waveRam.getByte(0xff30 + pos));
                    }
                    else
                    {
                        pos = pos & ~3;
                        for (int j = 0; j < 4; j++)
                        {
                            waveRam.setByte(0xff30 + j, waveRam.getByte(0xff30 + ((pos + j) % 0x10)));
                        }
                    }
                }
            }

            base.setNr4(value);
        }


        public override void start()
        {
            i = 0;
            buffer = 0;
            if (gbc)
            {
                length.reset();
            }

            length.start();
        }


        protected override void trigger()
        {
            i = 0;
            freqDivider = 6;
            triggered = !gbc;
            if (gbc)
            {
                getWaveEntry();
            }
        }


        public override int tick()
        {
            ticksSinceRead++;
            if (!updateLength())
            {
                return 0;
            }

            if (!dacEnabled)
            {
                return 0;
            }

            if ((getNr0() & (1 << 7)) == 0)
            {
                return 0;
            }

            if (--freqDivider == 0)
            {
                resetFreqDivider();
                if (triggered)
                {
                    lastOutput = (buffer >> 4) & 0x0f;
                    triggered = false;
                }
                else
                {
                    lastOutput = getWaveEntry();
                }

                i = (i + 1) % 32;
            }

            return lastOutput;
        }

        private int getVolume()
        {
            return (getNr2() >> 5) & 0b11;
        }

        private int getWaveEntry()
        {
            ticksSinceRead = 0;
            lastReadAddr = 0xff30 + i / 2;
            buffer = waveRam.getByte(lastReadAddr);
            int b = buffer;
            if (i % 2 == 0)
            {
                b = (b >> 4) & 0x0f;
            }
            else
            {
                b = b & 0x0f;
            }

            switch (getVolume())
            {
                case 0:
                    return 0;
                case 1:
                    return b;
                case 2:
                    return b >> 1;
                case 3:
                    return b >> 2;
                default:
                    throw new InvalidOperationException("Illegal state");
            }
        }

        private void resetFreqDivider()
        {
            freqDivider = getFrequency() * 2;
        }
    }

}