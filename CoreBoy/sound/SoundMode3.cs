using System;
using CoreBoy.memory;

namespace CoreBoy.sound
{
    public class SoundMode3 : AbstractSoundMode
    {
        private static readonly int[] DmgWave =
        {
            0x84, 0x40, 0x43, 0xaa, 0x2d, 0x78, 0x92, 0x3c,
            0x60, 0x59, 0x59, 0xb0, 0x34, 0xb8, 0x2e, 0xda
        };

        private static readonly int[] CgbWave =
        {
            0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff,
            0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff
        };

        private readonly Ram _waveRam = new Ram(0xff30, 0x10);
        private int _freqDivider;
        private int _lastOutput;
        private int _i;
        private int _ticksSinceRead = 65536;
        private int _lastReadAddress;
        private int _buffer;
        private bool _triggered;

        public SoundMode3(bool gbc) : base(0xff1a, 256, gbc)
        {
            foreach (var v in gbc ? CgbWave : DmgWave)
            {
                _waveRam.SetByte(0xff30, v);
            }
        }

        public override bool Accepts(int address) => _waveRam.Accepts(address) || base.Accepts(address);

        public override int GetByte(int address)
        {
            if (!_waveRam.Accepts(address))
            {
                return base.GetByte(address);
            }

            if (!isEnabled())
            {
                return _waveRam.GetByte(address);
            }

            if (_waveRam.Accepts(_lastReadAddress) && (gbc || _ticksSinceRead < 2))
            {
                return _waveRam.GetByte(_lastReadAddress);
            }

            return 0xff;
        }


        public override void SetByte(int address, int value)
        {
            if (!_waveRam.Accepts(address))
            {
                base.SetByte(address, value);
                return;
            }

            if (!isEnabled())
            {
                _waveRam.SetByte(address, value);
            }
            else if (_waveRam.Accepts(_lastReadAddress) && (gbc || _ticksSinceRead < 2))
            {
                _waveRam.SetByte(_lastReadAddress, value);
            }
        }

        protected override void setNr0(int value)
        {
            base.setNr0(value);
            dacEnabled = (value & (1 << 7)) != 0;
            channelEnabled &= dacEnabled;
        }

        protected override void setNr1(int value)
        {
            base.setNr1(value);
            length.SetLength(256 - value);
        }

        protected override void setNr4(int value)
        {
            if (!gbc && (value & (1 << 7)) != 0)
            {
                if (isEnabled() && _freqDivider == 2)
                {
                    var pos = _i / 2;
                    if (pos < 4)
                    {
                        _waveRam.SetByte(0xff30, _waveRam.GetByte(0xff30 + pos));
                    }
                    else
                    {
                        pos = pos & ~3;
                        for (var j = 0; j < 4; j++)
                        {
                            _waveRam.SetByte(0xff30 + j, _waveRam.GetByte(0xff30 + ((pos + j) % 0x10)));
                        }
                    }
                }
            }

            base.setNr4(value);
        }

        public override void start()
        {
            _i = 0;
            _buffer = 0;
            if (gbc)
            {
                length.Reset();
            }

            length.Start();
        }

        protected override void trigger()
        {
            _i = 0;
            _freqDivider = 6;
            _triggered = !gbc;
            if (gbc)
            {
                GetWaveEntry();
            }
        }

        public override int tick()
        {
            _ticksSinceRead++;
            if (!updateLength())
            {
                return 0;
            }

            if (!dacEnabled)
            {
                return 0;
            }

            if ((getNr0() & (1 << 7)) == 0)
            {
                return 0;
            }

            if (--_freqDivider == 0)
            {
                ResetFreqDivider();
                if (_triggered)
                {
                    _lastOutput = (_buffer >> 4) & 0x0f;
                    _triggered = false;
                }
                else
                {
                    _lastOutput = GetWaveEntry();
                }

                _i = (_i + 1) % 32;
            }

            return _lastOutput;
        }

        private int GetVolume() => (getNr2() >> 5) & 0b11;

        private int GetWaveEntry()
        {
            _ticksSinceRead = 0;
            _lastReadAddress = 0xff30 + _i / 2;
            _buffer = _waveRam.GetByte(_lastReadAddress);

            var b = _buffer;
            if (_i % 2 == 0)
            {
                b = (b >> 4) & 0x0f;
            }
            else
            {
                b = b & 0x0f;
            }

            return GetVolume() switch
            {
                0 => 0,
                1 => b,
                2 => b >> 1,
                3 => b >> 2,
                _ => throw new InvalidOperationException("Illegal state")
            };
        }

        private void ResetFreqDivider() => _freqDivider = getFrequency() * 2;
    }
}