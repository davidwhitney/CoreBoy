using CoreBoy.controller;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CoreBoy.MonoGame
{
    internal struct ConfigWindow
    {
        private const int GB_WIDTH = 160;
        private const int GB_HEIGHT = 144;

        public int Width { get; set; }
        public int Height { get; set; }
        public double Scale { get; set; }

        public int WindowWidth => (int)Math.Clamp(Width * Scale, GB_WIDTH, 3840);
        public int WindowHeight => (int)Math.Clamp(Height * Scale, GB_HEIGHT, 2160);
    }

    internal struct ConfigKeymap
    {
        public string A { get; set; }
        public string B { get; set; }
        public string Left { get; set; }
        public string Right { get; set; }
        public string Up { get; set; }
        public string Down { get; set; }
        public string Start { get; set; }
        public string Select { get; set; }

        private static (Button button, Keys key) GetButtonKeyPair(Button button, string keyStr, Keys fallbackKey)
        {
            var canParse = Enum.TryParse<Keys>(keyStr, out var key);
            if (!canParse)
            {
                key = fallbackKey;
            }

            return (button, key);
        }

        internal IReadOnlyDictionary<Button, Keys> GetButtonKeyMap()
        {
            return new[]
            {
                GetButtonKeyPair(Button.A, A, Keys.Z),
                GetButtonKeyPair(Button.B, B, Keys.X),
                GetButtonKeyPair(Button.Left, Left, Keys.Left),
                GetButtonKeyPair(Button.Right, Right, Keys.Right),
                GetButtonKeyPair(Button.Down, Down, Keys.Down),
                GetButtonKeyPair(Button.Up, Up, Keys.Up),
                GetButtonKeyPair(Button.Start, Start, Keys.Enter),
                GetButtonKeyPair(Button.Select, Select, Keys.Space),
            }.ToDictionary(k => k.button, v => v.key);
        }
    }

    internal sealed class Config
    {
        public string Game { get; set; }
        public ConfigWindow Window { get; set; }
        public ConfigKeymap Keymap { get; set; }

        internal static Config Load()
        {
            var filePath = Path.Combine(Environment.CurrentDirectory, "Config.json");
            var json = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var config = JsonSerializer.Deserialize<Config>(json, options);
            return config;
        }
    }
}
