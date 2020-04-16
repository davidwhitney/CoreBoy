using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreBoy.controller;
using CoreBoy.gpu;
using CoreBoy.memory.cart;
using CoreBoy.serial;
using CoreBoy.sound;

namespace CoreBoy.gui
{
    public class Emulator: IRunnable
    {
        private const int Scale = 2;

        public Gameboy Gameboy { get; set; }
        public IDisplay Display { get; set; } = new BitmapDisplay(Scale);
        public IController Controller { get; set; } = new NullController();
        public SerialEndpoint SerialEndpoint { get; set; } = new NullSerialEndpoint();
        public GameboyOptions Options { get; set; }

        private readonly List<Task> _runnables;

        public Emulator(string[] args, string properties)
        {
            _runnables = new List<Task>();
            Options = ParseArgs(args);
        }
        
        private static GameboyOptions ParseArgs(string[] args)
        {
            if (args.Length == 0)
            {
                GameboyOptions.PrintUsage(Console.Out);
                Environment.Exit(0);
                return null;
            }

            try
            {
                return CreateGameboyOptions(args);
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine();
                GameboyOptions.PrintUsage(Console.Error);
                Environment.Exit(1);
                return null;
            }
        }

        private static GameboyOptions CreateGameboyOptions(string[] args)
        {
            var longParams = new HashSet<string>();
            var shortParams = new HashSet<string>();

            string romPath = null;
            foreach (var a in args)
            {
                if (a.StartsWith("--"))
                {
                    longParams.Add(a.Substring(2));
                }
                else if (a.StartsWith("-"))
                {
                    shortParams.Add(a.Substring(1));
                }
                else
                {
                    romPath = a;
                }
            }

            if (romPath == null)
            {
                throw new ArgumentException("ROM path hasn't been specified");
            }

            var romFile = new FileInfo(romPath);
            if (!romFile.Exists)
            {
                throw new ArgumentException("The ROM path doesn't exist: " + romPath);
            }

            return new GameboyOptions(romFile, longParams, shortParams);
        }

        public void Run(CancellationToken token)
        {
            var rom = new Cartridge(Options);
            Gameboy = CreateGameboy(rom);

            if (Options.Headless)
            {
                Gameboy.Run(token);
                return;
            }

            
            if (Display is IRunnable runnableDisplay)
            {
                _runnables.Add(new Task(()=>runnableDisplay.Run(token), token, TaskCreationOptions.LongRunning));
            }

            _runnables.Add(
                new Task(() => Gameboy.Run(token),
                    token,
                    TaskCreationOptions.LongRunning)
            );

            _runnables.ForEach(t => t.Start());
        }

        private Gameboy CreateGameboy(Cartridge rom)
        {
            if (Options.Headless)
            {
                return new Gameboy(Options, rom, new NullDisplay(), new NullController(), new NullSoundOutput(), new NullSerialEndpoint());
            }

            // TODO: Make real things work
            // throw new NotImplementedException("Not implemented not headless.");
            //sound = new AudioSystemSoundOutput();
            //display = new SwingDisplay(SCALE);
            //controller = new SwingController(properties);
            //gameboy = new Gameboy(options, rom, display, controller, sound, serialEndpoint, console);

            return new Gameboy(Options, rom, Display, Controller, new NullSoundOutput(), SerialEndpoint);
        }
    }

}