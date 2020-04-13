using System;
using System.Collections.Generic;
using CoreBoy.cpu.op;
using CoreBoy.cpu.opcode;
using CoreBoy.gpu;

namespace CoreBoy.cpu
{
    public class Cpu
    {
        public enum State
        {
            OPCODE,
            EXT_OPCODE,
            OPERAND,
            RUNNING,
            IRQ_READ_IF,
            IRQ_READ_IE,
            IRQ_PUSH_1,
            IRQ_PUSH_2,
            IRQ_JUMP,
            STOPPED,
            HALTED
        }

        private readonly Registers registers;

        private readonly AddressSpace addressSpace;

        private readonly InterruptManager interruptManager;

        private readonly Gpu gpu;

        private readonly IDisplay display;

        private readonly SpeedMode speedMode;

        private int opcode1, opcode2;

        private int[] operand = new int[2];

        private Opcode currentOpcode;

        private List<Op> ops;

        private int operandIndex;

        private int opIndex;

        private State state = State.OPCODE;

        private int opContext;

        private int interruptFlag;

        private int interruptEnabled;

        private InterruptManager.InterruptType requestedIrq;

        private int clockCycle = 0;

        private bool haltBugMode;
        private Opcodes _opcodes;

        public Cpu(AddressSpace addressSpace, InterruptManager interruptManager, Gpu gpu, IDisplay display,
            SpeedMode speedMode)
        {
            this._opcodes = new Opcodes();
            this.registers = new Registers();
            this.addressSpace = addressSpace;
            this.interruptManager = interruptManager;
            this.gpu = gpu;
            this.display = display;
            this.speedMode = speedMode;
        }

        public void tick()
        {
            if (++clockCycle >= (4 / speedMode.getSpeedMode()))
            {
                clockCycle = 0;
            }
            else
            {
                return;
            }

            if (state == State.OPCODE || state == State.HALTED || state == State.STOPPED)
            {
                if (interruptManager.isIme() && interruptManager.isInterruptRequested())
                {
                    if (state == State.STOPPED)
                    {
                        display.EnableLcd();
                    }

                    state = State.IRQ_READ_IF;
                }
            }

            if (state == State.IRQ_READ_IF || state == State.IRQ_READ_IE || state == State.IRQ_PUSH_1 ||
                state == State.IRQ_PUSH_2 || state == State.IRQ_JUMP)
            {
                handleInterrupt();
                return;
            }

            if (state == State.HALTED && interruptManager.isInterruptRequested())
            {
                state = State.OPCODE;
            }

            if (state == State.HALTED || state == State.STOPPED)
            {
                return;
            }

            bool accessedMemory = false;
            while (true)
            {
                int pc = registers.getPC();
                switch (state)
                {
                    case State.OPCODE:
                        clearState();
                        opcode1 = addressSpace.getByte(pc);
                        accessedMemory = true;
                        if (opcode1 == 0xcb)
                        {
                            state = State.EXT_OPCODE;
                        }
                        else if (opcode1 == 0x10)
                        {
                            currentOpcode = _opcodes.COMMANDS[opcode1];
                            state = State.EXT_OPCODE;
                        }
                        else
                        {
                            state = State.OPERAND;
                            currentOpcode = _opcodes.COMMANDS[opcode1];
                            if (currentOpcode == null)
                            {
                                throw new InvalidOperationException(String.Format("No command for 0x%02x", opcode1));
                            }
                        }

                        if (!haltBugMode)
                        {
                            registers.incrementPC();
                        }
                        else
                        {
                            haltBugMode = false;
                        }

                        break;

                    case State.EXT_OPCODE:
                        if (accessedMemory)
                        {
                            return;
                        }

                        accessedMemory = true;
                        opcode2 = addressSpace.getByte(pc);
                        if (currentOpcode == null)
                        {
                            currentOpcode = _opcodes.EXT_COMMANDS[opcode2];
                        }

                        if (currentOpcode == null)
                        {
                            throw new InvalidOperationException(String.Format("No command for %0xcb 0x%02x", opcode2));
                        }

                        state = State.OPERAND;
                        registers.incrementPC();
                        break;

                    case State.OPERAND:
                        while (operandIndex < currentOpcode.getOperandLength())
                        {
                            if (accessedMemory)
                            {
                                return;
                            }

                            accessedMemory = true;
                            operand[operandIndex++] = addressSpace.getByte(pc);
                            registers.incrementPC();
                        }

                        ops = currentOpcode.getOps();
                        state = State.RUNNING;
                        break;

                    case State.RUNNING:
                        if (opcode1 == 0x10)
                        {
                            if (speedMode.onStop())
                            {
                                state = State.OPCODE;
                            }
                            else
                            {
                                state = State.STOPPED;
                                display.DisableLcd();
                            }

                            return;
                        }
                        else if (opcode1 == 0x76)
                        {
                            if (interruptManager.isHaltBug())
                            {
                                state = State.OPCODE;
                                haltBugMode = true;
                                return;
                            }
                            else
                            {
                                state = State.HALTED;
                                return;
                            }
                        }

                        if (opIndex < ops.Count)
                        {
                            Op op = ops[opIndex];
                            bool opAccessesMemory = op.readsMemory() || op.writesMemory();
                            if (accessedMemory && opAccessesMemory)
                            {
                                return;
                            }

                            opIndex++;

                            SpriteBug.CorruptionType? corruptionType = op.causesOemBug(registers, opContext);
                            if (corruptionType != null)
                            {
                                handleSpriteBug(corruptionType.Value);
                            }

                            opContext = op.execute(registers, addressSpace, operand, opContext);
                            op.switchInterrupts(interruptManager);

                            if (!op.proceed(registers))
                            {
                                opIndex = ops.Count;
                                break;
                            }

                            if (op.forceFinishCycle())
                            {
                                return;
                            }

                            if (opAccessesMemory)
                            {
                                accessedMemory = true;
                            }
                        }

                        if (opIndex >= ops.Count)
                        {
                            state = State.OPCODE;
                            operandIndex = 0;
                            interruptManager.onInstructionFinished();
                            return;
                        }

                        break;

                    case State.HALTED:
                    case State.STOPPED:
                        return;
                }
            }
        }

