using CoreBoy.cpu.op;
using IntRegistryFunction = System.Func<CoreBoy.cpu.Flags, int, int>;
using BiIntRegistryFunction = System.Func<CoreBoy.cpu.Flags, int, int, int>;
using AluFunctionsMap = System.Collections.Generic.Dictionary<CoreBoy.cpu.AluFunctions.FunctionKey, System.Func<CoreBoy.cpu.Flags, int, int>>;
using AluBiFunctionsMap = System.Collections.Generic.Dictionary<CoreBoy.cpu.AluFunctions.FunctionKey, System.Func<CoreBoy.cpu.Flags, int, int, int>>;

namespace CoreBoy.cpu
{
    public class AluFunctions
    {
        private readonly AluFunctionsMap _functions = new AluFunctionsMap();
        private readonly AluBiFunctionsMap _biFunctions = new AluBiFunctionsMap();

        public IntRegistryFunction GetFunction(string name, DataType argumentType) => _functions[new FunctionKey(name, argumentType)];
        public BiIntRegistryFunction GetFunction(string name, DataType arg1Type, DataType arg2Type) => _biFunctions[new FunctionKey(name, arg1Type, arg2Type)];
        private void AddFunction(string name, DataType dataType, IntRegistryFunction function) => _functions[new FunctionKey(name, dataType)] = function;
        private void AddFunction(string name, DataType dataType1, DataType dataType2, BiIntRegistryFunction function) => _biFunctions[new FunctionKey(name, dataType1, dataType2)] = function;

