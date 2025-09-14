using System.Collections.Generic;

namespace Lattice.Replay
{
    public struct ReplayPoint
    {
        public float X, Y, Z;
        public byte R, G, B;
    }

    public interface IFrameSource
    {
        int FrameCount { get; }
        int CurrentIndex { get; }
        bool TryRead(int index, List<ReplayPoint> outPoints);
        void Close();
    }
}
