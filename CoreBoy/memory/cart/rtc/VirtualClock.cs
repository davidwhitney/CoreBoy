using System;

namespace CoreBoy.memory.cart.rtc
{
    public class VirtualClock : IClock
    {
        private DateTimeOffset _clock = DateTimeOffset.UtcNow;
        public long CurrentTimeMillis() => _clock.ToUnixTimeMilliseconds();
        public void Forward(TimeSpan time) => _clock += time;
    }
}