using System;
using CoreBoy.memory;

namespace CoreBoy.sound
{
    public class Sound : AddressSpace
    {

        private static readonly int[] MASKS = new int[]
        {
            0x80, 0x3f, 0x00, 0xff, 0xbf,
            0xff, 0x3f, 0x00, 0xff, 0xbf,
            0x7f, 0xff, 0x9f, 0xff, 0xbf,
            0xff, 0xff, 0x00, 0x00, 0xbf,
            0x00, 0x00, 0x70,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private readonly AbstractSoundMode[] allModes = new AbstractSoundMode[4];

        private readonly Ram r = new Ram(0xff24, 0x03);

        private readonly SoundOutput output;

        private int[] channels = new int[4];

        private bool enabled;

        private bool[] overridenEnabled = {true, true, true, true};

        public Sound(SoundOutput output, bool gbc)
        {
            allModes[0] = new SoundMode1(gbc);
            allModes[1] = new SoundMode2(gbc);
            allModes[2] = new SoundMode3(gbc);
            allModes[3] = new SoundMode4(gbc);
            this.output = output;
        }

        public void tick()
        {
            if (!enabled)
            {
                return;
            }

            for (var i = 0; i < allModes.Length; i++)
            {
                var abstractSoundMode = allModes[i];
                var channel = abstractSoundMode.tick();
                channels[i] = channel;
            }

            var selection = r.getByte(0xff25);
            var left = 0;
            var right = 0;
            for (var i = 0; i < 4; i++)
            {
                if (!overridenEnabled[i])
                {
                    continue;
                }

                if ((selection & (1 << i + 4)) != 0)
                {
                    left += channels[i];
                }

                if ((selection & (1 << i)) != 0)
                {
                    right += channels[i];
                }
            }

            left /= 4;
            right /= 4;

            var volumes = r.getByte(0xff24);
            left *= ((volumes >> 4) & 0b111);
            right *= (volumes & 0b111);

            output.play((byte) left, (byte) right);
        }

        private AddressSpace getAddressSpace(int address)
        {
            foreach (var m in allModes)
            {
                if (m.accepts(address))
                {
                    return m;
                }
            }

            if (r.accepts(address))
            {
                return r;
            }

            return null;
        }


        public bool accepts(int address)
        {
            return getAddressSpace(address) != null;
        }


        public void setByte(int address, int value)
        {
            if (address == 0xff26)
            {
                if ((value & (1 << 7)) == 0)
                {
                    if (enabled)
                    {
                        enabled = false;
                        stop();
                    }
                }
                else
                {
                    if (!enabled)
                    {
                        enabled = true;
                        start();
                    }
                }

                return;
            }

            AddressSpace s = getAddressSpace(address);
            s?.setByte(address, value);
            // throw new ArgumentException();

        }


        public int getByte(int address)
        {
            int result;
            if (address == 0xff26)
            {
                result = 0;
                for (int i = 0; i < allModes.Length; i++)
                {
                    result |= allModes[i].isEnabled() ? (1 << i) : 0;
                }

                result |= enabled ? (1 << 7) : 0;
            }
            else
            {
                result = getUnmaskedByte(address);
            }

            return result | MASKS[address - 0xff10];
        }

        private int getUnmaskedByte(int address)
        {
            var s = getAddressSpace(address);
            if (s == null)
            {
                throw new ArgumentException();
            }

            return s.getByte(address);
        }

        private void start()
        {
            for (int i = 0xff10; i <= 0xff25; i++)
            {
                int v = 0;
                // lengths should be preserved
                if (i == 0xff11 || i == 0xff16 || i == 0xff20)
                {
                    // channel 1, 2, 4 lengths
                    v = getUnmaskedByte(i) & 0b00111111;
                }
                else if (i == 0xff1b)
                {
                    // channel 3 length
                    v = getUnmaskedByte(i);
                }

                setByte(i, v);
            }

            foreach (AbstractSoundMode m in allModes)
            {
                m.start();
            }

            output.start();
        }

        private void stop()
        {
            output.stop();
            foreach (AbstractSoundMode s in allModes)
            {
                s.stop();
            }
        }

        public void enableChannel(int i, bool enabled)
        {
            overridenEnabled[i] = enabled;
        }
    }
}