using System;
using System.IO;
using System.Linq;
using System.Text;
using CoreBoy.memory.cart.battery;
using CoreBoy.memory.cart.type;

namespace CoreBoy.memory.cart
{
    public class Cartridge : AddressSpace
    {
        public enum GameboyTypeFlag
        {
            UNIVERSAL = 0x80,
            CGB = 0xc0,
            NON_CGB = 0
        }

        public static class GameboyTypeFlagExtensions
        {
            public static GameboyTypeFlag getFlag(int value)
            {
                if (value == 0x80)
                {
                    return GameboyTypeFlag.UNIVERSAL;
                }
                else if (value == 0xc0)
                {
                    return GameboyTypeFlag.CGB;
                }
                else
                {
                    return GameboyTypeFlag.NON_CGB;
                }
            }
        }

        //private static readonly Logger LOG = LoggerFactory.getLogger(Cartridge.class);

        private readonly AddressSpace addressSpace;

        private readonly GameboyTypeFlag gameboyType;

        private readonly bool gbc;

        private readonly String title;

        private int dmgBoostrap;

        public Cartridge(GameboyOptions options)
        {
            var file = options.RomFile;
            int[] rom = loadFile(file);
            CartridgeType type = CartridgeTypeExtensions.getById(rom[0x0147]);
            title = getTitle(rom);
            // LOG.debug("Cartridge {}, type: {}", title, type);
            gameboyType = GameboyTypeFlagExtensions.getFlag(rom[0x0143]);
            int romBanks = getRomBanks(rom[0x0148]);
            int ramBanks = getRamBanks(rom[0x0149]);
            if (ramBanks == 0 && type.isRam())
            {
                // LOG.warn("RAM bank is defined to 0. Overriding to 1.");
                ramBanks = 1;
            }
            // LOG.debug("ROM banks: {}, RAM banks: {}", romBanks, ramBanks);

            Battery battery = new NullBattery();
            if (type.isBattery() && options.IsSupportBatterySaves())
            {
                throw new NotImplementedException("Implement battery loading");
                // battery = new FileBattery(file.getParentFile(), FilenameUtils.removeExtension(file.getName()));
            }

            if (type.isMbc1())
            {
                addressSpace = new Mbc1(rom, type, battery, romBanks, ramBanks);
            }
            else if (type.isMbc2())
            {
                addressSpace = new Mbc2(rom, type, battery, romBanks);
            }
            else if (type.isMbc3())
            {
                addressSpace = new Mbc3(rom, type, battery, romBanks, ramBanks);
            }
            else if (type.isMbc5())
            {
                addressSpace = new Mbc5(rom, type, battery, romBanks, ramBanks);
            }
            else
            {
                addressSpace = new Rom(rom, type, romBanks, ramBanks);
            }

            dmgBoostrap = options.UseBootstrap ? 0 : 1;
            if (options.ForceCgb)
            {
                gbc = true;
            }
            else if (gameboyType == Cartridge.GameboyTypeFlag.NON_CGB)
            {
                gbc = false;
            }
            else if (gameboyType == Cartridge.GameboyTypeFlag.CGB)
            {
                gbc = true;
            }
            else
            {
                // UNIVERSAL
                gbc = !options.ForceDmg;
            }
        }

        private String getTitle(int[] rom)
        {
            StringBuilder t = new StringBuilder();
            for (int i = 0x0134; i < 0x0143; i++)
            {
                char c = (char) rom[i];
                if (c == 0)
                {
                    break;
                }

                t.Append(c);
            }

            return t.ToString();
        }

        public String getTitle()
        {
            return title;
        }

        public bool isGbc()
        {
            return gbc;
        }


        public bool accepts(int address)
        {
            return addressSpace.accepts(address) || address == 0xff50;
        }


        public void setByte(int address, int value)
        {
            if (address == 0xff50)
            {
                dmgBoostrap = 1;
            }
            else
            {
                addressSpace.setByte(address, value);
            }
        }


        public int getByte(int address)
        {
            if (dmgBoostrap == 0 && !gbc && (address >= 0x0000 && address < 0x0100))
            {
                return BootRom.GAMEBOY_CLASSIC[address];
            }
            else if (dmgBoostrap == 0 && gbc && address >= 0x000 && address < 0x0100)
            {
                return BootRom.GAMEBOY_COLOR[address];
            }
            else if (dmgBoostrap == 0 && gbc && address >= 0x200 && address < 0x0900)
            {
                return BootRom.GAMEBOY_COLOR[address - 0x0100];
            }
            else if (address == 0xff50)
            {
                return 0xff;
            }
            else
            {
                return addressSpace.getByte(address);
            }
        }

        private static int[] loadFile(FileInfo file)
        {
            //string ext = file.Extension;

            return File.ReadAllBytes(file.FullName).Select(x => (int) x).ToArray();

            /*try (InputStream is = new FileInputStream(file)) {
                if ("zip".equalsIgnoreCase(ext)) {
                    try (ZipInputStream zis = new ZipInputStream(is)) {
                        ZipEntry entry;
                        while ((entry = zis.getNextEntry()) != null) {
                            String name = entry.getName();
                            String entryExt = FilenameUtils.getExtension(name);
                            if (Stream.of("gb", "gbc", "rom").anyMatch(e -> e.equalsIgnoreCase(entryExt))) {
                                return load(zis, (int) entry.getSize());
                            }
                            zis.closeEntry();
                        }
                    }
                    throw new ArgumentException("Can't find ROM file inside the zip.");
                } else {
                    return load(is, (int) file.length());
                }
            }*/

        }

        /* private static int[] load(InputStream is, int length) throws IOException {
             byte[] byteArray = IOUtils.toByteArray(is, length);
             int[] intArray = new int[byteArray.length];
             for (int i = 0; i < byteArray.length; i++) {
                 intArray[i] = byteArray[i] & 0xff;
             }
             return intArray;
         }*/

        private static int getRomBanks(int id)
        {
            switch (id)
            {
                case 0:
                    return 2;

                case 1:
                    return 4;

                case 2:
                    return 8;

                case 3:
                    return 16;

                case 4:
                    return 32;

                case 5:
                    return 64;

                case 6:
                    return 128;

                case 7:
                    return 256;

                case 0x52:
                    return 72;

                case 0x53:
                    return 80;

                case 0x54:
                    return 96;

                default:
                    throw new ArgumentException("Unsupported ROM size: " + Integer.ToHexString(id));
            }
        }

        private static int getRamBanks(int id)
        {
            switch (id)
            {
                case 0:
                    return 0;

                case 1:
                    return 1;

                case 2:
                    return 1;

                case 3:
                    return 4;

                case 4:
                    return 16;

                default:
                    throw new ArgumentException("Unsupported RAM size: " + Integer.ToHexString(id));
            }
        }
    }
}