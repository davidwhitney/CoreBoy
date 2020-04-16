using System;

namespace CoreBoy.sound
{
    public class PolynomialCounter
    {
        private int _i;
        private int _shiftedDivisor;

        public void SetNr43(int value)
        {
            var clockShift = value >> 4;
            
            var divisor = (value & 0b111) switch
            {
                0 => 8,
                1 => 16,
                2 => 32,
                3 => 48,
                4 => 64,
                5 => 80,
                6 => 96,
                7 => 112,
                _ => throw new InvalidOperationException()
            };

            _shiftedDivisor = divisor << clockShift;
            _i = 1;
        }

        public bool Tick()
        {
            if (--_i == 0)
            {
                _i = _shiftedDivisor;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}