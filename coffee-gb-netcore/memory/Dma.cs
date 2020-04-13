using eu.rekawek.coffeegb.cpu;

namespace eu.rekawek.coffeegb.memory
{
    public class Dma : AddressSpace
    {

        private readonly AddressSpace addressSpace;

        private readonly AddressSpace oam;

        private readonly SpeedMode speedMode;

        private bool transferInProgress;

        private bool restarted;

        private int from;

        private int ticks;

        private int regValue = 0xff;

        public Dma(AddressSpace addressSpace, AddressSpace oam, SpeedMode speedMode)
        {
            this.addressSpace = new DmaAddressSpace(addressSpace);
            this.speedMode = speedMode;
            this.oam = oam;
        }

        public bool accepts(int address)
        {
            return address == 0xff46;
        }

        public void tick()
        {
            if (transferInProgress)
            {
                if (++ticks >= 648 / speedMode.getSpeedMode())
                {
                    transferInProgress = false;
                    restarted = false;
                    ticks = 0;
                    for (int i = 0; i < 0xa0; i++)
                    {
                        oam.setByte(0xfe00 + i, addressSpace.getByte(from + i));
                    }
                }
            }
        }

        public void setByte(int address, int value)
        {
            from = value * 0x100;
            restarted = isOamBlocked();
            ticks = 0;
            transferInProgress = true;
            regValue = value;
        }

        public int getByte(int address)
        {
            return regValue;
        }

        public bool isOamBlocked()
        {
            return restarted || (transferInProgress && ticks >= 5);
        }
    }
}