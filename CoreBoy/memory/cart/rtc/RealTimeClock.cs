namespace CoreBoy.memory.cart.rtc
{
    public class RealTimeClock
    {
        private readonly IClock _clock;
        private long _offsetSec;
        private long _clockStart;
        private bool _halt;
        private long _latchStart;
        private int _haltSeconds;
        private int _haltMinutes;
        private int _haltHours;
        private int _haltDays;

        public RealTimeClock(IClock clock)
        {
            _clock = clock;
            _clockStart = clock.CurrentTimeMillis();
        }

        public void Latch()
        {
            _latchStart = _clock.CurrentTimeMillis();
        }

        public void Unlatch()
        {
            _latchStart = 0;
        }

        public int GetSeconds()
        {
            return (int) (ClockTimeInSec() % 60);
        }

        public int GetMinutes()
        {
            return (int) ((ClockTimeInSec() % (60 * 60)) / 60);
        }

        public int GetHours()
        {
            return (int) ((ClockTimeInSec() % (60 * 60 * 24)) / (60 * 60));
        }

        public int GetDayCounter()
        {
            return (int) (ClockTimeInSec() % (60 * 60 * 24 * 512) / (60 * 60 * 24));
        }

        public bool IsHalt()
        {
            return _halt;
        }

        public bool IsCounterOverflow()
        {
            return ClockTimeInSec() >= 60 * 60 * 24 * 512;
        }

        public void SetSeconds(int seconds)
        {
            if (!_halt)
            {
                return;
            }

            _haltSeconds = seconds;
        }

        public void SetMinutes(int minutes)
        {
            if (!_halt)
            {
                return;
            }

            _haltMinutes = minutes;
        }

        public void SetHours(int hours)
        {
            if (!_halt)
            {
                return;
            }

            _haltHours = hours;
        }

        public void SetDayCounter(int dayCounter)
        {
            if (!_halt)
            {
                return;
            }

            _haltDays = dayCounter;
        }

        public void SetHalt(bool halt)
        {
            if (halt && !_halt)
            {
                Latch();
                _haltSeconds = GetSeconds();
                _haltMinutes = GetMinutes();
                _haltHours = GetHours();
                _haltDays = GetDayCounter();
                Unlatch();
            }
            else if (!halt && _halt)
            {
                _offsetSec = _haltSeconds + _haltMinutes * 60 + _haltHours * 60 * 60 + _haltDays * 60 * 60 * 24;
                _clockStart = _clock.CurrentTimeMillis();
            }

            _halt = halt;
        }

        public void ClearCounterOverflow()
        {
            while (IsCounterOverflow())
            {
                _offsetSec -= 60 * 60 * 24 * 512;
            }
        }

        private long ClockTimeInSec()
        {
            var now = _latchStart == 0 ? _clock.CurrentTimeMillis() : _latchStart;
            return (now - _clockStart) / 1000 + _offsetSec;
        }

        public void Deserialize(long[] clockData)
        {
            var seconds = clockData[0];
            var minutes = clockData[1];
            var hours = clockData[2];
            var days = clockData[3];
            var daysHigh = clockData[4];
            var timestamp = clockData[10];

            _clockStart = timestamp * 1000;
            _offsetSec = seconds + minutes * 60 + hours * 60 * 60 + days * 24 * 60 * 60 +
                             daysHigh * 256 * 24 * 60 * 60;
        }

        public long[] Serialize()
        {
            var clockData = new long[11];
            Latch();
            clockData[0] = clockData[5] = GetSeconds();
            clockData[1] = clockData[6] = GetMinutes();
            clockData[2] = clockData[7] = GetHours();
            clockData[3] = clockData[8] = GetDayCounter() % 256;
            clockData[4] = clockData[9] = GetDayCounter() / 256;
            clockData[10] = _latchStart / 1000;
            Unlatch();
            return clockData;
        }
    }
}