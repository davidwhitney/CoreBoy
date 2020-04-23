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
    public class Game1 : Game, IController
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private IButtonListener _listener;

        private Texture2D _currentFrame;
        private readonly Emulator _emulator;
        private readonly GameboyOptions _gameboyOptions;
        private CancellationTokenSource _cancellation;

        private readonly object _updateLock = new object();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            _cancellation = new CancellationTokenSource();
            _gameboyOptions = new GameboyOptions();
            _emulator = new Emulator(_gameboyOptions);

            Exiting += Game1_Exiting;
        }

        private void Game1_Exiting(object sender, EventArgs e)
        {
            _emulator.Stop(_cancellation);
            _cancellation.Cancel();
        }

        protected override void Initialize()
        {
            _emulator.Controller = this;
            _emulator.Display.OnFrameProduced += UpdateDisplay;

            _gameboyOptions.Rom = Path.Combine(Environment.CurrentDirectory, "Content", "pokemon.gb");
            _emulator.Run(_cancellation.Token);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        private void UpdateDisplay(object _, byte[] frame)
        {
            if (!Monitor.TryEnter(_updateLock)) return;

            try
            {
                using var memoryStream = new MemoryStream(frame);
                _currentFrame = Texture2D.FromStream(GraphicsDevice, memoryStream);
            }
            catch
            {
                // YOLO
            }
            finally
            {
                Monitor.Exit(_updateLock);
            }
        }

        private List<Keys> _downKeys = new List<Keys>();
        private Dictionary<Button, Keys> _buttonKeyMap = new Dictionary<Button, Keys>()
        {
            { Button.Up, Keys.Up },
            { Button.Down, Keys.Down },
            { Button.Left, Keys.Left },
            { Button.Right, Keys.Right },
            { Button.Start, Keys.Enter },
            { Button.Select, Keys.Space },
            { Button.A, Keys.Z },
            { Button.B, Keys.X },
        };

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

                spriteBatch.Begin(samplerState: SamplerState.PointClamp);

                spriteBatch.Draw(_currentFrame, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height), Color.White);

                spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        public void SetButtonListener(IButtonListener listener) => _listener = listener;
    }
}
