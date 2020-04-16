using System.Collections.Generic;
using System.Linq;
using CoreBoy.cpu.op;

namespace CoreBoy.cpu.opcode
{
    public class Opcode
    {
        public int Value { get; }
        public string Label { get; }
        public List<Op> Ops { get; }
        public int Length { get; }

        public Opcode(OpcodeBuilder builder)
        {
            Value = builder.GetOpcode();
            Label = builder.GetLabel();
            Ops = builder.GetOps();
            Length = Ops.Count <= 0 ? 0 : Ops.Max(o => o.OperandLength());
        }

        public override string ToString() => $"{Value:X2} {Label}";
    }
}