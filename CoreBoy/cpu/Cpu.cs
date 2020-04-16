using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CoreBoy.cpu.op;
using CoreBoy.cpu.opcode;
using CoreBoy.gpu;

namespace CoreBoy.cpu
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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

    public class Cpu
    {
        public Registers Registers { get; set; }
        public Opcode CurrentOpcode { get; private set; }
        public State State { get; private set; } = State.OPCODE;

        private readonly IAddressSpace _addressSpace;
        private readonly InterruptManager _interruptManager;
        private readonly Gpu _gpu;
        private readonly IDisplay _display;
        private readonly SpeedMode _speedMode;

        private int _opcode1;
        private int _opcode2;
        private readonly int[] _operand = new int[2];
        private List<Op> _ops;
        private int _operandIndex;
        private int _opIndex;


        private int _opContext;
        private int _interruptFlag;
        private int _interruptEnabled;

        private InterruptManager.InterruptType _requestedIrq;

        private int _clockCycle;
        private bool _haltBugMode;
        private readonly Opcodes _opcodes;

        public Cpu(IAddressSpace addressSpace, InterruptManager interruptManager, Gpu gpu, IDisplay display, SpeedMode speedMode)
        {
            _opcodes = new Opcodes();
            Registers = new Registers();
            _addressSpace = addressSpace;
            _interruptManager = interruptManager;
            _gpu = gpu;
            _display = display;
            _speedMode = speedMode;
        }

        public void Tick()
        {
            _clockCycle++;
            var speed = _speedMode.GetSpeedMode();

            if (_clockCycle >= (4 / speed))
            {
                _clockCycle = 0;
            }
            else
            {
                return;
            }

            if (State == State.OPCODE || State == State.HALTED || State == State.STOPPED)
            {
                if (_interruptManager.IsIme() && _interruptManager.IsInterruptRequested())
                {
                    if (State == State.STOPPED)
                    {
                        _display.Enabled = true;
                    }

                    State = State.IRQ_READ_IF;
                }
            }

            switch (State)
            {
                case State.IRQ_READ_IF:
                case State.IRQ_READ_IE:
                case State.IRQ_PUSH_1:
                case State.IRQ_PUSH_2:
                case State.IRQ_JUMP:
                    HandleInterrupt();
                    return;
                case State.HALTED when _interruptManager.IsInterruptRequested():
                    State = State.OPCODE;
                    break;
            }

            if (State == State.HALTED || State == State.STOPPED)
            {
                return;
            }

            var accessedMemory = false;

            while (true)
            {
                var pc = Registers.PC;
                switch (State)
                {
                    case State.OPCODE:
                        ClearState();
                        _opcode1 = _addressSpace.GetByte(pc);
                        accessedMemory = true;
                        if (_opcode1 == 0xcb)
                        {
                            State = State.EXT_OPCODE;
                        }
                        else if (_opcode1 == 0x10)
                        {
                            CurrentOpcode = _opcodes.Commands[_opcode1];
                            State = State.EXT_OPCODE;
                        }
                        else
                        {
                            State = State.OPERAND;
                            CurrentOpcode = _opcodes.Commands[_opcode1];
                            if (CurrentOpcode == null)
                            {
                                throw new InvalidOperationException($"No command for 0x{_opcode1:X2}");
                            }
                        }

                        if (!_haltBugMode)
                        {
                            Registers.IncrementPc();
                        }
                        else
                        {
                            _haltBugMode = false;
                        }

                        break;

                    case State.EXT_OPCODE:
                        if (accessedMemory)
                        {
                            return;
                        }

                        accessedMemory = true;
                        _opcode2 = _addressSpace.GetByte(pc);
                        CurrentOpcode ??= _opcodes.ExtCommands[_opcode2];

                        if (CurrentOpcode == null)
                        {
                            throw new InvalidOperationException($"No command for {_opcode2:X}cb 0x{_opcode2:X2}");
                        }

                        State = State.OPERAND;
                        Registers.IncrementPc();
                        break;

                    case State.OPERAND:
                        while (_operandIndex < CurrentOpcode.Length)
                        {
                            if (accessedMemory)
                            {
                                return;
                            }

                            accessedMemory = true;
                            _operand[_operandIndex++] = _addressSpace.GetByte(pc);
                            Registers.IncrementPc();
                        }

                        _ops = CurrentOpcode.Ops.ToList();
                        State = State.RUNNING;
                        break;

                    case State.RUNNING:
                        if (_opcode1 == 0x10)
                        {
                            if (_speedMode.OnStop())
                            {
                                State = State.OPCODE;
                            }
                            else
                            {
                                State = State.STOPPED;
                                _display.Enabled = false;
                            }

                            return;
                        }
                        else if (_opcode1 == 0x76)
                        {
                            if (_interruptManager.IsHaltBug())
                            {
                                State = State.OPCODE;
                                _haltBugMode = true;
                                return;
                            }
                            else
                            {
                                State = State.HALTED;
                                return;
                            }
                        }

                        if (_opIndex < _ops.Count)
                        {
                            var op = _ops[_opIndex];
                            var opAccessesMemory = op.ReadsMemory() || op.WritesMemory();
                            if (accessedMemory && opAccessesMemory)
                            {
                                return;
                            }

                            _opIndex++;

                            var corruptionType = op.CausesOemBug(Registers, _opContext);
                            if (corruptionType != null)
                            {
                                HandleSpriteBug(corruptionType.Value);
                            }
                            
                            _opContext = op.Execute(Registers, _addressSpace, _operand, _opContext);
                            op.SwitchInterrupts(_interruptManager);

                            if (!op.Proceed(Registers))
                            {
                                _opIndex = _ops.Count;
                                break;
                            }

                            if (op.ForceFinishCycle())
                            {
                                return;
                            }

                            if (opAccessesMemory)
                            {
                                accessedMemory = true;
                            }
                        }

                        if (_opIndex >= _ops.Count)
                        {
                            State = State.OPCODE;
                            _operandIndex = 0;
                            _interruptManager.OnInstructionFinished();
                            return;
                        }

                        break;

                    case State.HALTED:
                    case State.STOPPED:
                        return;
                }
            }
        }

        private void HandleInterrupt()
        {
            switch (State)
            {
                case State.IRQ_READ_IF:
                    _interruptFlag = _addressSpace.GetByte(0xff0f);
                    State = State.IRQ_READ_IE;
                    break;

                case State.IRQ_READ_IE:
                    _interruptEnabled = _addressSpace.GetByte(0xffff);
                    _requestedIrq = null;
                    foreach (var irq in InterruptManager.InterruptType.Values())
                    {
                        if ((_interruptFlag & _interruptEnabled & (1 << irq.Ordinal)) != 0)
                        {
                            _requestedIrq = irq;
                            break;
                        }
                    }

                    if (_requestedIrq == null)
                    {
                        State = State.OPCODE;
                    }
                    else
                    {
                        State = State.IRQ_PUSH_1;
                        _interruptManager.ClearInterrupt(_requestedIrq);
                        _interruptManager.DisableInterrupts(false);
                    }

                    break;

                case State.IRQ_PUSH_1:
                    Registers.DecrementSp();
                    _addressSpace.SetByte(Registers.SP, (Registers.PC & 0xff00) >> 8);
                    State = State.IRQ_PUSH_2;
                    break;

                case State.IRQ_PUSH_2:
                    Registers.DecrementSp();
                    _addressSpace.SetByte(Registers.SP, Registers.PC & 0x00ff);
                    State = State.IRQ_JUMP;
                    break;

                case State.IRQ_JUMP:
                    Registers.PC = _requestedIrq.Handler;
                    _requestedIrq = null;
                    State = State.OPCODE;
                    break;
            }
        }

        private void HandleSpriteBug(SpriteBug.CorruptionType type)
        {
            if (!_gpu.GetLcdc().IsLcdEnabled())
            {
                return;
            }

            var stat = _addressSpace.GetByte(GpuRegister.Stat.Address);
            if ((stat & 0b11) == (int) Gpu.Mode.OamSearch && _gpu.GetTicksInLine() < 79)
            {
                SpriteBug.CorruptOam(_addressSpace, type, _gpu.GetTicksInLine());
            }
        }

        public void ClearState()
        {
            _opcode1 = 0;
            _opcode2 = 0;
            CurrentOpcode = null;
            _ops = null;

            _operand[0] = 0x00;
            _operand[1] = 0x00;
            _operandIndex = 0;

            _opIndex = 0;
            _opContext = 0;

            _interruptFlag = 0;
            _interruptEnabled = 0;
            _requestedIrq = null;
        }
    }
}