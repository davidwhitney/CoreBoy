using System.Runtime.CompilerServices;
using System.Threading;
using eu.rekawek.coffeegb.gpu;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;

namespace eu.rekawek.coffeegb.gui
{
    public class WinFormsDisplay : Display, IRunnable
    {
        public static readonly int DISPLAY_WIDTH = 160;
        public static readonly int DISPLAY_HEIGHT = 144;

        public static readonly int[] COLORS = new int[] {0xe6f8da, 0x99c886, 0x437969, 0x051f2a};

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
                // notifyAll();
            }
        }

        public void waitForRefresh()
        {
            lock (_lockObject)
            {
                while (doRefresh)
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


        /*protected void paintComponent(Graphics g)
        {
            base.paintComponent(g);

            Graphics2D g2d = (Graphics2D) g.create();
            if (enabled)
            {
                g2d.drawImage(img, 0, 0, DISPLAY_WIDTH * scale, DISPLAY_HEIGHT * scale, null);
            }
            else
            {
                g2d.setColor(new Color(COLORS[0]));
                g2d.drawRect(0, 0, DISPLAY_WIDTH * scale, DISPLAY_HEIGHT * scale);
            }

            g2d.dispose();
        }*/


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

                    var pixels = new Rgba32[DISPLAY_WIDTH, DISPLAY_HEIGHT];

                    var x = 0;
                    var y = 0;
                    foreach(var pixel in rgb)
                    {
                        if (x == DISPLAY_WIDTH)
                        {
                            x = 0;
                            y++;
                        }

                        pixels[x, y] = new Rgba32();

                        x++;
                    }


                    //img.setRGB(0, 0, DISPLAY_WIDTH, DISPLAY_HEIGHT, rgb, 0, DISPLAY_WIDTH);
                    // validate();
                    // repaint();

                    lock (_lockObject)
                    {
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