        private void handleInterrupt()
        {
            switch (state)
            {
                case State.IRQ_READ_IF:
                    interruptFlag = addressSpace.getByte(0xff0f);
                    state = State.IRQ_READ_IE;
                    break;

                case State.IRQ_READ_IE:
                    interruptEnabled = addressSpace.getByte(0xffff);
                    requestedIrq = null;
                    foreach (InterruptManager.InterruptType irq in InterruptManager.InterruptType.values())
                    {
                        if ((interruptFlag & interruptEnabled & (1 << irq.ordinal())) != 0)
                        {
                            requestedIrq = irq;
                            break;
                        }
                    }

                    if (requestedIrq == null)
                    {
                        state = State.OPCODE;
                    }
                    else
                    {
                        state = State.IRQ_PUSH_1;
                        interruptManager.clearInterrupt(requestedIrq);
                        interruptManager.disableInterrupts(false);
                    }

                    break;

                case State.IRQ_PUSH_1:
                    registers.decrementSP();
                    addressSpace.setByte(registers.getSP(), (registers.getPC() & 0xff00) >> 8);
                    state = State.IRQ_PUSH_2;
                    break;

                case State.IRQ_PUSH_2:
                    registers.decrementSP();
                    addressSpace.setByte(registers.getSP(), registers.getPC() & 0x00ff);
                    state = State.IRQ_JUMP;
                    break;

                case State.IRQ_JUMP:
                    registers.setPC(requestedIrq.getHandler());
                    requestedIrq = null;
                    state = State.OPCODE;
                    break;

            }
        }

        private void handleSpriteBug(SpriteBug.CorruptionType type)
        {
            if (!gpu.GetLcdc().isLcdEnabled())
            {
                return;
            }

            int stat = addressSpace.getByte(GpuRegister.STAT.getAddress());
            if ((stat & 0b11) == (int) Gpu.Mode.OamSearch && gpu.GetTicksInLine() < 79)
            {
                SpriteBug.corruptOam(addressSpace, type, gpu.GetTicksInLine());
            }
        }

        public Registers getRegisters()
        {
            return registers;
        }

        void clearState()
        {
            opcode1 = 0;
            opcode2 = 0;
            currentOpcode = null;
            ops = null;

            operand[0] = 0x00;
            operand[1] = 0x00;
            operandIndex = 0;

            opIndex = 0;
            opContext = 0;

            interruptFlag = 0;
            interruptEnabled = 0;
            requestedIrq = null;
        }


        public State getState()
        {
            return state;
        }

        Opcode getCurrentOpcode()
        {
            return currentOpcode;
        }

    }
}