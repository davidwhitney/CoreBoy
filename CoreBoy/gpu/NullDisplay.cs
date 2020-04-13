namespace CoreBoy.gpu
{
    public class NullDisplay : IDisplay
    {
        public void PutDmgPixel(int color)
        {
        }

        public void PutColorPixel(int gbcRgb)
        {
        }

        public void RequestRefresh()
        {
        }

        public void WaitForRefresh()
        {
        }

        public void EnableLcd()
        {
        }

        public void DisableLcd()
        {
        }
    }
}