//namespace eu.rekawek.coffeegb.cpu.opcode 

using System;
using System.Collections.Generic;
using CoreBoy.cpu.op;
using CoreBoy.gpu;
using static CoreBoy.cpu.BitUtils;

using IntRegistryFunction = System.Func<CoreBoy.cpu.Flags, int, int>;
using BiIntRegistryFunction = System.Func<CoreBoy.cpu.Flags, int, int, int>;

namespace CoreBoy.cpu.opcode
{
    public class OpcodeBuilder
    {
        private static readonly AluFunctions Alu;
        public static readonly List<IntRegistryFunction> OemBug;

        static OpcodeBuilder()
        {
            Alu = new AluFunctions();
            OemBug = new List<IntRegistryFunction>
            {
                Alu.GetFunction("INC", DataType.D16),
                Alu.GetFunction("DEC", DataType.D16)
            };
        }

        private readonly int _opcode;
        private readonly string _label;
        private readonly List<Op> _ops = new List<Op>();
        private DataType _lastDataType;

        public OpcodeBuilder(int opcode, string label)
        {
            _opcode = opcode;
            _label = label;
        }

        public OpcodeBuilder CopyByte(string target, string source)
        {
            load(source);
            store(target);
            return this;
        }

        private class LoadOp : Op
        {
            private readonly Argument _arg;
            public LoadOp(Argument arg) => _arg = arg;
            public override bool readsMemory() => _arg.isMemory();
            public override int operandLength() => _arg.getOperandLength();

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) =>
                _arg.read(registers, addressSpace, args);

            public override string ToString() =>
                string.Format(_arg.getDataType() == DataType.D16 ? "{0} → [__]" : "{0} → [_]", _arg.getLabel());
        }

        public OpcodeBuilder load(string source)
        {
            var arg = Argument.parse(source);
            _lastDataType = arg.getDataType();
            _ops.Add(new LoadOp(arg));
            return this;
        }


        private class LoadWordOp : Op
        {
            private readonly int _value;
            public LoadWordOp(int value) => _value = value;

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) =>
                _value;

