using System.Collections.Generic;
using CoreBoy.memory;

namespace CoreBoy.gpu
{
    public class GpuRegister : IRegister
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

        public int Address { get; }
        public RegisterType Type { get;  }

        public GpuRegister(int address, RegisterType type)
        {
            Address = address;
            Type = type;
        }

        public int GetAddress() => Address;
        public RegisterType GetRegisterType() => Type;

        public static IEnumerable<IRegister> Values()
        {
            yield return STAT;
            yield return SCY;
            yield return SCX;
            yield return LY;
            yield return LYC;
            yield return BGP;
            yield return OBP0;
            yield return OBP1;
            yield return WY;
            yield return WX;
            yield return VBK;
        }
    }
}