using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using eu.rekawek.coffeegb.controller;
using eu.rekawek.coffeegb.cpu;
using eu.rekawek.coffeegb.gpu;
using eu.rekawek.coffeegb.gui;
using eu.rekawek.coffeegb.memory;
using eu.rekawek.coffeegb.memory.cart;
using eu.rekawek.coffeegb.serial;
using eu.rekawek.coffeegb.sound;
using Timer = eu.rekawek.coffeegb.timer.Timer;

namespace eu.rekawek.coffeegb
{
    public class Gameboy : IRunnable
    {

        public static readonly int TICKS_PER_SEC = 4_194_304;

        private readonly InterruptManager interruptManager;

        private readonly Gpu gpu;

        private readonly Mmu mmu;

        private readonly Cpu cpu;

        private readonly Timer timer;

        private readonly Dma dma;

        private readonly Hdma hdma;

        private readonly Display display;

        private readonly Sound sound;

        private readonly SerialPort serialPort;

        private readonly bool gbc;

        private readonly SpeedMode speedMode;

        private readonly TextWriter console;

        private volatile bool doStop;

        private readonly List<Thread> tickListeners = new List<Thread>();

        public Gameboy(GameboyOptions options, Cartridge rom, Display display, Controller controller,
            SoundOutput soundOutput, SerialEndpoint serialEndpoint)
            : this(options, rom, display, controller, soundOutput, serialEndpoint, null)
        {
        }

        public Gameboy(GameboyOptions options, Cartridge rom, Display display, Controller controller,
            SoundOutput soundOutput, SerialEndpoint serialEndpoint, TextWriter console)
        {
            this.display = display;
            gbc = rom.isGbc();
            speedMode = new SpeedMode();
            interruptManager = new InterruptManager(gbc);
            timer = new Timer(interruptManager, speedMode);
            mmu = new Mmu();

            Ram oamRam = new Ram(0xfe00, 0x00a0);
            dma = new Dma(mmu, oamRam, speedMode);
            gpu = new Gpu(display, interruptManager, dma, oamRam, gbc);
            hdma = new Hdma(mmu);
            sound = new Sound(soundOutput, gbc);
            serialPort = new SerialPort(interruptManager, serialEndpoint, speedMode);
            mmu.addAddressSpace(rom);
            mmu.addAddressSpace(gpu);
            mmu.addAddressSpace(new Joypad(interruptManager, controller));
            mmu.addAddressSpace(interruptManager);
            mmu.addAddressSpace(serialPort);
            mmu.addAddressSpace(timer);
            mmu.addAddressSpace(dma);
            mmu.addAddressSpace(sound);

            mmu.addAddressSpace(new Ram(0xc000, 0x1000));
            if (gbc)
            {
                mmu.addAddressSpace(speedMode);
                mmu.addAddressSpace(hdma);
                mmu.addAddressSpace(new GbcRam());
                mmu.addAddressSpace(new UndocumentedGbcRegisters());
            }
            else
            {
                mmu.addAddressSpace(new Ram(0xd000, 0x1000));
            }

            mmu.addAddressSpace(new Ram(0xff80, 0x7f));
            mmu.addAddressSpace(new ShadowAddressSpace(mmu, 0xe000, 0xc000, 0x1e00));

            cpu = new Cpu(mmu, interruptManager, gpu, display, speedMode);

            interruptManager.disableInterrupts(false);
            if (!options.isUsingBootstrap())
            {
                initRegs();
            }

            this.console = console;
        }

        private void initRegs()
        {
            Registers r = cpu.getRegisters();

            r.setAF(0x01b0);
            if (gbc)
            {
                r.setA(0x11);
            }

            r.setBC(0x0013);
            r.setDE(0x00d8);
            r.setHL(0x014d);
            r.setSP(0xfffe);
            r.setPC(0x0100);
        }

        public void Run()
        {
            var requestedScreenRefresh = false;
            var lcdDisabled = false;
            doStop = false;
            while (!doStop)
            {
                var newMode = tick();
                if (newMode.HasValue)
                {
                    hdma.onGpuUpdate(newMode.Value);
                }

                if (!lcdDisabled && !gpu.isLcdEnabled())
                {
                    lcdDisabled = true;
                    display.requestRefresh();
                    hdma.onLcdSwitch(false);
                }
                else if (newMode == Gpu.Mode.VBlank)
                {
                    requestedScreenRefresh = true;
                    display.requestRefresh();
                }

                if (lcdDisabled && gpu.isLcdEnabled())
                {
                    lcdDisabled = false;
                    display.waitForRefresh();
                    hdma.onLcdSwitch(true);
                }
                else if (requestedScreenRefresh && newMode == Gpu.Mode.OamSearch)
                {
                    requestedScreenRefresh = false;
                    display.waitForRefresh();
                }

                // TODO: Port console stuff
                // console.ifPresent(Console::tick);
                tickListeners.ForEach(thread => thread.Start());
            }
        }

        public void Stop()
        {
            doStop = true;
        }

        public Gpu.Mode? tick()
        {
            timer.tick();
            if (hdma.isTransferInProgress())
            {
                hdma.tick();
            }
            else
            {
                cpu.tick();
            }

            dma.tick();
            sound.tick();
            serialPort.tick();
            return gpu.tick();
        }

        public AddressSpace getAddressSpace()
        {
            return mmu;
        }

        public Cpu getCpu()
        {
            return cpu;
        }

        public SpeedMode getSpeedMode()
        {
            return speedMode;
        }

        public Gpu getGpu()
        {
            return gpu;
        }

        public void registerTickListener(Thread tickListener)
        {
            tickListeners.Add(tickListener);
        }

        public void unregisterTickListener(Thread tickListener)
        {
            tickListeners.Remove(tickListener);
        }

        public Sound getSound()
        {
            return sound;
        }
    }
}