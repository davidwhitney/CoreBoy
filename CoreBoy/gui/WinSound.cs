using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CoreBoy.sound;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace CoreBoy.gui
{

    public class WinSound : ISoundOutput
    {
        private readonly byte[] _buffer = new byte[BufferSize];
        private int _i = 0;
        private int _tick;
        private readonly int _divider;
        private AudioPlaybackEngine _engine;

        private const int BufferSize = 1024;
        public const int SampleRate = 22050;

        public WinSound()
        {
            _divider = (int)(Gameboy.TicksPerSec / SampleRate);
        }

        public void Start()
        {
            _engine = new AudioPlaybackEngine(SampleRate, 2);
        }

        public void Stop()
        {
            _engine?.Dispose();
            _engine = null;
        }

        public void Play(int left, int right)
        {
            if (_tick++ != 0)
            {
                _tick %= _divider;
                return;
            }

            left = (int)(left * 0.25);
            right = (int)(right * 0.25);

            left = left < 0 ? 0 : (left > 255 ? 255 : left);
            right = right < 0 ? 0 : (right > 255 ? 255 : right);

            _buffer[_i++] = (byte)left;
            _buffer[_i++] = (byte)right;
            if (_i > BufferSize / 2)
            {
                _engine?.PlaySound(_buffer, 0, _i);
                _i = 0;
            }

            // wait until audio is done playing this data
            while (_engine?.GetQueuedAudioLength() > BufferSize)
            {
                Thread.Sleep(0);
            }
        }
    }

    public class AudioPlaybackEngine : IDisposable
    {
        private IWavePlayer _outputDevice;
        private readonly MixingSampleProvider _mixer;
        private readonly BufferedWaveProvider _bufferedWaveProvider;

        public AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2)
        {
            _outputDevice = new WasapiOut();
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
            {
                ReadFully = true
            };

            _bufferedWaveProvider = new BufferedWaveProvider(WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, sampleRate, 2, sampleRate, 8, 8))
            {
                ReadFully = true,
                DiscardOnBufferOverflow = true
            };

            AddMixerInput(_bufferedWaveProvider.ToSampleProvider());
            _outputDevice.Init(_mixer);
            _outputDevice.Play();
        }

        public int GetQueuedAudioLength()
        {
            return _bufferedWaveProvider.BufferedBytes;
        }

        public void PlaySound(byte[] buffer, int offset, int count)
        {
            _bufferedWaveProvider.AddSamples(buffer, offset, count);
        }

        private void AddMixerInput(ISampleProvider input)
        {
            _mixer.AddMixerInput(ConvertToRightChannelCount(input));
        }

        private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == _mixer.WaveFormat.Channels)
            {
                return input;
            }
            if (input.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
            {
                return new MonoToStereoSampleProvider(input);
            }
            throw new NotImplementedException("Not yet implemented this channel count conversion");
        }

        public void Dispose()
        {
            _outputDevice.Stop();
            _outputDevice.Dispose();
        }
    }
}