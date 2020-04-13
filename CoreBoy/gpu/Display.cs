namespace CoreBoy.gpu
{
    public interface Display
    {
        void putDmgPixel(int color);
        void putColorPixel(int gbcRgb);
        void requestRefresh();
        void waitForRefresh();
        void enableLcd();
        void disableLcd();
    }

    public class NullDisplay : Display
    {
        public void putDmgPixel(int color)
        {
        }

        public void putColorPixel(int gbcRgb)
        {
        }

        public void requestRefresh()
        {
        }

        public void waitForRefresh()
        {
        }

        public void enableLcd()
        {
        }

        public void disableLcd()
        {
        }
    }

}