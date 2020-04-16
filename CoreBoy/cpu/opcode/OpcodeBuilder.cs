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
        private static readonly AluFunctions AluFunctions;
        public static readonly List<IntRegistryFunction> OemBug;

        static OpcodeBuilder()
        {
            AluFunctions = new AluFunctions();
            OemBug = new List<IntRegistryFunction>
            {
                AluFunctions.GetFunction("INC", DataType.D16),
                AluFunctions.GetFunction("DEC", DataType.D16)
            };
        }

        private readonly int _opcode;
        private readonly string _label;
        private readonly List<Op> _ops = new List<Op>();
        private DataType _lastDataType;

        public int GetOpcode() => _opcode;
        public string GetLabel() => _label;
        public List<Op> GetOps() => _ops;

        public OpcodeBuilder(int opcode, string label)
        {
            _opcode = opcode;
            _label = label;
        }

        public OpcodeBuilder CopyByte(string target, string source)
        {
            Load(source);
            Store(target);
            return this;
        }

        private class LoadOp : Op
        {
            private readonly Argument _arg;
            public LoadOp(Argument arg) => _arg = arg;
            public override bool ReadsMemory() => _arg.IsMemory;
            public override int OperandLength() => _arg.OperandLength;

            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int context) =>
                _arg.Read(registers, addressSpace, args);

            public override string ToString() =>
                string.Format(_arg.DataType == DataType.D16 ? "{0} → [__]" : "{0} → [_]", _arg.Label);
        }

        public OpcodeBuilder Load(string source)
        {
            var arg = Argument.Parse(source);
            _lastDataType = arg.DataType;
            _ops.Add(new LoadOp(arg));
            return this;
        }


        private class LoadWordOp : Op
        {
            private readonly int _value;
            public LoadWordOp(int value) => _value = value;
            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int context) => _value;
            public override string ToString() => $"0x{_value:X2} → [__]";
        }

        public OpcodeBuilder LoadWord(int value)
        {
            _lastDataType = DataType.D16;
            _ops.Add(new LoadWordOp(value));
            return this;
        }

        private class StoreA16Op1 : Op
        {
            private readonly Argument _arg;
            public StoreA16Op1(Argument arg) => _arg = arg;
            public override bool WritesMemory() => _arg.IsMemory;
            public override int OperandLength() => _arg.OperandLength;

            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int context)
            {
                addressSpace.SetByte(ToWord(args), context & 0x00ff);
                return context;
            }

            public override string ToString() => $"[ _] → {_arg.Label}";
        }

        private class StoreA16Op2 : Op
        {
            private readonly Argument _arg;
            public StoreA16Op2(Argument arg) => _arg = arg;
            public override bool WritesMemory() => _arg.IsMemory;
            public override int OperandLength() => _arg.OperandLength;

            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int context)
            {
                addressSpace.SetByte((ToWord(args) + 1) & 0xffff, (context & 0xff00) >> 8);
                return context;
            }

            public override string ToString() => $"[_ ] → {_arg.Label}";
        }

        private class StoreLastDataType : Op
        {
            private readonly Argument _arg;
            public StoreLastDataType(Argument arg) => _arg = arg;
            public override bool WritesMemory() => _arg.IsMemory;
            public override int OperandLength() => _arg.OperandLength;

            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int context)
            {
                _arg.Write(registers, addressSpace, args, context);
                return context;
            }

            public override string ToString() =>
                string.Format(_arg.DataType == DataType.D16 ? "[__] → {0}" : "[_] → {0}", _arg.Label);
        }


        public OpcodeBuilder Store(string target)
        {
            var arg = Argument.Parse(target);

            if (_lastDataType == DataType.D16 && arg.Label == "(a16)")
            {
                _ops.Add(new StoreA16Op1(arg));
                _ops.Add(new StoreA16Op2(arg));

            }
            else if (_lastDataType == arg.DataType)
            {
                _ops.Add(new StoreLastDataType(arg));
            }
            else
            {
                throw new InvalidOperationException($"Can't write {_lastDataType} to {target}");
            }

            return this;
        }

        private class ProceedIfOp : Op
        {
            private readonly string _condition;
            public ProceedIfOp(string condition) => _condition = condition;

            public override bool Proceed(Registers registers)
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

        public OpcodeBuilder ProceedIf(string condition)
        {
            _ops.Add(new ProceedIfOp(condition));
            return this;
        }


        private class PushOp1 : Op
        {
            private readonly IntRegistryFunction _func;
            public PushOp1(IntRegistryFunction func) => _func = func;
            public override bool WritesMemory() => true;

            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int context)
            {
                registers.SP = _func(registers.Flags, registers.SP);
                addressSpace.SetByte(registers.SP, (context & 0xff00) >> 8);
                return context;
            }

            public override SpriteBug.CorruptionType? CausesOemBug(Registers registers, int context)
            {
                return InOamArea(registers.SP) ? SpriteBug.CorruptionType.PUSH_1 : (SpriteBug.CorruptionType?) null;
            }

            public override string ToString() => "[_ ] → (SP--)";
        }

        private class PushOp2 : Op
        {
            private readonly IntRegistryFunction _func;
            public PushOp2(IntRegistryFunction func) => _func = func;
            public override bool WritesMemory() => true;

            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int context)
            {
                registers.SP = _func(registers.Flags, registers.SP);
                addressSpace.SetByte(registers.SP, context & 0x00ff);
                return context;
            }

            public override SpriteBug.CorruptionType? CausesOemBug(Registers registers, int context)
            {
                return InOamArea(registers.SP) ? SpriteBug.CorruptionType.PUSH_2 : (SpriteBug.CorruptionType?) null;
            }


            public override string ToString() => "[ _] → (SP--)";
        }


        public OpcodeBuilder Push()
        {
            var dec = AluFunctions.GetFunction("DEC", DataType.D16);
            _ops.Add(new PushOp1(dec));
            _ops.Add(new PushOp2(dec));
            return this;
        }


        private class PopOp1 : Op
        {
            private readonly IntRegistryFunction _func;
            public PopOp1(IntRegistryFunction func) => _func = func;

            public override bool ReadsMemory()
            {
                return true;
            }

            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int context)
            {
                var lsb = addressSpace.GetByte(registers.SP);
                registers.SP = _func(registers.Flags, registers.SP);
                return lsb;
            }


            public override SpriteBug.CorruptionType? CausesOemBug(Registers registers, int context)
            {
                return InOamArea(registers.SP) ? SpriteBug.CorruptionType.POP_1 : (SpriteBug.CorruptionType?) null;
            }
            public override string ToString() => "(SP++) → [ _]";
        }

        private class PopOp2 : Op
        {
            private readonly IntRegistryFunction _func;
            public PopOp2(IntRegistryFunction func) => _func = func;
            public override bool ReadsMemory() => true;

            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int context)
            {
                var msb = addressSpace.GetByte(registers.SP);
                registers.SP = _func(registers.Flags, registers.SP);
                return context | (msb << 8);
            }
            
            public override SpriteBug.CorruptionType? CausesOemBug(Registers registers, int context)
            {
                return InOamArea(registers.SP) ? SpriteBug.CorruptionType.POP_2 : (SpriteBug.CorruptionType?) null;
            }

            public override string ToString() => "(SP++) → [_ ]";
        }

        public OpcodeBuilder Pop()
        {
            var inc = AluFunctions.GetFunction("INC", DataType.D16);
            _lastDataType = DataType.D16;
            _ops.Add(new PopOp1(inc));
            _ops.Add(new PopOp2(inc));
            return this;
        }

        private class AluOp1 : Op
        {
            private readonly BiIntRegistryFunction _func;
            private readonly Argument _arg2;
            private readonly string _operation;
            private readonly DataType _lastDataType;

            public AluOp1(BiIntRegistryFunction func, Argument arg2, string operation, DataType lastDataType)
            {
                _func = func;
                _arg2 = arg2;
                _operation = operation;
                _lastDataType = lastDataType;
            }

            public override bool ReadsMemory() => _arg2.IsMemory;
            public override int OperandLength() => _arg2.OperandLength;
            
            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int v1)
            {
                var v2 = _arg2.Read(registers, addressSpace, args);
                return _func(registers.Flags, v1, v2);
            }

            public override string ToString() => _lastDataType == DataType.D16
                ? $"{_operation}([__],{_arg2}) → [__]"
                : $"{_operation}([_],{_arg2}) → [_]";
        }

        public OpcodeBuilder Alu(string operation, string argument2)
        {
            var arg2 = Argument.Parse(argument2);
            var func = AluFunctions.GetFunction(operation, _lastDataType, arg2.DataType);
            _ops.Add(new AluOp1(func, arg2, operation, _lastDataType));

            if (_lastDataType == DataType.D16)
            {
                ExtraCycle();
            }

            return this;
        }

        private class AluOp2 : Op
        {
            private readonly BiIntRegistryFunction _func;
            private readonly string _operation;
            private readonly int _d8Value;

            public AluOp2(BiIntRegistryFunction func, string operation, int d8Value)
            {
                _func = func;
                _operation = operation;
                _d8Value = d8Value;
            }

            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int v1)
            {
                return _func(registers.Flags, v1, _d8Value);
            }


            public override string ToString()
            {
                return $"{_operation}({_d8Value:D},[_]) → [_]";
            }
        }

        public OpcodeBuilder Alu(string operation, int d8Value)
        {
            var func = AluFunctions.GetFunction(operation, _lastDataType, DataType.D8);
            _ops.Add(new AluOp2(func, operation, d8Value));

            if (_lastDataType == DataType.D16)
            {
                ExtraCycle();
            }

            return this;
        }

        private class AluOp3 : Op
        {
            private readonly IntRegistryFunction _func;
            private readonly string _operation;
            private readonly DataType _lastDataType;

            public AluOp3(IntRegistryFunction func, string operation, DataType lastDataType)
            {
                _func = func;
                _operation = operation;
                _lastDataType = lastDataType;
            }

            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int value) => _func(registers.Flags, value);

            public override SpriteBug.CorruptionType? CausesOemBug(Registers registers, int context) =>
                OpcodeBuilder.CausesOemBug(_func, context)
                    ? SpriteBug.CorruptionType.INC_DEC
                    : (SpriteBug.CorruptionType?) null;

            public override string ToString() => _lastDataType == DataType.D16 ? $"{_operation}([__]) → [__]" : $"{_operation}([_]) → [_]";
        }

        public OpcodeBuilder Alu(string operation)
        {
            var func = AluFunctions.GetFunction(operation, _lastDataType);
            _ops.Add(new AluOp3(func, operation, _lastDataType));

            if (_lastDataType == DataType.D16)
            {
                ExtraCycle();
            }

            return this;
        }

        private class AluHLOp : Op
        {
            private readonly IntRegistryFunction _func;

            public AluHLOp(IntRegistryFunction func)
            {
                _func = func;
            }

            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int value)
            {
                return _func(registers.Flags, value);
            }

            public override SpriteBug.CorruptionType? CausesOemBug(Registers registers, int context)
            {
                return OpcodeBuilder.CausesOemBug(_func, context) ? SpriteBug.CorruptionType.LD_HL : (SpriteBug.CorruptionType?) null;
            }

            public override string ToString()
            {
                return "%s(HL) → [__]";
            }
        }

        public OpcodeBuilder AluHL(string operation)
        {
            Load("HL");
            _ops.Add(new AluHLOp(AluFunctions.GetFunction(operation, DataType.D16)));
            Store("HL");
            return this;
        }

        private class BitHLOp : Op
        {
            private readonly int _bit;
            public BitHLOp(int bit) => _bit = bit;
            public override bool ReadsMemory() => true;
            
            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int context)
            {
                var value = addressSpace.GetByte(registers.HL);
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

        public OpcodeBuilder BitHL(int bit)
        {
            _ops.Add(new BitHLOp(bit));
            return this;
        }

        private class ClearZOp : Op
        {
            public override int Execute(Registers registers, IAddressSpace addressSpace, int[] args, int context)
            {
                registers.Flags.SetZ(false);
                return context;
            }

            public override string ToString() => "0 → Z";
        }

        public OpcodeBuilder ClearZ()
        {
            _ops.Add(new ClearZOp());
            return this;
        }

        private class SwitchInterruptsOp : Op
        {
            private readonly bool _enable;
            private readonly bool _withDelay;

            public SwitchInterruptsOp(bool enable, bool withDelay)
            {
                _enable = enable;
                _withDelay = withDelay;
            }

            public override void SwitchInterrupts(InterruptManager interruptManager)
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

        public OpcodeBuilder SwitchInterrupts(bool enable, bool withDelay)
        {
            _ops.Add(new SwitchInterruptsOp(enable, withDelay));
            return this;
        }

        public OpcodeBuilder Op(Op op)
        {
            _ops.Add(op);
            return this;
        }

        private class ExtraCycleOp : Op
        {
            public override bool ReadsMemory() => true;
            public override string ToString() => "wait cycle";
        }

        public OpcodeBuilder ExtraCycle()
        {
            _ops.Add(new ExtraCycleOp());
            return this;
        }

        private class ForceFinishOp : Op
        {
            public override bool ForceFinishCycle() => true;
            public override string ToString() => "finish cycle";
        }

        public OpcodeBuilder ForceFinish()
        {
            _ops.Add(new ForceFinishOp());
            return this;
        }

        public Opcode Build() => new Opcode(this);

        public override string ToString() => _label;

        public static bool CausesOemBug(IntRegistryFunction function, int context)
        {
            return OemBug.Contains(function) && InOamArea(context);
        }

        private static bool InOamArea(int address) => address >= 0xfe00 && address <= 0xfeff;
    }
}