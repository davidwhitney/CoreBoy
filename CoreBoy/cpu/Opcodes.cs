using System;
using System.Collections.Generic;
using System.Linq;
using CoreBoy.cpu.opcode;

namespace CoreBoy.cpu
{
    public class Opcodes
    {
        public readonly Dictionary<int, Opcode> COMMANDS;
        public readonly Dictionary<int, Opcode> EXT_COMMANDS;

        public Opcodes()
        {
            var opcodes = new OpcodeBuilder[0x100];
            var extOpcodes = new OpcodeBuilder[0x100];

            RegCmd(opcodes, 0x00, "NOP");

            foreach (var (key, value) in OpcodesForValues(0x01, 0x10, "BC", "DE", "HL", "SP"))
            {
                RegLoad(opcodes, key, value, "d16");
            }

            foreach (var (key, value) in OpcodesForValues(0x02, 0x10, "(BC)", "(DE)"))
            {
                RegLoad(opcodes, key, value, "A");
            }

            foreach (var t in OpcodesForValues(0x03, 0x10, "BC", "DE", "HL", "SP"))
            {
                RegCmd(opcodes, t, "INC {}").load(t.Value).alu("INC").store(t.Value);
            }

            foreach (var t in OpcodesForValues(0x04, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                RegCmd(opcodes, t, "INC {}").load(t.Value).alu("INC").store(t.Value);
            }

            foreach (var t in OpcodesForValues(0x05, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                RegCmd(opcodes, t, "DEC {}").load(t.Value).alu("DEC").store(t.Value);
            }

            foreach (var (key, value) in OpcodesForValues(0x06, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                RegLoad(opcodes, key, value, "d8");
            }

            foreach (var o in OpcodesForValues(0x07, 0x08, "RLC", "RRC", "RL", "RR"))
            {
                RegCmd(opcodes, o, o.Value + "A").load("A").alu(o.Value).clearZ().store("A");
            }

            RegLoad(opcodes, 0x08, "(a16)", "SP");

            foreach (var t in OpcodesForValues(0x09, 0x10, "BC", "DE", "HL", "SP"))
            {
                RegCmd(opcodes, t, "ADD HL,{}").load("HL").alu("ADD", t.Value).store("HL");
            }

            foreach (var (key, value) in OpcodesForValues(0x0a, 0x10, "(BC)", "(DE)"))
            {
                RegLoad(opcodes, key, "A", value);
            }

            foreach (var t in OpcodesForValues(0x0b, 0x10, "BC", "DE", "HL", "SP"))
            {
                RegCmd(opcodes, t, "DEC {}").load(t.Value).alu("DEC").store(t.Value);
            }

            RegCmd(opcodes, 0x10, "STOP");

            RegCmd(opcodes, 0x18, "JR r8").load("PC").alu("ADD", "r8").store("PC");

            foreach (var c in OpcodesForValues(0x20, 0x08, "NZ", "Z", "NC", "C"))
            {
                RegCmd(opcodes, c, "JR {},r8").load("PC").proceedIf(c.Value).alu("ADD", "r8").store("PC");
            }

            RegCmd(opcodes, 0x22, "LD (HL+),A").copyByte("(HL)", "A").aluHL("INC");
            RegCmd(opcodes, 0x2a, "LD A,(HL+)").copyByte("A", "(HL)").aluHL("INC");

            RegCmd(opcodes, 0x27, "DAA").load("A").alu("DAA").store("A");
            RegCmd(opcodes, 0x2f, "CPL").load("A").alu("CPL").store("A");

            RegCmd(opcodes, 0x32, "LD (HL-),A").copyByte("(HL)", "A").aluHL("DEC");
            RegCmd(opcodes, 0x3a, "LD A,(HL-)").copyByte("A", "(HL)").aluHL("DEC");

            RegCmd(opcodes, 0x37, "SCF").load("A").alu("SCF").store("A");
            RegCmd(opcodes, 0x3f, "CCF").load("A").alu("CCF").store("A");

            foreach (var (key, value) in OpcodesForValues(0x40, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                foreach (var s in OpcodesForValues(key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
                {
                    if (s.Key == 0x76)
                    {
                        continue;
                    }

                    RegLoad(opcodes, s.Key, value, s.Value);
                }
            }

            RegCmd(opcodes, 0x76, "HALT");

            foreach (var (key, value) in OpcodesForValues(0x80, 0x08, "ADD", "ADC", "SUB", "SBC", "AND", "XOR", "OR", "CP"))
            {
                foreach (var t in OpcodesForValues(key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
                {
                    RegCmd(opcodes, t, value + " {}").load("A").alu(value, t.Value).store("A");
                }
            }

            foreach (var c in OpcodesForValues(0xc0, 0x08, "NZ", "Z", "NC", "C"))
            {
                RegCmd(opcodes, c, "RET {}").extraCycle().proceedIf(c.Value).pop().forceFinish().store("PC");
            }

            foreach (var t in OpcodesForValues(0xc1, 0x10, "BC", "DE", "HL", "AF"))
            {
                RegCmd(opcodes, t, "POP {}").pop().store(t.Value);
            }

            foreach (var c in OpcodesForValues(0xc2, 0x08, "NZ", "Z", "NC", "C"))
            {
                RegCmd(opcodes, c, "JP {},a16").load("a16").proceedIf(c.Value).store("PC").extraCycle();
            }

            RegCmd(opcodes, 0xc3, "JP a16").load("a16").store("PC").extraCycle();

            foreach (var c in OpcodesForValues(0xc4, 0x08, "NZ", "Z", "NC", "C"))
            {
                RegCmd(opcodes, c, "CALL {},a16").proceedIf(c.Value).extraCycle().load("PC").push().load("a16")
                    .store("PC");
            }

            foreach (var t in OpcodesForValues(0xc5, 0x10, "BC", "DE", "HL", "AF"))
            {
                RegCmd(opcodes, t, "PUSH {}").extraCycle().load(t.Value).push();
            }

            foreach (var o in OpcodesForValues(0xc6, 0x08, "ADD", "ADC", "SUB", "SBC", "AND", "XOR", "OR", "CP"))
            {
                RegCmd(opcodes, o, o.Value + " d8").load("A").alu(o.Value, "d8").store("A");
            }

            for (int i = 0xc7, j = 0x00; i <= 0xf7; i += 0x10, j += 0x10)
            {
                RegCmd(opcodes, i, $"RST {j:X2}H").load("PC").push().forceFinish().loadWord(j)
                    .store("PC");
            }

            RegCmd(opcodes, 0xc9, "RET").pop().forceFinish().store("PC");

            RegCmd(opcodes, 0xcd, "CALL a16").load("PC").extraCycle().push().load("a16").store("PC");

            for (int i = 0xcf, j = 0x08; i <= 0xff; i += 0x10, j += 0x10)
            {
                RegCmd(opcodes, i, $"RST {j:X2}H").load("PC").push().forceFinish().loadWord(j)
                    .store("PC");
            }

            RegCmd(opcodes, 0xd9, "RETI").pop().forceFinish().store("PC").switchInterrupts(true, false);

            RegLoad(opcodes, 0xe2, "(C)", "A");
            RegLoad(opcodes, 0xf2, "A", "(C)");

            RegCmd(opcodes, 0xe9, "JP (HL)").load("HL").store("PC");

            RegCmd(opcodes, 0xe0, "LDH (a8),A").copyByte("(a8)", "A");
            RegCmd(opcodes, 0xf0, "LDH A,(a8)").copyByte("A", "(a8)");

            RegCmd(opcodes, 0xe8, "ADD SP,r8").load("SP").alu("ADD_SP", "r8").extraCycle().store("SP");
            RegCmd(opcodes, 0xf8, "LD HL,SP+r8").load("SP").alu("ADD_SP", "r8").store("HL");

            RegLoad(opcodes, 0xea, "(a16)", "A");
            RegLoad(opcodes, 0xfa, "A", "(a16)");

            RegCmd(opcodes, 0xf3, "DI").switchInterrupts(false, true);
            RegCmd(opcodes, 0xfb, "EI").switchInterrupts(true, true);

            RegLoad(opcodes, 0xf9, "SP", "HL").extraCycle();

            foreach (var (key, value) in OpcodesForValues(0x00, 0x08, "RLC", "RRC", "RL", "RR", "SLA", "SRA", "SWAP", "SRL"))
            {
                foreach (var t in OpcodesForValues(key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
                {
                    RegCmd(extOpcodes, t, value + " {}").load(t.Value).alu(value).store(t.Value);
                }
            }

            foreach (var (key, value) in OpcodesForValues(0x40, 0x40, "BIT", "RES", "SET"))
            {
                for (var b = 0; b < 0x08; b++)
                {
                    foreach (var t in OpcodesForValues(key + b * 0x08, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
                    {
                        if ("BIT".Equals(value) && "(HL)".Equals(t.Value))
                        {
                            RegCmd(extOpcodes, t, $"BIT {b},(HL)").bitHL(b);
                        }
                        else
                        {
                            RegCmd(extOpcodes, t, $"{value} {b},{t.Value}").load(t.Value)
                                .alu(value, b).store(t.Value);
                        }
                    }
                }
            }
            
            var commands = new Dictionary<int, Opcode>(0x100);
            var extCommands = new Dictionary<int, Opcode>(0x100);

            foreach (var b in opcodes.Where(x => x != null))
            {
                commands.Add(b.getOpcode(), b.build());
            }

            foreach (var b in extOpcodes.Where(x => x != null))
            {
                extCommands.Add(b.getOpcode(), b.build());
            }

            COMMANDS = commands;
            EXT_COMMANDS = extCommands;
        }

        private static OpcodeBuilder RegLoad(IList<OpcodeBuilder> commands, int opcode, string target, string source)
        {
            return RegCmd(commands, opcode, $"LD {target},{source}").copyByte(target, source);
        }

        private static OpcodeBuilder RegCmd(IList<OpcodeBuilder> commands, KeyValuePair<int, string> opcode, string label)
        {
            return RegCmd(commands, opcode.Key, label.Replace("{}", opcode.Value));
        }

        private static OpcodeBuilder RegCmd(IList<OpcodeBuilder> commands, int opcode, string label)
        {
            if (commands[opcode] != null)
            {
                throw new InvalidOperationException($"Opcode {opcode:X} already exists: {commands[opcode]}");
            }

            var builder = new OpcodeBuilder(opcode, label);
            commands[opcode] = builder;
            return builder;
        }

        private static Dictionary<int, string> OpcodesForValues(int start, int step, params string[] values)
        {
            var map = new Dictionary<int, string>();
            var i = start;
            foreach (var e in values)
            {
                map.Add(i, e);
                i += step;
            }

            return map;
        }
    }
}