using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CoreBoy.cpu.op;

namespace CoreBoy.cpu.opcode
{
    public class Opcode
    {
        public int Value { get; }
        public string Label { get; }
        public ImmutableList<Op> Ops { get; }
        public int Length { get; }

        public Opcode(OpcodeBuilder builder)
        {
            Value = builder.GetOpcode();
            Label = builder.GetLabel();
            Ops = ImmutableList.Create(builder.GetOps().ToArray());
            Length = Ops.Count <= 0 ? 0 : Ops.Max(o => o.operandLength());
        }

        public override string ToString() => $"{Value:X2} {Label}";
    }
}