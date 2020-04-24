using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CoreBoy.controller;
using CoreBoy.gui;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Button = CoreBoy.controller.Button;

namespace CoreBoy.Avalonia
{
    public class MainWindow : Window, IController, IDisposable
    {
        #region Private fields

        private bool isDisposed;
        private readonly object _updateLock = new object();

        private IButtonListener _listener;
        private Dictionary<Key, Button> _controls;
        private byte[] _lastFrame;
        private readonly Emulator _emulator;
        private readonly GameboyOptions _gameboyOptions;
        private CancellationTokenSource _cancellation;

        #endregion

        #region Constructor

        public MainWindow()
        {
            Opened += OnWindowOpenedBindWindowEvents;
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            BuildMenuViewModel();
            BindKeysToButtons();
            AdjustEmulatorScreenSize();

            _cancellation = new CancellationTokenSource();
            _gameboyOptions = new GameboyOptions();
            _emulator = new Emulator(_gameboyOptions);

            ConnectEmulatorToUI();
        }

        private void OnWindowOpenedBindWindowEvents(object sender, EventArgs e)
        {
            PropertyChanged += OnWindowSizeChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void BuildMenuViewModel()
        {
            var vm = new MainWindowViewModel();

            vm.MenuItems = new[]
                {
                new MenuItemViewModel()
                {
                    Header = "_Emulator",
                    Items = new[]
                    {
                        new MenuItemViewModel()
                        {
                            Header = "_Load ROM",
                            Command = ReactiveCommand.CreateFromTask(LoadROM)
                        },
                        new MenuItemViewModel()
                        {
                            Header = "_Pause",
                            Command = ReactiveCommand.Create(Pause)
                        },
                        new MenuItemViewModel()
                        {
                            Header = "_Quit",
                            Command = ReactiveCommand.Create(Quit)
                        }
                    }
                },
                new MenuItemViewModel()
                {
                    Header = "Graphics",
                    Items = new[]
                    {
                        new MenuItemViewModel()
                        {
                            Header = "Screenshot",
                            Command = ReactiveCommand.CreateFromTask(Screenshot)
                        }
                    }
                }
            };

            DataContext = vm;
        }

        private void BindKeysToButtons()
        {
            _controls = new Dictionary<Key, Button>
            {
                {Key.Left, Button.Left},
                {Key.Right, Button.Right},
                {Key.Up, Button.Up},
                {Key.Down, Button.Down},
                {Key.Z, Button.A},
                {Key.X, Button.B},
                {Key.Enter, Button.Start},
                {Key.Back, Button.Select}
            };
        }

        private void AdjustEmulatorScreenSize()
        {
            var imageBox = this.FindControl<Image>("ImageBox");
            if (imageBox != null)
            {
                imageBox.Width = BitmapDisplay.DisplayWidth * 5;
                imageBox.Height = BitmapDisplay.DisplayHeight * 5;

                MinHeight = imageBox.Height + 25;
                MinWidth = imageBox.Width;

                Height = imageBox.Height + 25;
                Width = imageBox.Width;
            }
        }

        private void ConnectEmulatorToUI()
        {
            _emulator.Controller = this;
            _emulator.Display.OnFrameProduced += UpdateDisplay;

            KeyDown += EmulatorSurface_KeyDown;
            KeyUp += EmulatorSurface_KeyUp;
            Closed += (_, e) => { _cancellation.Cancel(); };
        }

        #endregion

        #region Menu Items command methods

        private async Task LoadROM()
        {
            if (_emulator.Active)
            {
                _emulator.Stop(_cancellation);
                _cancellation = new CancellationTokenSource();
                Thread.Sleep(100);
            }

            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                AllowMultiple = false
            };

            if (openFileDialog.Filters == null)
                openFileDialog.Filters = new List<FileDialogFilter>();

            openFileDialog.Filters.Add(new FileDialogFilter()
            {
                Name = "Gameboy ROM (*.gb)",
                Extensions = { "gb" }
            });
            openFileDialog.Filters.Add(new FileDialogFilter()
            {
                Name = "All files(*.*)",
                Extensions = { "*.*" }
            });

            var results = await openFileDialog.ShowAsync(this).ConfigureAwait(true);

            var (success, romPath) = results.Any()
                ? (true, results.FirstOrDefault())
                : (false, null);

            if (success)
            {
                _gameboyOptions.Rom = romPath;
                _emulator.Run(_cancellation.Token);
            }
        }

        private void Pause()
        {
            _emulator.TogglePause();
        }

        private void Quit()
        {
            Close();
        }

        private async Task Screenshot()
        {
            _emulator.TogglePause();

            SaveFileDialog saveFileDialog = new SaveFileDialog();

            if (saveFileDialog.Filters == null)
                saveFileDialog.Filters = new List<FileDialogFilter>();

            saveFileDialog.Filters.Add(new FileDialogFilter()
            {
                Name = "Bitmap (*.bmp)",
                Extensions = { "*.bmp" }
            });

            var result = await saveFileDialog.ShowAsync(this).ConfigureAwait(true);

            var (success, screenshotPath) = !string.IsNullOrWhiteSpace(result)
                ? (true, result)
                : (false, null);

            if (success)
            {
                try
                {
                    Monitor.Enter(_updateLock);
                    File.WriteAllBytes(screenshotPath, _lastFrame);
                }
                finally
                {
                    Monitor.Exit(_updateLock);
                }
            }

            _emulator.TogglePause();
        }

        #endregion

        #region Emulator events

        private void OnWindowSizeChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name.Equals("ClientSize", StringComparison.CurrentCulture))
            {
                var imageBox = this.FindControl<Image>("ImageBox");
                if (imageBox != null)
                {
                    imageBox.Width = Width;
                    imageBox.Height = Height - 25;
                }
            }
        }

        public void UpdateDisplay(object sender, byte[] frame)
        {
            if (!Monitor.TryEnter(_updateLock)) return;

            try
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _lastFrame = frame;
                    using var memoryStream = new MemoryStream(frame);

                    var imageBox = this.FindControl<Image>("ImageBox");
                    if (imageBox != null)
                    {
                        imageBox.Source = new Bitmap(memoryStream);
                    }
                });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            finally
            {
                Monitor.Exit(_updateLock);
            }
        }

        private void EmulatorSurface_KeyDown(object sender, KeyEventArgs e)
        {
            var button = _controls.ContainsKey(e.Key) ? _controls[e.Key] : null;
            if (button != null)
            {
                _listener?.OnButtonPress(button);
            }
        }

        private void EmulatorSurface_KeyUp(object sender, KeyEventArgs e)
        {
            var button = _controls.ContainsKey(e.Key) ? _controls[e.Key] : null;
            if (button != null)
            {
                _listener?.OnButtonRelease(button);
            }
        }

        #endregion

        #region IController methods

        public void SetButtonListener(IButtonListener listener) => _listener = listener;

        #endregion

        #region IDisposable pattern

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                _cancellation.Dispose();
            }

            isDisposed = true;
        }

        #endregion
    }
}
