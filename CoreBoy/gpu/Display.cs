namespace CoreBoy.gpu
{
    public delegate void FrameProducedEventHandler(object sender, byte[] frameData);

    public interface IDisplay
    {
        event FrameProducedEventHandler OnFrameProduced;

        void PutDmgPixel(int color);
        void PutColorPixel(int gbcRgb);
        void RequestRefresh();
        void WaitForRefresh();
        void EnableLcd();
        void DisableLcd();
    }
}