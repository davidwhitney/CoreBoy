using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBoy.gpu
{
    public class ColorPalette : IAddressSpace
    {
        private readonly int _indexAddress;
        private readonly int _dataAddress;
        private int _index;
        private bool _autoIncrement;

        private readonly List<List<int>> _palettes;

        public ColorPalette(int offset)
        {
            _palettes = new List<List<int>>();
            for (var x = 0; x < 8; x++)
            {
                var row = new List<int>(4);
                for (var y = 0; y < 4; y++)
                {
                    row.Add(0);
                }

                _palettes.Add(row);
            }

            _indexAddress = offset;
            _dataAddress = offset + 1;
        }

        public bool Accepts(int address) => address == _indexAddress || address == _dataAddress;

        public void SetByte(int address, int value)
        {
            if (address == _indexAddress)
            {
                _index = value & 0x3f;
                _autoIncrement = (value & (1 << 7)) != 0;
            }
            else if (address == _dataAddress)
            {
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
            else
            {
                throw new InvalidOperationException();
            }
        }

        public int GetByte(int address)
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
                    b.Append($"{c:X4}").Append(' ');
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