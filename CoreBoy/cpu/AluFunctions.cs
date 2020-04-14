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

        public IntRegistryFunction FindAluFunction(string name, DataType argumentType) => _functions[new FunctionKey(name, argumentType)];
        public BiIntRegistryFunction FindAluFunction(string name, DataType arg1Type, DataType arg2Type) => _biFunctions[new FunctionKey(name, arg1Type, arg2Type)];
        private void RegisterAluFunction(string name, DataType dataType, IntRegistryFunction function) => _functions[new FunctionKey(name, dataType)] = function;
        private void RegisterAluFunction(string name, DataType dataType1, DataType dataType2, BiIntRegistryFunction function) => _biFunctions[new FunctionKey(name, dataType1, dataType2)] = function;

        public AluFunctions()
        {
            RegisterAluFunction("INC", DataType.D8, (flags, arg) =>
            {
                var result = (arg + 1) & 0xff;
                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH((arg & 0x0f) == 0x0f);
                return result;
            });

            RegisterAluFunction("INC", DataType.D16, (flags, arg) => (arg + 1) & 0xffff);
            RegisterAluFunction("DEC", DataType.D8, (flags, arg) =>
            {
                var result = (arg - 1) & 0xff;
                flags.setZ(result == 0);
                flags.setN(true);
                flags.setH((arg & 0x0f) == 0x0);
                return result;
            });
            RegisterAluFunction("DEC", DataType.D16, (flags, arg) => (arg - 1) & 0xffff);
            RegisterAluFunction("ADD", DataType.D16, DataType.D16, (flags, arg1, arg2) =>
            {
                flags.setN(false);
                flags.setH((arg1 & 0x0fff) + (arg2 & 0x0fff) > 0x0fff);
                flags.setC(arg1 + arg2 > 0xffff);
                return (arg1 + arg2) & 0xffff;
            });
            RegisterAluFunction("ADD", DataType.D16, DataType.R8, (flags, arg1, arg2) => (arg1 + arg2) & 0xffff);
            RegisterAluFunction("ADD_SP", DataType.D16, DataType.R8, (flags, arg1, arg2) =>
            {
                flags.setZ(false);
                flags.setN(false);

                var result = arg1 + arg2;
                flags.setC((((arg1 & 0xff) + (arg2 & 0xff)) & 0x100) != 0);
                flags.setH((((arg1 & 0x0f) + (arg2 & 0x0f)) & 0x10) != 0);
                return result & 0xffff;
            });
            RegisterAluFunction("DAA", DataType.D8, (flags, arg) =>
            {
                var result = arg;
                if (flags.isN())
                {
                    if (flags.isH())
                    {
                        result = (result - 6) & 0xff;
                    }

                    if (flags.isC())
                    {
                        result = (result - 0x60) & 0xff;
                    }
                }
                else
                {
                    if (flags.isH() || (result & 0xf) > 9)
                    {
                        result += 0x06;
                    }

                    if (flags.isC() || result > 0x9f)
                    {
                        result += 0x60;
                    }
                }

                flags.setH(false);
                if (result > 0xff)
                {
                    flags.setC(true);
                }

                result &= 0xff;
                flags.setZ(result == 0);
                return result;
            });
            RegisterAluFunction("CPL", DataType.D8, (flags, arg) =>
            {
                flags.setN(true);
                flags.setH(true);
                return (~arg) & 0xff;
            });
            RegisterAluFunction("SCF", DataType.D8, (flags, arg) =>
            {
                flags.setN(false);
                flags.setH(false);
                flags.setC(true);
                return arg;
            });
            RegisterAluFunction("CCF", DataType.D8, (flags, arg) =>
            {
                flags.setN(false);
                flags.setH(false);
                flags.setC(!flags.isC());
                return arg;
            });
            RegisterAluFunction("ADD", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                flags.setZ(((byte1 + byte2) & 0xff) == 0);
                flags.setN(false);
                flags.setH((byte1 & 0x0f) + (byte2 & 0x0f) > 0x0f);
                flags.setC(byte1 + byte2 > 0xff);
                return (byte1 + byte2) & 0xff;
            });
            RegisterAluFunction("ADC", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                var carry = flags.isC() ? 1 : 0;
                flags.setZ(((byte1 + byte2 + carry) & 0xff) == 0);
                flags.setN(false);
                flags.setH((byte1 & 0x0f) + (byte2 & 0x0f) + carry > 0x0f);
                flags.setC(byte1 + byte2 + carry > 0xff);
                return (byte1 + byte2 + carry) & 0xff;
            });
            RegisterAluFunction("SUB", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                flags.setZ(((byte1 - byte2) & 0xff) == 0);
                flags.setN(true);
                flags.setH((0x0f & byte2) > (0x0f & byte1));
                flags.setC(byte2 > byte1);
                return (byte1 - byte2) & 0xff;
            });
            RegisterAluFunction("SBC", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                var carry = flags.isC() ? 1 : 0;
                var res = byte1 - byte2 - carry;

                flags.setZ((res & 0xff) == 0);
                flags.setN(true);
                flags.setH(((byte1 ^ byte2 ^ (res & 0xff)) & (1 << 4)) != 0);
                flags.setC(res < 0);
                return res & 0xff;
            });
            RegisterAluFunction("AND", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                var result = byte1 & byte2;
                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH(true);
                flags.setC(false);
                return result;
            });
            RegisterAluFunction("OR", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                var result = byte1 | byte2;
                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH(false);
                flags.setC(false);
                return result;
            });
            RegisterAluFunction("XOR", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                var result = (byte1 ^ byte2) & 0xff;
                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH(false);
                flags.setC(false);
                return result;
            });
            RegisterAluFunction("CP", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
            {
                flags.setZ(((byte1 - byte2) & 0xff) == 0);
                flags.setN(true);
                flags.setH((0x0f & byte2) > (0x0f & byte1));
                flags.setC(byte2 > byte1);
                return byte1;
            });
            RegisterAluFunction("RLC", DataType.D8, (flags, arg) =>
            {
                var result = (arg << 1) & 0xff;
                if ((arg & (1 << 7)) != 0)
                {
                    result |= 1;
                    flags.setC(true);
                }
                else
                {
                    flags.setC(false);
                }

                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH(false);
                return result;
            });
            RegisterAluFunction("RRC", DataType.D8, (flags, arg) =>
            {
                var result = arg >> 1;
                if ((arg & 1) == 1)
                {
                    result |= (1 << 7);
                    flags.setC(true);
                }
                else
                {
                    flags.setC(false);
                }

                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH(false);
                return result;
            });
            RegisterAluFunction("RL", DataType.D8, (flags, arg) =>
            {
                var result = (arg << 1) & 0xff;
                result |= flags.isC() ? 1 : 0;
                flags.setC((arg & (1 << 7)) != 0);
                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH(false);
                return result;
            });
            RegisterAluFunction("RR", DataType.D8, (flags, arg) =>
            {
                var result = arg >> 1;
                result |= flags.isC() ? (1 << 7) : 0;
                flags.setC((arg & 1) != 0);
                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH(false);
                return result;
            });
            RegisterAluFunction("SLA", DataType.D8, (flags, arg) =>
            {
                var result = (arg << 1) & 0xff;
                flags.setC((arg & (1 << 7)) != 0);
                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH(false);
                return result;
            });
            RegisterAluFunction("SRA", DataType.D8, (flags, arg) =>
            {
                var result = (arg >> 1) | (arg & (1 << 7));
                flags.setC((arg & 1) != 0);
                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH(false);
                return result;
            });
            RegisterAluFunction("SWAP", DataType.D8, (flags, arg) =>
            {
                var upper = arg & 0xf0;
                var lower = arg & 0x0f;
                var result = (lower << 4) | (upper >> 4);
                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH(false);
                flags.setC(false);
                return result;
            });
            RegisterAluFunction("SRL", DataType.D8, (flags, arg) =>
            {
                var result = (arg >> 1);
                flags.setC((arg & 1) != 0);
                flags.setZ(result == 0);
                flags.setN(false);
                flags.setH(false);
                return result;
            });
            RegisterAluFunction("BIT", DataType.D8, DataType.D8, (flags, arg1, arg2) =>
            {
                var bit = arg2;
                flags.setN(false);
                flags.setH(true);
                if (bit < 8)
                {
                    flags.setZ(!BitUtils.getBit(arg1, arg2));
                }

                return arg1;
            });
            RegisterAluFunction("RES", DataType.D8, DataType.D8, (flags, arg1, arg2) => BitUtils.clearBit(arg1, arg2));
            RegisterAluFunction("SET", DataType.D8, DataType.D8, (flags, arg1, arg2) => BitUtils.setBit(arg1, arg2));
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