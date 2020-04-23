using System;
using System.Windows.Forms;

namespace CoreBoy.Windows
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();

            Application.Run(new WinFormsEmulatorSurface());
        }
    }
}
