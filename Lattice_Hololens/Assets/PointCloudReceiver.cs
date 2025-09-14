using UnityEngine;
using System;

#if WINDOWS_UWP
using NetworkCommunication;
#else
using System.Net.Sockets;
#endif

public class PointCloudReceiver : MonoBehaviour
{
#if WINDOWS_UWP
    private TransferSocket networkSocket;
#else
    private TcpClient networkSocket;
#endif
    
    [Header("Network Configuration")]
    public int port = 48002;

    private PointCloudRenderer cloudRenderer;
    private bool isReadyForFrame = true;
    private bool isConnected = false;

    void Start()
    {
        cloudRenderer = GetComponent<PointCloudRenderer>();
    }

    void Update()
    {
        if (!isConnected) return;

        ProcessFrameData();
    }

    private void ProcessFrameData()
    {
        if (isReadyForFrame)
        {
            RequestNewFrame();
            isReadyForFrame = false;
        }

        if (TryReceiveFrame(out float[] vertices, out byte[] colors))
        {
            cloudRenderer.Render(vertices, colors);
            isReadyForFrame = true;
        }
    }

    public void Connect(string ipAddress)
    {
#if WINDOWS_UWP
        networkSocket = new TransferSocket(ipAddress, port);
#else
        networkSocket = new TcpClient(ipAddress, port);
#endif
        isConnected = true;
        Debug.Log($"Connected to {ipAddress}:{port}");
    }

    private void RequestNewFrame()
    {
        Debug.Log("Requesting frame");
#if WINDOWS_UWP
        networkSocket.RequestFrame();
        networkSocket.ReceiveFrameAsync();
#else
        SendFrameRequest();
#endif
    }

    private bool TryReceiveFrame(out float[] vertices, out byte[] colors)
    {
#if WINDOWS_UWP
        if (networkSocket.GetFrame(out vertices, out colors))
        {
            Debug.Log("Frame received");
            return true;
        }
#else
        if (ReceiveFrameData(out vertices, out colors))
        {
            Debug.Log("Frame received");
            return true;
        }
#endif
        vertices = null;
        colors = null;
        return false;
    }

#if !WINDOWS_UWP
    private void SendFrameRequest()
    {
        byte[] requestByte = { 0 };
        networkSocket.GetStream().Write(requestByte, 0, 1);
    }

    private int ReadInteger()
    {
        byte[] buffer = new byte[4];
        int bytesRead = 0;
        
        while (bytesRead < 4)
            bytesRead += networkSocket.GetStream().Read(buffer, bytesRead, 4 - bytesRead);

        return BitConverter.ToInt32(buffer, 0);
    }

    private bool ReceiveFrameData(out float[] vertices, out byte[] colors)
    {
        int pointCount = ReadInteger();

        vertices = new float[3 * pointCount];
        short[] shortVertices = new short[3 * pointCount];
        colors = new byte[3 * pointCount];

        ReadVertexData(shortVertices, pointCount);
        ConvertVertices(shortVertices, vertices);
        ReadColorData(colors, pointCount);

        return true;
    }

    private void ReadVertexData(short[] shortVertices, int pointCount)
    {
        int bytesToRead = sizeof(short) * 3 * pointCount;
        int bytesRead = 0;
        byte[] buffer = new byte[bytesToRead];

        while (bytesRead < bytesToRead)
        {
            int chunkSize = Math.Min(bytesToRead - bytesRead, 64000);
            bytesRead += networkSocket.GetStream().Read(buffer, bytesRead, chunkSize);
        }

        System.Buffer.BlockCopy(buffer, 0, shortVertices, 0, bytesToRead);
    }

    private void ConvertVertices(short[] shortVertices, float[] vertices)
    {
        for (int i = 0; i < shortVertices.Length; i++)
            vertices[i] = shortVertices[i] / 1000.0f;
    }

    private void ReadColorData(byte[] colors, int pointCount)
    {
        int bytesToRead = sizeof(byte) * 3 * pointCount;
        int bytesRead = 0;
        byte[] buffer = new byte[bytesToRead];

        while (bytesRead < bytesToRead)
        {
            int chunkSize = Math.Min(bytesToRead - bytesRead, 64000);
            bytesRead += networkSocket.GetStream().Read(buffer, bytesRead, chunkSize);
        }

        System.Buffer.BlockCopy(buffer, 0, colors, 0, bytesToRead);
    }
#endif
}
