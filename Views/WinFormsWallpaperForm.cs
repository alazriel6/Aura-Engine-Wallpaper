using System;
using System.IO;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using LiveWallpaperApp.Models;
using LiveWallpaperApp.Native;

namespace LiveWallpaperApp.Views
{
    public class WinFormsWallpaperForm : Form, IDisposable
    {
        private readonly MonitorInfo _monitor;
        private readonly LibVLC _sharedLibVlc;
        private MediaPlayer? _mediaPlayer;
        private Media? _currentMedia;
        private string? _currentPath;
        private VideoView _videoView;
        private int _currentVolume = 0;
        private bool _isMuted = true;

        public MonitorInfo Monitor => _monitor;
        public string? CurrentPath => _currentPath;

        public WinFormsWallpaperForm(MonitorInfo monitor, LibVLC sharedLibVlc)
        {
            _monitor = monitor;
            _sharedLibVlc = sharedLibVlc;

            // Form settings for a borderless child window
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = System.Drawing.Color.Black;
            this.AutoScaleMode = AutoScaleMode.None;
            
            // Start OFFSCREEN so it never flashes on the user's display
            this.Left = -32000;
            this.Top = -32000;
            this.Width = monitor.Bounds.Width;
            this.Height = monitor.Bounds.Height;

            _videoView = new VideoView
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Black
            };
            this.Controls.Add(_videoView);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // WS_EX_TRANSPARENT (0x20) | WS_EX_TOOLWINDOW (0x80) | WS_EX_NOACTIVATE (0x08000000)
                cp.ExStyle |= 0x20 | 0x80 | 0x08000000;
                return cp;
            }
        }

        public void Play(string videoPath)
        {
            if (!File.Exists(videoPath))
                throw new FileNotFoundException("Wallpaper video was not found.", videoPath);

            _currentPath = videoPath;

            if (_mediaPlayer == null)
            {
                _mediaPlayer = new MediaPlayer(_sharedLibVlc);
                _mediaPlayer.EnableHardwareDecoding = true;
                
                // Force VLC to crop the video to the monitor's exact aspect ratio
                // This eliminates black bars on displays with different ratios (e.g. 16:10 vs 16:9)
                string ratio = $"{_monitor.Bounds.Width}:{_monitor.Bounds.Height}";
                _mediaPlayer.CropGeometry = ratio;
                _mediaPlayer.Volume = _currentVolume;
                _mediaPlayer.Mute = _isMuted;
                
                // Set VideoView AFTER all config so VLC uses our HWND from the start
                _videoView.MediaPlayer = _mediaPlayer;
            }

            _currentMedia?.Dispose();
            _currentMedia = new Media(_sharedLibVlc, new Uri(videoPath));
            _mediaPlayer.Play(_currentMedia);

            // Asynchronously ensure VLC surfaces become click-through
            StartTransparencyEnforcer();
        }

        public void SetVolume(int volume, bool isMuted)
        {
            _currentVolume = volume;
            _isMuted = isMuted;

            if (_mediaPlayer is not null)
            {
                _mediaPlayer.Volume = volume;
                _mediaPlayer.Mute = isMuted;
            }
        }

        public void SetPlaybackRate(float rate)
        {
            if (_mediaPlayer is not null)
            {
                _mediaPlayer.SetRate(rate);
            }
        }

        private void StartTransparencyEnforcer()
        {
            int ticks = 0;
            var timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (s, e) =>
            {
                ticks++;
                if (this.IsDisposed || !this.IsHandleCreated)
                {
                    timer.Stop();
                    timer.Dispose();
                    return;
                }

                LiveWallpaperApp.Native.Win32.MakeWindowAndChildrenTransparent(this.Handle);

                // Stop after 3 seconds (6 ticks)
                if (ticks >= 6)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        public void Pause() => _mediaPlayer?.Pause();
        
        public void Resume() => _mediaPlayer?.Play();

        public void Stop()
        {
            _mediaPlayer?.Stop();
            _currentPath = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mediaPlayer?.Stop();
                _mediaPlayer?.Dispose();
                _currentMedia?.Dispose();
                _videoView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
