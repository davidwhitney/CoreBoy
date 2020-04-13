using System.Collections.Generic;
using CoreBoy.gpu.phase;
using CoreBoy.memory;

namespace CoreBoy.gpu
{
    using static cpu.BitUtils;
    using static GpuRegister;

    public class Fetcher
    {

        private enum State
        {
            READ_TILE_ID,
            READ_DATA_1,
            READ_DATA_2,
            PUSH,
            READ_SPRITE_TILE_ID,
            READ_SPRITE_FLAGS,
            READ_SPRITE_DATA_1,
            READ_SPRITE_DATA_2,
            PUSH_SPRITE
        }

        private static readonly int[] EMPTY_PIXEL_LINE = new int[8];

        private readonly PixelFifo fifo;

        private readonly AddressSpace videoRam0;

        private readonly AddressSpace videoRam1;

        private readonly AddressSpace oemRam;

        private readonly MemoryRegisters r;

        private readonly Lcdc lcdc;

        private readonly bool gbc;

        private readonly int[] pixelLine = new int[8];

        private State state;

        private bool _fetchingDisabled;

        private int mapAddress;

        private int xOffset;

        private int tileDataAddress;

        private bool tileIdSigned;

        private int tileLine;

        private int tileId;

        private TileAttributes tileAttributes;

        private int tileData1;

        private int tileData2;

        private int spriteTileLine;

        private OamSearch.SpritePosition sprite;

        private TileAttributes spriteAttributes;

        private int spriteOffset;

        private int spriteOamIndex;

        private int divider = 2;

        public Fetcher(PixelFifo fifo, AddressSpace videoRam0, AddressSpace videoRam1, AddressSpace oemRam, Lcdc lcdc,
            MemoryRegisters registers, bool gbc)
        {
            this.gbc = gbc;
            this.fifo = fifo;
            this.videoRam0 = videoRam0;
            this.videoRam1 = videoRam1;
            this.oemRam = oemRam;
            r = registers;
            this.lcdc = lcdc;
        }

        public void init()
        {
            state = State.READ_TILE_ID;
            tileId = 0;
            tileData1 = 0;
            tileData2 = 0;
            divider = 2;
            _fetchingDisabled = false;
        }

        public void startFetching(int mapAddress, int tileDataAddress, int xOffset, bool tileIdSigned, int tileLine)
        {
            this.mapAddress = mapAddress;
            this.tileDataAddress = tileDataAddress;
            this.xOffset = xOffset;
            this.tileIdSigned = tileIdSigned;
            this.tileLine = tileLine;
            fifo.clear();

            state = State.READ_TILE_ID;
            tileId = 0;
            tileData1 = 0;
            tileData2 = 0;
            divider = 2;
        }

        public void fetchingDisabled()
        {
            _fetchingDisabled = true;
        }

        public void addSprite(OamSearch.SpritePosition sprite, int offset, int oamIndex)
        {
            this.sprite = sprite;
            state = State.READ_SPRITE_TILE_ID;
            spriteTileLine = r.get(LY) + 16 - sprite.getY();
            spriteOffset = offset;
            spriteOamIndex = oamIndex;
        }

