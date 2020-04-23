using System;
using System.Collections.Generic;
using System.Threading;
using CoreBoy.controller;
using CoreBoy.gpu;
using CoreBoy.memory.cart;
using CoreBoy.serial;
using CoreBoy.sound;

namespace CoreBoy.gui
{
    public class Emulator: IRunnable
    {
        public Gameboy Gameboy { get; set; }
        public IDisplay Display { get; set; } = new BitmapDisplay();
        public IController Controller { get; set; } = new NullController();
        public SerialEndpoint SerialEndpoint { get; set; } = new NullSerialEndpoint();
        public GameboyOptions Options { get; set; }
        public bool Active { get; set; }

        private readonly List<Thread> _runnables;
        
        public Emulator(GameboyOptions options)
        {
            _runnables = new List<Thread>();
            Options = options;
        }

        public void Run(CancellationToken token)
        {
            if (!Options.RomSpecified || !Options.RomFile.Exists)
            {
                throw new ArgumentException("The ROM path doesn't exist: " + Options.RomFile);
            }

            var rom = new Cartridge(Options);
            Gameboy = CreateGameboy(rom);

            if (Options.Headless)
            {
                Gameboy.Run(token);
                return;
            }

            if (Display is IRunnable runnableDisplay)
            {
                _runnables.Add(new Thread(() => runnableDisplay.Run(token))
                {
                    Priority = ThreadPriority.AboveNormal
                });
            }

            _runnables.Add(new Thread(() => Gameboy.Run(token))
            {
                Priority = ThreadPriority.AboveNormal
            });

            _runnables.ForEach(t => t.Start());
            Active = true;
        }

        public void Stop(CancellationTokenSource source)
        {
            if (!Active)
            {
                return;
            }
            
            source.Cancel();
            _runnables.Clear();
        }

        public void TogglePause() => Gameboy.Pause = !Gameboy.Pause;

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

            return new Gameboy(Options, rom, Display, Controller, new WinSound(), SerialEndpoint);
        }
    }
}