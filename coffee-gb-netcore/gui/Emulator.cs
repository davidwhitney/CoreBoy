using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using eu.rekawek.coffeegb.controller;
using eu.rekawek.coffeegb.gpu;
using eu.rekawek.coffeegb.memory.cart;
using eu.rekawek.coffeegb.serial;
using eu.rekawek.coffeegb.sound;

namespace eu.rekawek.coffeegb.gui
{
    public class Emulator: IRunnable
    {
        private const int Scale = 2;

        public Gameboy Gameboy { get; }
        public Display Display { get; }
        public GameboyOptions Options { get; }

        private readonly List<Thread> _runnables;

        public Emulator(string[] args, string properties)
        {
            _runnables = new List<Thread>();
            Options = ParseArgs(args);
            var rom = new Cartridge(Options);

            SerialEndpoint serialEndpoint = new NullSerialEndpoint();
            var console = Options.isDebug() ? Console.Out : null;


            if (Options.isHeadless())
            {
                Gameboy = new Gameboy(Options, rom, new NullDisplay(), new NullController(), new NullSoundOutput(), serialEndpoint, console);
            }
            else
            {
                // TODO: Make real things work
                // throw new NotImplementedException("Not implemented not headless.");
                //sound = new AudioSystemSoundOutput();
                //display = new SwingDisplay(SCALE);
                //controller = new SwingController(properties);
                //gameboy = new Gameboy(options, rom, display, controller, sound, serialEndpoint, console);
                
                Display = new BitmapDisplay(Scale);
                Gameboy = new Gameboy(Options, rom, Display, new NullController(), new NullSoundOutput(), serialEndpoint, console);
            }
        }

        private static GameboyOptions ParseArgs(string[] args)
        {
            if (args.Length == 0)
            {
                GameboyOptions.printUsage(Console.Out);
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
                GameboyOptions.printUsage(Console.Error);
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

        public void Run()
        {
            if (Options.isHeadless())
            {
                Gameboy.Run();
                return;
            }
            
            if (Display is IRunnable runnableDisplay)
            {
                _runnables.Add(new Thread(() => runnableDisplay.Run()));
            }

            _runnables.Add(new Thread(() => Gameboy.Run()));
            _runnables.ForEach(t => t.Start());
        }

        public void Stop()
        {
            //_runnables.ForEach(t => t.Abort());
        }

        /*
        private void startGui() {
            display.setPreferredSize(new Dimension(160 * SCALE, 144 * SCALE));
    
            mainWindow = new JFrame("Coffee GB: " + rom.getTitle());
            mainWindow.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
            mainWindow.setLocationRelativeTo(null);
    
            mainWindow.setContentPane(display);
            mainWindow.setResizable(false);
            mainWindow.setVisible(true);
            mainWindow.pack();
    
            mainWindow.addKeyListener(controller);
    
            new Thread(display).start();
            new Thread(gameboy).start();
        }
    
        private void stopGui() {
            display.stop();
            gameboy.stop();
            mainWindow.dispose();
        }*/
    }

}