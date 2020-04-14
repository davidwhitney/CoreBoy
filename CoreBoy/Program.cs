using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using CoreBoy.gui;

namespace CoreBoy
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();

            var properties = LoadProperties();
            var emulator = new Emulator(args, properties);
            var ui = new WinFormsEmulatorSurface();

            emulator.Controller = ui;
            emulator.Display.OnFrameProduced += ui.UpdateDisplay;
            ui.Closed += (sender, e) => { emulator.Stop(); };


            new Thread(emulator.Run).Start();
            Application.Run(ui);
        }

        private static string LoadProperties()
        {
            var fileName = Path.Combine(Environment.GetEnvironmentVariable("user.home") ?? "", ".coffeegb.properties");
            var propFile = new FileInfo(fileName);
            return propFile.Exists ? File.ReadAllText(propFile.FullName) : "";
        }
    }
}