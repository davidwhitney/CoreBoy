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

        private readonly Registers _registers;
        private readonly AddressSpace _addressSpace;
        private readonly InterruptManager _interruptManager;
        private readonly Gpu _gpu;
        private readonly IDisplay _display;
        private readonly SpeedMode _speedMode;

        private int _opcode1;
        private int _opcode2;
        private readonly int[] _operand = new int[2];
        private Opcode _currentOpcode;
        private List<Op> _ops;
        private int _operandIndex;
        private int _opIndex;

        private State _state = State.OPCODE;

        private int _opContext;
        private int _interruptFlag;
        private int _interruptEnabled;

        private InterruptManager.InterruptType _requestedIrq;

        private int _clockCycle;
        private bool _haltBugMode;
        private readonly Opcodes _opcodes;

        public Cpu(AddressSpace addressSpace, InterruptManager interruptManager, Gpu gpu, IDisplay display, SpeedMode speedMode)
        {
            _opcodes = new Opcodes();
            _registers = new Registers();
            _addressSpace = addressSpace;
            _interruptManager = interruptManager;
            _gpu = gpu;
            _display = display;
            _speedMode = speedMode;
        }

        public void Tick()
        {
            if (++_clockCycle >= (4 / _speedMode.GetSpeedMode()))
            {
                _clockCycle = 0;
            }
            else
            {
                return;
            }

            if (_state == State.OPCODE || _state == State.HALTED || _state == State.STOPPED)
            {
                if (_interruptManager.IsIme() && _interruptManager.IsInterruptRequested())
                {
                    if (_state == State.STOPPED)
                    {
                        _display.EnableLcd();
                    }

                    _state = State.IRQ_READ_IF;
                }
            }

            switch (_state)
            {
                case State.IRQ_READ_IF:
                case State.IRQ_READ_IE:
                case State.IRQ_PUSH_1:
                case State.IRQ_PUSH_2:
                case State.IRQ_JUMP:
                    HandleInterrupt();
                    return;
                case State.HALTED when _interruptManager.IsInterruptRequested():
                    _state = State.OPCODE;
                    break;
            }

            if (_state == State.HALTED || _state == State.STOPPED)
            {
                return;
            }

            var accessedMemory = false;
            while (true)
            {
                var pc = _registers.PC;
                switch (_state)
                {
                    case State.OPCODE:
                        ClearState();
                        _opcode1 = _addressSpace.getByte(pc);
                        accessedMemory = true;
                        if (_opcode1 == 0xcb)
                        {
                            _state = State.EXT_OPCODE;
                        }
                        else if (_opcode1 == 0x10)
                        {
                            _currentOpcode = _opcodes.COMMANDS[_opcode1];
                            _state = State.EXT_OPCODE;
                        }
                        else
                        {
                            _state = State.OPERAND;
                            _currentOpcode = _opcodes.COMMANDS[_opcode1];
                            if (_currentOpcode == null)
                            {
                                throw new InvalidOperationException(String.Format("No command for 0x%02x", _opcode1));
                            }
                        }

                        if (!_haltBugMode)
                        {
                            _registers.IncrementPc();
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
                        _opcode2 = _addressSpace.getByte(pc);
                        if (_currentOpcode == null)
                        {
                            _currentOpcode = _opcodes.EXT_COMMANDS[_opcode2];
                        }

                        if (_currentOpcode == null)
                        {
                            throw new InvalidOperationException(String.Format("No command for %0xcb 0x%02x", _opcode2));
                        }

                        _state = State.OPERAND;
                        _registers.IncrementPc();
                        break;

                    case State.OPERAND:
                        while (_operandIndex < _currentOpcode.Length)
                        {
                            if (accessedMemory)
                            {
                                return;
                            }

                            accessedMemory = true;
                            _operand[_operandIndex++] = _addressSpace.getByte(pc);
                            _registers.IncrementPc();
                        }

                        _ops = _currentOpcode.Ops;
                        _state = State.RUNNING;
                        break;

                    case State.RUNNING:
                        if (_opcode1 == 0x10)
                        {
                            if (_speedMode.OnStop())
                            {
                                _state = State.OPCODE;
                            }
                            else
                            {
                                _state = State.STOPPED;
                                _display.DisableLcd();
                            }

                            return;
                        }
                        else if (_opcode1 == 0x76)
                        {
                            if (_interruptManager.IsHaltBug())
                            {
                                _state = State.OPCODE;
                                _haltBugMode = true;
                                return;
                            }
                            else
                            {
                                _state = State.HALTED;
                                return;
                            }
                        }

                        if (_opIndex < _ops.Count)
                        {
                            var op = _ops[_opIndex];
                            var opAccessesMemory = op.readsMemory() || op.writesMemory();
                            if (accessedMemory && opAccessesMemory)
                            {
                                return;
                            }

                            _opIndex++;

                            var corruptionType = op.causesOemBug(_registers, _opContext);
                            if (corruptionType != null)
                            {
                                HandleSpriteBug(corruptionType.Value);
                            }

                            _opContext = op.execute(_registers, _addressSpace, _operand, _opContext);
                            op.switchInterrupts(_interruptManager);

                            if (!op.proceed(_registers))
                            {
                                _opIndex = _ops.Count;
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

                        if (_opIndex >= _ops.Count)
                        {
                            _state = State.OPCODE;
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
            switch (_state)
            {
                case State.IRQ_READ_IF:
                    _interruptFlag = _addressSpace.getByte(0xff0f);
                    _state = State.IRQ_READ_IE;
                    break;

                case State.IRQ_READ_IE:
                    _interruptEnabled = _addressSpace.getByte(0xffff);
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
                        _state = State.OPCODE;
                    }
                    else
                    {
                        _state = State.IRQ_PUSH_1;
                        _interruptManager.ClearInterrupt(_requestedIrq);
                        _interruptManager.DisableInterrupts(false);
                    }

                    break;

                case State.IRQ_PUSH_1:
                    _registers.DecrementSp();
                    _addressSpace.setByte(_registers.SP, (_registers.PC & 0xff00) >> 8);
                    _state = State.IRQ_PUSH_2;
                    break;

                case State.IRQ_PUSH_2:
                    _registers.DecrementSp();
                    _addressSpace.setByte(_registers.SP, _registers.PC & 0x00ff);
                    _state = State.IRQ_JUMP;
                    break;

                case State.IRQ_JUMP:
                    _registers.PC = _requestedIrq.Handler;
                    _requestedIrq = null;
                    _state = State.OPCODE;
                    break;

            }
        }

        private void HandleSpriteBug(SpriteBug.CorruptionType type)
        {
            if (!_gpu.GetLcdc().isLcdEnabled())
            {
                return;
            }

            var stat = _addressSpace.getByte(GpuRegister.STAT.GetAddress());
            if ((stat & 0b11) == (int) Gpu.Mode.OamSearch && _gpu.GetTicksInLine() < 79)
            {
                SpriteBug.CorruptOam(_addressSpace, type, _gpu.GetTicksInLine());
            }
        }

        public Registers GetRegisters()
        {
            return _registers;
        }

        private void ClearState()
        {
            _opcode1 = 0;
            _opcode2 = 0;
            _currentOpcode = null;
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