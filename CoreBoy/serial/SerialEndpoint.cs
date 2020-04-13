namespace CoreBoy.serial
{
    public interface SerialEndpoint
    {
        int transfer(int outgoing);
    }


    public class NullSerialEndpoint : SerialEndpoint
    {
        public int transfer(int outgoing)
        {
            return 0;
        }
    }
}