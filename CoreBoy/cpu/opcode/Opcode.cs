using System;
using System.Collections.Generic;
using System.Linq;
using CoreBoy.cpu.op;

namespace CoreBoy.cpu.opcode
{
    public class Opcode
    {

        private readonly int opcode;

        private readonly String label;

        private readonly List<Op> ops;

        private readonly int length;

        public Opcode(OpcodeBuilder builder)
        {
            this.opcode = builder.getOpcode();
            this.label = builder.getLabel();
            this.ops = new List<Op>(builder.getOps());
            this.length = ops.Count <= 0 ? 0 : ops.Max(o => o.operandLength());
        }

        public int getOperandLength()
        {
            return length;
        }


        public String toString()
        {
            return String.Format("%02x %s", opcode, label);
        }

        public List<Op> getOps()
        {
            return ops;
        }

        public String getLabel()
        {
            return label;
        }

        public int getOpcode()
        {
            return opcode;
        }
    }
}