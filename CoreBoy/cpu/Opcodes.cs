using System;
using System.Collections.Generic;
using System.Linq;
using CoreBoy.cpu.opcode;

namespace CoreBoy.cpu
{
    public class Opcodes
    {
        public List<Opcode> Commands { get; }
        public List<Opcode> ExtCommands { get; }

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
                RegCmd(opcodes, t, "INC {}").Load(t.Value).Alu("INC").Store(t.Value);
            }

            foreach (var t in OpcodesForValues(0x04, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                RegCmd(opcodes, t, "INC {}").Load(t.Value).Alu("INC").Store(t.Value);
            }

            foreach (var t in OpcodesForValues(0x05, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                RegCmd(opcodes, t, "DEC {}").Load(t.Value).Alu("DEC").Store(t.Value);
            }

            foreach (var (key, value) in OpcodesForValues(0x06, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                RegLoad(opcodes, key, value, "d8");
            }

            foreach (var o in OpcodesForValues(0x07, 0x08, "RLC", "RRC", "RL", "RR"))
            {
                RegCmd(opcodes, o, o.Value + "A").Load("A").Alu(o.Value).ClearZ().Store("A");
            }

            RegLoad(opcodes, 0x08, "(a16)", "SP");

            foreach (var t in OpcodesForValues(0x09, 0x10, "BC", "DE", "HL", "SP"))
            {
                RegCmd(opcodes, t, "ADD HL,{}").Load("HL").Alu("ADD", t.Value).Store("HL");
            }

            foreach (var (key, value) in OpcodesForValues(0x0a, 0x10, "(BC)", "(DE)"))
            {
                RegLoad(opcodes, key, "A", value);
            }

            foreach (var t in OpcodesForValues(0x0b, 0x10, "BC", "DE", "HL", "SP"))
            {
                RegCmd(opcodes, t, "DEC {}").Load(t.Value).Alu("DEC").Store(t.Value);
            }

            RegCmd(opcodes, 0x10, "STOP");

            RegCmd(opcodes, 0x18, "JR r8").Load("PC").Alu("ADD", "r8").Store("PC");

            foreach (var c in OpcodesForValues(0x20, 0x08, "NZ", "Z", "NC", "C"))
            {
                RegCmd(opcodes, c, "JR {},r8").Load("PC").ProceedIf(c.Value).Alu("ADD", "r8").Store("PC");
            }

            RegCmd(opcodes, 0x22, "LD (HL+),A").CopyByte("(HL)", "A").AluHL("INC");
            RegCmd(opcodes, 0x2a, "LD A,(HL+)").CopyByte("A", "(HL)").AluHL("INC");

            RegCmd(opcodes, 0x27, "DAA").Load("A").Alu("DAA").Store("A");
            RegCmd(opcodes, 0x2f, "CPL").Load("A").Alu("CPL").Store("A");

            RegCmd(opcodes, 0x32, "LD (HL-),A").CopyByte("(HL)", "A").AluHL("DEC");
            RegCmd(opcodes, 0x3a, "LD A,(HL-)").CopyByte("A", "(HL)").AluHL("DEC");

            RegCmd(opcodes, 0x37, "SCF").Load("A").Alu("SCF").Store("A");
            RegCmd(opcodes, 0x3f, "CCF").Load("A").Alu("CCF").Store("A");

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
                    RegCmd(opcodes, t, value + " {}").Load("A").Alu(value, t.Value).Store("A");
                }
            }

            foreach (var c in OpcodesForValues(0xc0, 0x08, "NZ", "Z", "NC", "C"))
            {
                RegCmd(opcodes, c, "RET {}").ExtraCycle().ProceedIf(c.Value).Pop().ForceFinish().Store("PC");
            }

            foreach (var t in OpcodesForValues(0xc1, 0x10, "BC", "DE", "HL", "AF"))
            {
                RegCmd(opcodes, t, "POP {}").Pop().Store(t.Value);
            }

            foreach (var c in OpcodesForValues(0xc2, 0x08, "NZ", "Z", "NC", "C"))
            {
                RegCmd(opcodes, c, "JP {},a16").Load("a16").ProceedIf(c.Value).Store("PC").ExtraCycle();
            }

            RegCmd(opcodes, 0xc3, "JP a16").Load("a16").Store("PC").ExtraCycle();

            foreach (var c in OpcodesForValues(0xc4, 0x08, "NZ", "Z", "NC", "C"))
            {
                RegCmd(opcodes, c, "CALL {},a16").ProceedIf(c.Value).ExtraCycle().Load("PC").Push().Load("a16")
                    .Store("PC");
            }

            foreach (var t in OpcodesForValues(0xc5, 0x10, "BC", "DE", "HL", "AF"))
            {
                RegCmd(opcodes, t, "PUSH {}").ExtraCycle().Load(t.Value).Push();
            }

            foreach (var o in OpcodesForValues(0xc6, 0x08, "ADD", "ADC", "SUB", "SBC", "AND", "XOR", "OR", "CP"))
            {
                RegCmd(opcodes, o, o.Value + " d8").Load("A").Alu(o.Value, "d8").Store("A");
            }

            for (int i = 0xc7, j = 0x00; i <= 0xf7; i += 0x10, j += 0x10)
            {
                RegCmd(opcodes, i, $"RST {j:X2}H").Load("PC").Push().ForceFinish().LoadWord(j)
                    .Store("PC");
            }

            RegCmd(opcodes, 0xc9, "RET").Pop().ForceFinish().Store("PC");

            RegCmd(opcodes, 0xcd, "CALL a16").Load("PC").ExtraCycle().Push().Load("a16").Store("PC");

            for (int i = 0xcf, j = 0x08; i <= 0xff; i += 0x10, j += 0x10)
            {
                RegCmd(opcodes, i, $"RST {j:X2}H").Load("PC").Push().ForceFinish().LoadWord(j)
                    .Store("PC");
            }

            RegCmd(opcodes, 0xd9, "RETI").Pop().ForceFinish().Store("PC").SwitchInterrupts(true, false);

            RegLoad(opcodes, 0xe2, "(C)", "A");
            RegLoad(opcodes, 0xf2, "A", "(C)");

            RegCmd(opcodes, 0xe9, "JP (HL)").Load("HL").Store("PC");

            RegCmd(opcodes, 0xe0, "LDH (a8),A").CopyByte("(a8)", "A");
            RegCmd(opcodes, 0xf0, "LDH A,(a8)").CopyByte("A", "(a8)");

            RegCmd(opcodes, 0xe8, "ADD SP,r8").Load("SP").Alu("ADD_SP", "r8").ExtraCycle().Store("SP");
            RegCmd(opcodes, 0xf8, "LD HL,SP+r8").Load("SP").Alu("ADD_SP", "r8").Store("HL");

            RegLoad(opcodes, 0xea, "(a16)", "A");
            RegLoad(opcodes, 0xfa, "A", "(a16)");

            RegCmd(opcodes, 0xf3, "DI").SwitchInterrupts(false, true);
            RegCmd(opcodes, 0xfb, "EI").SwitchInterrupts(true, true);

            RegLoad(opcodes, 0xf9, "SP", "HL").ExtraCycle();

            foreach (var (key, value) in OpcodesForValues(0x00, 0x08, "RLC", "RRC", "RL", "RR", "SLA", "SRA", "SWAP", "SRL"))
            {
                foreach (var t in OpcodesForValues(key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
                {
                    RegCmd(extOpcodes, t, value + " {}").Load(t.Value).Alu(value).Store(t.Value);
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
                            RegCmd(extOpcodes, t, $"BIT {b},(HL)").BitHL(b);
                        }
                        else
                        {
                            RegCmd(extOpcodes, t, $"{value} {b},{t.Value}").Load(t.Value)
                                .Alu(value, b).Store(t.Value);
                        }
                    }
                }
            }
            
            var commands = new List<Opcode>(0x100);
            var extCommands = new List<Opcode>(0x100);

            commands.AddRange(opcodes.Select(b => b?.Build()));
            extCommands.AddRange(extOpcodes.Select(b => b?.Build()));

            Commands = commands;
            ExtCommands = extCommands;
        }

        private static OpcodeBuilder RegLoad(IList<OpcodeBuilder> commands, int opcode, string target, string source)
        {
            return RegCmd(commands, opcode, $"LD {target},{source}").CopyByte(target, source);
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