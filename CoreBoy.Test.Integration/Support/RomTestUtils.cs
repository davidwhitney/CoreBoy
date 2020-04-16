using System;
using System.IO;
using NUnit.Framework;

namespace CoreBoy.Test.Integration.Support
{
    public class RomTestUtils
    {
        public static void testRomWithMemory(FileInfo romFileInfoInfo, bool trace = false)
        {
            Console.WriteLine($"\n### Running test rom {romFileInfoInfo.FullName} ###");
            var runner = new MemoryTestRunner(romFileInfoInfo, Console.Out, trace);

            var result = runner.RunTest();

            Assert.AreEqual(0, result.GetStatus(), "Non-zero return value");
        }

        public static void testRomWithSerial(FileInfo romFileInfoInfo, bool trace = false)
        {
            Console.WriteLine($"\n### Running test rom {romFileInfoInfo.FullName} ###");
            var runner = new SerialTestRunner(romFileInfoInfo, Console.Out, trace);

            var result = runner.RunTest();

            Assert.True(result.Contains("Passed"));
        }

        public static void testMooneyeRom(FileInfo romFileInfoInfo, bool trace = false)
        {
            Console.WriteLine($"\n### Running test rom {romFileInfoInfo.FullName} ###");
            var runner = new MooneyeTestRunner(romFileInfoInfo, Console.Out, trace);
            var result = runner.RunTest();
            Assert.True(result);
        }
    }
}