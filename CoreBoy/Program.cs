using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CommandLine;
using CoreBoy.gui;

namespace CoreBoy
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

            if (!arguments.RomSpecified && arguments.ShowUi)
            {
                var (success, romPath) = WinFormsEmulatorSurface.PromptForRom();
                arguments.Rom = success ? romPath : string.Empty;
            }

            if (!arguments.RomSpecified)
            {
                GameboyOptions.PrintUsage(Console.Out);
                Console.Out.Flush();
                Environment.Exit(1);
            }
            
            if (arguments.ShowUi)
            {
                var ui = new WinFormsEmulatorSurface();
                ui.Closed += (_, e) => { cancellation.Cancel(); };
                emulator.Controller = ui;
                emulator.Display.OnFrameProduced += ui.UpdateDisplay;

                emulator.Run(cancellation.Token);
                Application.Run(ui);
            }
            else
            {
                emulator.Run(cancellation.Token);
                Console.WriteLine("Emulator running headless.");
                Console.WriteLine("Press ANY key to exit.");
                Console.ReadKey(true);

                cancellation.Cancel();
            }
        }
    }
}