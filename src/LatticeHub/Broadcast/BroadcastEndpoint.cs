using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Lattice.Hub.Fusion;
using Lattice.Hub.Math;

namespace Lattice.Hub.Broadcast
{
    public class BroadcastEndpoint : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly Thread _accept;
        private readonly List<TcpClient> _subscribers = new List<TcpClient>();
        private readonly object _lock = new object();
        private volatile bool _running = true;

        public BroadcastEndpoint()
        {
            _listener = new TcpListener(IPAddress.Any, Protocol.BroadcastPort);
            _listener.Start();
            _accept = new Thread(AcceptLoop) { IsBackground = true, Name = "Broadcast" };
            _accept.Start();
        }

        public void Publish(FusedScene scene)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Protocol.Magic);
                bw.Write(Protocol.Version);
                bw.Write((ushort)0);
                bw.Write(scene.Count);
                for (int i = 0; i < scene.Count; i++)
                {
                    bw.Write(scene.Vertices[i].X);
                    bw.Write(scene.Vertices[i].Y);
                    bw.Write(scene.Vertices[i].Z);
                }
                for (int i = 0; i < scene.Count; i++)
                {
                    var c = scene.Colors[i];
                    bw.Write(c.R); bw.Write(c.G); bw.Write(c.B);
                }
                var payload = ms.ToArray();

                List<TcpClient> snap;
                lock (_lock) snap = new List<TcpClient>(_subscribers);
                foreach (var client in snap)
                {
                    try { client.GetStream().Write(payload, 0, payload.Length); }
                    catch { Drop(client); }
                }
            }
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    var c = _listener.AcceptTcpClient();
                    lock (_lock) _subscribers.Add(c);
                }
                catch
                {
                    if (!_running) return;
                }
            }
        }

        private void Drop(TcpClient c)
        {
            lock (_lock) _subscribers.Remove(c);
            try { c.Close(); } catch { }
        }

        public void Dispose()
        {
            _running = false;
            try { _listener.Stop(); } catch { }
            lock (_lock)
            {
                foreach (var c in _subscribers) try { c.Close(); } catch { }
                _subscribers.Clear();
            }
        }
    }
}
