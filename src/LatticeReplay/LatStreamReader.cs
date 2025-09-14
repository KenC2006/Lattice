using System.Collections.Generic;
using System.IO;

namespace Lattice.Replay
{
    public class LatStreamReader : IFrameSource
    {
        private readonly FileStream _stream;
        private readonly BinaryReader _reader;
        private readonly List<long> _index = new List<long>();
        private int _cursor;

        public int FrameCount => _index.Count;
        public int CurrentIndex => _cursor;

        public LatStreamReader(string path)
        {
            _stream = File.OpenRead(path);
            _reader = new BinaryReader(_stream);
            _reader.ReadUInt32();
            _reader.ReadUInt16();
            _reader.ReadUInt16();
            BuildIndex();
        }

        private void BuildIndex()
        {
            while (_stream.Position < _stream.Length)
            {
                long mark = _stream.Position;
                int n;
                try { n = _reader.ReadInt32(); }
                catch (EndOfStreamException) { break; }
                _reader.ReadInt32();
                _index.Add(mark);
                _stream.Seek(n * 6 + n * 4, SeekOrigin.Current);
            }
        }

        public bool TryRead(int index, List<ReplayPoint> outPoints)
        {
            if (index < 0 || index >= _index.Count) return false;
            _stream.Seek(_index[index], SeekOrigin.Begin);
            int n = _reader.ReadInt32();
            _reader.ReadInt32();
            outPoints.Clear();
            outPoints.Capacity = System.Math.Max(outPoints.Capacity, n);

            var raw = new ReplayPoint[n];
            for (int i = 0; i < n; i++)
            {
                raw[i].X = _reader.ReadInt16() * 0.001f;
                raw[i].Y = _reader.ReadInt16() * 0.001f;
                raw[i].Z = _reader.ReadInt16() * 0.001f;
            }
            for (int i = 0; i < n; i++)
            {
                raw[i].B = _reader.ReadByte();
                raw[i].G = _reader.ReadByte();
                raw[i].R = _reader.ReadByte();
                _reader.ReadByte();
                outPoints.Add(raw[i]);
            }
            _cursor = index;
            return true;
        }

        public void Close()
        {
            _reader.Dispose();
            _stream.Dispose();
        }
    }
}
