using System;
using System.Threading;
using System.Windows.Forms;
using CoreBoy.gui;

namespace CoreBoy.Windows
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var cancellation = new CancellationTokenSource();
            var arguments = GameboyOptions.Parse(args);

            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();

            var emulator = new Emulator(arguments);

            if (!arguments.RomSpecified)
            {
                var (success, romPath) = WinFormsEmulatorSurface.PromptForRom();
                arguments.Rom = success ? romPath : string.Empty;
            }

            var ui = new WinFormsEmulatorSurface();
            ui.Closed += (_, e) => { cancellation.Cancel(); };
            emulator.Controller = ui;
            emulator.Display.OnFrameProduced += ui.UpdateDisplay;

            emulator.Run(cancellation.Token);
            Application.Run(ui);
        }
    }
}
