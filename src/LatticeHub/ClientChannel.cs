using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

using Lattice.Hub.Math;

namespace Lattice.Hub
{
    public class ClientChannel : IDisposable
    {
        public string RemoteEndpoint { get; }
        public ushort SensorId { get; set; }
        public bool Calibrated { get; private set; }
        public RigidTransform World { get; set; } = new RigidTransform();
        public List<AnchorPose> Anchors { get; } = new List<AnchorPose>();

        public List<Vec3f> LastVertices { get; private set; } = new List<Vec3f>();
        public List<Pixel> LastColors { get; private set; } = new List<Pixel>();
        public readonly List<List<Vec3f>> StoredVertices = new List<List<Vec3f>>();
        public readonly List<List<Pixel>> StoredColors = new List<List<Pixel>>();

        private readonly Socket _socket;
        private readonly NetworkStream _stream;
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;
        private readonly object _wlock = new object();

        public event Action<ClientChannel> FrameArrived;
        public event Action<ClientChannel> CalibrationCompleted;

        public ClientChannel(Socket socket, ushort sensorId)
        {
            _socket = socket;
            SensorId = sensorId;
            _socket.NoDelay = true;
            _stream = new NetworkStream(_socket, ownsSocket: true);
            _reader = new BinaryReader(_stream);
            _writer = new BinaryWriter(_stream);
            RemoteEndpoint = _socket.RemoteEndPoint?.ToString() ?? "unknown";
        }

        public void SendCommand(Inbound op)
        {
            lock (_wlock) _writer.Write((byte)op);
        }

        public void SendSettings(HubSettings s)
        {
            lock (_wlock)
            {
                _writer.Write((byte)Inbound.ApplySettings);
                WriteVec3(s.MinBounds);
                WriteVec3(s.MaxBounds);
                _writer.Write(s.Filter);
                _writer.Write(s.BodyOnly);
                _writer.Write(s.MarkerId);
                _writer.Write(s.Compress);
            }
        }

        public void PushCalibration(RigidTransform t, int markerId)
        {
            lock (_wlock)
            {
                _writer.Write((byte)Inbound.PushCalibration);
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++) _writer.Write(t.R[i, j]);
                for (int i = 0; i < 3; i++) _writer.Write(t.T[i]);
                _writer.Write(markerId);
            }
        }

        public bool Pump()
        {
            if (!_socket.Connected || _socket.Available < 1) return false;
            byte op = _reader.ReadByte();
            switch ((Outbound)op)
            {
                case Outbound.GrabAck:
                    break;
                case Outbound.CalibrationAck:
                    Calibrated = true;
                    CalibrationCompleted?.Invoke(this);
                    break;
                case Outbound.StoredFrame:
                case Outbound.LastFrame:
                    ReadFrame((Outbound)op);
                    break;
            }
            return true;
        }

        private void ReadFrame(Outbound kind)
        {
            uint magic = _reader.ReadUInt32();
            ushort version = _reader.ReadUInt16();
            ushort sensor = _reader.ReadUInt16();
            uint count = _reader.ReadUInt32();
            ulong ticks = _reader.ReadUInt64();
            byte compressed = _reader.ReadByte();
            _reader.ReadBytes(3);
            SensorId = sensor;

            var verts = new List<Vec3f>((int)count);
            var cols = new List<Pixel>((int)count);
            for (uint i = 0; i < count; i++)
            {
                short x = _reader.ReadInt16();
                short y = _reader.ReadInt16();
                short z = _reader.ReadInt16();
                verts.Add(new Vec3f(x * 0.001f, y * 0.001f, z * 0.001f));
            }
            for (uint i = 0; i < count; i++)
            {
                cols.Add(new Pixel {
                    B = _reader.ReadByte(),
                    G = _reader.ReadByte(),
                    R = _reader.ReadByte(),
                    A = _reader.ReadByte(),
                });
            }

            if (kind == Outbound.LastFrame)
            {
                LastVertices = verts;
                LastColors = cols;
            }
            else
            {
                StoredVertices.Add(verts);
                StoredColors.Add(cols);
            }
            FrameArrived?.Invoke(this);
        }

        private void WriteVec3(Vec3f v)
        {
            _writer.Write(v.X); _writer.Write(v.Y); _writer.Write(v.Z);
        }

        public void Dispose()
        {
            try { _stream?.Close(); } catch { }
            try { _socket?.Close(); } catch { }
        }
    }
}
