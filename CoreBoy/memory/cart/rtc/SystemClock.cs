using System;

namespace CoreBoy.memory.cart.rtc
{
    public class SystemClock : IClock
    {
        public long CurrentTimeMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}