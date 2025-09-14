using System;
using System.IO;

using Lattice.Hub.Math;

namespace Lattice.Hub.Fusion
{
    public class Recorder : IDisposable
    {
        private BinaryWriter _writer;
        private int _frameIndex;

        public bool Active => _writer != null;
        public string Path { get; private set; }

        public void Open(string path)
        {
            Close();
            Path = path;
            _writer = new BinaryWriter(File.Create(path));
            _writer.Write((uint)0x5441544Cu);
            _writer.Write((ushort)1);
            _writer.Write((ushort)0);
            _frameIndex = 0;
        }

        public void Close()
        {
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }
        }

        public void Append(FusedScene scene)
        {
            if (_writer == null) return;
            _writer.Write(scene.Count);
            _writer.Write(_frameIndex++);
            for (int i = 0; i < scene.Count; i++)
            {
                var p = scene.Vertices[i];
                _writer.Write((short)(p.X * 1000));
                _writer.Write((short)(p.Y * 1000));
                _writer.Write((short)(p.Z * 1000));
            }
            for (int i = 0; i < scene.Count; i++)
            {
                var c = scene.Colors[i];
                _writer.Write(c.B); _writer.Write(c.G); _writer.Write(c.R); _writer.Write(c.A);
            }
        }

        public void Dispose() => Close();
    }
}
