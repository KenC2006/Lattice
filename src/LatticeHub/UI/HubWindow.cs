using System;
using System.Drawing;
using System.Windows.Forms;

using Lattice.Hub.Broadcast;
using Lattice.Hub.Fusion;

namespace Lattice.Hub.UI
{
    public class HubWindow : Form
    {
        private readonly HubListener _listener;
        private readonly BroadcastEndpoint _broadcast;
        private readonly Recorder _recorder = new Recorder();
        private readonly FusedScene _scene = new FusedScene();
        private readonly CloudViewport _viewport;
        private readonly ListBox _clientList;
        private readonly Label _status;
        private readonly Timer _refreshTimer;
        private HubSettings _settings = HubSettings.LoadFrom("hub-settings.xml");
        private bool _recording;

        public HubWindow()
        {
            Text = "Lattice Hub";
            Width = 1024;
            Height = 720;
            StartPosition = FormStartPosition.CenterScreen;

            _viewport = new CloudViewport { Dock = DockStyle.Fill, Scene = _scene };

            _clientList = new ListBox { Width = 220, Dock = DockStyle.Left };

            var captureBtn = new Button { Text = "Capture", Dock = DockStyle.Top };
            captureBtn.Click += (s, e) => _listener.Broadcast(Inbound.GrabFrame);

            var calibrateBtn = new Button { Text = "Calibrate", Dock = DockStyle.Top };
            calibrateBtn.Click += (s, e) => _listener.Broadcast(Inbound.RunCalibration);

            var recordBtn = new Button { Text = "Record", Dock = DockStyle.Top };
            recordBtn.Click += (s, e) => ToggleRecord(recordBtn);

            var pushSettingsBtn = new Button { Text = "Sync Settings", Dock = DockStyle.Top };
            pushSettingsBtn.Click += (s, e) => _listener.Broadcast(_settings);

            _status = new Label { Dock = DockStyle.Bottom, Height = 24, BackColor = Color.DarkSlateGray, ForeColor = Color.White };

            var side = new Panel { Dock = DockStyle.Left, Width = 220 };
            side.Controls.Add(_clientList);
            side.Controls.Add(captureBtn);
            side.Controls.Add(calibrateBtn);
            side.Controls.Add(recordBtn);
            side.Controls.Add(pushSettingsBtn);

            Controls.Add(_viewport);
            Controls.Add(side);
            Controls.Add(_status);

            _listener = new HubListener();
            _listener.ChannelOpened += ch => Invoke((Action)(() => _clientList.Items.Add($"#{ch.SensorId} {ch.RemoteEndpoint}")));
            _broadcast = new BroadcastEndpoint();

            _refreshTimer = new Timer { Interval = 50 };
            _refreshTimer.Tick += (s, e) => Refresh();
            _refreshTimer.Start();

            FormClosing += (s, e) => Shutdown();
        }

        private void ToggleRecord(Button b)
        {
            if (_recording)
            {
                _recorder.Close();
                _recording = false;
                b.Text = "Record";
            }
            else
            {
                _recorder.Open($"capture-{DateTime.Now:yyyyMMdd-HHmmss}.lat");
                _recording = true;
                b.Text = "Stop";
            }
        }

        private void Refresh()
        {
            var channels = _listener.Snapshot();
            _scene.Clear();
            foreach (var ch in channels)
            {
                _scene.Append(ch.LastVertices, ch.LastColors, ch.World);
            }
            if (channels.Count > 1)
            {
                Lattice.Hub.Fusion.RegistrationPass.Refine(channels, maxIterations: 6);
            }
            if (_recording) _recorder.Append(_scene);
            _broadcast.Publish(_scene);
            _viewport.Invalidate();
            _status.Text = $" clients: {channels.Count}   points: {_scene.Count}";
        }

        private void Shutdown()
        {
            try { _settings.SaveTo("hub-settings.xml"); } catch { }
            _recorder.Dispose();
            _listener.Dispose();
            _broadcast.Dispose();
        }
    }
}
