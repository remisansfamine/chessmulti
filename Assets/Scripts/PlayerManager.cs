using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.Events;

public class PlayerManager : MonoBehaviour
{
    bool isHost = false;
    bool partyReady = false;

    #region Client
    TcpClient currClient = null;
    #endregion

    #region Host
    TcpListener server = null;
    TcpClient connectedClient = null;
    #endregion

    NetworkStream stream = null;

    public int m_port = 30000;

    public UnityEvent OnPartyReady = new UnityEvent();
    public UnityEvent OnGameStartEvent = new UnityEvent();

    public static byte[] ObjectToByteArray(object obj)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (var ms = new MemoryStream())
        {
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    public static object ByteArrayToObject(byte[] arrBytes)
    {
        using (var memStream = new MemoryStream())
        {
            var binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);

            return binForm.Deserialize(memStream);
        }
    }

    async void WaitPlayer()
    {
        connectedClient = await server.AcceptTcpClientAsync();

        stream = connectedClient.GetStream();

        partyReady = true;

        byte[] data = BitConverter.GetBytes(true);
        stream.Write(data, 0, sizeof(bool));
    }

    async void StartServer(int port)
    {
        isHost = true;

        m_port = port;

        IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, port);

        server = new TcpListener(serverEP);

        server.Start();

        WaitPlayer();
    }

    public void Host(int port)
    {
        StartServer(port);
    }

    public void StopHost()
    {
        connectedClient.Close();

        server.Stop();
    }

    public void SendPacket()
    {
        Packet packet = new Packet();

        byte[] bytes = packet.Serialize(EPacketType.MESSAGE, "SetReady");

        stream.Write(bytes);
    }

    private void InterpretPacket(Packet toInterpret)
    {
        switch (toInterpret.header.type)
        {
            case EPacketType.MOVEMENTS:
                break;

            case EPacketType.MESSAGE:
                string message = toInterpret.FillObject<string>();
                SendMessage(message);
                break;

            case EPacketType.UNDEFINED:
            default:
                break;
        }
    }

    public void ListenPackets()
    {
        int headerSize = Packet.PacketSize();

        byte[] headerBytes = new byte[headerSize];
        stream.Read(headerBytes);

        Packet packet = Packet.DeserializeHeader(headerBytes);

        packet.datas = new byte[packet.header.size];
        stream.Read(packet.datas);

        InterpretPacket(packet);
    }

    async public void WaitToJoin()
    {
        byte[] bytes = new byte[sizeof(bool)];
        await stream.ReadAsync(bytes, 0, bytes.Length);

        partyReady = BitConverter.ToBoolean(bytes, 0);
    }

    public void Join(string ip, int port)
    {
        currClient = new TcpClient(ip, port);

        stream = currClient.GetStream();

        WaitToJoin();
    }

    public void DisconnectFromServer()
    {
        stream.Close();
        currClient.Close();
    }

    public string ReceiveNetMessage()
    {
        byte[] data = new byte[256];

        int bytes = stream.Read(data, 0, data.Length);
        
        return System.Text.Encoding.ASCII.GetString(data, 0, bytes);
    }

    public void SendNetMessage(string message)
    {
        try
        {
            byte[] datas = Encoding.ASCII.GetBytes(message);
            stream.Write(datas, 0, datas.Length);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending message {e}");
        }
    }

    public void Update()
    {

    }

    public void SetReady()
    {
        partyReady = true;

        if (isHost)
        {
            SendPacket();
        }
        else
            ListenPackets();

        OnPartyReady.Invoke();
    }

    public void StartGame()
    {
        if (!partyReady)
            return;

        if (isHost)
        {
            byte[] data = BitConverter.GetBytes(true);
            stream.Write(data, 0, sizeof(bool));
        }

        OnGameStartEvent.Invoke();
    }
}
