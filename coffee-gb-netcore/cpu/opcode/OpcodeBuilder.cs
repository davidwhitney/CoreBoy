//namespace eu.rekawek.coffeegb.cpu.opcode 


using System;
using System.Collections.Generic;
using eu.rekawek.coffeegb;
using eu.rekawek.coffeegb.cpu;
using eu.rekawek.coffeegb.cpu.op;
using eu.rekawek.coffeegb.cpu.opcode;
using eu.rekawek.coffeegb.gpu;
using static eu.rekawek.coffeegb.cpu.BitUtils;

using IntRegistryFunction = System.Func<eu.rekawek.coffeegb.cpu.Flags, int, int>;
using BiIntRegistryFunction = System.Func<eu.rekawek.coffeegb.cpu.Flags, int, int, int>;

public class OpcodeBuilder {

    private static readonly AluFunctions ALU = new AluFunctions();

    private static readonly List<IntRegistryFunction> OEM_BUG;

    static OpcodeBuilder() 
    {
        var oemBugFunctions = new List<IntRegistryFunction>();
        oemBugFunctions.Add(ALU.findAluFunction("INC", DataType.D16));
        oemBugFunctions.Add(ALU.findAluFunction("DEC", DataType.D16));
        OEM_BUG = oemBugFunctions;
    }

    private readonly int opcode;

    private readonly String label;

    private readonly List<Op> ops = new List<Op>();

    private DataType lastDataType;

    public OpcodeBuilder(int opcode, String label) {
        this.opcode = opcode;
        this.label = label;
    }

    public OpcodeBuilder copyByte(String target, String source) {
        load(source);
        store(target);
        return this;
    }

    private class LoadOp : Op
    {
        private Argument arg;

        public LoadOp(Argument arg)
        {
            this.arg = arg;
        }
        public bool readsMemory()
        {
            return arg.isMemory();
        }

        public int operandLength()
        {
            return arg.getOperandLength();
        }


        public int execute(Registers registers, AddressSpace addressSpace, int[] args, int context)
        {
            return arg.read(registers, addressSpace, args);
        }


        public String toString()
        {
            if (arg.getDataType() == DataType.D16)
            {
                return String.Format("%s → [__]", arg.getLabel());
            }
            else
            {
                return String.Format("%s → [_]", arg.getLabel());
            }
        }

    }

    public OpcodeBuilder load(String source) {
        Argument arg = Argument.parse(source);
        lastDataType = arg.getDataType();
        ops.Add(new LoadOp(arg));
        return this;
    }

