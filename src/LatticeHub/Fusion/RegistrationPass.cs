using System.Collections.Generic;

using Lattice.Hub.Math;
using Lattice.Hub.Native;

namespace Lattice.Hub.Fusion
{
    public static class RegistrationPass
    {
        public static void Refine(IReadOnlyList<ClientChannel> channels, int maxIterations)
        {
            if (channels.Count < 2) return;

            var anchor = channels[0];
            float[] reference = ToFlat(anchor.LastVertices);
            if (reference.Length == 0) return;

            for (int i = 1; i < channels.Count; i++)
            {
                var moving = channels[i];
                float[] src = ToFlat(moving.LastVertices);
                if (src.Length == 0) continue;

                var delta = new RigidTransform();
                AlignBridge.Align(src, reference, delta, maxIterations, 0.8f);
                moving.World = delta.Compose(moving.World);
            }
        }

        private static float[] ToFlat(IList<Vec3f> verts)
        {
            var flat = new float[verts.Count * 3];
            for (int i = 0; i < verts.Count; i++)
            {
                flat[i * 3 + 0] = verts[i].X;
                flat[i * 3 + 1] = verts[i].Y;
                flat[i * 3 + 2] = verts[i].Z;
            }
            return flat;
        }
    }
}
