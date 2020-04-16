using System;

namespace CoreBoy.sound
{

    public class SoundMode2 : AbstractSoundMode
    {

        private int freqDivider;

        private int lastOutput;

        private int i;

        private VolumeEnvelope volumeEnvelope;

        public SoundMode2(bool gbc) : base(0xff15, 64, gbc)
        {
            volumeEnvelope = new VolumeEnvelope();
        }
        
        public override void start()
        {
            i = 0;
            if (gbc)
            {
                length.Reset();
            }

            length.Start();
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

            var e = updateLength();
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

        protected override void setNr1(int value)
        {
            base.setNr1(value);
            length.SetLength(64 - (value & 0b00111111));
        }

        protected override void setNr2(int value)
        {
            base.setNr2(value);
            volumeEnvelope.setNr2(value);
            dacEnabled = (value & 0b11111000) != 0;
            channelEnabled &= dacEnabled;
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
                    throw new InvalidOperationException("Illegal operation");
            }
        }

        private void resetFreqDivider()
        {
            freqDivider = getFrequency() * 4;
        }
    }
}