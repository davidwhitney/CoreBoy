using System;
using System.IO;
using eu.rekawek.coffeegb.cpu;

namespace eu.rekawek.coffeegb.serial
{


    public class SerialPort : AddressSpace
    {

        // private static readonly Logger LOG = LoggerFactory.getLogger(SerialPort.class);

        private readonly SerialEndpoint serialEndpoint;

        private readonly InterruptManager interruptManager;

        private readonly SpeedMode speedMode;

        private int sb;

        private int sc;

        private bool transferInProgress;

        private int divider;

        public SerialPort(InterruptManager interruptManager, SerialEndpoint serialEndpoint, SpeedMode speedMode)
        {
            this.interruptManager = interruptManager;
            this.serialEndpoint = serialEndpoint;
            this.speedMode = speedMode;
        }

        public void tick()
        {
            if (!transferInProgress)
            {
                return;
            }
            
            //if (++divider >= Gameboy.TICKS_PER_SEC / 8192 / speedMode.getSpeedMode())
            int TICKS_PER_SEC = 4_194_304;
            if (++divider >= TICKS_PER_SEC / 8192 / speedMode.getSpeedMode())
            {
                transferInProgress = false;
                try
                {
                    sb = serialEndpoint.transfer(sb);
                }
                catch (IOException e)
                {
                    //LOG.error("Can't transfer byte", e);
                    sb = 0;
                }

                interruptManager.requestInterrupt(InterruptManager.InterruptType.Serial);
            }
        }


        public bool accepts(int address)
        {
            return address == 0xff01 || address == 0xff02;
        }

        

        public void setByte(int address, int value)
        {
            if (address == 0xff01)
            {
                sb = value;
            }
            else if (address == 0xff02)
            {
                sc = value;
                if ((sc & (1 << 7)) != 0)
                {
                    startTransfer();
                }
            }
        }

        

        public int getByte(int address)
        {
            if (address == 0xff01)
            {
                return sb;
            }
            else if (address == 0xff02)
            {
                return sc | 0b01111110;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private void startTransfer()
        {
            transferInProgress = true;
            divider = 0;
        }
    }
}