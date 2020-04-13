using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace eu.rekawek.coffeegb.gui
{
    public partial class WinFormsEmulatorSurface : Form
    {
        private readonly Action _onFormClosed;
        public BitmapDisplay GameboyDisplay { get; set; }

        private readonly PictureBox _pictureBox;
        private readonly Timer _timer;

        public WinFormsEmulatorSurface(Action onFormClosed)
        {
            _onFormClosed = onFormClosed ?? (() => { });
            InitializeComponent();

            _pictureBox = new PictureBox
            {
                Width = BitmapDisplay.DisplayWidth * 5, 
                Height = BitmapDisplay.DisplayHeight * 5, 
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            Controls.Add(_pictureBox);

            const int fps = 30;

            _timer = new Timer(Math.Abs(1000 / fps)) { AutoReset = true };
            _timer.Elapsed += OnInterval;
            _timer.Start();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (_pictureBox != null)
            {
                _pictureBox.Width = this.Width;
                _pictureBox.Height = this.Height;
            }
        }

        private void OnInterval(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (GameboyDisplay == null) return;
            if (GameboyDisplay.CurrentScreenBytes.Length == 0)
            {
                return;
            }

            using var memoryStream = new MemoryStream(GameboyDisplay.CurrentScreenBytes);
            _pictureBox.Image = Image.FromStream(memoryStream);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _onFormClosed();
            base.OnFormClosed(e);
            _timer.Dispose();
        }
    }
}
