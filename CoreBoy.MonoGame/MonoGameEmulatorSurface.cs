using CoreBoy.controller;
using CoreBoy.gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace CoreBoy.MonoGame
{
    internal class MonoGameEmulatorSurface : Game, IController
    {
        private readonly Config _config;
        private readonly Emulator _emulator;
        private readonly GameboyOptions _gameboyOptions;
        private readonly List<Keys> _downKeys = new List<Keys>();
        private readonly IReadOnlyDictionary<Button, Keys> _buttonKeyMap;
        private readonly object _updateLock = new object();
        private readonly GraphicsDeviceManager _graphics;
        private readonly CancellationTokenSource _cancellation;

        private Texture2D _currentFrame;
        private SpriteBatch _spriteBatch;
        private IButtonListener _listener;

        internal MonoGameEmulatorSurface()
        {
            Content.RootDirectory = "Content";
            _graphics = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            _config = Config.Load();
            _buttonKeyMap = _config.Keymap.GetButtonKeyMap();

            _graphics.PreferredBackBufferWidth = _config.Window.WindowWidth;
            _graphics.PreferredBackBufferHeight = _config.Window.WindowHeight;
            _graphics.ApplyChanges();

            _cancellation = new CancellationTokenSource();
            _gameboyOptions = new GameboyOptions();
            _emulator = new Emulator(_gameboyOptions);

            Exiting += Game_Exiting;
        }

        protected override void Initialize()
        {
            _emulator.Controller = this;
            _emulator.Display.OnFrameProduced += UpdateDisplay;

            try
            {
                _gameboyOptions.Rom = Path.Combine(Environment.CurrentDirectory, "Games", _config.Game);
                _emulator.Run(_cancellation.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start emulator. " + ex.Message);
                Exit();
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            var kState = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || kState.IsKeyDown(Keys.Escape))
                Exit();

            foreach (var buttonKey in _buttonKeyMap)
            {
                var button = buttonKey.Key;
                var key = buttonKey.Value;
                if (!_downKeys.Contains(key) && kState.IsKeyDown(key))
                {
                    _downKeys.Add(key);
                    _listener.OnButtonPress(button);
                }
                else if (_downKeys.Contains(key) && kState.IsKeyUp(key))
                {
                    _downKeys.Remove(key);
                    _listener.OnButtonRelease(button);
                }
            }


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_currentFrame != null)
            {
                GraphicsDevice.Clear(Color.Black);

                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _spriteBatch.Draw(_currentFrame, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height), Color.White);
                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        private void Game_Exiting(object sender, EventArgs e)
        {
            _emulator.Stop(_cancellation);
            _cancellation.Cancel();
        }

        private void UpdateDisplay(object _, byte[] frame)
        {
            if (!Monitor.TryEnter(_updateLock)) return;

            try
            {
                using var memoryStream = new MemoryStream(frame);
                _currentFrame = Texture2D.FromStream(GraphicsDevice, memoryStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Received error reading frame " + ex.Message);
            }
            finally
            {
                Monitor.Exit(_updateLock);
            }
        }

        public void SetButtonListener(IButtonListener listener) => _listener = listener;
    }
}
