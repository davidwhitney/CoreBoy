namespace CoreBoy.sound
{
    public class LengthCounter
    {
        private long _i;
        private readonly int _divider = Gameboy.TicksPerSec / 256;
        private readonly int _fullLength;

        public bool Enabled { get; private set; }
        public int Length { get; private set; }

        public LengthCounter(int fullLength)
        {
            _fullLength = fullLength;
        }

        public void Start()
        {
            _i = 8192;
        }

        public void Tick()
        {
            _i++;

            if (_i == _divider)
            {
                _i = 0;
                if (Enabled && Length > 0)
                {
                    Length--;
                }
            }
        }

        public void SetLength(int len)
        {
            Length = len == 0 ? _fullLength : len;
        }

        public void SetNr4(int value)
        {
            var enable = (value & (1 << 6)) != 0;
            var trigger = (value & (1 << 7)) != 0;

            if (Enabled)
            {
                if (Length == 0 && trigger)
                {
                    if (enable && _i < _divider / 2)
                    {
                        SetLength(_fullLength - 1);
                    }
                    else
                    {
                        SetLength(_fullLength);
                    }
                }
            }
            else if (enable)
            {
                if (Length > 0 && _i < _divider / 2)
                {
                    Length--;
                }

                if (Length == 0 && trigger && _i < _divider / 2)
                {
                    SetLength(_fullLength - 1);
                }
            }
            else
            {
                if (Length == 0 && trigger)
                {
                    SetLength(_fullLength);
                }
            }

            Enabled = enable;
        }

        public override string ToString()
        {
            return $"LengthCounter[l={Length},f={_fullLength},c={_i},{(Enabled ? "enabled" : "disabled")}]";
        }

        public void Reset()
        {
            Enabled = true;
            _i = 0;
            Length = 0;
        }
    }

}