namespace Lattice.Hub
{
    public static class Protocol
    {
        public const ushort CapturePort = 48201;
        public const ushort BroadcastPort = 48202;
        public const uint Magic = 0x4C415454u;
        public const ushort Version = 1;
    }

    public enum Inbound : byte
    {
        GrabFrame = 0,
        RunCalibration = 1,
        ApplySettings = 2,
        FetchStored = 3,
        FetchLast = 4,
        PushCalibration = 5,
        DropStored = 6,
    }

    public enum Outbound : byte
    {
        GrabAck = 0,
        CalibrationAck = 1,
        StoredFrame = 2,
        LastFrame = 3,
    }
}
