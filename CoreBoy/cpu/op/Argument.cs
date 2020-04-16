using System;
using System.Collections.Generic;

namespace CoreBoy.cpu.op
{
	public class Argument
    {
        public string Label { get; }
        public int OperandLength { get; }
        public bool IsMemory { get; }
        public DataType DataType { get; }
        public static List<Argument> Values { get; }

        static Argument()
        {
            Values = new List<Argument>
            {
                new Argument("A").Handle((r, a, args) => r.A, (r, a, i1, value) => r.A = value),
                new Argument("B").Handle((r, a, args) => r.B, (r, a, i1, value) => r.B = value),
                new Argument("C").Handle((r, a, args) => r.C, (r, a, i1, value) => r.C = value),
                new Argument("D").Handle((r, a, args) => r.D, (r, a, i1, value) => r.D = value),
                new Argument("E").Handle((r, a, args) => r.E, (r, a, i1, value) => r.E = value),
                new Argument("H").Handle((r, a, args) => r.H, (r, a, i1, value) => r.H = value),
                new Argument("L").Handle((r, a, args) => r.L, (r, a, i1, value) => r.L = value),
                
                new Argument("AF", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.AF, (r, a, i1, value) => r.SetAf(value)),

                new Argument("BC", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.BC, (r, a, i1, value) => r.SetBc(value)),

                new Argument("DE", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.DE, (r, a, i1, value) => r.SetDe(value)),

                new Argument("HL", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.HL, (r, a, i1, value) => r.SetHl(value)),

                new Argument("SP", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.SP, (r, a, i1, value) => r.SP = value),

                new Argument("PC", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.PC, (r, a, i1, value) => r.PC = value),

                new Argument("d8", 1, false, DataType.D8)
                    .Handle((r, a, args) => args[0], (r, a, i1, value) => throw new InvalidOperationException("Unsupported")),

                new Argument("d16", 2, false, DataType.D16)
                    .Handle((r, a, args) => BitUtils.ToWord(args), (r, a, i1, value) => throw new InvalidOperationException("Unsupported")),

                new Argument("r8", 1, false, DataType.R8)
                    .Handle((r, a, args) => BitUtils.ToSigned(args[0]), (r, a, i1, value) => throw new InvalidOperationException("Unsupported")),

                new Argument("a16", 2, false, DataType.D16)
                    .Handle((r, a, args) => BitUtils.ToWord(args), (r, a, i1, value) => throw new InvalidOperationException("Unsupported")),
				
                // _BC
                new Argument("(BC)", 0, true, DataType.D8)
                    .Handle((r, a, args) => a.GetByte(r.BC), (r, a, i1, value) => a.SetByte(r.BC, value)),

                // _DE
                new Argument("(DE)", 0, true, DataType.D8)
                    .Handle((r, a, args) => a.GetByte(r.DE), (r, a, i1, value) => a.SetByte(r.DE, value)),

                // _HL
                new Argument("(HL)", 0, true, DataType.D8)
                    .Handle((r, a, args) => a.GetByte(r.HL), (r, a, i1, value) => a.SetByte(r.HL, value)),

                // _a8
                new Argument("(a8)", 1, true, DataType.D8)
                    .Handle((r, a, args) => a.GetByte(0xff00 | args[0]), (r, a, i1, value) => a.SetByte(0xff00 | i1[0], value)),

                // _a16
                new Argument("(a16)", 2, true, DataType.D8)
                    .Handle((r, a, args) => a.GetByte(BitUtils.ToWord(args)), (r, a, i1, value) => a.SetByte(BitUtils.ToWord(i1), value)),

                // _C
                new Argument("(C)", 0, true, DataType.D8)
                    .Handle((r, a, args) => a.GetByte(0xff00 | r.C), (r, a, i1, value) => a.SetByte(0xff00 | r.C, value))
            };
        }

        private Func<Registers, IAddressSpace, int[], int> _readFunc;
        private Action<Registers, IAddressSpace, int[], int> _writeAction;

        public Argument(string label) 
            : this(label, 0, false, DataType.D8)
        {
        }

        public Argument(string label, int operandLength, bool isMemory, DataType dataType)
        {
            Label = label;
            OperandLength = operandLength;
            IsMemory = isMemory;
            DataType = dataType;
        }

		public Argument Handle(Func<Registers, IAddressSpace, int[], int> readFunc, Action<Registers, IAddressSpace, int[], int> writeAction)
        {
            _readFunc = readFunc;
            _writeAction = writeAction;
            return this;
        }

        public int Read(Registers registers, IAddressSpace addressSpace, int[] args) => _readFunc(registers, addressSpace, args);
        public void Write(Registers registers, IAddressSpace addressSpace, int[] args, int value) => _writeAction(registers, addressSpace, args, value);

        public static Argument Parse(string @string)
        {
            foreach (var a in Values)
            {
                if (a.Label.Equals(@string))
                {
                    return a;
                }
            }

            throw new ArgumentException("Unknown argument: " + @string);
        }
    }
}