using System;
using System.IO;
using System.Windows.Forms;
using coffee_gb_netcore;
using eu.rekawek.coffeegb.gui;

namespace eu.rekawek.coffeegb
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            var emulator = new Emulator(args, LoadProperties());
            emulator.run();
        }

        private static string LoadProperties()
        {
            var fileName = Path.Combine(Environment.GetEnvironmentVariable("user.home"), ".coffeegb.properties");
            var propFile = new FileInfo(fileName);
            return propFile.Exists ? File.ReadAllText(propFile.FullName) : "";
        }
    }
}