using System;

namespace Lattice.Hub.Math
{
    [Serializable]
    public struct Vec3f
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3f(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static Vec3f operator +(Vec3f a, Vec3f b) => new Vec3f(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3f operator -(Vec3f a, Vec3f b) => new Vec3f(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3f operator *(Vec3f a, float s) => new Vec3f(a.X * s, a.Y * s, a.Z * s);
    }

    [Serializable]
    public struct Vec2f
    {
        public float X;
        public float Y;
    }

    [Serializable]
    public struct Pixel
    {
        public byte B;
        public byte G;
        public byte R;
        public byte A;
    }
}
