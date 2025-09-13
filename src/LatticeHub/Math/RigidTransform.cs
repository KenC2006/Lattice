using System;

namespace Lattice.Hub.Math
{
    [Serializable]
    public class RigidTransform
    {
        public float[,] R;
        public float[] T;

        public RigidTransform()
        {
            R = new float[3, 3];
            T = new float[3];
            for (int i = 0; i < 3; i++) R[i, i] = 1;
        }

        public Vec3f Apply(Vec3f p)
        {
            return new Vec3f(
                R[0, 0] * p.X + R[0, 1] * p.Y + R[0, 2] * p.Z + T[0],
                R[1, 0] * p.X + R[1, 1] * p.Y + R[1, 2] * p.Z + T[1],
                R[2, 0] * p.X + R[2, 1] * p.Y + R[2, 2] * p.Z + T[2]);
        }

        public RigidTransform Compose(RigidTransform other)
        {
            var result = new RigidTransform();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    float sum = 0;
                    for (int k = 0; k < 3; k++) sum += R[i, k] * other.R[k, j];
                    result.R[i, j] = sum;
                }
                result.T[i] = R[i, 0] * other.T[0] + R[i, 1] * other.T[1] + R[i, 2] * other.T[2] + T[i];
            }
            return result;
        }
    }

    [Serializable]
    public class AnchorPose
    {
        public RigidTransform Pose = new RigidTransform();
        public int FiducialId = -1;
    }
}
