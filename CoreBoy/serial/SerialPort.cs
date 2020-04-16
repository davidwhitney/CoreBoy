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
        private bool _transferInProgress;
        private int _divider;

        public SerialPort(InterruptManager interruptManager, SerialEndpoint serialEndpoint, SpeedMode speedMode)
        {
            _interruptManager = interruptManager;
            _serialEndpoint = serialEndpoint;
            _speedMode = speedMode;
        }

        public void Tick()
        {
            if (!_transferInProgress)
            {
                return;
            }
            
            if (++_divider >= Gameboy.TicksPerSec / 8192 / _speedMode.GetSpeedMode())
            {
                _transferInProgress = false;
                try
                {
                    _sb = _serialEndpoint.transfer(_sb);
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"Can't transfer byte {e}");
                    _sb = 0;
                }

                _interruptManager.RequestInterrupt(InterruptManager.InterruptType.Serial);
            }
        }

        public bool Accepts(int address)
        {
            return address == 0xff01 || address == 0xff02;
        }
        
        public void SetByte(int address, int value)
        {
            if (address == 0xff01)
            {
                _sb = value;
            }
            else if (address == 0xff02)
            {
                _sc = value;
                if ((_sc & (1 << 7)) != 0)
                {
                    StartTransfer();
                }
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

        private void StartTransfer()
        {
            _transferInProgress = true;
            _divider = 0;
        }
    }
}