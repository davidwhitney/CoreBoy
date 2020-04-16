using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CoreBoy.controller;
using Button = CoreBoy.controller.Button;

namespace CoreBoy.gui
{
    public partial class WinFormsEmulatorSurface : Form, IController
    {
        private IButtonListener _listener;
        private readonly PictureBox _pictureBox;
        private readonly Dictionary<Keys, Button> _controls;

        public WinFormsEmulatorSurface()
        {
            InitializeComponent();

            _pictureBox = new PictureBox
            {
                Width = BitmapDisplay.DisplayWidth * 5,
                Height = BitmapDisplay.DisplayHeight * 5,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };

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

            KeyDown += WinFormsEmulatorSurface_KeyDown;
            KeyUp += WinFormsEmulatorSurface_KeyUp;
            Controls.Add(_pictureBox);
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

        public void SetButtonListener(IButtonListener listener)
        {
            _listener = listener;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_pictureBox == null) return;

            _pictureBox.Width = Width;
            _pictureBox.Height = Height;
        }

        public void UpdateDisplay(object _, byte[] frame)
        {
            using var memoryStream = new MemoryStream(frame);
            _pictureBox.Image = Image.FromStream(memoryStream);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _pictureBox.Dispose();
        }
    }
}