using System;
using CoreBoy.gpu;

namespace CoreBoy.memory
{
    public class Hdma : AddressSpace
    {
        private const int Hdma1 = 0xff51;
        private const int Hdma2 = 0xff52;
        private const int Hdma3 = 0xff53;
        private const int Hdma4 = 0xff54;
        private const int Hdma5 = 0xff55;

        private readonly AddressSpace _addressSpace;
        private readonly Ram _hdma1234 = new Ram(Hdma1, 4);
        private Gpu.Mode? _gpuMode;

        private bool _transferInProgress;
        private bool _hblankTransfer;
        private bool _lcdEnabled;

        private int _length;
        private int _src;
        private int _dst;
        private int _tick;

        public Hdma(AddressSpace addressSpace) => _addressSpace = addressSpace;
        public bool accepts(int address) => address >= Hdma1 && address <= Hdma5;

        public void Tick()
        {
            if (!IsTransferInProgress())
            {
                return;
            }

            if (++_tick < 0x20)
            {
                return;
            }

            for (var j = 0; j < 0x10; j++)
            {
                _addressSpace.setByte(_dst + j, _addressSpace.getByte(_src + j));
            }

            _src += 0x10;
            _dst += 0x10;
            if (_length-- == 0)
            {
                _transferInProgress = false;
                _length = 0x7f;
            }
            else if (_hblankTransfer)
            {
                _gpuMode = null; // wait until next HBlank
            }
        }

        public void setByte(int address, int value)
        {
            if (_hdma1234.accepts(address))
            {
                _hdma1234.setByte(address, value);
            }
            else if (address == Hdma5)
            {
                if (_transferInProgress && (address & (1 << 7)) == 0)
                {
                    StopTransfer();
                }
                else
                {
                    StartTransfer(value);
                }
            }
        }

        public int getByte(int address)
        {
            if (_hdma1234.accepts(address))
            {
                return 0xff;
            }

            if (address == Hdma5)
            {
                return (_transferInProgress ? 0 : (1 << 7)) | _length;
            }

            throw new ArgumentException();
        }

        public void OnGpuUpdate(Gpu.Mode newGpuMode) => _gpuMode = newGpuMode;
        public void OnLcdSwitch(bool lcdEnabled) => _lcdEnabled = lcdEnabled;

        public bool IsTransferInProgress()
        {
            if (!_transferInProgress)
            {
                return false;
            }

            if (_hblankTransfer && (_gpuMode == Gpu.Mode.HBlank || !_lcdEnabled))
            {
                return true;
            }

            return !_hblankTransfer;
        }

        private void StartTransfer(int reg)
        {
            _hblankTransfer = (reg & (1 << 7)) != 0;
            _length = reg & 0x7f;

            _src = (_hdma1234.getByte(Hdma1) << 8) | (_hdma1234.getByte(Hdma2) & 0xf0);
            _dst = ((_hdma1234.getByte(Hdma3) & 0x1f) << 8) | (_hdma1234.getByte(Hdma4) & 0xf0);
            _src = _src & 0xfff0;
            _dst = (_dst & 0x1fff) | 0x8000;

            _transferInProgress = true;
        }

        private void StopTransfer() => _transferInProgress = false;
    }
}