            public override string ToString() => $"0x{_value:X2} → [__]";
        }

        public OpcodeBuilder loadWord(int value)
        {
            _lastDataType = DataType.D16;
            _ops.Add(new LoadWordOp(value));
            return this;
        }

        private class Store_a16_Op1 : Op
        {
            private readonly Argument _arg;
            public Store_a16_Op1(Argument arg) => _arg = arg;
            public override bool writesMemory() => _arg.isMemory();
            public override int operandLength() => _arg.getOperandLength();

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int context)
            {
                addressSpace.setByte(ToWord(args), context & 0x00ff);
                return context;
            }

            public override string ToString() => $"[ _] → {_arg.getLabel()}";
        }

        private class Store_a16_Op2 : Op
        {
            private readonly Argument _arg;
            public Store_a16_Op2(Argument arg) => _arg = arg;
            public override bool writesMemory() => _arg.isMemory();
            public override int operandLength() => _arg.getOperandLength();

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int context)
            {
                addressSpace.setByte((ToWord(args) + 1) & 0xffff, (context & 0xff00) >> 8);
                return context;
            }

            public override string ToString() => $"[_ ] → {_arg.getLabel()}";
        }

        private class Store_LastDataType : Op
        {
            private readonly Argument _arg;
            public Store_LastDataType(Argument arg) => _arg = arg;
            public override bool writesMemory() => _arg.isMemory();
            public override int operandLength() => _arg.getOperandLength();

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int context)
            {
                _arg.write(registers, addressSpace, args, context);
                return context;
            }

            public override string ToString() =>
                string.Format(_arg.getDataType() == DataType.D16 ? "[__] → {0}" : "[_] → {0}", _arg.getLabel());
        }


        public OpcodeBuilder store(string target)
        {
            var arg = Argument.parse(target);

            if (_lastDataType == DataType.D16 && arg.getLabel() == "(a16)")
            {
                _ops.Add(new Store_a16_Op1(arg));
                _ops.Add(new Store_a16_Op2(arg));

            }
            else if (_lastDataType == arg.getDataType())
            {
                _ops.Add(new Store_LastDataType(arg));
            }
            else
            {
                throw new InvalidOperationException($"Can't write {_lastDataType} to {target}");
            }

            return this;
        }

        private class ProceedIf : Op
        {
            private readonly string _condition;
            public ProceedIf(string condition) => _condition = condition;

            public override bool proceed(Registers registers)
            {
                return _condition switch
                {
                    "NZ" => !registers.Flags.IsZ(),
                    "Z" => registers.Flags.IsZ(),
                    "NC" => !registers.Flags.IsC(),
                    "C" => registers.Flags.IsC(),
                    _ => false
                };
            }

            public override string ToString() => $"? {_condition}:";
        }

        public OpcodeBuilder proceedIf(string condition)
        {
            _ops.Add(new ProceedIf(condition));
            return this;
        }


        private class Push1 : Op
        {
            private readonly IntRegistryFunction _func;
            public Push1(IntRegistryFunction func) => _func = func;
            public override bool writesMemory() => true;

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int context)
            {
                registers.SP = _func(registers.Flags, registers.SP);
                addressSpace.setByte(registers.SP, (context & 0xff00) >> 8);
                return context;
            }

            public override SpriteBug.CorruptionType? causesOemBug(Registers registers, int context)
            {
                return inOamArea(registers.SP) ? SpriteBug.CorruptionType.PUSH_1 : (SpriteBug.CorruptionType?) null;
            }

            public override string ToString() => "[_ ] → (SP--)";
        }

        private class Push2 : Op
        {
            private readonly IntRegistryFunction _func;
            public Push2(IntRegistryFunction func) => _func = func;
            public override bool writesMemory() => true;

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int context)
            {
                registers.SP = _func(registers.Flags, registers.SP);
                addressSpace.setByte(registers.SP, context & 0x00ff);
                return context;
            }

            public override SpriteBug.CorruptionType? causesOemBug(Registers registers, int context)
            {
                return inOamArea(registers.SP) ? SpriteBug.CorruptionType.PUSH_2 : (SpriteBug.CorruptionType?) null;
            }


            public override string ToString() => "[ _] → (SP--)";
        }


        public OpcodeBuilder push()
        {
            var dec = Alu.GetFunction("DEC", DataType.D16);
            _ops.Add(new Push1(dec));
            _ops.Add(new Push2(dec));
            return this;
        }


        private class Pop1 : Op
        {
            private readonly IntRegistryFunction _func;
            public Pop1(IntRegistryFunction func) => _func = func;

            public override bool readsMemory()
            {
                return true;
            }

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int context)
            {
                int lsb = addressSpace.getByte(registers.SP);
                registers.SP = _func(registers.Flags, registers.SP);
                return lsb;
            }


            public override SpriteBug.CorruptionType? causesOemBug(Registers registers, int context)
            {
                return inOamArea(registers.SP) ? SpriteBug.CorruptionType.POP_1 : (SpriteBug.CorruptionType?) null;
            }


            public override string ToString()
            {
                return string.Format("(SP++) → [ _]");
            }
        }

        private class Pop2 : Op
        {
            private readonly IntRegistryFunction _func;
            public Pop2(IntRegistryFunction func) => _func = func;

            public override bool readsMemory()
            {
                return true;
            }

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int context)
            {
                int msb = addressSpace.getByte(registers.SP);
                registers.SP = _func(registers.Flags, registers.SP);
                return context | (msb << 8);
            }


            public override SpriteBug.CorruptionType? causesOemBug(Registers registers, int context)
            {
                return inOamArea(registers.SP) ? SpriteBug.CorruptionType.POP_2 : (SpriteBug.CorruptionType?) null;
            }

            public override string ToString()
            {
                return string.Format("(SP++) → [_ ]");
            }
        }

        public OpcodeBuilder pop()
        {
            var inc = Alu.GetFunction("INC", DataType.D16);
            _lastDataType = DataType.D16;
            _ops.Add(new Pop1(inc));
            _ops.Add(new Pop2(inc));
            return this;
        }


        private class Alu1 : Op
        {
            private readonly BiIntRegistryFunction _func;
            private Argument _arg2;
            private readonly string _operation;
            private readonly DataType _lastDataType;

            public Alu1(BiIntRegistryFunction func, Argument arg2, string operation, DataType lastDataType)
            {
                _func = func;
                _arg2 = arg2;
                _operation = operation;
                _lastDataType = lastDataType;
            }

            public override bool readsMemory()
            {
                return _arg2.isMemory();
            }


            public override int operandLength()
            {
                return _arg2.getOperandLength();
            }


            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int v1)
            {
                var v2 = _arg2.read(registers, addressSpace, args);
                return _func(registers.Flags, v1, v2);
            }


            public override string ToString()
            {
                if (_lastDataType == DataType.D16)
                {
                    return $"{_operation}([__],{_arg2}) → [__]";
                }

                return $"{_operation}([_],{_arg2}) → [_]";
            }
        }

        public OpcodeBuilder alu(string operation, string argument2)
        {
            var arg2 = Argument.parse(argument2);
            var func = Alu.GetFunction(operation, _lastDataType, arg2.getDataType());
            _ops.Add(new Alu1(func, arg2, operation, _lastDataType));

            if (_lastDataType == DataType.D16)
            {
                extraCycle();
            }

            return this;
        }

        private class Alu2 : Op
        {
            private readonly BiIntRegistryFunction _func;
            private readonly string _operation;
            private readonly int _d8Value;

            public Alu2(BiIntRegistryFunction func, string operation, int d8Value)
            {
                _func = func;
                _operation = operation;
                _d8Value = d8Value;
            }

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int v1)
            {
                return _func(registers.Flags, v1, _d8Value);
            }


            public override string ToString()
            {
                return $"{_operation}({_d8Value:D},[_]) → [_]";
            }
        }

        public OpcodeBuilder alu(string operation, int d8Value)
        {
            var func = Alu.GetFunction(operation, _lastDataType, DataType.D8);
            _ops.Add(new Alu2(func, operation, d8Value));

            if (_lastDataType == DataType.D16)
            {
                extraCycle();
            }

            return this;
        }


        private class Alu3 : Op
        {
            private readonly IntRegistryFunction _func;
            private readonly string _operation;
            private readonly DataType _lastDataType;

            public Alu3(IntRegistryFunction func, string operation, DataType lastDataType)
            {
                _func = func;
                _operation = operation;
                _lastDataType = lastDataType;
            }

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int value)
            {
                return _func(registers.Flags, value);
            }


            public override SpriteBug.CorruptionType? causesOemBug(Registers registers, int context)
            {
                return CausesOemBug(_func, context)
                    ? SpriteBug.CorruptionType.INC_DEC
                    : (SpriteBug.CorruptionType?) null;
            }

            public override string ToString()
            {
                if (_lastDataType == DataType.D16)
                {
                    return $"{_operation}([__]) → [__]";
                }
                else
                {
                    return $"{_operation}([_]) → [_]";
                }
            }
        }

        public OpcodeBuilder alu(string operation)
        {
            var func = Alu.GetFunction(operation, _lastDataType);
            _ops.Add(new Alu3(func, operation, _lastDataType));

            if (_lastDataType == DataType.D16)
            {
                extraCycle();
            }

            return this;
        }

        private class AluHL : Op
        {
            private readonly IntRegistryFunction _func;

            public AluHL(IntRegistryFunction func)
            {
                _func = func;
            }

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int value)
            {
                return _func(registers.Flags, value);
            }

            public override SpriteBug.CorruptionType? causesOemBug(Registers registers, int context)
            {
                return CausesOemBug(_func, context) ? SpriteBug.CorruptionType.LD_HL : (SpriteBug.CorruptionType?) null;
            }

            public override string ToString()
            {
                return string.Format("%s(HL) → [__]");
            }
        }

        public OpcodeBuilder aluHL(string operation)
        {
            load("HL");
            _ops.Add(new AluHL(Alu.GetFunction(operation, DataType.D16)));
            store("HL");
            return this;
        }


        private class BitHL : Op
        {
            private readonly int _bit;

            public BitHL(int bit)
            {
                _bit = bit;
            }

            public override bool readsMemory()
            {
                return true;
            }

            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int context)
            {
                var value = addressSpace.getByte(registers.HL);
                var flags = registers.Flags;
                flags.SetN(false);
                flags.SetH(true);
                if (_bit < 8)
                {
                    flags.SetZ(!GetBit(value, _bit));
                }

                return context;
            }


            public override string ToString() => $"BIT({_bit:D},HL)";
        }

        public OpcodeBuilder bitHL(int bit)
        {
            _ops.Add(new BitHL(bit));
            return this;
        }

        private class ClearZ : Op
        {
            public override int execute(Registers registers, AddressSpace addressSpace, int[] args, int context)
            {
                registers.Flags.SetZ(false);
                return context;
            }

            public override string ToString() => "0 → Z";
        }

        public OpcodeBuilder clearZ()
        {
            _ops.Add(new ClearZ());
            return this;
        }

        private class SwitchInterrupts : Op
        {
            private readonly bool _enable;
            private readonly bool _withDelay;

            public SwitchInterrupts(bool enable, bool withDelay)
            {
                _enable = enable;
                _withDelay = withDelay;
            }

            public override void switchInterrupts(InterruptManager interruptManager)
            {
                if (_enable)
                {
                    interruptManager.EnableInterrupts(_withDelay);
                }
                else
                {
                    interruptManager.DisableInterrupts(_withDelay);
                }
            }


            public override string ToString()
            {
                return (_enable ? "enable" : "disable") + " interrupts";
            }
        }

        public OpcodeBuilder switchInterrupts(bool enable, bool withDelay)
        {
            _ops.Add(new SwitchInterrupts(enable, withDelay));
            return this;
        }

        public OpcodeBuilder op(Op op)
        {
            _ops.Add(op);
            return this;
        }

        private class ExtraCycleOp : Op
        {
            public override bool readsMemory() => true;
            public override string ToString() => "wait cycle";
        }

        public OpcodeBuilder extraCycle()
        {
            _ops.Add(new ExtraCycleOp());
            return this;
        }

        private class ForceFinishOp : Op
        {
            public override bool forceFinishCycle() => true;
            public override string ToString() => "finish cycle";
        }

        public OpcodeBuilder ForceFinish()
        {
            _ops.Add(new ForceFinishOp());
            return this;
        }

        public Opcode Build()
        {
            return new Opcode(this);
        }

        public int GetOpcode()
        {
            return _opcode;
        }

        public string GetLabel()
        {
            return _label;
        }

        public List<Op> GetOps()
        {
            return _ops;
        }


        public override string ToString() => _label;

        public static bool CausesOemBug(IntRegistryFunction function, int context)
        {
            return OemBug.Contains(function) && InOamArea(context);
        }

        private static bool InOamArea(int address)
        {
            return address >= 0xfe00 && address <= 0xfeff;
        }
    }
}