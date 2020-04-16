namespace CoreBoy.sound
{
    public class FrequencySweep
    {
        private static readonly int Divider = Gameboy.TicksPerSec / 128;

        // sweep parameters
        private int _period;
        private bool _negate;
        private int _shift;

        // current process variables
        private int _timer;
        private int _shadowFreq;
        private int _nr13;
        private int _nr14;
        private int _i;
        private bool _overflow;
        private bool _counterEnabled;
        private bool _negging;

        public void Start()
        {
            _counterEnabled = false;
            _i = 8192;
        }

        public void Trigger()
        {
            _negging = false;
            _overflow = false;

            _shadowFreq = _nr13 | ((_nr14 & 0b111) << 8);
            _timer = _period == 0 ? 8 : _period;
            _counterEnabled = _period != 0 || _shift != 0;

            if (_shift > 0)
            {
                Calculate();
            }
        }

        public void SetNr10(int value)
        {
            _period = (value >> 4) & 0b111;
            _negate = (value & (1 << 3)) != 0;
            _shift = value & 0b111;
            if (_negging && !_negate)
            {
                _overflow = true;
            }
        }

        public void SetNr13(int value) => _nr13 = value;

        public void SetNr14(int value)
        {
            _nr14 = value;
            if ((value & (1 << 7)) != 0)
            {
                Trigger();
            }
        }

        public int GetNr13() => _nr13;
        public int GetNr14() => _nr14;

        public void Tick()
        {
            _i++;

            if (_i != Divider) return;

            _i = 0;

            if (!_counterEnabled) return;
            
            _timer--;

            if (_timer != 0) return;

            _timer = _period == 0 ? 8 : _period;

            if (_period == 0) return;

            var newFreq = Calculate();

            if (_overflow || _shift == 0) return;

            _shadowFreq = newFreq;
            _nr13 = _shadowFreq & 0xff;
            _nr14 = (_shadowFreq & 0x700) >> 8;

            Calculate();
        }

        private int Calculate()
        {
            var freq = _shadowFreq >> _shift;
            if (_negate)
            {
                freq = _shadowFreq - freq;
                _negging = true;
            }
            else
            {
                freq = _shadowFreq + freq;
            }

            if (freq > 2047)
            {
                _overflow = true;
            }

            return freq;
        }

        public bool IsEnabled() => !_overflow;
    }
}