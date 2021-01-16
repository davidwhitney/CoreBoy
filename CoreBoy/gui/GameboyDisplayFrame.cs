using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CoreBoy.gui
{
    public class GameboyDisplayFrame
    {
        public static readonly int DisplayWidth = 160;
        public static readonly int DisplayHeight = 144;

        private readonly int[] _pixels;
        public GameboyDisplayFrame(int[] pixels) => _pixels = pixels;

        public IEnumerable<int[]> Rows()
        {
            var offset = 0;
            for (var row = 0; row < DisplayHeight; row++)
            {
                var thisRow = new int[DisplayWidth];
                Array.Copy(_pixels, offset, thisRow, 0, 160);
                yield return thisRow;
                offset += 160;
            }
        }

        public byte[] ToBitmap()
        {
            var pixels = new Image<Rgba32>(DisplayWidth, DisplayHeight);

            var x = 0;
            var y = 0;

            foreach (var pixel in _pixels)
            {
                if (x == DisplayWidth)
                {
                    x = 0;
                    y++;
                }

                var (r, g, b) = pixel.ToRgb();
                pixels[x, y] = new Rgba32((byte)r, (byte)g, (byte)b, 255);

                x++;
            }

            using var memoryStream = new MemoryStream();
            pixels.SaveAsBmp(memoryStream);
            pixels.Dispose();
            return memoryStream.ToArray();
        }
    }
    
    public static class GameboyDisplayFrameHelperExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int, int) ToRgb(this int pixel)
        {
            var b = pixel & 255;
            var g = (pixel >> 8) & 255;
            var r = (pixel >> 16) & 255;
            return (r, g, b);
        }
    }
}