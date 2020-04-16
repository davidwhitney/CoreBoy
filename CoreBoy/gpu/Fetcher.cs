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
            ReadTileId,
            ReadData1,
            ReadData2,
            Push,
            ReadSpriteTileId,
            ReadSpriteFlags,
            ReadSpriteData1,
            ReadSpriteData2,
            PushSprite
        }

        private static readonly int[] EmptyPixelLine = new int[8];

        private static readonly List<State> SpritesInProgress = new List<State> {
            State.ReadSpriteTileId,
            State.ReadSpriteFlags,
            State.ReadSpriteData1,
            State.ReadSpriteData2,
            State.PushSprite
        };

        private readonly IPixelFifo _fifo;
        private readonly IAddressSpace _videoRam0;
        private readonly IAddressSpace _videoRam1;
        private readonly IAddressSpace _oemRam;
        private readonly MemoryRegisters _r;
        private readonly Lcdc _lcdc;
        private readonly bool _gbc;

        private readonly int[] _pixelLine = new int[8];
        private State _state;
        private bool _fetchingDisabled;

        private int _mapAddress;
        private int _xOffset;

        private int _tileDataAddress;
        private bool _tileIdSigned;
        private int _tileLine;
        private int _tileId;
        private TileAttributes _tileAttributes;
        private int _tileData1;
        private int _tileData2;

        private int _spriteTileLine;
        private OamSearch.SpritePosition _sprite;
        private TileAttributes _spriteAttributes;
        private int _spriteOffset;
        private int _spriteOamIndex;

        private int _divider = 2;

        public Fetcher(IPixelFifo fifo, IAddressSpace videoRam0, IAddressSpace videoRam1, IAddressSpace oemRam, Lcdc lcdc,
            MemoryRegisters registers, bool gbc)
        {
            _gbc = gbc;
            _fifo = fifo;
            _videoRam0 = videoRam0;
            _videoRam1 = videoRam1;
            _oemRam = oemRam;
            _r = registers;
            _lcdc = lcdc;
        }

        public void Init()
        {
            _state = State.ReadTileId;
            _tileId = 0;
            _tileData1 = 0;
            _tileData2 = 0;
            _divider = 2;
            _fetchingDisabled = false;
        }

        public void StartFetching(int mapAddress, int tileDataAddress, int xOffset, bool tileIdSigned, int tileLine)
        {
            _mapAddress = mapAddress;
            _tileDataAddress = tileDataAddress;
            _xOffset = xOffset;
            _tileIdSigned = tileIdSigned;
            _tileLine = tileLine;
            _fifo.Clear();

            _state = State.ReadTileId;
            _tileId = 0;
            _tileData1 = 0;
            _tileData2 = 0;
            _divider = 2;
        }

        public void FetchingDisabled()
        {
            _fetchingDisabled = true;
        }

        public void AddSprite(OamSearch.SpritePosition sprite, int offset, int oamIndex)
        {
            _sprite = sprite;
            _state = State.ReadSpriteTileId;
            _spriteTileLine = _r.Get(Ly) + 16 - sprite.GetY();
            _spriteOffset = offset;
            _spriteOamIndex = oamIndex;
        }

        public void Tick()
        {
            if (_fetchingDisabled && _state == State.ReadTileId)
            {
                if (_fifo.GetLength() <= 8)
                {
                    _fifo.Enqueue8Pixels(EmptyPixelLine, _tileAttributes);
                }

                return;
            }

            if (--_divider == 0)
            {
                _divider = 2;
            }
            else
            {
                return;
            }

            stateSwitch:

            switch (_state)
            {
                case State.ReadTileId:
                    _tileId = _videoRam0.GetByte(_mapAddress + _xOffset);
                   
                    _tileAttributes = _gbc
                            ? TileAttributes.ValueOf(_videoRam1.GetByte(_mapAddress + _xOffset))
                            : TileAttributes.Empty;

                    _state = State.ReadData1;
                    break;

                case State.ReadData1:
                    _tileData1 = GetTileData(_tileId, _tileLine, 0, _tileDataAddress, _tileIdSigned, _tileAttributes, 8);
                    _state = State.ReadData2;
                    break;

                case State.ReadData2:
                    _tileData2 = GetTileData(_tileId, _tileLine, 1, _tileDataAddress, _tileIdSigned, _tileAttributes, 8);
                    _state = State.Push;
                    goto stateSwitch; // Sorry mum

                case State.Push:
                    if (_fifo.GetLength() <= 8)
                    {
                        _fifo.Enqueue8Pixels(Zip(_tileData1, _tileData2, _tileAttributes.IsXFlip()), _tileAttributes);
                        _xOffset = (_xOffset + 1) % 0x20;
                        _state = State.ReadTileId;
                    }

                    break;

                case State.ReadSpriteTileId:
                    _tileId = _oemRam.GetByte(_sprite.GetAddress() + 2);
                    _state = State.ReadSpriteFlags;
                    break;

                case State.ReadSpriteFlags:
                    _spriteAttributes = TileAttributes.ValueOf(_oemRam.GetByte(_sprite.GetAddress() + 3));
                    _state = State.ReadSpriteData1;
                    break;

                case State.ReadSpriteData1:
                    if (_lcdc.GetSpriteHeight() == 16)
                    {
                        _tileId &= 0xfe;
                    }

                    _tileData1 = GetTileData(_tileId, _spriteTileLine, 0, 0x8000, false, _spriteAttributes,
                        _lcdc.GetSpriteHeight());
                    _state = State.ReadSpriteData2;
                    break;

                case State.ReadSpriteData2:
                    _tileData2 = GetTileData(_tileId, _spriteTileLine, 1, 0x8000, false, _spriteAttributes,
                        _lcdc.GetSpriteHeight());
                    _state = State.PushSprite;
                    break;

                case State.PushSprite:
                    _fifo.SetOverlay(Zip(_tileData1, _tileData2, _spriteAttributes.IsXFlip()), _spriteOffset,
                        _spriteAttributes, _spriteOamIndex);
                    _state = State.ReadTileId;
                    break;
            }
        }

        private int GetTileData(int tileId, int line, int byteNumber, int tileDataAddress, bool signed,
            TileAttributes attr, int tileHeight)
        {
            var effectiveLine = attr.IsYFlip() ? tileHeight - 1 - line : line;
            var tileAddress = signed ? tileDataAddress + ToSigned(tileId) * 0x10 : tileDataAddress + tileId * 0x10;

            var videoRam = (attr.GetBank() == 0 || !_gbc) ? _videoRam0 : _videoRam1;
            return videoRam.GetByte(tileAddress + effectiveLine * 2 + byteNumber);
        }

        public bool SpriteInProgress()
        {
            return SpritesInProgress.Contains(_state);
        }

        public int[] Zip(int data1, int data2, bool reverse)
        {
            return Zip(data1, data2, reverse, _pixelLine);
        }

        public static int[] Zip(int data1, int data2, bool reverse, int[] pixelLine)
        {
            for (var i = 7; i >= 0; i--)
            {
                var mask = (1 << i);
                var p = 2 * ((data2 & mask) == 0 ? 0 : 1) + ((data1 & mask) == 0 ? 0 : 1);
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