using System;
using System.Collections.Generic;
using eu.rekawek.coffeegb.cpu.opcode;

namespace eu.rekawek.coffeegb.cpu
{
    public static class Opcodes
    {
        public static readonly Dictionary<int, Opcode> COMMANDS;
        public static readonly Dictionary<int, Opcode> EXT_COMMANDS;

        static Opcodes()
        {
            var opcodes = new OpcodeBuilder[0x100];
            var extOpcodes = new OpcodeBuilder[0x100];

            regCmd(opcodes, 0x00, "NOP");

            foreach (var t in indexedList(0x01, 0x10, "BC", "DE", "HL", "SP"))
            {
                regLoad(opcodes, t.Key, t.Value, "d16");
            }

            foreach (var t in indexedList(0x02, 0x10, "(BC)", "(DE)"))
            {
                regLoad(opcodes, t.Key, t.Value, "A");
            }

            foreach (var t in indexedList(0x03, 0x10, "BC", "DE", "HL", "SP"))
            {
                regCmd(opcodes, t, "INC {}").load(t.Value).alu("INC").store(t.Value);
            }

            foreach (var t in indexedList(0x04, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                regCmd(opcodes, t, "INC {}").load(t.Value).alu("INC").store(t.Value);
            }

            foreach (var t in indexedList(0x05, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                regCmd(opcodes, t, "DEC {}").load(t.Value).alu("DEC").store(t.Value);
            }

            foreach (var t in indexedList(0x06, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                regLoad(opcodes, t.Key, t.Value, "d8");
            }

            foreach (var o in indexedList(0x07, 0x08, "RLC", "RRC", "RL", "RR"))
            {
                regCmd(opcodes, o, o.Value + "A").load("A").alu(o.Value).clearZ().store("A");
            }

            regLoad(opcodes, 0x08, "(a16)", "SP");

            foreach (var t in indexedList(0x09, 0x10, "BC", "DE", "HL", "SP"))
            {
                regCmd(opcodes, t, "ADD HL,{}").load("HL").alu("ADD", t.Value).store("HL");
            }

            foreach (var t in indexedList(0x0a, 0x10, "(BC)", "(DE)"))
            {
                regLoad(opcodes, t.Key, "A", t.Value);
            }

            foreach (var t in indexedList(0x0b, 0x10, "BC", "DE", "HL", "SP"))
            {
                regCmd(opcodes, t, "DEC {}").load(t.Value).alu("DEC").store(t.Value);
            }

            regCmd(opcodes, 0x10, "STOP");

            regCmd(opcodes, 0x18, "JR r8").load("PC").alu("ADD", "r8").store("PC");

            foreach (var c in indexedList(0x20, 0x08, "NZ", "Z", "NC", "C"))
            {
                regCmd(opcodes, c, "JR {},r8").load("PC").proceedIf(c.Value).alu("ADD", "r8").store("PC");
            }

            regCmd(opcodes, 0x22, "LD (HL+),A").copyByte("(HL)", "A").aluHL("INC");
            regCmd(opcodes, 0x2a, "LD A,(HL+)").copyByte("A", "(HL)").aluHL("INC");

            regCmd(opcodes, 0x27, "DAA").load("A").alu("DAA").store("A");
            regCmd(opcodes, 0x2f, "CPL").load("A").alu("CPL").store("A");

            regCmd(opcodes, 0x32, "LD (HL-),A").copyByte("(HL)", "A").aluHL("DEC");
            regCmd(opcodes, 0x3a, "LD A,(HL-)").copyByte("A", "(HL)").aluHL("DEC");

            regCmd(opcodes, 0x37, "SCF").load("A").alu("SCF").store("A");
            regCmd(opcodes, 0x3f, "CCF").load("A").alu("CCF").store("A");

            foreach (var t in indexedList(0x40, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                foreach (var s in indexedList(t.Key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
                {
                    if (s.Key == 0x76)
                    {
                        continue;
                    }

                    regLoad(opcodes, s.Key, t.Value, s.Value);
                }
            }

            regCmd(opcodes, 0x76, "HALT");

            foreach (var o in indexedList(0x80, 0x08, "ADD", "ADC", "SUB", "SBC", "AND", "XOR", "OR", "CP"))
            {
                foreach (var t in indexedList(o.Key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
                {
                    regCmd(opcodes, t, o.Value + " {}").load("A").alu(o.Value, t.Value).store("A");
                }
            }

            foreach (var c in indexedList(0xc0, 0x08, "NZ", "Z", "NC", "C"))
            {
                regCmd(opcodes, c, "RET {}").extraCycle().proceedIf(c.Value).pop().forceFinish().store("PC");
            }

            foreach (var t in indexedList(0xc1, 0x10, "BC", "DE", "HL", "AF"))
            {
                regCmd(opcodes, t, "POP {}").pop().store(t.Value);
            }

            foreach (var c in indexedList(0xc2, 0x08, "NZ", "Z", "NC", "C"))
            {
                regCmd(opcodes, c, "JP {},a16").load("a16").proceedIf(c.Value).store("PC").extraCycle();
            }

            regCmd(opcodes, 0xc3, "JP a16").load("a16").store("PC").extraCycle();

            foreach (var c in indexedList(0xc4, 0x08, "NZ", "Z", "NC", "C"))
            {
                regCmd(opcodes, c, "CALL {},a16").proceedIf(c.Value).extraCycle().load("PC").push().load("a16")
                    .store("PC");
            }

            foreach (var t in indexedList(0xc5, 0x10, "BC", "DE", "HL", "AF"))
            {
                regCmd(opcodes, t, "PUSH {}").extraCycle().load(t.Value).push();
            }

            foreach (var o in indexedList(0xc6, 0x08, "ADD", "ADC", "SUB", "SBC", "AND", "XOR", "OR", "CP"))
            {
                regCmd(opcodes, o, o.Value + " d8").load("A").alu(o.Value, "d8").store("A");
            }

            for (int i = 0xc7, j = 0x00; i <= 0xf7; i += 0x10, j += 0x10)
            {
                regCmd(opcodes, i, String.Format("RST %02XH", j)).load("PC").push().forceFinish().loadWord(j)
                    .store("PC");
            }

            regCmd(opcodes, 0xc9, "RET").pop().forceFinish().store("PC");

            regCmd(opcodes, 0xcd, "CALL a16").load("PC").extraCycle().push().load("a16").store("PC");

            for (int i = 0xcf, j = 0x08; i <= 0xff; i += 0x10, j += 0x10)
            {
                regCmd(opcodes, i, String.Format("RST %02XH", j)).load("PC").push().forceFinish().loadWord(j)
                    .store("PC");
            }

            regCmd(opcodes, 0xd9, "RETI").pop().forceFinish().store("PC").switchInterrupts(true, false);

            regLoad(opcodes, 0xe2, "(C)", "A");
            regLoad(opcodes, 0xf2, "A", "(C)");

            regCmd(opcodes, 0xe9, "JP (HL)").load("HL").store("PC");

            regCmd(opcodes, 0xe0, "LDH (a8),A").copyByte("(a8)", "A");
            regCmd(opcodes, 0xf0, "LDH A,(a8)").copyByte("A", "(a8)");

            regCmd(opcodes, 0xe8, "ADD SP,r8").load("SP").alu("ADD_SP", "r8").extraCycle().store("SP");
            regCmd(opcodes, 0xf8, "LD HL,SP+r8").load("SP").alu("ADD_SP", "r8").store("HL");

            regLoad(opcodes, 0xea, "(a16)", "A");
            regLoad(opcodes, 0xfa, "A", "(a16)");

            regCmd(opcodes, 0xf3, "DI").switchInterrupts(false, true);
            regCmd(opcodes, 0xfb, "EI").switchInterrupts(true, true);

            regLoad(opcodes, 0xf9, "SP", "HL").extraCycle();

            foreach (var o in indexedList(0x00, 0x08, "RLC", "RRC", "RL", "RR", "SLA", "SRA", "SWAP", "SRL"))
            {
                foreach (var t in indexedList(o.Key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
                {
                    regCmd(extOpcodes, t, o.Value + " {}").load(t.Value).alu(o.Value).store(t.Value);
                }
            }

            foreach (var o in indexedList(0x40, 0x40, "BIT", "RES", "SET"))
            {
                for (var b = 0; b < 0x08; b++)
                {
                    foreach (var t in indexedList(o.Key + b * 0x08, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
                    {
                        if ("BIT".Equals(o.Value) && "(HL)".Equals(t.Value))
                        {
                            regCmd(extOpcodes, t, String.Format("BIT %d,(HL)", b)).bitHL(b);
                        }
                        else
                        {
                            regCmd(extOpcodes, t, String.Format("%s %d,%s", o.Value, b, t.Value)).load(t.Value)
                                .alu(o.Value, b).store(t.Value);
                        }
                    }
                }
            }
            
            var commands = new Dictionary<int, Opcode>(0x100);
            var extCommands = new Dictionary<int, Opcode>(0x100);

            foreach (var b in opcodes)
            {
                commands.Add(b.getOpcode(), b.build());
            }

            foreach (var b in extOpcodes)
            {
                extCommands.Add(b.getOpcode(), b.build());
            }

            COMMANDS = commands;
            EXT_COMMANDS = extCommands;
        }

        private static OpcodeBuilder regLoad(OpcodeBuilder[] commands, int opcode, String target, String source)
        {
            return regCmd(commands, opcode, String.Format("LD %s,%s", target, source)).copyByte(target, source);
        }

        private static OpcodeBuilder regCmd(OpcodeBuilder[] commands, int opcode, String label)
        {
            if (commands[opcode] != null)
            {
                throw new InvalidOperationException(String.Format("Opcode %02X already exists: %s", opcode,
                    commands[opcode]));
            }

            var builder = new OpcodeBuilder(opcode, label);
            commands[opcode] = builder;
            return builder;
        }

        private static OpcodeBuilder regCmd(OpcodeBuilder[] commands, KeyValuePair<int, string> opcode, string label)
        {
            return regCmd(commands, opcode.Key, label.Replace("{}", opcode.Value));
        }

        private static Dictionary<int, string> indexedList(int start, int step, params string[] values)
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