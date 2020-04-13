using CoreBoy.cpu;

namespace CoreBoy.memory
{
    public class Dma : AddressSpace
    {
        private readonly AddressSpace _addressSpace;
        private readonly AddressSpace _oam;
        private readonly SpeedMode _speedMode;

        private bool _transferInProgress;
        private bool _restarted;
        private int _from;
        private int _ticks;
        private int _regValue = 0xff;

        public Dma(AddressSpace addressSpace, AddressSpace oam, SpeedMode speedMode)
        {
            _addressSpace = new DmaAddressSpace(addressSpace);
            _speedMode = speedMode;
            _oam = oam;
        }

        public bool accepts(int address)
        {
            return address == 0xff46;
        }

        public void Tick()
        {
            if (!_transferInProgress) return;
            if (++_ticks < 648 / _speedMode.getSpeedMode()) return;

            _transferInProgress = false;
            _restarted = false;
            _ticks = 0;
            
            for (var i = 0; i < 0xa0; i++)
            {
                _oam.setByte(0xfe00 + i, _addressSpace.getByte(_from + i));
            }
        }

        public void setByte(int address, int value)
        {
            _from = value * 0x100;
            _restarted = IsOamBlocked();
            _ticks = 0;
            _transferInProgress = true;
            _regValue = value;
        }

        public int getByte(int address) => _regValue;
        public bool IsOamBlocked() => _restarted || _transferInProgress && _ticks >= 5;
    }
}