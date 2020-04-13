using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreBoy.gpu
{
    public class ColorPalette : AddressSpace
    {
        private readonly int _indexAddress;
        private readonly int _dataAddress;
        private int _index;
        private bool _autoIncrement;

        private readonly List<List<int>> _palettes;

        public ColorPalette(int offset)
        {
            _palettes = Enumerable.Repeat(Enumerable.Repeat(0, 4).ToList(), 8).ToList();
            _indexAddress = offset;
            _dataAddress = offset + 1;
        }

        public bool accepts(int address) => address == _indexAddress || address == _dataAddress;

        public void setByte(int address, int value)
        {
            if (address == _indexAddress)
            {
                _index = value & 0x3f;
                _autoIncrement = (value & (1 << 7)) != 0;
                return;
            }

            if (address != _dataAddress)
            {
                throw new ArgumentException();
            }

            var color = _palettes[_index / 8][(_index % 8) / 2];
            if (_index % 2 == 0)
            {
                color = (color & 0xff00) | value;
            }
            else
            {
                color = (color & 0x00ff) | (value << 8);
            }

            _palettes[_index / 8][(_index % 8) / 2] = color;
            if (_autoIncrement)
            {
                _index = (_index + 1) & 0x3f;
            }
        }

        public int getByte(int address)
        {
            if (address == _indexAddress)
            {
                return _index | (_autoIncrement ? 0x80 : 0x00) | 0x40;
            }

            if (address != _dataAddress)
            {
                throw new ArgumentException();
            }

            var color = _palettes[_index / 8][(_index % 8) / 2];
            if (_index % 2 == 0)
            {
                return color & 0xff;
            }

            return (color >> 8) & 0xff;
        }

        public int[] GetPalette(int index)
        {
            return _palettes[index].ToArray();
        }

        public override string ToString()
        {
            var b = new StringBuilder();
            for (var i = 0; i < 8; i++)
            {
                b.Append(i).Append(": ");

                var palette = GetPalette(i);

                foreach (var c in palette)
                {
                    b.Append(string.Format("%04X", c)).Append(' ');
                }

                b[^1] = '\n';
            }

            return b.ToString();
        }

        public void FillWithFf()
        {
            for (var i = 0; i < 8; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    _palettes[i][j] = 0x7fff;
                }
            }
        }
    }
}