using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Lattice.Replay
{
    public class ReplayWindow : Form
    {
        private readonly GLControl _gl;
        private readonly TrackBar _scrub;
        private readonly Button _open;
        private readonly Label _info;
        private readonly Timer _tick = new Timer { Interval = 33 };
        private IFrameSource _source;
        private readonly List<ReplayPoint> _current = new List<ReplayPoint>();
        private bool _playing;
        private float _yaw, _pitch = 0.2f, _zoom = 3.5f;
        private Point _mouseRef;
        private bool _dragging;

        public ReplayWindow()
        {
            Text = "Lattice Replay";
            Width = 960;
            Height = 640;

            _gl = new GLControl(new GraphicsMode(32, 24, 0, 4)) { Dock = DockStyle.Fill };
            _gl.Paint += Render;
            _gl.MouseDown += (s, e) => { _dragging = true; _mouseRef = e.Location; };
            _gl.MouseUp += (s, e) => _dragging = false;
            _gl.MouseMove += OnDrag;
            _gl.MouseWheel += (s, e) => { _zoom *= e.Delta > 0 ? 0.9f : 1.1f; _gl.Invalidate(); };

            _scrub = new TrackBar { Dock = DockStyle.Bottom, Minimum = 0, Maximum = 0 };
            _scrub.Scroll += (s, e) => { Seek(_scrub.Value); };

            _open = new Button { Text = "Open…", Dock = DockStyle.Top, Height = 28 };
            _open.Click += (s, e) => Open();

            var playBtn = new Button { Text = "Play", Dock = DockStyle.Top, Height = 28 };
            playBtn.Click += (s, e) => { _playing = !_playing; playBtn.Text = _playing ? "Pause" : "Play"; };

            _info = new Label { Dock = DockStyle.Top, Height = 22, BackColor = Color.Black, ForeColor = Color.LightGray };

            Controls.Add(_gl);
            Controls.Add(_scrub);
            Controls.Add(_info);
            Controls.Add(playBtn);
            Controls.Add(_open);

            _tick.Tick += (s, e) =>
            {
                if (_playing && _source != null && _scrub.Value < _scrub.Maximum)
                {
                    _scrub.Value = Math.Min(_scrub.Maximum, _scrub.Value + 1);
                    Seek(_scrub.Value);
                }
            };
            _tick.Start();
        }

        private void OnDrag(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _yaw += (e.X - _mouseRef.X) * 0.01f;
            _pitch += (e.Y - _mouseRef.Y) * 0.01f;
            _mouseRef = e.Location;
            _gl.Invalidate();
        }

        private void Open()
        {
            using (var d = new OpenFileDialog { Filter = "Lattice Captures|*.lat;*.ply" })
            {
                if (d.ShowDialog() != DialogResult.OK) return;
                _source?.Close();
                _source = Path.GetExtension(d.FileName).Equals(".ply", StringComparison.OrdinalIgnoreCase)
                    ? (IFrameSource)new PlyReader(d.FileName)
                    : new LatStreamReader(d.FileName);
                _scrub.Maximum = Math.Max(0, _source.FrameCount - 1);
                _scrub.Value = 0;
                Seek(0);
            }
        }

        private void Seek(int frame)
        {
            if (_source == null) return;
            _source.TryRead(frame, _current);
            _info.Text = $" frame {frame + 1} / {_source.FrameCount}    points {_current.Count}";
            _gl.Invalidate();
        }

        private void Render(object sender, PaintEventArgs e)
        {
            _gl.MakeCurrent();
            GL.Viewport(0, 0, _gl.Width, _gl.Height);
            GL.ClearColor(0.05f, 0.05f, 0.08f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);

            var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, (float)_gl.Width / Math.Max(1, _gl.Height), 0.05f, 50f);
            GL.MatrixMode(MatrixMode.Projection); GL.LoadMatrix(ref proj);

            var eye = new Vector3((float)Math.Sin(_yaw) * _zoom, 1.0f + _pitch, (float)Math.Cos(_yaw) * _zoom);
            var view = Matrix4.LookAt(eye, Vector3.Zero, Vector3.UnitY);
            GL.MatrixMode(MatrixMode.Modelview); GL.LoadMatrix(ref view);

            GL.PointSize(2.0f);
            GL.Begin(PrimitiveType.Points);
            foreach (var p in _current)
            {
                GL.Color3(p.R / 255f, p.G / 255f, p.B / 255f);
                GL.Vertex3(p.X, p.Y, p.Z);
            }
            GL.End();

            _gl.SwapBuffers();
        }
    }
}
