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

    bool enableListener = false;

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

        SendNetMessage("SetReady");
    }

    async void StartServer(int port)
    {
        isHost = true;

        m_port = port;

        IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, port);

        server = new TcpListener(serverEP);

        server.Start();

        WaitPlayer();

        enableListener = true;

        ListenPackets();
    }

    public void Host(int port)
    {
        StartServer(port);
    }

    public void StopHost()
    {
        enableListener = false;

        connectedClient.Close();

        server.Stop();
    }

    public void SendNetMessage(string message) => SendPacket(EPacketType.UNITY_MESSAGE, message);
    public void SendChatMessage(string message) => SendPacket(EPacketType.CHAT_MESSAGE, message);

    public void SendPacket(EPacketType type, object toSend)
    {
        Packet packet = new Packet();

        byte[] bytes = packet.Serialize(type, toSend);

        stream.Write(bytes);
    }

    private void InterpretPacket(Packet toInterpret)
    {
        switch (toInterpret.header.type)
        {
            case EPacketType.MOVEMENTS:
                break;

            case EPacketType.UNITY_MESSAGE:
                string unity_message = toInterpret.FillObject<string>();
                SendMessage(unity_message);
                break;

            case EPacketType.CHAT_MESSAGE:
                string chat_message = toInterpret.FillObject<string>();
                Debug.Log(chat_message);
                break;

            case EPacketType.UNDEFINED:
            default:
                break;
        }
    }
    public async void ListenPackets()
    {
        while (enableListener)
        {
            int headerSize = Packet.PacketSize();

            byte[] headerBytes = new byte[headerSize];
            await stream.ReadAsync(headerBytes);

            Packet packet = Packet.DeserializeHeader(headerBytes);

            packet.datas = new byte[packet.header.size];
            await stream.ReadAsync(packet.datas);

            InterpretPacket(packet);
        }
    }

    public void Join(string ip, int port)
    {
        currClient = new TcpClient(ip, port);

        stream = currClient.GetStream();

        enableListener = true;

        ListenPackets();
    }

    public void DisconnectFromServer()
    {
        enableListener = false;

        stream.Close();
        currClient.Close();
    }

    public void SetReady()
    {
        partyReady = true;

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
