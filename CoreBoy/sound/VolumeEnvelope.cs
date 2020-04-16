namespace CoreBoy.sound
{
	public class VolumeEnvelope
    {
        private int _initialVolume;
        private int _envelopeDirection;
        private int _sweep;
        private int _volume;
        private bool _finished;
        private int _i;

        public void SetNr2(int register)
        {
            _initialVolume = register >> 4;
            _envelopeDirection = (register & (1 << 3)) == 0 ? -1 : 1;
            _sweep = register & 0b111;
        }

        public bool IsEnabled() => _sweep > 0;

        public void Start()
        {
            _finished = true;
            _i = 8192;
        }

        public void Trigger()
        {
            _i = 0;
            _volume = _initialVolume;
            _finished = false;
        }

        public void Tick()
        {
            if (_finished)
            {
                return;
            }

            if ((_volume == 0 && _envelopeDirection == -1) || (_volume == 15 && _envelopeDirection == 1))
            {
                _finished = true;
                return;
            }

            if (++_i == _sweep * Gameboy.TicksPerSec / 64)
            {
                _i = 0;
                _volume += _envelopeDirection;
            }
        }

        public int GetVolume() => IsEnabled() ? _volume : _initialVolume;
    }

}