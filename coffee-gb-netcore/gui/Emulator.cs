using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using eu.rekawek.coffeegb.controller;
using eu.rekawek.coffeegb.cpu;
using eu.rekawek.coffeegb.gpu;
using eu.rekawek.coffeegb.memory.cart;
using eu.rekawek.coffeegb.serial;
using eu.rekawek.coffeegb.sound;

namespace eu.rekawek.coffeegb.gui
{
    public interface IRunnable
    {
        void run();
    }

    public class Emulator
    {
        private static readonly int SCALE = 2;
        private readonly GameboyOptions options;
        private readonly Cartridge rom;
        private readonly SoundOutput sound;
        private readonly Display display;
        private readonly Controller controller;
        private readonly SerialEndpoint serialEndpoint;
        private readonly SpeedMode speedMode;
        private readonly Gameboy gameboy;
        private readonly TextWriter console;

        public Emulator(String[] args, string properties)
        {
            options = parseArgs(args);
            rom = new Cartridge(options);
            speedMode = new SpeedMode();
            serialEndpoint = new NullSerialEndpoint();
            console = options.isDebug() ? Console.Out : null;

            // BUG: What?
            // console.map(Thread::new).ifPresent(Thread::start);

            if (options.isHeadless())
            {
                sound = null;
                display = null;
                controller = null;
                gameboy = new Gameboy(options, rom, new NullDisplay(), new NullController(), new NullSoundOutput(), serialEndpoint, console);
            }
            else
            {
                // TODO: Make real things work
                // throw new NotImplementedException("Not implemented not headless.");
                //sound = new AudioSystemSoundOutput();
                //display = new SwingDisplay(SCALE);
                //controller = new SwingController(properties);
                //gameboy = new Gameboy(options, rom, display, controller, sound, serialEndpoint, console);

               
                sound = new NullSoundOutput();
                display = new WinFormsDisplay(SCALE);
                controller = new NullController();
                gameboy = new Gameboy(options, rom, display, controller, sound, serialEndpoint, console);
            }

            // TODO: Do I even want to port this?
            //console.ifPresent(c -> c.init(gameboy));
        }

        private static GameboyOptions parseArgs(String[] args)
        {
            if (args.Length == 0)
            {
                GameboyOptions.printUsage(Console.Out);
                Environment.Exit(0);
                return null;
            }

            try
            {
                return createGameboyOptions(args);
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

        private static GameboyOptions createGameboyOptions(string[] args)
        {
            var paramz = new HashSet<string>();
            var shortparamz = new HashSet<string>();

            string romPath = null;
            foreach (var a in args)
            {
                if (a.StartsWith("--"))
                {
                    paramz.Add(a.Substring(2));
                }
                else if (a.StartsWith("-"))
                {
                    shortparamz.Add(a.Substring(1));
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

            return new GameboyOptions(romFile, paramz, shortparamz);
        }

        public void run()
        {
            if (options.isHeadless())
            {
                gameboy.run();
            }
            else
            {
                // TODO: Implement
                // throw new NotImplementedException();
                //System.setProperty("sun.java2d.opengl", "true");

                //UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
                //SwingUtilities.invokeLater(() -> startGui());

                if (display is IRunnable runnableDisplay)
                {
                    var thread = new Thread(() => runnableDisplay.run());
                    thread.Start();
                }

                gameboy.run();
            }
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