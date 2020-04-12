using System;
using System.Windows.Forms;
using coffee_gb_netcore;

namespace eu.rekawek.coffeegb
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            var mmuuu = new eu.rekawek.coffeegb.memory.Mmu();
        }
    }
}
