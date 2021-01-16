using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public event FrameProducedEventHandler OnFrameProduced;

        private readonly object _lockObject = new object();

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

        public void RequestRefresh() => SetRefreshFlag(true);

        public void WaitForRefresh()
        {
            while (_doRefresh)
            {
                Thread.Sleep(1);
            }
        }
        
        public void Run(CancellationToken token)
        {
            SetRefreshFlag(false);
            
            Enabled = true;
            
            while (!token.IsCancellationRequested)
            {
                if (!_doRefresh)
                {
                    Thread.Sleep(1);
                    continue;
                }

                RefreshScreen();


                SetRefreshFlag(false);
            }
        }

        private void RefreshScreen()
        {
            var frame = new GameboyDisplayFrame(_rgb);
            var bytes = frame.ToBitmap();

            OnFrameProduced?.Invoke(this, bytes);

            _i = 0;
        }

        private void SetRefreshFlag(bool flag)
        {
            lock (_lockObject)
            {
                _doRefresh = flag;
            }
        }
    }

}