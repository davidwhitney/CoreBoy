using eu.rekawek.coffeegb.memory;

namespace eu.rekawek.coffeegb.gpu
{

    public class GpuRegister : Register
    {
        public static GpuRegister STAT = new GpuRegister(0xff41, RegisterType.RW);
        public static GpuRegister SCY = new GpuRegister(0xff42, RegisterType.RW);
        public static GpuRegister SCX = new GpuRegister(0xff43, RegisterType.RW);
        public static GpuRegister LY = new GpuRegister(0xff44, RegisterType.R);
        public static GpuRegister LYC = new GpuRegister(0xff45, RegisterType.RW);
        public static GpuRegister BGP = new GpuRegister(0xff47, RegisterType.RW);
        public static GpuRegister OBP0 = new GpuRegister(0xff48, RegisterType.RW);
        public static GpuRegister OBP1 = new GpuRegister(0xff49, RegisterType.RW);
        public static GpuRegister WY = new GpuRegister(0xff4a, RegisterType.RW);
        public static GpuRegister WX = new GpuRegister(0xff4b, RegisterType.RW);
        public static GpuRegister VBK = new GpuRegister(0xff4f, RegisterType.W);

        private int address;

        private RegisterType type;

        public GpuRegister(int address, RegisterType type)
        {
            this.address = address;
            this.type = type;
        }

        public int getAddress()
        {
            return address;
        }

        public RegisterType getType()
        {
            return type;
        }
    }
}