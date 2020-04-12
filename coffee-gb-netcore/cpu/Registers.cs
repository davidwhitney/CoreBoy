using System;

namespace eu.rekawek.coffeegb.cpu
{
    using static BitUtils;

    public class Registers
    {
        private int a, b, c, d, e, h, l;
        private int sp;
        private int pc;
        private Flags flags = new Flags();

        public int getA() => a;
        public int getB() => b;
        public int getC() => c;
        public int getD() => d;
        public int getE() => e;
        public int getH() => h;
        public int getL() => l;

        public int getAF()
        {
            return a << 8 | flags.getFlagsByte();
        }

        public int getBC()
        {
            return b << 8 | c;
        }

        public int getDE()
        {
            return d << 8 | e;
        }

        public int getHL()
        {
            return h << 8 | l;
        }

        public int getSP() => sp;
        public int getPC() => pc;
        public Flags getFlags() => flags;

        public void setA(int a)
        {
            checkByteArgument("a", a);
            this.a = a;
        }

        public void setB(int b)
        {
            checkByteArgument("b", b);
            this.b = b;
        }

        public void setC(int c)
        {
            checkByteArgument("c", c);
            this.c = c;
        }

        public void setD(int d)
        {
            checkByteArgument("d", d);
            this.d = d;
        }

        public void setE(int e)
        {
            checkByteArgument("e", e);
            this.e = e;
        }

        public void setH(int h)
        {
            checkByteArgument("h", h);
            this.h = h;
        }

        public void setL(int l)
        {
            checkByteArgument("l", l);
            this.l = l;
        }

        public void setAF(int af)
        {
            checkWordArgument("af", af);
            a = getMSB(af);
            flags.setFlagsByte(getLSB(af));
        }

        public void setBC(int bc)
        {
            checkWordArgument("bc", bc);
            b = getMSB(bc);
            c = getLSB(bc);
        }

        public void setDE(int de)
        {
            checkWordArgument("de", de);
            d = getMSB(de);
            e = getLSB(de);
        }

        public void setHL(int hl)
        {
            checkWordArgument("hl", hl);
            h = getMSB(hl);
            l = getLSB(hl);
        }

        public void setSP(int sp)
        {
            checkWordArgument("sp", sp);
            this.sp = sp;
        }

        public void setPC(int pc)
        {
            checkWordArgument("pc", pc);
            this.pc = pc;
        }

        public void incrementPC()
        {
            pc = (pc + 1) & 0xffff;
        }

        public void decrementSP()
        {
            sp = (sp - 1) & 0xffff;
        }

        public void incrementSP()
        {
            sp = (sp + 1) & 0xffff;
        }

        public string toString() => ToString();

        public override string ToString()
        {
            return string.Format("AF=%04x, BC=%04x, DE=%04x, HL=%04x, SP=%04x, PC=%04x, %s", getAF(), getBC(), getDE(),
                getHL(), getSP(), getPC(), getFlags().toString());
        }
    }
}