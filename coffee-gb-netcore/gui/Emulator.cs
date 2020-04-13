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
    public class Emulator
    {
        private const int Scale = 2;

        private readonly GameboyOptions _options;
        private readonly Display _display;
        private readonly Gameboy _gameboy;

        public Emulator(string[] args, string properties)
        {
            _options = ParseArgs(args);
            var rom = new Cartridge(_options);

            SerialEndpoint serialEndpoint = new NullSerialEndpoint();
            
            var console = _options.isDebug() ? Console.Out : null;

            // BUG: What?
            // console.map(Thread::new).ifPresent(Thread::start);

            if (_options.isHeadless())
            {
                _display = null;
                _gameboy = new Gameboy(_options, rom, new NullDisplay(), new NullController(), new NullSoundOutput(), serialEndpoint, console);
            }
            else
            {
                // TODO: Make real things work
                // throw new NotImplementedException("Not implemented not headless.");
                //sound = new AudioSystemSoundOutput();
                //display = new SwingDisplay(SCALE);
                //controller = new SwingController(properties);
                //gameboy = new Gameboy(options, rom, display, controller, sound, serialEndpoint, console);
                
                _display = new WinFormsDisplay(Scale);
                _gameboy = new Gameboy(_options, rom, _display, new NullController(), new NullSoundOutput(), serialEndpoint, console);
            }

            // TODO: Do I even want to port this?
            //console.ifPresent(c -> c.init(gameboy));
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
            if (_options.isHeadless())
            {
                _gameboy.Run();
                return;
            }

            var threads = new List<Thread>();

            if (_display is IRunnable runnableDisplay)
            {
                threads.Add(new Thread(() => runnableDisplay.Run()));
            }

            threads.Add(new Thread(() => _gameboy.Run()));
            threads.ForEach(t => t.Start());
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