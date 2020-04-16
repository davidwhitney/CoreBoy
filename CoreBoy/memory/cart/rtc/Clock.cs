namespace CoreBoy.memory.cart.rtc
{

    public static class Clock
    {
        public static IClock SystemClock { get; } = new SystemClock();
    }
}