using System;

namespace CoreBoy.sound
{

    public class SoundMode2 : SoundModeBase
    {
        private int _freqDivider;
        private int _lastOutput;
        private int _i;
        private readonly VolumeEnvelope _volumeEnvelope;

        public SoundMode2(bool gbc) 
            : base(0xff15, 64, gbc)
        {
            _volumeEnvelope = new VolumeEnvelope();
        }
        
        public override void Start()
        {
            _i = 0;
            if (Gbc)
            {
                Length.Reset();
            }

            Length.Start();
            _volumeEnvelope.Start();
        }


        protected override void Trigger()
        {
            _i = 0;
            _freqDivider = 1;
            _volumeEnvelope.Trigger();
        }
        

        public override int Tick()
        {
            _volumeEnvelope.Tick();

            var e = UpdateLength();
            e = DacEnabled && e;
            if (!e)
            {
                return 0;
            }

            if (--_freqDivider == 0)
            {
                ResetFreqDivider();
                _lastOutput = ((GetDuty() & (1 << _i)) >> _i);
                _i = (_i + 1) % 8;
            }

            return _lastOutput * _volumeEnvelope.GetVolume();
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

        private int GetDuty()
        {
            var i = GetNr1() >> 6;
            return i switch
            {
                0 => 0b00000001,
                1 => 0b10000001,
                2 => 0b10000111,
                3 => 0b01111110,
                _ => throw new InvalidOperationException("Illegal operation")
            };
        }

        private void ResetFreqDivider()
        {
            _freqDivider = GetFrequency() * 4;
        }
    }
}