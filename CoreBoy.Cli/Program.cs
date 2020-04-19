using System;
using System.Collections.Generic;
using System.Threading;
using CoreBoy.controller;
using CoreBoy.gui;
using Button = CoreBoy.controller.Button;

namespace CoreBoy.Cli
{
    public class Program
    {
        static void Main(string[] args)
        {
            var cancellation = new CancellationTokenSource();
            var arguments = GameboyOptions.Parse(args);
            var emulator = new Emulator(arguments);

            if (!arguments.RomSpecified)
            {
                GameboyOptions.PrintUsage(Console.Out);
                Console.Out.Flush();
                Environment.Exit(1);
            }

            if (arguments.Interactive)
            {
                var ui = new CommandLineInteractivity();
                emulator.Controller = ui;
                emulator.Display.OnFrameProduced += ui.UpdateDisplay; 
                emulator.Run(cancellation.Token);
                ui.ProcessInput();
            }
            else
            {
                emulator.Run(cancellation.Token);
                Console.WriteLine("Running headless.");
                Console.WriteLine("Press ANY key to exit.");
                Console.ReadKey(true);
            }

            cancellation.Cancel();
        }
    }

    public class CommandLineInteractivity : IController
    {
        private IButtonListener _listener;
        private readonly Dictionary<ConsoleKey, Button> _controls;

        public CommandLineInteractivity()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WindowHeight = 92;

            _controls = new Dictionary<ConsoleKey, Button>
            {
                {ConsoleKey.LeftArrow, Button.Left},
                {ConsoleKey.RightArrow, Button.Right},
                {ConsoleKey.UpArrow, Button.Up},
                {ConsoleKey.DownArrow, Button.Down},
                {ConsoleKey.Z, Button.A},
                {ConsoleKey.X, Button.B},
                {ConsoleKey.Enter, Button.Start},
                {ConsoleKey.Backspace, Button.Select}
            };
        }

        public void SetButtonListener(IButtonListener listener) => _listener = listener;

        // Should probably be called "try to process input" amirite
        // ☜(ﾟヮﾟ☜)  (❁´◡`❁)  ( •_•)>⌐■-■
        public void ProcessInput()
        {
            Button lastButton = null;
            var input = Console.ReadKey(true);
            while (input.Key != ConsoleKey.Escape)
            {
                var button = _controls.ContainsKey(input.Key) ? _controls[input.Key] : null;

                if (button != null)
                {
                    if (lastButton != button)
                    {
                        _listener?.OnButtonRelease(lastButton);
                    }

                    _listener?.OnButtonPress(button);

                    var snapshot = button;
                    new Thread(() =>
                    {
                        Thread.Sleep(500);
                        _listener?.OnButtonRelease(snapshot);
                    }).Start();

                    lastButton = button;
                }

                input = Console.ReadKey(true);
            }
        }

        public void UpdateDisplay(object sender, byte[] framedata)
        {
            var frame = SillyAsciiArtCreator.GenerateArt(framedata);
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(frame);
        }
    }
}
