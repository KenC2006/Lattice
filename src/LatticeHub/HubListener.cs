using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Lattice.Hub
{
    public class HubListener : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly Thread _accept;
        private readonly Thread _pump;
        private readonly object _lock = new object();
        private readonly List<ClientChannel> _channels = new List<ClientChannel>();
        private volatile bool _running = true;

        public event Action<ClientChannel> ChannelOpened;
        public event Action<ClientChannel> ChannelClosed;

        public HubListener()
        {
            _listener = new TcpListener(IPAddress.Any, Protocol.CapturePort);
            _listener.Start();
            _accept = new Thread(AcceptLoop) { IsBackground = true, Name = "Hub-Accept" };
            _pump = new Thread(PumpLoop) { IsBackground = true, Name = "Hub-Pump" };
            _accept.Start();
            _pump.Start();
        }

        public IReadOnlyList<ClientChannel> Snapshot()
        {
            lock (_lock) return _channels.ToArray();
        }

        public void Broadcast(Inbound op)
        {
            foreach (var c in Snapshot()) c.SendCommand(op);
        }

        public void Broadcast(HubSettings s)
        {
            foreach (var c in Snapshot()) c.SendSettings(s);
        }

        private void AcceptLoop()
        {
            ushort nextId = 0;
            while (_running)
            {
                try
                {
                    var sock = _listener.AcceptSocket();
                    var ch = new ClientChannel(sock, nextId++);
                    lock (_lock) _channels.Add(ch);
                    ChannelOpened?.Invoke(ch);
                }
                catch
                {
                    if (!_running) break;
                }
            }
        }

        private void PumpLoop()
        {
            while (_running)
            {
                ClientChannel[] list;
                lock (_lock) list = _channels.ToArray();

                bool any = false;
                foreach (var ch in list)
                {
                    try
                    {
                        if (ch.Pump()) any = true;
                    }
                    catch
                    {
                        ch.Dispose();
                        lock (_lock) _channels.Remove(ch);
                        ChannelClosed?.Invoke(ch);
                    }
                }
                if (!any) Thread.Sleep(2);
            }
        }

        public void Dispose()
        {
            _running = false;
            try { _listener.Stop(); } catch { }
            foreach (var ch in Snapshot()) ch.Dispose();
        }
    }
}
