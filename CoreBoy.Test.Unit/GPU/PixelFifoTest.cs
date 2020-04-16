using System.Collections.Generic;
using System.Linq;
using CoreBoy.gpu;
using CoreBoy.memory;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.GPU
{
    [TestFixture, Parallelizable(ParallelScope.None)]
    public class PixelFifoTest
    {
        private DmgPixelFifo _fifo; 
        
        [SetUp]
        public void SetUp()
        {
            var registers = new MemoryRegisters(GpuRegister.Values().ToArray());
            registers.Put(GpuRegister.Bgp, 0b11100100);
            _fifo = new DmgPixelFifo(new NullDisplay(), registers);
        }

        [Test]
        public void TestEnqueue()
        {
            _fifo.Enqueue8Pixels(Zip(0b11001001, 0b11110000, false), TileAttributes.Empty);
            Assert.AreEqual(new List<int>{3, 3, 2, 2, 1, 0, 0, 1}, ArrayQueueAsList(_fifo.Pixels));
        }

        [Test]
        public void TestDequeue()
        {
            _fifo.Enqueue8Pixels(Zip(0b11001001, 0b11110000, false), TileAttributes.Empty);
            _fifo.Enqueue8Pixels(Zip(0b10101011, 0b11100111, false), TileAttributes.Empty);
            Assert.AreEqual(0b11, _fifo.DequeuePixel());
            Assert.AreEqual(0b11, _fifo.DequeuePixel());
            Assert.AreEqual(0b10, _fifo.DequeuePixel());
            Assert.AreEqual(0b10, _fifo.DequeuePixel());
            Assert.AreEqual(0b01, _fifo.DequeuePixel());
        }

        [Test]
        public void TestZip()
        {
            Assert.AreEqual(new int[] { 3, 3, 2, 2, 1, 0, 0, 1 }, Zip(0b11001001, 0b11110000, false));
            Assert.AreEqual(new int[] { 1, 0, 0, 1, 2, 2, 3, 3 }, Zip(0b11001001, 0b11110000, true));
        }

        private static int[] Zip(int data1, int data2, bool reverse) => Fetcher.Zip(data1, data2, reverse, new int[8]);

        private static List<int> ArrayQueueAsList(IntQueue queue)
        {
            var l = new List<int>(queue.Size());
            for (var i = 0; i < queue.Size(); i++)
            {
                l.Add(queue.Get(i));
            }
            return l;
        }
    }
}
