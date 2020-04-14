using System;
using CoreBoy.memory.cart.rtc;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.Memory.Cart.Rtc
{
    public class RealTimeClockTest
    {
        private RealTimeClock _rtc;
        private VirtualClock _clock;

        [SetUp]
        public void SetUp()
        {
            _clock = new VirtualClock();
            _rtc = new RealTimeClock(_clock);
        }

        [Test]
        public void TestBasicGet()
        {
            _clock.Forward(new TimeSpan(5, 8, 12, 2));
            AssertClockEquals(5, 8, 12, 2);
        }

        [Test]
        public void TestLatch()
        {
            _clock.Forward(new TimeSpan(5, 8, 12, 2));

            _rtc.latch();
            _clock.Forward(new TimeSpan(10, 5, 19, 4));

            AssertClockEquals(5, 8, 12, 2);

            _rtc.unlatch();

            AssertClockEquals(5 + 10, 8 + 5, 12 + 19, 2 + 4);
        }

        [Test]
        public void TestCounterOverflow()
        {
            _clock.Forward(new TimeSpan(511, 23, 59, 59));

            Assert.False(_rtc.isCounterOverflow());

            _clock.Forward(TimeSpan.FromSeconds(1));

            AssertClockEquals(0, 0, 0, 0);
            Assert.True(_rtc.isCounterOverflow());

            _clock.Forward(new TimeSpan(10, 5, 19, 4));

            AssertClockEquals(10, 5, 19, 4);
            Assert.True(_rtc.isCounterOverflow());

            _rtc.clearCounterOverflow();

            AssertClockEquals(10, 5, 19, 4);
            Assert.False(_rtc.isCounterOverflow());
        }

        [Test]
        public void SetClock()
        {
            _clock.Forward(new TimeSpan(10, 5, 19, 4));

            AssertClockEquals(10, 5, 19, 4);

            _rtc.setHalt(true);

            Assert.True(_rtc.isHalt());

            _rtc.setDayCounter(10);
            _rtc.setHours(16);
            _rtc.setMinutes(21);
            _rtc.setSeconds(32);

            _clock.Forward(new TimeSpan(1, 1, 1, 1)); // should be ignored after unhalt
            _rtc.setHalt(false);

            Assert.False(_rtc.isHalt());

            AssertClockEquals(10, 16, 21, 32);

            _clock.Forward(new TimeSpan(2, 2, 2, 2));

            AssertClockEquals(12, 18, 23, 34);
        }

        private void AssertClockEquals(int days, int hours, int minutes, int seconds)
        {
            Assert.AreEqual(days, _rtc.getDayCounter());
            Assert.AreEqual(hours, _rtc.getHours());
            Assert.AreEqual(minutes, _rtc.getMinutes());
            Assert.AreEqual(seconds, _rtc.getSeconds());
        }
    }
}