        public AluFunctions()
        {
            AddFunction("INC", DataType.D8, (flags, arg) =>
            {
                var result = (arg + 1) & 0xff;
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH((arg & 0x0f) == 0x0f);
                return result;
            });

            AddFunction("INC", DataType.D16, (flags, arg) => (arg + 1) & 0xffff);
            AddFunction("DEC", DataType.D8, (flags, arg) =>
            {
                var result = (arg - 1) & 0xff;
                flags.SetZ(result == 0);
                flags.SetN(true);
                flags.SetH((arg & 0x0f) == 0x0);
                return result;
            });
            AddFunction("DEC", DataType.D16, (flags, arg) => (arg - 1) & 0xffff);
            AddFunction("ADD", DataType.D16, DataType.D16, (flags, arg1, arg2) =>
            {
                flags.SetN(false);
                flags.SetH((arg1 & 0x0fff) + (arg2 & 0x0fff) > 0x0fff);
                flags.SetC(arg1 + arg2 > 0xffff);
                return (arg1 + arg2) & 0xffff;
            });
            AddFunction("ADD", DataType.D16, DataType.R8, (flags, arg1, arg2) => (arg1 + arg2) & 0xffff);
            AddFunction("ADD_SP", DataType.D16, DataType.R8, (flags, arg1, arg2) =>
            {
                flags.SetZ(false);
                flags.SetN(false);

                var result = arg1 + arg2;
                flags.SetC((((arg1 & 0xff) + (arg2 & 0xff)) & 0x100) != 0);
                flags.SetH((((arg1 & 0x0f) + (arg2 & 0x0f)) & 0x10) != 0);
                return result & 0xffff;
            });
            AddFunction("DAA", DataType.D8, (flags, arg) =>
            {
                var result = arg;
                if (flags.IsN())
                {
                    if (flags.IsH())
                    {
                        result = (result - 6) & 0xff;
                    }

                    if (flags.IsC())
                    {
                        result = (result - 0x60) & 0xff;
                    }
                }
                else
                {
                    if (flags.IsH() || (result & 0xf) > 9)
                    {
                        result += 0x06;
                    }

                    if (flags.IsC() || result > 0x9f)
                    {
                        result += 0x60;
                    }
                }

                flags.SetH(false);
                if (result > 0xff)
                {
                    flags.SetC(true);
                }

                result &= 0xff;
                flags.SetZ(result == 0);
                return result;
            });
            AddFunction("CPL", DataType.D8, (flags, arg) =>
            {
                flags.SetN(true);
                flags.SetH(true);
                return (~arg) & 0xff;
            });
            AddFunction("SCF", DataType.D8, (flags, arg) =>
            {
                flags.SetN(false);
                flags.SetH(false);
                flags.SetC(true);
                return arg;
            });
            AddFunction("CCF", DataType.D8, (flags, arg) =>
            {
                flags.SetN(false);
                flags.SetH(false);
                flags.SetC(!flags.IsC());
                return arg;
            });
            AddFunction("ADD", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                flags.SetZ(((byte1 + byte2) & 0xff) == 0);
                flags.SetN(false);
                flags.SetH((byte1 & 0x0f) + (byte2 & 0x0f) > 0x0f);
                flags.SetC(byte1 + byte2 > 0xff);
                return (byte1 + byte2) & 0xff;
            });
            AddFunction("ADC", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                var carry = flags.IsC() ? 1 : 0;
                flags.SetZ(((byte1 + byte2 + carry) & 0xff) == 0);
                flags.SetN(false);
                flags.SetH((byte1 & 0x0f) + (byte2 & 0x0f) + carry > 0x0f);
                flags.SetC(byte1 + byte2 + carry > 0xff);
                return (byte1 + byte2 + carry) & 0xff;
            });
            AddFunction("SUB", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                flags.SetZ(((byte1 - byte2) & 0xff) == 0);
                flags.SetN(true);
                flags.SetH((0x0f & byte2) > (0x0f & byte1));
                flags.SetC(byte2 > byte1);
                return (byte1 - byte2) & 0xff;
            });
            AddFunction("SBC", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                var carry = flags.IsC() ? 1 : 0;
                var res = byte1 - byte2 - carry;

                flags.SetZ((res & 0xff) == 0);
                flags.SetN(true);
                flags.SetH(((byte1 ^ byte2 ^ (res & 0xff)) & (1 << 4)) != 0);
                flags.SetC(res < 0);
                return res & 0xff;
            });
            AddFunction("AND", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                var result = byte1 & byte2;
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(true);
                flags.SetC(false);
                return result;
            });
            AddFunction("OR", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                var result = byte1 | byte2;
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                flags.SetC(false);
                return result;
            });
            AddFunction("XOR", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                var result = (byte1 ^ byte2) & 0xff;
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                flags.SetC(false);
                return result;
            });
            AddFunction("CP", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                flags.SetZ(((byte1 - byte2) & 0xff) == 0);
                flags.SetN(true);
                flags.SetH((0x0f & byte2) > (0x0f & byte1));
                flags.SetC(byte2 > byte1);
                return byte1;
            });
            AddFunction("RLC", DataType.D8, (flags, arg) =>
            {
                var result = (arg << 1) & 0xff;
                if ((arg & (1 << 7)) != 0)
                {
                    result |= 1;
                    flags.SetC(true);
                }
                else
                {
                    flags.SetC(false);
                }

                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });
            AddFunction("RRC", DataType.D8, (flags, arg) =>
            {
                var result = arg >> 1;
                if ((arg & 1) == 1)
                {
                    result |= (1 << 7);
                    flags.SetC(true);
                }
                else
                {
                    flags.SetC(false);
                }

                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });
            AddFunction("RL", DataType.D8, (flags, arg) =>
            {
                var result = (arg << 1) & 0xff;
                result |= flags.IsC() ? 1 : 0;
                flags.SetC((arg & (1 << 7)) != 0);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });
            AddFunction("RR", DataType.D8, (flags, arg) =>
            {
                var result = arg >> 1;
                result |= flags.IsC() ? (1 << 7) : 0;
                flags.SetC((arg & 1) != 0);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });
            AddFunction("SLA", DataType.D8, (flags, arg) =>
            {
                var result = (arg << 1) & 0xff;
                flags.SetC((arg & (1 << 7)) != 0);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });
            AddFunction("SRA", DataType.D8, (flags, arg) =>
            {
                var result = (arg >> 1) | (arg & (1 << 7));
                flags.SetC((arg & 1) != 0);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });
            AddFunction("SWAP", DataType.D8, (flags, arg) =>
            {
                var upper = arg & 0xf0;
                var lower = arg & 0x0f;
                var result = (lower << 4) | (upper >> 4);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                flags.SetC(false);
                return result;
            });
            AddFunction("SRL", DataType.D8, (flags, arg) =>
            {
                var result = (arg >> 1);
                flags.SetC((arg & 1) != 0);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });
            AddFunction("BIT", DataType.D8, DataType.D8, (flags, arg1, arg2) =>
            {
                var bit = arg2;
                flags.SetN(false);
                flags.SetH(true);
                if (bit < 8)
                {
                    flags.SetZ(!BitUtils.GetBit(arg1, arg2));
                }

                return arg1;
            });
            AddFunction("RES", DataType.D8, DataType.D8, (flags, arg1, arg2) => BitUtils.ClearBit(arg1, arg2));
            AddFunction("SET", DataType.D8, DataType.D8, (flags, arg1, arg2) => BitUtils.SetBit(arg1, arg2));
        }

        public class FunctionKey
        {
            private readonly string _name;
            private readonly DataType _type1;
            private readonly DataType _type2;

            public FunctionKey(string name, DataType type1, DataType type2)
            {
                _name = name;
                _type1 = type1;
                _type2 = type2;
            }

            public FunctionKey(string name, DataType type)
            {
                _name = name;
                _type1 = type;
                _type2 = DataType.Unset;
            }

            protected bool Equals(FunctionKey other)
            {
                return _name == other._name && _type1 == other._type1 && _type2 == other._type2;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((FunctionKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (_name != null ? _name.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (int) _type1;
                    hashCode = (hashCode * 397) ^ (int) _type2;
                    return hashCode;
                }
            }
        }
    }
}