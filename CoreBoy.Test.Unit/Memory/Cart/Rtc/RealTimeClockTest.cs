using System;
using CoreBoy.memory.cart.rtc;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.Memory.Cart.Rtc
{
    [TestFixture, Parallelizable(ParallelScope.None)]
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

            _rtc.Latch();
            _clock.Forward(new TimeSpan(10, 5, 19, 4));

            AssertClockEquals(5, 8, 12, 2);

            _rtc.Unlatch();

            AssertClockEquals(5 + 10, 8 + 5, 12 + 19, 2 + 4);
        }

        [Test]
        public void TestCounterOverflow()
        {
            _clock.Forward(new TimeSpan(511, 23, 59, 59));

            Assert.False(_rtc.IsCounterOverflow());

            _clock.Forward(TimeSpan.FromSeconds(1));

            AssertClockEquals(0, 0, 0, 0);
            Assert.True(_rtc.IsCounterOverflow());

            _clock.Forward(new TimeSpan(10, 5, 19, 4));

            AssertClockEquals(10, 5, 19, 4);
            Assert.True(_rtc.IsCounterOverflow());

            _rtc.ClearCounterOverflow();

            AssertClockEquals(10, 5, 19, 4);
            Assert.False(_rtc.IsCounterOverflow());
        }

        [Test]
        public void SetClock()
        {
            _clock.Forward(new TimeSpan(10, 5, 19, 4));

            AssertClockEquals(10, 5, 19, 4);

            _rtc.SetHalt(true);

            Assert.True(_rtc.IsHalt());

            _rtc.SetDayCounter(10);
            _rtc.SetHours(16);
            _rtc.SetMinutes(21);
            _rtc.SetSeconds(32);

            _clock.Forward(new TimeSpan(1, 1, 1, 1)); // should be ignored after unhalt
            _rtc.SetHalt(false);

            Assert.False(_rtc.IsHalt());

            AssertClockEquals(10, 16, 21, 32);

            _clock.Forward(new TimeSpan(2, 2, 2, 2));

            AssertClockEquals(12, 18, 23, 34);
        }

        private void AssertClockEquals(int days, int hours, int minutes, int seconds)
        {
            Assert.AreEqual(days, _rtc.GetDayCounter());
            Assert.AreEqual(hours, _rtc.GetHours());
            Assert.AreEqual(minutes, _rtc.GetMinutes());
            Assert.AreEqual(seconds, _rtc.GetSeconds());
        }
    }
}
