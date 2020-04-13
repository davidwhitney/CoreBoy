using System;
using System.Collections.Generic;
using System.IO;

namespace CoreBoy
{
    public class GameboyOptions
    {
        private readonly FileInfo romFile;
        private readonly bool forceDmg;
        private readonly bool forceCgb;
        private readonly bool useBootstrap;
        private readonly bool disableBatterySaves;
        private readonly bool debug;
        private readonly bool headless;

        public GameboyOptions(FileInfo romFile) : this(romFile, new string[0], new string[0])
        {
        }

        public GameboyOptions(FileInfo romFile, ICollection<string> paramz, ICollection<string> shortParams)
        {
            this.romFile = romFile;
            this.forceDmg = paramz.Contains("force-dmg") || shortParams.Contains("d");
            this.forceCgb = paramz.Contains("force-cgb") || shortParams.Contains("c");
            if (forceDmg && forceCgb)
            {
                throw new ArgumentException("force-dmg and force-cgb options are can't be used together");
            }

            this.useBootstrap = paramz.Contains("use-bootstrap") || shortParams.Contains("b");
            this.disableBatterySaves = paramz.Contains("disable-battery-saves") || shortParams.Contains("db");
            this.debug = paramz.Contains("debug");
            this.headless = paramz.Contains("headless");
        }

        public FileInfo getRomFile()
        {
            return romFile;
        }

        public bool isForceDmg()
        {
            return forceDmg;
        }

        public bool isForceCgb()
        {
            return forceCgb;
        }

        public bool isUsingBootstrap()
        {
            return useBootstrap;
        }

        public bool isSupportBatterySaves()
        {
            return !disableBatterySaves;
        }

        public bool isDebug()
        {
            return debug;
        }

        public bool isHeadless()
        {
            return headless;
        }

        public static void printUsage(TextWriter stream)
        {
            stream.WriteLine("Usage:");
            stream.WriteLine("java -jar coffee-gb.jar [OPTIONS] ROM_FILE");
            stream.WriteLine();
            stream.WriteLine("Available options:");
            stream.WriteLine("  -d  --force-dmg                Emulate classic GB (DMG) for universal ROMs");
            stream.WriteLine("  -c  --force-cgb                Emulate color GB (CGB) for all ROMs");
            stream.WriteLine("  -b  --use-bootstrap            Start with the GB bootstrap");
            stream.WriteLine("  -db --disable-battery-saves    Disable battery saves");
            stream.WriteLine("      --debug                    Enable debug console");
            stream.WriteLine("      --headless                 Start in the headless mode");
            stream.Flush();
        }

    }
}
    