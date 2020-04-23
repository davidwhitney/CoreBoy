using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using CoreBoy.controller;
using CoreBoy.gui;
using Button = CoreBoy.controller.Button;

namespace CoreBoy.Windows
{
    public partial class WinFormsEmulatorSurface : Form, IController
    {
        private IButtonListener _listener;

        private byte[] _lastFrame;
        private readonly MenuStrip _menu;
        private readonly PictureBox _pictureBox;
        private readonly Dictionary<Keys, Button> _controls;

        private readonly Emulator _emulator;
        private readonly GameboyOptions _gameboyOptions;
        private CancellationTokenSource _cancellation;

        private readonly object _updateLock = new object();

        public WinFormsEmulatorSurface()
        {
            InitializeComponent();

            Controls.Add(_menu = new MenuStrip
            {
                Items =
                {
                    new ToolStripMenuItem("Emulator")
                    {
                        DropDownItems =
                        {
                            new ToolStripMenuItem("Load ROM", null, (sender, args) => { StartEmulation(); }),
                            new ToolStripMenuItem("Pause", null, (sender, args) => { _emulator.TogglePause(); }),
                            new ToolStripMenuItem("Quit", null, (sender, args) => { Close(); })
                        }
                    },
                    new ToolStripMenuItem("Graphics")
                    {
                        DropDownItems =
                        {
                            new ToolStripMenuItem("Screenshot", null, (sender, args) => { Screenshot(); })
                        }
                    }
                }
            });

            Controls.Add(_pictureBox = new PictureBox
            {
                Top = _menu.Height,
                Width = BitmapDisplay.DisplayWidth * 5,
                Height = BitmapDisplay.DisplayHeight * 5,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            });

            _controls = new Dictionary<Keys, Button>
            {
                {Keys.Left, Button.Left},
                {Keys.Right, Button.Right},
                {Keys.Up, Button.Up},
                {Keys.Down, Button.Down},
                {Keys.Z, Button.A},
                {Keys.X, Button.B},
                {Keys.Enter, Button.Start},
                {Keys.Back, Button.Select}
            };

            Height = _menu.Height + _pictureBox.Height + 50;
            Width = _pictureBox.Width;

            _cancellation = new CancellationTokenSource();
            _gameboyOptions = new GameboyOptions();
            _emulator = new Emulator(_gameboyOptions);

            ConnectEmulatorToPanel();
        }

        private void ConnectEmulatorToPanel()
        {
            _emulator.Controller = this;
            _emulator.Display.OnFrameProduced += UpdateDisplay;

            KeyDown += WinFormsEmulatorSurface_KeyDown;
            KeyUp += WinFormsEmulatorSurface_KeyUp;
            Closed += (_, e) => { _cancellation.Cancel(); };
        }

        private void StartEmulation()
        {
            if (_emulator.Active)
            {
                _emulator.Stop(_cancellation);
                _cancellation = new CancellationTokenSource();
                _pictureBox.Image = null;
                Thread.Sleep(100);
            }

            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Gameboy ROM (*.gb)|*.gb| All files(*.*) |*.*",
                FilterIndex = 0,
                RestoreDirectory = true
            };

            var (success, romPath) = openFileDialog.ShowDialog() == DialogResult.OK
                ? (true, openFileDialog.FileName)
                : (false, null);

            if (success)
            {
                _gameboyOptions.Rom = romPath;
                _emulator.Run(_cancellation.Token);
            }
        }

        private void Screenshot()
        {
            _emulator.TogglePause();

            using var sfd = new SaveFileDialog
            {
                Filter = "Bitmap (*.bmp)|*.bmp",
                FilterIndex = 0,
                RestoreDirectory = true
            };

            var (success, romPath) = sfd.ShowDialog() == DialogResult.OK
                ? (true, sfd.FileName)
                : (false, null);

            if (success)
            {
                try
                {
                    Monitor.Enter(_updateLock);
                    File.WriteAllBytes(sfd.FileName, _lastFrame);
                }
                finally
                {
                    Monitor.Exit(_updateLock);
                }
            }

            _emulator.TogglePause();
        }

        private void WinFormsEmulatorSurface_KeyDown(object sender, KeyEventArgs e)
        {
            var button = _controls.ContainsKey(e.KeyCode) ? _controls[e.KeyCode] : null;
            if (button != null)
            {
                _listener?.OnButtonPress(button);
            }
        }

        private void WinFormsEmulatorSurface_KeyUp(object sender, KeyEventArgs e)
        {
            var button = _controls.ContainsKey(e.KeyCode) ? _controls[e.KeyCode] : null;
            if (button != null)
            {
                _listener?.OnButtonRelease(button);
            }
        }

        public void SetButtonListener(IButtonListener listener) => _listener = listener;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_pictureBox == null) return;

            _pictureBox.Width = Width;
            _pictureBox.Height = Height - _menu.Height - 50;
        }

        public void UpdateDisplay(object _, byte[] frame)
        {
            if (!Monitor.TryEnter(_updateLock)) return;
            
            try
            {
                _lastFrame = frame;
                using var memoryStream = new MemoryStream(frame);
                _pictureBox.Image = Image.FromStream(memoryStream);
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _pictureBox.Dispose();
        }
    }
}