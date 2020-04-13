using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using eu.rekawek.coffeegb.gpu;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;

namespace eu.rekawek.coffeegb.gui
{
    public class WinFormsDisplay : Display, IRunnable
    {
        public static readonly int DISPLAY_WIDTH = 160;
        public static readonly int DISPLAY_HEIGHT = 144;

        public static readonly int[] COLORS = { 0xe6f8da, 0x99c886, 0x437969, 0x051f2a };

        private readonly int[] rgb;
        private bool enabled;
        private int scale;
        private bool doStop;
        private bool doRefresh;
        private int i;

        private readonly object _lockObject = new object();
        
        public WinFormsDisplay(int scale)
        {
            rgb = new int[DISPLAY_WIDTH * DISPLAY_HEIGHT];
            this.scale = scale;
        }


        public void putDmgPixel(int color)
        {
            rgb[i++] = COLORS[color];
            i = i % rgb.Length;
        }


        public void putColorPixel(int gbcRgb)
        {
            rgb[i++] = translateGbcRgb(gbcRgb);
        }

        public static int translateGbcRgb(int gbcRgb)
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
                doRefresh = true;
            }
        }

        public void waitForRefresh()
        {
            while (doRefresh)
            {
                lock (_lockObject)
                {
                    Thread.Sleep(1);
                }
            }
        }


        public void enableLcd()
        {
            enabled = true;
        }


        public void disableLcd()
        {
            enabled = false;
        }
        
        public void run()
        {
            doStop = false;
            doRefresh = false;
            enabled = true;
            while (!doStop)
            {
                /*synchronized(this) {
                    try
                    {
                        wait(1);
                    }
                    catch (InterruptedException e)
                    {
                        break;
                    }
                }*/

                if (doRefresh)
                {

                    //var pixels = new Rgba32[DISPLAY_WIDTH, DISPLAY_HEIGHT];
                    var pixels = new Image<Rgba32>(DISPLAY_WIDTH, DISPLAY_HEIGHT);
                    //var aaaa = 0xE6F8DA;
                    var x = 0;
                    var y = 0;
                    foreach(var pixel in rgb)
                    {
                        if (x == DISPLAY_WIDTH)
                        {
                            x = 0;
                            y++;
                        }

                        var hex = "#" + pixel.ToString("X6");
                        pixels[x, y] = Rgba32.FromHex(hex);

                        x++;
                    }

                        

                    //img.setRGB(0, 0, DISPLAY_WIDTH, DISPLAY_HEIGHT, rgb, 0, DISPLAY_WIDTH);
                    // validate();
                    // repaint();

                    lock (_lockObject)
                    {
                        var memoryStream = new MemoryStream();
                        pixels.SaveAsJpeg(memoryStream);
                        var bytes = memoryStream.ToArray();

                        File.WriteAllBytes("image.jpg", bytes);

                        i = 0;
                        doRefresh = false;
                        //notifyAll();
                    }
                }
            }
        }

        public void stop()
        {
            doStop = true;
        }
    }
}