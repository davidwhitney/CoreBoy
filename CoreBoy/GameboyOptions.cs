using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace CoreBoy
{
    public class GameboyOptions
    {
        public FileInfo? RomFile => string.IsNullOrWhiteSpace(Rom) ? null : new FileInfo(Rom);

        [Option('r', "rom", Required = false, HelpText = "Rom file.")]
        public string Rom { get; set; }

        [Option('d', "force-dmg", Required = false, HelpText = "ForceDmg.")]
        public bool ForceDmg { get; set; }

        [Option('c', "force-cgb", Required = false, HelpText = "ForceCgb.")]
        public bool ForceCgb { get; set; }

        [Option('b', "use-bootstrap", Required = false, HelpText = "UseBootstrap.")]
        public bool UseBootstrap { get; set; }

        [Option("disable-battery-saves", Required = false, HelpText = "disable-battery-saves.")]
        public bool DisableBatterySaves { get; set; }

        [Option("debug", Required = false, HelpText = "Debug.")]
        public bool Debug { get; set; }

        [Option("headless", Required = false, HelpText = "headless.")]
        public bool Headless { get; set; }

        [Option("interactive", Required = false, HelpText = "Play on the console!")]
        public bool Interactive { get; set; }

        public bool ShowUi => !Headless;

        public bool IsSupportBatterySaves() => !DisableBatterySaves;

        public bool RomSpecified => !string.IsNullOrWhiteSpace(Rom);

        public GameboyOptions()
        {
        }

        public GameboyOptions(FileInfo romFile) : this(romFile, new string[0], new string[0])
        {
        }

        public GameboyOptions(FileInfo romFile, ICollection<string> longParameters, ICollection<string> shortParams)
        {
            Rom = romFile.FullName;
            ForceDmg = longParameters.Contains("force-dmg") || shortParams.Contains("d");
            ForceCgb = longParameters.Contains("force-cgb") || shortParams.Contains("c");


            UseBootstrap = longParameters.Contains("use-bootstrap") || shortParams.Contains("b");
            DisableBatterySaves = longParameters.Contains("disable-battery-saves") || shortParams.Contains("db");
            Debug = longParameters.Contains("debug");
            Headless = longParameters.Contains("headless");

            Verify();
        }

        public void Verify()
        {
            if (ForceDmg && ForceCgb)
            {
                throw new ArgumentException("force-dmg and force-cgb options are can't be used together");
            }
        }
        
        public static void PrintUsage(TextWriter stream)
        {
            stream.WriteLine("Usage:");
            stream.WriteLine("coreboy.cli.exe my-totally-not-pirate-rom-file.gb");
            stream.WriteLine();
            stream.WriteLine("Available options:");
            stream.WriteLine("  -d  --force-dmg                Emulate classic GB (DMG) for universal ROMs");
            stream.WriteLine("  -c  --force-cgb                Emulate color GB (CGB) for all ROMs");
            stream.WriteLine("  -b  --use-bootstrap            Start with the GB bootstrap");
            stream.WriteLine("      --disable-battery-saves    Disable battery saves");
            stream.WriteLine("      --debug                    Enable debug console");
            stream.WriteLine("      --headless                 Start in the headless mode");
            stream.WriteLine("      --interactive              Play on the console!");
            stream.Flush();
        }

        public static GameboyOptions Parse(string[] args)
        {
            var parser = new Parser(cfg =>
            {
                cfg.AutoHelp = true;
                cfg.HelpWriter = Console.Out;
            });

            var result = parser.ParseArguments<GameboyOptions>(args)
                .WithParsed(o => { o.Verify(); });


            if (result is Parsed<GameboyOptions> parsed)
            {
                if (args.Length == 1 && args[0].Contains(".gb"))
                {
                    parsed.Value.Rom = args[0];
                }

                return parsed.Value;
            }
            else
            {
                Console.WriteLine("Failed to parsed!");
                return null;
            }
        }
    }
}
    