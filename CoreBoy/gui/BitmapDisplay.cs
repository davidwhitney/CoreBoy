using System.IO;
using System.Threading;
using CoreBoy.gpu;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CoreBoy.gui
{
    public class BitmapDisplay : IDisplay
    {
        public static readonly int DisplayWidth = 160;
        public static readonly int DisplayHeight = 144;

        public static readonly int[] Colors = { 0xe6f8da, 0x99c886, 0x437969, 0x051f2a };

        private readonly int[] _rgb;
        public bool Enabled { get; set; }
        private bool _doRefresh;
        private int _i;

        private readonly object _lockObject = new object();
        
        public event FrameProducedEventHandler OnFrameProduced;

        private byte[] _currentScreenBytes = { };
        
        public BitmapDisplay()
        {
            _rgb = new int[DisplayWidth * DisplayHeight];
        }

        public void PutDmgPixel(int color)
        {
            _rgb[_i++] = Colors[color];
            _i = _i % _rgb.Length;
        }
        
        public void PutColorPixel(int gbcRgb)
        {
            _rgb[_i++] = TranslateGbcRgb(gbcRgb);
        }

        public static int TranslateGbcRgb(int gbcRgb)
        {
            var r = (gbcRgb >> 0) & 0x1f;
            var g = (gbcRgb >> 5) & 0x1f;
            var b = (gbcRgb >> 10) & 0x1f;
            var result = (r * 8) << 16;
            result |= (g * 8) << 8;
            result |= (b * 8) << 0;
            return result;
        }

        public void RequestRefresh()
        {
            lock (_lockObject)
            {
                _doRefresh = true;
            }
        }

        public void WaitForRefresh()
        {
            while (_doRefresh)
            {
                lock (_lockObject)
                {
                    Thread.Sleep(10);
                }
            }
        }

        public void EnableLcd()
        {
            Enabled = true;
        }

        public void DisableLcd()
        {
            Enabled = false;
        }
        
        public void Run(CancellationToken token)
        {
            _doRefresh = false;
            Enabled = true;
            
            while (!token.IsCancellationRequested)
            {
                if (!_doRefresh)
                {
                    Thread.Sleep(10);
                    continue;
                }

                var pixels = new Image<Rgba32>(DisplayWidth, DisplayHeight);

                var x = 0;
                var y = 0;

                foreach(var pixel in _rgb)
                {
                    if (x == DisplayWidth)
                    {
                        x = 0;
                        y++;
                    }

                    var hex = "#" + pixel.ToString("X6");
                    pixels[x, y] = Rgba32.FromHex(hex);

                    x++;
                }

                lock (_lockObject)
                {
                    byte[] bytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        pixels.SaveAsBmp(memoryStream);
                        pixels.Dispose();
                        bytes = memoryStream.ToArray();
                    }

                    lock(_currentScreenBytes)
                    {
                        _currentScreenBytes = bytes;
                    }

                    OnFrameProduced?.Invoke(this, bytes);
                    

                    _i = 0;
                    _doRefresh = false;
                }
            }
        }
    }
}