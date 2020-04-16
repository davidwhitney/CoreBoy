namespace CoreBoy.sound
{
    public class SoundMode4 : SoundModeBase
    {
        private int _lastResult;
        private readonly VolumeEnvelope _volumeEnvelope;
        private readonly PolynomialCounter _polynomialCounter;
        private readonly Lfsr _lfsr = new Lfsr();

        public SoundMode4(bool gbc):base(0xff1f, 64, gbc)
        {
            _volumeEnvelope = new VolumeEnvelope();
            _polynomialCounter = new PolynomialCounter();
        }

        public override void Start()
        {
            if (Gbc)
            {
                Length.Reset();
            }

            Length.Start();
            _lfsr.Start();
            _volumeEnvelope.Start();
        }


        protected override void Trigger()
        {
            _lfsr.Reset();
            _volumeEnvelope.Trigger();
        }

        public override int Tick()
        {
            _volumeEnvelope.Tick();

            if (!UpdateLength())
            {
                return 0;
            }

            if (!DacEnabled)
            {
                return 0;
            }

            if (_polynomialCounter.Tick())
            {
                _lastResult = _lfsr.NextBit((Nr3 & (1 << 3)) != 0);
            }

            return _lastResult * _volumeEnvelope.GetVolume();
        }

        protected override void SetNr1(int value)
        {
            base.SetNr1(value);
            Length.SetLength(64 - (value & 0b00111111));
        }

        protected override void SetNr2(int value)
        {
            base.SetNr2(value);
            _volumeEnvelope.SetNr2(value);
            DacEnabled = (value & 0b11111000) != 0;
            ChannelEnabled &= DacEnabled;
        }

        protected override void SetNr3(int value)
        {
            base.SetNr3(value);
            _polynomialCounter.SetNr43(value);
        }
    }
}