using System;

namespace CoreBoy.sound
{

    public class SoundMode1 : AbstractSoundMode
    {
        private int freqDivider;
        private int lastOutput;
        private int i;
        private FrequencySweep frequencySweep;
        private VolumeEnvelope volumeEnvelope;

        public SoundMode1(bool gbc) : base(0xff10, 64, gbc)
        {
            this.frequencySweep = new FrequencySweep();
            this.volumeEnvelope = new VolumeEnvelope();
        }

        public override void start()
        {
            i = 0;
            if (gbc)
            {
                length.reset();
            }

            length.start();
            frequencySweep.start();
            volumeEnvelope.start();
        }

        protected override void trigger()
        {
            i = 0;
            freqDivider = 1;
            volumeEnvelope.trigger();
        }

        public override int tick()
        {
            volumeEnvelope.tick();

            bool e = true;
            e = updateLength() && e;
            e = updateSweep() && e;
            e = dacEnabled && e;
            if (!e)
            {
                return 0;
            }

            if (--freqDivider == 0)
            {
                resetFreqDivider();
                lastOutput = ((getDuty() & (1 << i)) >> i);
                i = (i + 1) % 8;
            }

            return lastOutput * volumeEnvelope.getVolume();
        }

        protected void setNr0(int value)
        {
            base.setNr0(value);
            frequencySweep.setNr10(value);
        }

        protected void setNr1(int value)
        {
            base.setNr1(value);
            length.setLength(64 - (value & 0b00111111));
        }

        protected void setNr2(int value)
        {
            base.setNr2(value);
            volumeEnvelope.setNr2(value);
            dacEnabled = (value & 0b11111000) != 0;
            channelEnabled &= dacEnabled;
        }

        protected void setNr3(int value)
        {
            base.setNr3(value);
            frequencySweep.setNr13(value);
        }

        protected void setNr4(int value)
        {
            base.setNr4(value);
            frequencySweep.setNr14(value);
        }

        protected override int getNr3()
        {
            return frequencySweep.getNr13();
        }

        protected override int getNr4()
        {
            return (base.getNr4() & 0b11111000) | (frequencySweep.getNr14() & 0b00000111);
        }

        private int getDuty()
        {
            switch (getNr1() >> 6)
            {
                case 0:
                    return 0b00000001;
                case 1:
                    return 0b10000001;
                case 2:
                    return 0b10000111;
                case 3:
                    return 0b01111110;
                default:
                    throw new InvalidOperationException("Illegal state exception");
            }
        }

        private void resetFreqDivider()
        {
            freqDivider = getFrequency() * 4;
        }

        protected bool updateSweep()
        {
            frequencySweep.tick();
            if (channelEnabled && !frequencySweep.isEnabled())
            {
                channelEnabled = false;
            }

            return channelEnabled;
        }
    }

}