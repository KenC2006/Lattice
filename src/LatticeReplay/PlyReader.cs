using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Lattice.Replay
{
    public class PlyReader : IFrameSource
    {
        private readonly List<ReplayPoint> _points = new List<ReplayPoint>();

        public int FrameCount => 1;
        public int CurrentIndex => 0;

        public PlyReader(string path) => Parse(path);

        private void Parse(string path)
        {
            using (var reader = new StreamReader(path))
            {
                int vertexCount = 0;
                bool dataStarted = false;
                while (!dataStarted)
                {
                    var line = reader.ReadLine();
                    if (line == null) return;
                    if (line.StartsWith("element vertex"))
                    {
                        var parts = line.Split(' ');
                        vertexCount = int.Parse(parts[2], CultureInfo.InvariantCulture);
                    }
                    else if (line == "end_header")
                    {
                        dataStarted = true;
                    }
                }
                for (int i = 0; i < vertexCount; i++)
                {
                    var fields = reader.ReadLine()?.Split(' ');
                    if (fields == null || fields.Length < 6) continue;
                    _points.Add(new ReplayPoint
                    {
                        X = float.Parse(fields[0], CultureInfo.InvariantCulture),
                        Y = float.Parse(fields[1], CultureInfo.InvariantCulture),
                        Z = float.Parse(fields[2], CultureInfo.InvariantCulture),
                        R = byte.Parse(fields[3]),
                        G = byte.Parse(fields[4]),
                        B = byte.Parse(fields[5]),
                    });
                }
            }
        }

        public bool TryRead(int index, List<ReplayPoint> outPoints)
        {
            if (index != 0) return false;
            outPoints.Clear();
            outPoints.AddRange(_points);
            return true;
        }

        public void Close() { }
    }
}
