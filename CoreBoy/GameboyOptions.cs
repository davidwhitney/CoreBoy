using System;
using System.Collections.Generic;
using System.IO;

namespace CoreBoy
{
    public class GameboyOptions
    {
        public FileInfo RomFile { get; }
        public bool ForceDmg { get; }
        public bool ForceCgb { get; }
        public bool UseBootstrap { get; }
        public bool DisableBatterySaves { get; }
        public bool Debug { get; }
        public bool Headless { get; }
        public bool IsSupportBatterySaves() => !DisableBatterySaves;

        public GameboyOptions(FileInfo romFile) : this(romFile, new string[0], new string[0])
        {
        }

        public GameboyOptions(FileInfo romFile, ICollection<string> longParameters, ICollection<string> shortParams)
        {
            RomFile = romFile;
            ForceDmg = longParameters.Contains("force-dmg") || shortParams.Contains("d");
            ForceCgb = longParameters.Contains("force-cgb") || shortParams.Contains("c");

            if (ForceDmg && ForceCgb)
            {
                throw new ArgumentException("force-dmg and force-cgb options are can't be used together");
            }

            UseBootstrap = longParameters.Contains("use-bootstrap") || shortParams.Contains("b");
            DisableBatterySaves = longParameters.Contains("disable-battery-saves") || shortParams.Contains("db");
            Debug = longParameters.Contains("debug");
            Headless = longParameters.Contains("headless");
        }
        
        public static void PrintUsage(TextWriter stream)
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
    