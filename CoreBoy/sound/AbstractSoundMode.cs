using System;

namespace CoreBoy.sound
{
    public abstract class AbstractSoundMode : AddressSpace
    {

        protected readonly int offset;

        protected readonly bool gbc;

        protected bool channelEnabled;

        protected bool dacEnabled;

        protected int nr0, nr1, nr2, nr3, nr4;

        protected LengthCounter length;

        public AbstractSoundMode(int offset, int length, bool gbc)
        {
            this.offset = offset;
            this.length = new LengthCounter(length);
            this.gbc = gbc;
        }

        public abstract int tick();

        protected abstract void trigger();

        public bool isEnabled()
        {
            return channelEnabled && dacEnabled;
        }

        public virtual bool accepts(int address)
        {
            return address >= offset && address < offset + 5;
        }

        public virtual void setByte(int address, int value)
        {
            switch (address - offset)
            {
                case 0:
                    setNr0(value);
                    break;

                case 1:
                    setNr1(value);
                    break;

                case 2:
                    setNr2(value);
                    break;

                case 3:
                    setNr3(value);
                    break;

                case 4:
                    setNr4(value);
                    break;
            }
        }

        public virtual int getByte(int address)
        {
            switch (address - offset)
            {
                case 0:
                    return getNr0();

                case 1:
                    return getNr1();

                case 2:
                    return getNr2();

                case 3:
                    return getNr3();

                case 4:
                    return getNr4();

                default:
                    throw new ArgumentException("Illegal address for sound mode: " + Integer.ToHexString(address));
            }
        }

        protected virtual void setNr0(int value)
        {
            nr0 = value;
        }

        protected virtual void setNr1(int value)
        {
            nr1 = value;
        }

        protected virtual void setNr2(int value)
        {
            nr2 = value;
        }

        protected virtual void setNr3(int value)
        {
            nr3 = value;
        }

        protected virtual void setNr4(int value)
        {
            nr4 = value;
            length.setNr4(value);
            if ((value & (1 << 7)) != 0)
            {
                channelEnabled = dacEnabled;
                trigger();
            }
        }

        protected virtual int getNr0()
        {
            return nr0;
        }

        protected virtual int getNr1()
        {
            return nr1;
        }

        protected virtual int getNr2()
        {
            return nr2;
        }

        protected virtual int getNr3()
        {
            return nr3;
        }

        protected virtual int getNr4()
        {
            return nr4;
        }

        protected virtual int getFrequency()
        {
            return 2048 - (getNr3() | ((getNr4() & 0b111) << 8));
        }

        public abstract void start();

        public void stop()
        {
            channelEnabled = false;

        }

        protected bool updateLength()
        {
            length.tick();
            if (!length.isEnabled())
            {
                return channelEnabled;
            }

            if (channelEnabled && length.getValue() == 0)
            {
                channelEnabled = false;
            }

            return channelEnabled;
        }
    }
}