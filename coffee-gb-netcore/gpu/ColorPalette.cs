using System;
using System.Collections.Generic;
using System.Text;

namespace eu.rekawek.coffeegb.gpu
{
    public class ColorPalette : AddressSpace
    {
        private int indexAddr;
        private int dataAddr;

        private List<List<int>> palettes = new List<List<int>>();

        private int index;

        private bool autoIncrement;

        public ColorPalette(int offset)
        {
            this.indexAddr = offset;
            this.dataAddr = offset + 1;
        }

        public bool accepts(int address)
        {
            return address == indexAddr || address == dataAddr;
        }

        public void setByte(int address, int value)
        {
            if (address == indexAddr)
            {
                index = value & 0x3f;
                autoIncrement = (value & (1 << 7)) != 0;
            }
            else if (address == dataAddr)
            {
                int color = palettes[index / 8][(index % 8) / 2];
                if (index % 2 == 0)
                {
                    color = (color & 0xff00) | value;
                }
                else
                {
                    color = (color & 0x00ff) | (value << 8);
                }

                palettes[index / 8][(index % 8) / 2] = color;
                if (autoIncrement)
                {
                    index = (index + 1) & 0x3f;
                }
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public int getByte(int address)
        {
            if (address == indexAddr)
            {
                return index | (autoIncrement ? 0x80 : 0x00) | 0x40;
            }
            else if (address == dataAddr)
            {
                int color = palettes[index / 8][(index % 8) / 2];
                if (index % 2 == 0)
                {
                    return color & 0xff;
                }
                else
                {
                    return (color >> 8) & 0xff;
                }
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public int[] getPalette(int index)
        {
            return palettes[index].ToArray();
        }

        public override string ToString() => toString();
        public string toString()
        {
            var b = new StringBuilder();
            for (var i = 0; i < 8; i++)
            {
                b.Append(i).Append(": ");

                var palette = getPalette(i);

                foreach (var c in palette)
                {
                    b.Append(string.Format("%04X", c)).Append(' ');
                }

                b[b.Length - 1] = '\n';
            }

            return b.ToString();
        }

        public void fillWithFF()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    palettes[i][j] = 0x7fff;
                }
            }
        }
    }

}