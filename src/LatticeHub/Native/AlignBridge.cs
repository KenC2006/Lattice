using System;
using System.Runtime.InteropServices;

using Lattice.Hub.Math;

namespace Lattice.Hub.Native
{
    public static class AlignBridge
    {
        [DllImport("LatticeAlign.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern float lattice_align(
            [In] float[] sourceXYZ, int sourceCount,
            [In] float[] targetXYZ, int targetCount,
            [Out] float[] outRotation,
            [Out] float[] outTranslation,
            int maxIterations,
            float trimRatio);

        public static float Align(float[] src, float[] dst, RigidTransform output,
                                  int maxIterations = 10, float trimRatio = 0.8f)
        {
            var r = new float[9];
            var t = new float[3];
            float err = lattice_align(src, src.Length / 3, dst, dst.Length / 3,
                                      r, t, maxIterations, trimRatio);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    output.R[i, j] = r[i * 3 + j];
            for (int i = 0; i < 3; i++) output.T[i] = t[i];
            return err;
        }
    }
}
