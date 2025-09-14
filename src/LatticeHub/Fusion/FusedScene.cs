using System.Collections.Generic;
using System.Linq;

using Lattice.Hub.Math;

namespace Lattice.Hub.Fusion
{
    public class FusedScene
    {
        public List<Vec3f> Vertices { get; } = new List<Vec3f>();
        public List<Pixel> Colors { get; } = new List<Pixel>();

        public int Count => Vertices.Count;

        public void Clear()
        {
            Vertices.Clear();
            Colors.Clear();
        }

        public void Append(IList<Vec3f> verts, IList<Pixel> cols, RigidTransform transform)
        {
            int n = System.Math.Min(verts.Count, cols.Count);
            Vertices.Capacity = System.Math.Max(Vertices.Capacity, Vertices.Count + n);
            for (int i = 0; i < n; i++)
            {
                Vertices.Add(transform != null ? transform.Apply(verts[i]) : verts[i]);
                Colors.Add(cols[i]);
            }
        }

        public float[] FlatXYZ()
        {
            var flat = new float[Vertices.Count * 3];
            for (int i = 0; i < Vertices.Count; i++)
            {
                flat[i * 3 + 0] = Vertices[i].X;
                flat[i * 3 + 1] = Vertices[i].Y;
                flat[i * 3 + 2] = Vertices[i].Z;
            }
            return flat;
        }
    }
}
