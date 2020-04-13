using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using eu.rekawek.coffeegb.gpu;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace eu.rekawek.coffeegb.gui
{
    public class BitmapDisplay : Display, IRunnable
    {
        public static readonly int DisplayWidth = 160;
        public static readonly int DisplayHeight = 144;

        public static readonly int[] Colors = { 0xe6f8da, 0x99c886, 0x437969, 0x051f2a };

        private readonly int[] _rgb;
        private bool _enabled;
        private int _scale;
        private bool _doStop;
        private bool _doRefresh;
        private int _i;

        private readonly object _lockObject = new object();

        private byte[] _currentScreenBytes = { };
        public byte[] CurrentScreenBytes
        {
            get
            {
                lock (_currentScreenBytes)
                {
                    return _currentScreenBytes;
                }
            }

        }
        
        public BitmapDisplay(int scale)
        {
            _rgb = new int[DisplayWidth * DisplayHeight];
            this._scale = scale;
        }


        public void putDmgPixel(int color)
        {
            _rgb[_i++] = Colors[color];
            _i = _i % _rgb.Length;
        }


        public void putColorPixel(int gbcRgb)
        {
            _rgb[_i++] = TranslateGbcRgb(gbcRgb);
        }

        public static int TranslateGbcRgb(int gbcRgb)
        {
            int r = (gbcRgb >> 0) & 0x1f;
            int g = (gbcRgb >> 5) & 0x1f;
            int b = (gbcRgb >> 10) & 0x1f;
            int result = (r * 8) << 16;
            result |= (g * 8) << 8;
            result |= (b * 8) << 0;
            return result;
        }

        public void requestRefresh()
        {
            lock (_lockObject)
            {
                _doRefresh = true;
            }
        }

        public void waitForRefresh()
        {
            while (_doRefresh)
            {
                lock (_lockObject)
                {
                    Thread.Sleep(10);
                }
            }
        }


        public void enableLcd()
        {
            _enabled = true;
        }


        public void disableLcd()
        {
            _enabled = false;
        }
        
        public void Run()
        {
            _doStop = false;
            _doRefresh = false;
            _enabled = true;


            while (!_doStop)
            {
                if (!_doRefresh) continue;

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

                    lock (_currentScreenBytes)
                    {

                        _currentScreenBytes = new byte[bytes.Length];
                        bytes.CopyTo(_currentScreenBytes, 0);
                        // Ha, crap copy.
                    }

                    // File.WriteAllBytes("image.bmp", bytes);

                    _i = 0;
                    _doRefresh = false;
                }
            }
        }

        public void Stop()
        {
            _doStop = true;
        }
    }
}