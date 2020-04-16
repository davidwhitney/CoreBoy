using System;
using CoreBoy.cpu;

namespace CoreBoy.timer
{
    public class Timer : IAddressSpace
    {
        private readonly SpeedMode _speedMode;
        private readonly InterruptManager _interruptManager;
        private static readonly int[] FreqToBit = {9, 3, 5, 7};

        private int _div;
        private int _tac;
        private int _tma;
        private int _tima;
        private bool _previousBit;
        private bool _overflow;
        private int _ticksSinceOverflow;

        public Timer(InterruptManager interruptManager, SpeedMode speedMode)
        {
            _speedMode = speedMode;
            _interruptManager = interruptManager;
        }

        public void Tick()
        {
            UpdateDiv((_div + 1) & 0xffff);
            if (!_overflow)
            {
                return;
            }

            _ticksSinceOverflow++;
            if (_ticksSinceOverflow == 4)
            {
                _interruptManager.RequestInterrupt(InterruptManager.InterruptType.Timer);
            }

            if (_ticksSinceOverflow == 5)
            {
                _tima = _tma;
            }

            if (_ticksSinceOverflow == 6)
            {
                _tima = _tma;
                _overflow = false;
                _ticksSinceOverflow = 0;
            }
        }

        private void IncTima()
        {
            _tima++;
            _tima %= 0x100;
            if (_tima == 0)
            {
                _overflow = true;
                _ticksSinceOverflow = 0;
            }
        }

        private void UpdateDiv(int newDiv)
        {
            _div = newDiv;
            int bitPos = FreqToBit[_tac & 0b11];
            bitPos <<= _speedMode.GetSpeedMode() - 1;
            bool bit = (_div & (1 << bitPos)) != 0;
            bit &= (_tac & (1 << 2)) != 0;
            if (!bit && _previousBit)
            {
                IncTima();
            }

            _previousBit = bit;
        }

        public bool Accepts(int address) => address >= 0xff04 && address <= 0xff07;

        public void SetByte(int address, int value)
        {
            switch (address)
            {
                case 0xff04:
                    UpdateDiv(0);
                    break;

                case 0xff05:
                    if (_ticksSinceOverflow < 5)
                    {
                        _tima = value;
                        _overflow = false;
                        _ticksSinceOverflow = 0;
                    }

                    break;

                case 0xff06:
                    _tma = value;
                    break;

                case 0xff07:
                    _tac = value;
                    break;
            }
        }

        public int GetByte(int address)
        {
            return address switch
            {
                0xff04 => _div >> 8,
                0xff05 => _tima,
                0xff06 => _tma,
                0xff07 => _tac | 0b11111000,
                _ => throw new ArgumentException()
            };
        }
    }
}