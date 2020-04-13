namespace CoreBoy.gpu
{
    public interface IPixelFifo
    {
        int GetLength();
        void PutPixelToScreen();
        void DropPixel();
        void Enqueue8Pixels(int[] pixels, TileAttributes tileAttributes);
        void SetOverlay(int[] pixelLine, int offset, TileAttributes flags, int oamIndex);
        void Clear();
    }
}