using System;
using System.Diagnostics;
using System.IO;
using CoreBoy.cpu;

namespace CoreBoy.serial
{
    public class SerialPort : IAddressSpace
    {
        private readonly SerialEndpoint _serialEndpoint;
        private readonly InterruptManager _interruptManager;
        private readonly SpeedMode _speedMode;
        private int _sb;
        private int _sc;
        private int _divider;
        private int _shiftClock;

        public SerialPort(InterruptManager interruptManager, SerialEndpoint serialEndpoint, SpeedMode speedMode)
        {
            _interruptManager = interruptManager;
            _serialEndpoint = serialEndpoint;
            _speedMode = speedMode;
        }

        public void Tick()
        {
            if (!TransferInProgress)
            {
                return;
            }
            
            if (++_divider >= Gameboy.TicksPerSec / 8192 / (FastMode ? 4 : 1) / _speedMode.GetSpeedMode())
            {
                var clockPulsed = false;
                if (InternalClockEnabled || _serialEndpoint.externalClockPulsed())
                {
                    _shiftClock++;
                    clockPulsed = true;
                }

                if (_shiftClock >= 8)
                {
                    TransferInProgress = false;
                    _interruptManager.RequestInterrupt(InterruptManager.InterruptType.Serial);
                    return;
                }

                if (clockPulsed)
                {
                    try
                    {
                        _sb = _serialEndpoint.transfer(_sb);
                    }
                    catch (IOException e)
                    {
                        Debug.WriteLine($"Can't transfer byte {e}");
                        _sb = 0;
                    }
                }

                _divider = 0;
            }
        }

        public bool Accepts(int address)
        {
            return address == 0xff01 || address == 0xff02;
        }
        
        public void SetByte(int address, int value)
        {
            if (address == 0xff01 && !TransferInProgress)
            {
                _sb = value;
            }
            else if (address == 0xff02)
            {
                TransferInProgress = value.GetBit(7);
                FastMode = value.GetBit(1);
                InternalClockEnabled = value.GetBit(0);
            }
        }

        public int GetByte(int address)
        {
            if (address == 0xff01)
            {
                return _sb;
            }
            else if (address == 0xff02)
            {
                return _sc | 0b01111110;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private bool TransferInProgress
        {
            get => (_sc & (1 << 7)) != 0;
            set
            {
                if (value)
                {
                    _sc = _sc.SetBit(7);
                    _divider = 0;
                    _shiftClock = 0;
                }
                else
                {
                    _sc = _sc.ClearBit(7);
                }
            }
        }

        private bool FastMode
        {
            get => (_sc & 2) != 0;
            set
            {
                if (value)
                {
                    _sc = _sc.SetBit(1);
                }
                else
                {
                    _sc = _sc.ClearBit(1);
                }
            }
        }

        private bool InternalClockEnabled
        {
            get => (_sc & 1) != 0;
            set
            {
                if (value)
                {
                    _sc = _sc.SetBit(0);
                }
                else
                {
                    _sc = _sc.ClearBit(0);
                }
            }
        }
    }
}