using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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

    public void Host(int port)
    {
        isHost = true;
        
        m_port = port;

        IPAddress localAddr = IPAddress.Parse("127.0.0.1");

        server = new TcpListener(localAddr, m_port);

        server.Start();

        connectedClient = server.AcceptTcpClient();

        partyReady = true;

        byte[] data = ObjectToByteArray(true);

        stream.Write(data, 0, 1);
    }

    public void StopHost()
    {
        connectedClient.Close();

        server.Stop();
    }

    public void Join(string ip, int port)
    {
        currClient = new TcpClient(ip, port);

        stream = currClient.GetStream();

        byte[] bytes = new byte[256];
        stream.Read(bytes, 0, bytes.Length);
        partyReady = (bool)ByteArrayToObject(bytes);
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
        if (partyReady)
            Communicate();
    }

    private void Communicate()
    {
        if (isHost)
        {
            SendMessage("Bienvenue dans la partie");
        }
        else
        {
            string welcomeMessage = ReceiveNetMessage();
            Debug.Log(welcomeMessage);

            SendMessage("Heureux d'être parmis vous !");
        }

        if (isHost)
        {
            string enjoyMessage = ReceiveNetMessage();
            Debug.Log(enjoyMessage);
        }
    }
}
