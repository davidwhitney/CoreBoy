using System;
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

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            var emulator = new Emulator(args);
            var ui = new WinFormsEmulatorSurface();

            emulator.Controller = ui;
            emulator.Display.OnFrameProduced += ui.UpdateDisplay;
            ui.Closed += (sender, e) =>
            {
                cancellationTokenSource.Cancel();
            };

            emulator.Run(token);
            Application.Run(ui);
        }
    }
}