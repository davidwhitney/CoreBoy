using CoreBoy.memory;

namespace CoreBoy.gpu.phase
{
    public class OamSearch : GpuPhase
    {
        private enum State
        {
            READING_Y,
            READING_X
        }

        public sealed class SpritePosition
        {

            private readonly int x;
            private readonly int y;
            private readonly int address;

            public SpritePosition(int x, int y, int address)
            {
                this.x = x;
                this.y = y;
                this.address = address;
            }

            public int getX()
            {
                return x;
            }

            public int getY()
            {
                return y;
            }

            public int getAddress()
            {
                return address;
            }
        }

        private readonly AddressSpace oemRam;
        private readonly MemoryRegisters registers;
        private readonly SpritePosition[] sprites;
        private readonly Lcdc lcdc;
        private int spritePosIndex;
        private State state;
        private int spriteY;
        private int spriteX;
        private int i;

        public OamSearch(AddressSpace oemRam, Lcdc lcdc, MemoryRegisters registers)
        {
            this.oemRam = oemRam;
            this.registers = registers;
            this.lcdc = lcdc;
            this.sprites = new SpritePosition[10];
        }

        public OamSearch start()
        {
            spritePosIndex = 0;
            state = State.READING_Y;
            spriteY = 0;
            spriteX = 0;
            i = 0;
            for (int j = 0; j < sprites.Length; j++)
            {
                sprites[j] = null;
            }

            return this;
        }


        public bool tick()
        {
            int spriteAddress = 0xfe00 + 4 * i;
            switch (state)
            {
                case State.READING_Y:
                    spriteY = oemRam.getByte(spriteAddress);
                    state = State.READING_X;
                    break;

                case State.READING_X:
                    spriteX = oemRam.getByte(spriteAddress + 1);
                    if (spritePosIndex < sprites.Length && between(spriteY, registers.Get(GpuRegister.LY) + 16,
                            spriteY + lcdc.getSpriteHeight()))
                    {
                        sprites[spritePosIndex++] = new SpritePosition(spriteX, spriteY, spriteAddress);
                    }

                    i++;
                    state = State.READING_Y;
                    break;
            }

            return i < 40;
        }

        public SpritePosition[] getSprites()
        {
            return sprites;
        }

        private static bool between(int from, int x, int to)
        {
            return from <= x && x < to;
        }
    }
}