        public void tick()
        {
            if (_fetchingDisabled && state == State.READ_TILE_ID)
            {
                if (fifo.getLength() <= 8)
                {
                    fifo.enqueue8Pixels(EMPTY_PIXEL_LINE, tileAttributes);
                }

                return;
            }

            if (--divider == 0)
            {
                divider = 2;
            }
            else
            {
                return;
            }

			stateSwitch:

            switch (state)
            {
                case State.READ_TILE_ID:
                    tileId = videoRam0.getByte(mapAddress + xOffset);
                    if (gbc)
                    {
                        tileAttributes = TileAttributes.valueOf(videoRam1.getByte(mapAddress + xOffset));
                    }
                    else
                    {
                        tileAttributes = TileAttributes.EMPTY;
                    }

                    state = State.READ_DATA_1;
                    break;

                case State.READ_DATA_1:
                    tileData1 = getTileData(tileId, tileLine, 0, tileDataAddress, tileIdSigned, tileAttributes, 8);
                    state = State.READ_DATA_2;
                    break;

                case State.READ_DATA_2:
                    tileData2 = getTileData(tileId, tileLine, 1, tileDataAddress, tileIdSigned, tileAttributes, 8);
                    state = State.PUSH;
					goto stateSwitch; // Sorry mum

                case State.PUSH:
                    if (fifo.getLength() <= 8)
                    {
                        fifo.enqueue8Pixels(zip(tileData1, tileData2, tileAttributes.isXflip()), tileAttributes);
                        xOffset = (xOffset + 1) % 0x20;
                        state = State.READ_TILE_ID;
                    }

                    break;

                case State.READ_SPRITE_TILE_ID:
                    tileId = oemRam.getByte(sprite.getAddress() + 2);
                    state = State.READ_SPRITE_FLAGS;
                    break;

                case State.READ_SPRITE_FLAGS:
                    spriteAttributes = TileAttributes.valueOf(oemRam.getByte(sprite.getAddress() + 3));
                    state = State.READ_SPRITE_DATA_1;
                    break;

                case State.READ_SPRITE_DATA_1:
                    if (lcdc.getSpriteHeight() == 16)
                    {
                        tileId &= 0xfe;
                    }

                    tileData1 = getTileData(tileId, spriteTileLine, 0, 0x8000, false, spriteAttributes,
                        lcdc.getSpriteHeight());
                    state = State.READ_SPRITE_DATA_2;
                    break;

                case State.READ_SPRITE_DATA_2:
                    tileData2 = getTileData(tileId, spriteTileLine, 1, 0x8000, false, spriteAttributes,
                        lcdc.getSpriteHeight());
                    state = State.PUSH_SPRITE;
                    break;

                case State.PUSH_SPRITE:
                    fifo.setOverlay(zip(tileData1, tileData2, spriteAttributes.isXflip()), spriteOffset,
                        spriteAttributes, spriteOamIndex);
                    state = State.READ_TILE_ID;
                    break;
            }
        }

        private int getTileData(int tileId, int line, int byteNumber, int tileDataAddress, bool signed,
            TileAttributes attr, int tileHeight)
        {
            int effectiveLine;
            if (attr.isYflip())
            {
                effectiveLine = tileHeight - 1 - line;
            }
            else
            {
                effectiveLine = line;
            }

            int tileAddress;
            if (signed)
            {
                tileAddress = tileDataAddress + toSigned(tileId) * 0x10;
            }
            else
            {
                tileAddress = tileDataAddress + tileId * 0x10;
            }

            AddressSpace videoRam = (attr.getBank() == 0 || !gbc) ? videoRam0 : videoRam1;
            return videoRam.getByte(tileAddress + effectiveLine * 2 + byteNumber);
        }

        public bool spriteInProgress()
        {
            var set = new List<State>
            {
                State.READ_SPRITE_TILE_ID, 
                State.READ_SPRITE_FLAGS, 
                State.READ_SPRITE_DATA_1,
                State.READ_SPRITE_DATA_2, 
                State.PUSH_SPRITE
            };

            return set.Contains(state);
        }

        public int[] zip(int data1, int data2, bool reverse)
        {
            return zip(data1, data2, reverse, pixelLine);
        }

        public static int[] zip(int data1, int data2, bool reverse, int[] pixelLine)
        {
            for (int i = 7; i >= 0; i--)
            {
                int mask = (1 << i);
                int p = 2 * ((data2 & mask) == 0 ? 0 : 1) + ((data1 & mask) == 0 ? 0 : 1);
                if (reverse)
                {
                    pixelLine[i] = p;
                }
                else
                {
                    pixelLine[7 - i] = p;
                }
            }

            return pixelLine;
        }

    }

}