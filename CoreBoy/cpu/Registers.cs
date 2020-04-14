namespace CoreBoy.cpu
{
    using static BitUtils;

    public class Registers
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public int D { get; set; }
        public int E { get; set; }
        public int H { get; set; }
        public int L { get; set; }
        public int SP { get; set; }
        public int PC { get; set; }

        public Flags Flags { get; } = new Flags();

        public int AF => A << 8 | Flags.FlagsByte;
        public int BC =>  B << 8 | C;
        public int DE => D << 8 | E;
        public int HL =>  H << 8 | L;

        public void SetAf(int af)
        {
            A = GetMsb(af);
            Flags.SetFlagsByte(GetLsb(af));
        }

        public void SetBc(int bc)
        {
            B = GetMsb(bc);
            C = GetLsb(bc);
        }

        public void SetDe(int de)
        {
            D = GetMsb(de);
            E = GetLsb(de);
        }

        public void SetHl(int hl)
        {
            H = GetMsb(hl);
            L = GetLsb(hl);
        }

        public void IncrementPc() => PC = (PC + 1) & 0xffff;
        public void IncrementSp() => SP = (SP + 1) & 0xffff;
        public void DecrementSp() => SP = (SP - 1) & 0xffff;
        
        public override string ToString()
        {
            return
                $"AF={AF:X4}, BC={BC:X4}, DE={DE:X4}, HL={HL:X4}, SP={SP:X4}, PC={PC:X4}, {Flags}";
        }
    }
}