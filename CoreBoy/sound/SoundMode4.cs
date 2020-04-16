namespace CoreBoy.sound
{
    public class SoundMode4 : AbstractSoundMode
    {

        private VolumeEnvelope volumeEnvelope;

        private PolynomialCounter polynomialCounter;

        private int lastResult;

        private Lfsr lfsr = new Lfsr();

        public SoundMode4(bool gbc):base(0xff1f, 64, gbc)
        {
            
            volumeEnvelope = new VolumeEnvelope();
            polynomialCounter = new PolynomialCounter();
        }

        

        public override void start()
        {
            if (gbc)
            {
                length.Reset();
            }

            length.Start();
            lfsr.Start();
            volumeEnvelope.start();
        }


        protected override void trigger()
        {
            lfsr.Reset();
            volumeEnvelope.trigger();
        }

        

        public override int tick()
        {
            volumeEnvelope.tick();

            if (!updateLength())
            {
                return 0;
            }

            if (!dacEnabled)
            {
                return 0;
            }

            if (polynomialCounter.tick())
            {
                lastResult = lfsr.NextBit((nr3 & (1 << 3)) != 0);
            }

            return lastResult * volumeEnvelope.getVolume();
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

        

        protected override void setNr3(int value)
        {
            base.setNr3(value);
            polynomialCounter.setNr43(value);
        }
    }
}