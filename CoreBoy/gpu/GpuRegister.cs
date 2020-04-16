using System.Collections.Generic;
using CoreBoy.memory;

namespace CoreBoy.gpu
{
    public class GpuRegister : IRegister
    {
        public static GpuRegister Stat = new GpuRegister(0xff41, RegisterType.RW);
        public static GpuRegister Scy = new GpuRegister(0xff42, RegisterType.RW);
        public static GpuRegister Scx = new GpuRegister(0xff43, RegisterType.RW);
        public static GpuRegister Ly = new GpuRegister(0xff44, RegisterType.R);
        public static GpuRegister Lyc = new GpuRegister(0xff45, RegisterType.RW);
        public static GpuRegister Bgp = new GpuRegister(0xff47, RegisterType.RW);
        public static GpuRegister Obp0 = new GpuRegister(0xff48, RegisterType.RW);
        public static GpuRegister Obp1 = new GpuRegister(0xff49, RegisterType.RW);
        public static GpuRegister Wy = new GpuRegister(0xff4a, RegisterType.RW);
        public static GpuRegister Wx = new GpuRegister(0xff4b, RegisterType.RW);
        public static GpuRegister Vbk = new GpuRegister(0xff4f, RegisterType.W);

        public int Address { get; }
        public RegisterType Type { get; }

        public GpuRegister(int address, RegisterType type)
        {
            Address = address;
            Type = type;
        }
        
        public static IEnumerable<IRegister> Values()
        {
            yield return Stat;
            yield return Scy;
            yield return Scx;
            yield return Ly;
            yield return Lyc;
            yield return Bgp;
            yield return Obp0;
            yield return Obp1;
            yield return Wy;
            yield return Wx;
            yield return Vbk;
        }
    }
}