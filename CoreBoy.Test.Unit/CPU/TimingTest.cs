using System;
using System.Text;
using CoreBoy.cpu;
using CoreBoy.cpu.opcode;
using CoreBoy.gpu;
using CoreBoy.memory;
using NUnit.Framework;

namespace CoreBoy.Test.Unit.CPU
{
    [TestFixture]
    public class TimingTest
    {
        private static readonly int Offset = 0x100;
        private Ram _memory;
        private Cpu _cpu;

        [SetUp]
        public void Setup()
        {
            _memory = new Ram(0x00, 0x10000);
            _cpu = new Cpu(_memory, new InterruptManager(false), null, new NullDisplay(), new SpeedMode());
        }
        
        [Test]
        public void TestTiming()
        {
            AssertTiming(16, 0xc9, 0, 0); // RET
            AssertTiming(16, 0xd9, 0, 0); // RETI
            _cpu.Registers.Flags.SetZ(false);
            AssertTiming(20, 0xc0, 0, 0); // RET NZ
            _cpu.Registers.Flags.SetZ(true);
            AssertTiming(8, 0xc0, 0, 0); // RET NZ
            AssertTiming(24, 0xcd, 0, 0); // CALL a16
            AssertTiming(16, 0xc5); // PUSH BC
            AssertTiming(12, 0xf1); // POP AF

            AssertTiming(8, 0xd6, 00); // SUB A,d8

            _cpu.Registers.Flags.SetC(true);
            AssertTiming(8, 0x30, 00); // JR nc,r8

            _cpu.Registers.Flags.SetC(false);
            AssertTiming(12, 0x30, 00); // JR nc,r8

            _cpu.Registers.Flags.SetC(true);
            AssertTiming(12, 0xd2, 00); // JP nc,a16

            _cpu.Registers.Flags.SetC(false);
            AssertTiming(16, 0xd2, 00); // JP nc,a16

            AssertTiming(16, 0xc3, 00, 00); // JP a16

            AssertTiming(4, 0xaf); // XOR a
            AssertTiming(12, 0xe0, 0x05); // LD (ff00+05),A
            AssertTiming(12, 0xf0, 0x05); // LD A,(ff00+05)
            AssertTiming(4, 0xb7); // OR

            AssertTiming(4, 0x7b); // LDA A,E
            AssertTiming(8, 0xd6, 0x00); // SUB A,d8
            AssertTiming(8, 0xcb, 0x12); // RL D
            AssertTiming(4, 0x87); // ADD A
            AssertTiming(4, 0xf3); // DI
            AssertTiming(8, 0x32); // LD (HL-),A
            AssertTiming(12, 0x36); // LD (HL),d8
            AssertTiming(16, 0xea, 0x00, 0x00); // LD (a16),A
            AssertTiming(8, 0x09); // ADD HL,BC
            AssertTiming(16, 0xc7); // RST 00H


            AssertTiming(8, 0x3e, 0x51); // LDA A,51
            AssertTiming(4, 0x1f); // RRA
            AssertTiming(8, 0xce, 0x01); // ADC A,01
            AssertTiming(4, 0x00); // NOP
        }

        private void AssertTiming(int expectedTiming, params int[] opcodes)
        {
            for (int i = 0; i < opcodes.Length; i++)
            {
                _memory.setByte(Offset + i, opcodes[i]);
            }

            _cpu.ClearState();
            _cpu.Registers.PC = Offset;

            int ticks = 0;
            Opcode opcode = null;
            do
            {
                _cpu.Tick();
                if (opcode == null && _cpu.CurrentOpcode != null)
                {
                    opcode = _cpu.CurrentOpcode;
                }

                ticks++;
            } while (_cpu.State != State.OPCODE || ticks < 4);

            if (opcode == null)
            {
                Assert.That(expectedTiming, Is.EqualTo(ticks), "Invalid timing value for " + HexArray(opcodes));
            }
            else
            {
                Assert.That(expectedTiming, Is.EqualTo(ticks), $"Invalid timing value for [{opcode}]");
            }
        }

        private static String HexArray(int[] data)
        {
            var b = new StringBuilder("[");
            for (var i = 0; i < data.Length; i++)
            {
                b.Append($"{data[i]:X2}");
                if (i < data.Length - 1)
                {
                    b.Append(" ");
                }
            }

            b.Append(']');
            return b.ToString();
        }
    }
}