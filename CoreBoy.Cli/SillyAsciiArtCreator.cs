using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CoreBoy.Cli
{
    public class SillyAsciiArtCreator
    {
        private static readonly Dictionary<float, string> Map;

        static SillyAsciiArtCreator()
        {
            Map = new Dictionary<float, string>
            {
                {200, " "},
                {190, " "},
                {180, " "},
                {170, " "},
                {160, " "},
                {150, "."},
                {140, "o"},
                {130, "O"},
                {120, "+"},
                {110, "#"},
                {100, "@"},
                {080, "%"},
                {060, "░"},
                {040, "▒"},
                {020, "▓"},
                {000, "█"},
            };
        }

        public static string GenerateArt(byte[] jpg)
        {
            var image = Image.Load(jpg);
            image.Mutate(x => x.Resize(106, 72));

            var map = Map.OrderBy(kvp => kvp.Key).ToList();
            var sb = new StringBuilder();

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    var currentChar = MapToAscii(map, pixel);

                    sb.Append(currentChar);
                }

                sb.Append(Environment.NewLine);
            }

            image.Dispose();

            return PostProcessOutput(sb);
        }

        private static string MapToAscii(IEnumerable<KeyValuePair<float, string>> map, Rgba32 pixel)
        {
            var currentChar = "";
            foreach (var (key, value) in map)
            {
                if (key <= pixel.R)
                {
                    currentChar = value;
                }
            }
            return currentChar;
        }

        private static string PostProcessOutput(StringBuilder sb)
        {
            var output = sb.ToString();
            sb.Clear();
            output = output.Substring(0, output.Length - Environment.NewLine.Length);
            return output;
        }
    }
}
