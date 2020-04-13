using System;

namespace CoreBoy.memory.cart.rtc {

    public static class Clock
    {
        public static IClock SYSTEM_CLOCK { get; } = new SystemClock();
    }

    public interface IClock
    {
        long currentTimeMillis();
    }

    public class SystemClock : IClock
    {
        public long currentTimeMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}