    public OpcodeBuilder loadWord(int value) {
        lastDataType = DataType.D16;
        ops.add(new Op() {
            
            public int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) {
                return value;
            }

            
            public String toString() {
                return String.Format("0x%02X → [__]", value);
            }
        });
        return this;
    }

    public OpcodeBuilder store(String target) {
        Argument arg = Argument.parse(target);
        if (lastDataType == DataType.D16 && arg == Argument._a16) {
            ops.add(new Op() {
                
                public bool writesMemory() {
                    return arg.isMemory();
                }

                
                public int operandLength() {
                    return arg.getOperandLength();
                }

                
                public int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) {
                    addressSpace.setByte(toWord(args), context & 0x00ff);
                    return context;
                }

                
                public String toString() {
                    return String.Format("[ _] → %s", arg.getLabel());
                }
            });
            ops.add(new Op() {
                
                public bool writesMemory() {
                    return arg.isMemory();
                }

                
                public int operandLength() {
                    return arg.getOperandLength();
                }

                
                public int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) {
                    addressSpace.setByte((toWord(args) + 1) & 0xffff, (context & 0xff00) >> 8);
                    return context;
                }

                
                public String toString() {
                    return String.Format("[_ ] → %s", arg.getLabel());
                }
            });
        } else if (lastDataType == arg.getDataType()) {
            ops.add(new Op() {
                
                public bool writesMemory() {
                    return arg.isMemory();
                }

                
                public int operandLength() {
                    return arg.getOperandLength();
                }

                
                public int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) {
                    arg.write(registers, addressSpace, args, context);
                    return context;
                }

                
                public String toString() {
                    if (arg.getDataType() == DataType.D16) {
                        return String.Format("[__] → %s", arg.getLabel());
                    } else {
                        return String.Format("[_] → %s", arg.getLabel());
                    }
                }
            });
        } else {
            throw new IllegalStateException("Can't write " + lastDataType + " to " + target);
        }
        return this;
    }

    public OpcodeBuilder proceedIf(String condition) {
        ops.add(new Op() {
            
            public bool proceed(Registers registers) {
                switch (condition) {
                    case "NZ":
                        return !registers.getFlags().isZ();

                    case "Z":
                        return registers.getFlags().isZ();

                    case "NC":
                        return !registers.getFlags().isC();

                    case "C":
                        return registers.getFlags().isC();
                }
                return false;
            }

            
            public String toString() {
                return String.Format("? %s:", condition);
            }
        });
        return this;
    }

    public OpcodeBuilder push() {
        AluFunctions.IntRegistryFunction dec = ALU.findAluFunction("DEC", DataType.D16);
        ops.add(new Op() {
            
            public bool writesMemory() {
                return true;
            }

            
            public int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) {
                registers.setSP(dec.apply(registers.getFlags(), registers.getSP()));
                addressSpace.setByte(registers.getSP(), (context & 0xff00) >> 8);
                return context;
            }

            
            public SpriteBug.CorruptionType causesOemBug(Registers registers, int context) {
                return inOamArea(registers.getSP()) ? SpriteBug.CorruptionType.PUSH_1 : null;
            }

            
            public String toString() {
                return String.Format("[_ ] → (SP--)");
            }
        });
        ops.add(new Op() {
            
            public bool writesMemory() {
                return true;
            }

            
            public int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) {
                registers.setSP(dec.apply(registers.getFlags(), registers.getSP()));
                addressSpace.setByte(registers.getSP(), context & 0x00ff);
                return context;
            }

            
            public SpriteBug.CorruptionType causesOemBug(Registers registers, int context) {
                return inOamArea(registers.getSP()) ? SpriteBug.CorruptionType.PUSH_2 : null;
            }

            
            public String toString() {
                return String.Format("[ _] → (SP--)");
            }
        });
        return this;
    }

    public OpcodeBuilder pop() {
        AluFunctions.IntRegistryFunction inc = ALU.findAluFunction("INC", DataType.D16);

        lastDataType = DataType.D16;
        ops.add(new Op() {
            
            public bool readsMemory() {
                return true;
            }

            
            public int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) {
                int lsb = addressSpace.getByte(registers.getSP());
                registers.setSP(inc.apply(registers.getFlags(), registers.getSP()));
                return lsb;
            }

            
            public SpriteBug.CorruptionType causesOemBug(Registers registers, int context) {
                return inOamArea(registers.getSP()) ? SpriteBug.CorruptionType.POP_1 : null;
            }

            
            public String toString() {
                return String.Format("(SP++) → [ _]");
            }
        });
        ops.add(new Op() {
            
            public bool readsMemory() {
                return true;
            }

            
            public int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) {
                int msb = addressSpace.getByte(registers.getSP());
                registers.setSP(inc.apply(registers.getFlags(), registers.getSP()));
                return context | (msb << 8);
            }

            
            public SpriteBug.CorruptionType causesOemBug(Registers registers, int context) {
                return inOamArea(registers.getSP()) ? SpriteBug.CorruptionType.POP_2 : null;
            }

            
            public String toString() {
                return String.Format("(SP++) → [_ ]");
            }
        });
        return this;
    }

    public OpcodeBuilder alu(String operation, String argument2) {
        Argument arg2 = Argument.parse(argument2);
        AluFunctions.BiIntRegistryFunction func = ALU.findAluFunction(operation, lastDataType, arg2.getDataType());
        ops.add(new Op() {
            
            public bool readsMemory() {
                return arg2.isMemory();
            }

            
            public int operandLength() {
                return arg2.getOperandLength();
            }

            
            public int execute(Registers registers, AddressSpace addressSpace, int[] args, int v1) {
                int v2 = arg2.read(registers, addressSpace, args);
                return func.apply(registers.getFlags(), v1, v2);
            }

            
            public String toString() {
                if (lastDataType == DataType.D16) {
                    return String.Format("%s([__],%s) → [__]", operation, arg2);
                } else {
                    return String.Format("%s([_],%s) → [_]", operation, arg2);
                }
            }
        });
        if (lastDataType == DataType.D16) {
            extraCycle();
        }
        return this;
    }

    public OpcodeBuilder alu(String operation, int d8Value) {
        AluFunctions.BiIntRegistryFunction func = ALU.findAluFunction(operation, lastDataType, DataType.D8);
        ops.add(new Op() {
            
            public int execute(Registers registers, AddressSpace addressSpace, int[] args, int v1) {
                return func.apply(registers.getFlags(), v1, d8Value);
            }

            
            public String toString() {
                return String.Format("%s(%d,[_]) → [_]", operation, d8Value);
            }
        });
        if (lastDataType == DataType.D16) {
            extraCycle();
        }
        return this;
    }

    public OpcodeBuilder alu(String operation) {
        AluFunctions.IntRegistryFunction func = ALU.findAluFunction(operation, lastDataType);
        ops.add(new Op() {
            
            public int execute(Registers registers, AddressSpace addressSpace, int[] args, int value) {
                return func.apply(registers.getFlags(), value);
            }

            
            public SpriteBug.CorruptionType causesOemBug(Registers registers, int context) {
                return OpcodeBuilder.causesOemBug(func, context) ? SpriteBug.CorruptionType.INC_DEC : null;
            }

            
            public String toString() {
                if (lastDataType == DataType.D16) {
                    return String.Format("%s([__]) → [__]", operation);
                } else {
                    return String.Format("%s([_]) → [_]", operation);
                }
            }
        });
        if (lastDataType == DataType.D16) {
            extraCycle();
        }
        return this;
    }

    public OpcodeBuilder aluHL(String operation) {
        load("HL");
        AluFunctions.IntRegistryFunction func = ALU.findAluFunction(operation, DataType.D16);
        ops.add(new Op() {
            
            public int execute(Registers registers, AddressSpace addressSpace, int[] args, int value) {
                return func.apply(registers.getFlags(), value);
            }

            
            public SpriteBug.CorruptionType causesOemBug(Registers registers, int context) {
                return OpcodeBuilder.causesOemBug(func, context) ? SpriteBug.CorruptionType.LD_HL : null;
            }

            
            public String toString() {
                return String.Format("%s(HL) → [__]");
            }
        });
        store("HL");
        return this;
    }

    public OpcodeBuilder bitHL(int bit) {
        ops.add(new Op() {
            
            public bool readsMemory() {
                return true;
            }

            
            public int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) {
                int value = addressSpace.getByte(registers.getHL());
                Flags flags = registers.getFlags();
                flags.setN(false);
                flags.setH(true);
                if (bit < 8) {
                    flags.setZ(!BitUtils.getBit(value, bit));
                }
                return context;
            }

            
            public String toString() {
                return String.Format("BIT(%d,HL)", bit);
            }
        });
        return this;
    }

    public OpcodeBuilder clearZ() {
        ops.add(new Op() {
            
            public int execute(Registers registers, AddressSpace addressSpace, int[] args, int context) {
                registers.getFlags().setZ(false);
                return context;
            }

            
            public String toString() {
                return String.Format("0 → Z");
            }
        });
        return this;
    }

    public OpcodeBuilder switchInterrupts(bool enable, bool withDelay) {
        ops.add(new Op() {
            
            public void switchInterrupts(InterruptManager interruptManager) {
                if (enable) {
                    interruptManager.enableInterrupts(withDelay);
                } else {
                    interruptManager.disableInterrupts(withDelay);
                }
            }

            
            public String toString() {
                return (enable ? "enable" : "disable") + " interrupts";
            }
        });
        return this;
    }

    public OpcodeBuilder op(Op op) {
        ops.add(op);
        return this;
    }

    public OpcodeBuilder extraCycle() {
        ops.add(new Op() {
            
            public bool readsMemory() {
                return true;
            }

            
            public String toString() {
                return "wait cycle";
            }
        });
        return this;
    }

    public OpcodeBuilder forceFinish() {
        ops.add(new Op() {
            
            public bool forceFinishCycle() {
                return true;
            }

            
            public String toString() {
                return "finish cycle";
            }
        });
        return this;
    }

    public Opcode build() {
        return new Opcode(this);
    }

    int getOpcode() {
        return opcode;
    }

    String getLabel() {
        return label;
    }

    List<Op> getOps() {
        return ops;
    }

    
    public String toString() {
        return label;
    }

    private static bool causesOemBug(IntRegistryFunction function, int context) {
        return OEM_BUG.Contains(function) && inOamArea(context);
    }

    private static bool inOamArea(int address) {
        return address >= 0xfe00 && address <= 0xfeff;
    }
}