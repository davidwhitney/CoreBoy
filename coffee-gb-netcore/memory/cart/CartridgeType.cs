using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.rekawek.coffeegb.memory.cart
{
    public enum CartridgeType
    {
        ROM = 0x00,
        ROM_MBC1 = 0x01,
        ROM_MBC1_RAM = 0x02,
        ROM_MBC1_RAM_BATTERY = 0x03,
        ROM_MBC2 = 0x05,
        ROM_MBC2_BATTERY = 0x06,
        ROM_RAM = 0x08,
        ROM_RAM_BATTERY = 0x09,
        ROM_MMM01 = 0x0b,
        ROM_MMM01_SRAM = 0x0c,
        ROM_MMM01_SRAM_BATTERY = 0x0d,
        ROM_MBC3_TIMER_BATTERY = 0x0f,
        ROM_MBC3_TIMER_RAM_BATTERY = 0x10,
        ROM_MBC3 = 0x11,
        ROM_MBC3_RAM = 0x12,
        ROM_MBC3_RAM_BATTERY = 0x13,
        ROM_MBC5 = 0x19,
        ROM_MBC5_RAM = 0x1a,
        ROM_MBC5_RAM_BATTERY = 0x01b,
        ROM_MBC5_RUMBLE = 0x1c,
        ROM_MBC5_RUMBLE_SRAM = 0x1d,
        ROM_MBC5_RUMBLE_SRAM_BATTERY = 0x1e
    }

    public static class CartridgeTypeExtensions
    {
        public static IEnumerable<CartridgeType> values(this CartridgeType src)
        {
            return Enum.GetValues(typeof(CartridgeType)).Cast<CartridgeType>();
        }

        public static bool isMbc1(this CartridgeType src)
        {
            return src.nameContainsSegment("MBC1");
        }

        public static bool isMbc2(this CartridgeType src)
        {
            return src.nameContainsSegment("MBC2");
        }

        public static bool isMbc3(this CartridgeType src)
        {
            return src.nameContainsSegment("MBC3");
        }

        public static bool isMbc5(this CartridgeType src)
        {
            return src.nameContainsSegment("MBC5");
        }

        public static bool isMmm01(this CartridgeType src)
        {
            return src.nameContainsSegment("MMM01");
        }

        public static bool isRam(this CartridgeType src)
        {
            return src.nameContainsSegment("RAM");
        }

        public static bool isSram(this CartridgeType src)
        {
            return src.nameContainsSegment("SRAM");
        }

        public static bool isTimer(this CartridgeType src)
        {
            return src.nameContainsSegment("TIMER");
        }

        public static bool isBattery(this CartridgeType src)
        {
            return src.nameContainsSegment("BATTERY");
        }

        public static bool isRumble(this CartridgeType src)
        {
            return src.nameContainsSegment("RUMBLE");
        }

        private static bool nameContainsSegment(this CartridgeType src, string segment)
        {
            return src.ToString().Contains($"_{segment}_");
        }

        public static CartridgeType getById(int id)
        {
            return (CartridgeType) id;
        }
    }
}