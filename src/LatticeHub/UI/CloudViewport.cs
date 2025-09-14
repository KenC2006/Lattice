using System;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Lattice.Hub.Fusion;

namespace Lattice.Hub.UI
{
    public class CloudViewport : GLControl
    {
        private float _yaw = 0;
        private float _pitch = 0;
        private float _zoom = 3.5f;
        private System.Drawing.Point _lastMouse;
        private bool _dragging;

        public FusedScene Scene { get; set; }

        public CloudViewport() : base(new GraphicsMode(32, 24, 0, 4))
        {
            Paint += OnPaint;
            Resize += (s, e) => Invalidate();
            MouseDown += (s, e) => { _dragging = true; _lastMouse = e.Location; };
            MouseUp += (s, e) => _dragging = false;
            MouseMove += OnDrag;
            MouseWheel += (s, e) => { _zoom *= e.Delta > 0 ? 0.9f : 1.1f; Invalidate(); };
        }

        private void OnDrag(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _yaw += (e.X - _lastMouse.X) * 0.01f;
            _pitch += (e.Y - _lastMouse.Y) * 0.01f;
            _lastMouse = e.Location;
            Invalidate();
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            MakeCurrent();
            GL.Viewport(0, 0, Width, Height);
            GL.ClearColor(0.07f, 0.08f, 0.10f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);

            var proj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, (float)Width / Math.Max(1, Height), 0.05f, 50f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref proj);

            var view = Matrix4.LookAt(
                new Vector3((float)Math.Sin(_yaw) * _zoom, 1.0f + _pitch, (float)Math.Cos(_yaw) * _zoom),
                Vector3.Zero, Vector3.UnitY);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref view);

            DrawAxes();
            DrawCloud();

            SwapBuffers();
        }

        private void DrawAxes()
        {
            GL.LineWidth(1.0f);
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(1.0f, 0.2f, 0.2f); GL.Vertex3(0, 0, 0); GL.Vertex3(0.3f, 0, 0);
            GL.Color3(0.2f, 1.0f, 0.2f); GL.Vertex3(0, 0, 0); GL.Vertex3(0, 0.3f, 0);
            GL.Color3(0.2f, 0.4f, 1.0f); GL.Vertex3(0, 0, 0); GL.Vertex3(0, 0, 0.3f);
            GL.End();
        }

        private void DrawCloud()
        {
            if (Scene == null || Scene.Count == 0) return;
            GL.PointSize(2.0f);
            GL.Begin(PrimitiveType.Points);
            for (int i = 0; i < Scene.Count; i++)
            {
                var c = Scene.Colors[i];
                GL.Color3(c.R / 255f, c.G / 255f, c.B / 255f);
                var p = Scene.Vertices[i];
                GL.Vertex3(p.X, p.Y, p.Z);
            }
            GL.End();
        }
    }
}
