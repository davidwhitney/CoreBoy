namespace CoreBoy.serial
{
    public interface SerialEndpoint
    {
        bool externalClockPulsed();
        int transfer(int outgoing);
    }


    public class NullSerialEndpoint : SerialEndpoint
    {
        public bool externalClockPulsed() => false;

        public int transfer(int outgoing)
        {
            return (outgoing << 1) & 0xFF;
        }
    }
}