using System;
using System.Collections.Generic;

namespace eu.rekawek.coffeegb.cpu.op
{
	public class Argument
    {
        private readonly string label;

        private readonly int operandLength;

        private readonly bool memory;

        private readonly DataType dataType;

        private static readonly List<Argument> _values;
        
        public static List<Argument> values() => _values;
        static Argument()
        {
            _values = new List<Argument>
            {
                new Argument("A").Handle((r, a, args) => r.getA(), (r, a, i1, value) => r.setA(value)),
                new Argument("B").Handle((r, a, args) => r.getB(), (r, a, i1, value) => r.setB(value)),
                new Argument("C").Handle((r, a, args) => r.getC(), (r, a, i1, value) => r.setC(value)),
                new Argument("D").Handle((r, a, args) => r.getD(), (r, a, i1, value) => r.setD(value)),
                new Argument("E").Handle((r, a, args) => r.getE(), (r, a, i1, value) => r.setE(value)),
                new Argument("H").Handle((r, a, args) => r.getH(), (r, a, i1, value) => r.setH(value)),
                new Argument("L").Handle((r, a, args) => r.getL(), (r, a, i1, value) => r.setL(value)),
                
                new Argument("AF", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.getAF(), (r, a, i1, value) => r.setAF(value)),

                new Argument("BC", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.getBC(), (r, a, i1, value) => r.setBC(value)),

                new Argument("DE", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.getDE(), (r, a, i1, value) => r.setDE(value)),

                new Argument("HL", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.getHL(), (r, a, i1, value) => r.setHL(value)),

                new Argument("SP", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.getSP(), (r, a, i1, value) => r.setSP(value)),

                new Argument("PC", 0, false, DataType.D16)
                    .Handle((r, a, args) => r.getPC(), (r, a, i1, value) => r.setPC(value)),

                new Argument("d8", 1, false, DataType.D8)
                    .Handle((r, a, args) => args[0], (r, a, i1, value) => throw new InvalidOperationException("Unsupported")),

                new Argument("d16", 2, false, DataType.D16)
                    .Handle((r, a, args) => BitUtils.toWord(args), (r, a, i1, value) => throw new InvalidOperationException("Unsupported")),

                new Argument("r8", 1, false, DataType.R8)
                    .Handle((r, a, args) => BitUtils.toSigned(args[0]), (r, a, i1, value) => throw new InvalidOperationException("Unsupported")),

                new Argument("a16", 2, false, DataType.D16)
                    .Handle((r, a, args) => BitUtils.toWord(args), (r, a, i1, value) => throw new InvalidOperationException("Unsupported")),
				
                // _BC
                new Argument("(BC)", 0, true, DataType.D8)
                    .Handle((r, a, args) => a.getByte(r.getBC()), (r, a, i1, value) => a.setByte(r.getBC(), value)),

                // _DE
                new Argument("(DE)", 0, true, DataType.D8)
                    .Handle((r, a, args) => a.getByte(r.getDE()), (r, a, i1, value) => a.setByte(r.getDE(), value)),

                // _HL
                new Argument("(HL)", 0, true, DataType.D8)
                    .Handle((r, a, args) => a.getByte(r.getHL()), (r, a, i1, value) => a.setByte(r.getHL(), value)),

                // _a8
                new Argument("(a8)", 1, true, DataType.D8)
                    .Handle((r, a, args) => a.getByte(0xff00 | args[0]), (r, a, i1, value) => a.setByte(0xff00 | i1[0], value)),

                // _a16
                new Argument("(a16)", 2, true, DataType.D8)
                    .Handle((r, a, args) => a.getByte(BitUtils.toWord(args)), (r, a, i1, value) => a.setByte(BitUtils.toWord(i1), value)),

                // _C
                new Argument("(C)", 0, true, DataType.D8)
                    .Handle((r, a, args) => a.getByte(0xff00 | r.getC()), (r, a, i1, value) => a.setByte(0xff00 | r.getC(), value))
            };
        }

        private Func<Registers, AddressSpace, int[], int> _readFunc;
        private Action<Registers, AddressSpace, int[], int> _writeAction;

        public Argument(string label) : this(label, 0, false, DataType.D8)
        {
        }

        public Argument(string label, int operandLength, bool memory, DataType dataType)
        {
            this.label = label;
            this.operandLength = operandLength;
            this.memory = memory;
            this.dataType = dataType;
        }

		public Argument Handle(Func<Registers, AddressSpace, int[], int> readFunc, Action<Registers, AddressSpace, int[], int> writeAction)
        {
            _readFunc = readFunc;
            _writeAction = writeAction;
            return this;
        }

        public int getOperandLength()
        {
            return operandLength;
        }

        public bool isMemory()
        {
            return memory;
        }

		public int read(Registers registers, AddressSpace addressSpace, int[] args) => _readFunc(registers, addressSpace, args);
        public void write(Registers registers, AddressSpace addressSpace, int[] args, int value) => _writeAction(registers, addressSpace, args, value);

        public DataType getDataType()
        {
            return dataType;
        }

        public static Argument parse(string @string)
        {
            foreach (var a in values())
            {
                if (a.label.Equals(@string))
                {
                    return a;
                }
            }

            throw new ArgumentException("Unknown argument: " + @string);
        }

        public string getLabel()
        {
            return label;
        }
    }
}