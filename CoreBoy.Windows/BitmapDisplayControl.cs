using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using CoreBoy.gpu;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = System.Drawing.Image;

namespace CoreBoy.Windows
{
    public sealed class BitmapDisplayControl : Control, IDisplay
    {
        public static readonly int DisplayWidth = 160;
        public static readonly int DisplayHeight = 144;
        public static readonly float AspectRatio = DisplayWidth / (DisplayHeight * 1f);

        public static readonly int[] Colors = { 0xe6f8da, 0x99c886, 0x437969, 0x051f2a };

        private readonly int[] _rgb;
        private readonly MemoryStream _imageStream = new MemoryStream();
        private readonly Image<Rgba32> _imageBuffer = new Image<Rgba32>(DisplayWidth, DisplayHeight);

        private bool _doStop;
        private bool _doRefresh;
        private int _i;

        private readonly object _lockObject = new object();

        public BitmapDisplayControl()
        {
            _rgb = new int[DisplayWidth * DisplayHeight];
            SetStyle(ControlStyles.Opaque | ControlStyles.Selectable, false);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = System.Drawing.Color.FromArgb(Colors[0]);
            TabStop = false;
        }

        public event FrameProducedEventHandler OnFrameProduced;

        bool IDisplay.Enabled
        {
            get => DisplayEnabled;
            set => DisplayEnabled = value;
        }

        public bool DisplayEnabled { get; set; }

        public void PutDmgPixel(int color)
        {
            _rgb[_i++] = Colors[color];
            _i %= _rgb.Length;
        }

        public void PutColorPixel(int gbcRgb)
        {
            if (_i >= _rgb.Length)
            {
                return;
            }
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
                Monitor.PulseAll(_lockObject);
            }
        }

        public void WaitForRefresh()
        {
            lock (_lockObject)
            {
                while (_doRefresh)
                {
                    try
                    {
                        Monitor.Wait(_lockObject, 1);
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                }
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new PaintEventHandler((_, __) => OnPaint(e)));
                return;
            }

            base.OnPaint(e);

            var width = ClientRectangle.Width;
            var height = ClientRectangle.Height;
            var aspectRatio = width * 1f / height;

            if (aspectRatio <= AspectRatio)
            {
                height = (int)Math.Floor(width / AspectRatio);
            }
            else
            {
                width = (int)Math.Floor(height * AspectRatio);
            }

            var x = 0;
            var y = 0;
            if (width < ClientRectangle.Width)
            {
                x += (ClientRectangle.Width - width) / 2;
            }
            else
            {
                y += (ClientRectangle.Height - height) / 2;
            }

            try
            {
                if (DisplayEnabled)
                {
                    _imageStream.Seek(0, SeekOrigin.Begin);
                    _imageBuffer.SaveAsBmp(_imageStream);
                    using var img = Image.FromStream(_imageStream);

                    e.Graphics.DrawImage(img, x, y, width, height);
                }
                else
                {
                    using var brush = new SolidBrush(System.Drawing.Color.FromArgb(0xe6f8da));
                    e.Graphics.FillRectangle(brush, x, y, width, height);
                }
            }
            catch (ObjectDisposedException) { }
        }

        public void SaveLastFrame(string path)
        {
            using var fs = File.OpenWrite(path);
            _imageBuffer.SaveAsBmp(fs);
            fs.Flush();
            fs.Close();
        }

        public void Run(CancellationToken token)
        {
            _doStop = false;
            _doRefresh = false;
            DisplayEnabled = true;

            while (!_doStop)
            {
                lock (_lockObject)
                {
                    try
                    {
                        Monitor.Wait(_lockObject, 1);
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                }

                if (_doRefresh)
                {
                    FillAndDrawBuffer();

                    lock (_lockObject)
                    {
                        _i = 0;
                        _doRefresh = false;
                        Monitor.PulseAll(_lockObject);
                    }
                }

                _doStop = token.IsCancellationRequested;
            }
        }

        private void FillAndDrawBuffer()
        {
            try
            {
                var pi = 0;
                while (pi < _rgb.Length)
                {
                    var (r, g, b) = ToRgb(_rgb[pi]);
                    _imageBuffer[pi % DisplayWidth, pi++ / DisplayWidth] = new Rgba32((byte)r, (byte)g, (byte)b, 255);
                }

                Invalidate();
            }
            catch (ObjectDisposedException) { }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int, int) ToRgb(int pixel)
        {
            var b = pixel & 255;
            var g = (pixel >> 8) & 255;
            var r = (pixel >> 16) & 255;
            return (r, g, b);
        }
    }
}