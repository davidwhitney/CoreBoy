using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CoreBoy.memory.cart
{
    public static class CartridgeTypeExtensions
    {
        public static IEnumerable<CartridgeType> Values(this CartridgeType src)
        {
            return Enum.GetValues(typeof(CartridgeType)).Cast<CartridgeType>();
        }

        public static bool IsMbc1(this CartridgeType src) => src.NameContainsSegment("MBC1");
        public static bool IsMbc2(this CartridgeType src) => src.NameContainsSegment("MBC2");
        public static bool IsMbc3(this CartridgeType src) => src.NameContainsSegment("MBC3");
        public static bool IsMbc5(this CartridgeType src) => src.NameContainsSegment("MBC5");
        public static bool IsMmm01(this CartridgeType src) => src.NameContainsSegment("MMM01");
        public static bool IsRam(this CartridgeType src) => src.NameContainsSegment("RAM");
        public static bool IsSram(this CartridgeType src) => src.NameContainsSegment("SRAM");
        public static bool IsTimer(this CartridgeType src) => src.NameContainsSegment("TIMER");
        public static bool IsBattery(this CartridgeType src) => src.NameContainsSegment("BATTERY");
        public static bool IsRumble(this CartridgeType src) => src.NameContainsSegment("RUMBLE");
        private static bool NameContainsSegment(this CartridgeType src, string segment)
        {
            var regex = new Regex("(^|_)" + Regex.Escape(segment) + "($|_)");
            return regex.IsMatch(src.ToString());
            //return src.ToString().Contains($"_{segment}_");
        }

        public static CartridgeType GetById(int id) => (CartridgeType) id;
    }
}