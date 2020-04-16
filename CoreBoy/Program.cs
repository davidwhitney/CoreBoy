using System;
using System.Collections.Generic;
using System.Linq;
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

            var arguments = new List<string>(args);

            PromptForRom(arguments);

            var emulator = new Emulator(arguments);
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

        private static void PromptForRom(List<string> arguments)
        {
            if (arguments.Any()) return;

            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Gameboy ROM (*.gb)|*.gb| All files(*.*) |*.*", FilterIndex = 0, RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                arguments.Add(openFileDialog.FileName);
            }
        }
    }
}