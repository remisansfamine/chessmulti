using UnityEngine;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Events;


public class Host : NetworkUser
{

    #region Variables

    TcpListener server = null;
    TcpClient connectedClient = null;

    protected List<NetworkStream> m_spectatorsStream = new List<NetworkStream>();

    #endregion

    #region MonoBehaviour

    private void OnDestroy()
    {
        Disconnect();
    }

    #endregion

    #region Functions


    public override void SendChatMessage(Message message)
    {
        SendPacket(EPacketType.CHAT_MESSAGE, message);
        SendSpectatorsPacket(EPacketType.CHAT_MESSAGE, message);
    }

    public void SendSpectatorsPacket(EPacketType type, object toSend)
    {
        foreach (NetworkStream stream in m_spectatorsStream)
        {
            stream.Write(Packet.SerializePacket(type, toSend));
        }
    }


    public async void WaitPlayer()
    {
        try
        {
            connectedClient = await server.AcceptTcpClientAsync();

            m_stream = connectedClient.GetStream();

        }
        catch (Exception e)
        {
            Debug.LogError("Server seems stopped " + e);
        }
        finally
        {
            if (m_stream != null) ChessGameMgr.Instance.EnableAI(false);

            ListenPackets();
        }
    }

    public void OpenServer(int port)
    {
        m_port = port;

        try
        {
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, port);

            server = new TcpListener(serverEP);

            server.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("Error during server creation  " + e);
        }

        WaitPlayer();
    }

    public override void ListenPackets()
    {
        // Create 2 different threads to avoid getting stuck on client read
        ListeClientPackets();
        ListeSpectatorPackets();
    }

    public async void ListeClientPackets()
    {
        while (m_stream != null)
        {
            int headerSize = Packet.PacketSize();

            byte[] headerBytes = new byte[headerSize];

            try
            {
                await m_stream.ReadAsync(headerBytes);

                Packet packet = Packet.DeserializeHeader(headerBytes);

                packet.datas = new byte[packet.header.size];
                await m_stream.ReadAsync(packet.datas);

                InterpretPacket(packet);
            }
            catch (IOException ioe)
            {
                ListenPacketCatch(ioe);
            }
            catch (Exception e)
            {
                ListenPacketCatch(e);
            }
        }
    }

    public async void ListeSpectatorPackets()
    {
        while (m_spectatorsStream.Count > 0)
        {
            foreach(NetworkStream stream in m_spectatorsStream)
            {

            }
        }
    }

    protected override void ExecuteMovement(Packet toExecute)
    {
        ChessGameMgr.Move move = toExecute.FillObject<ChessGameMgr.Move>();

        ChessGameMgr.Instance.CheckMove(move);
    }

    protected override void InterpretPacket(Packet toInterpret)
    {
        switch (toInterpret.header.type)
        {
            case EPacketType.MOVEMENTS:
                ExecuteMovement(toInterpret);
                break;
            case EPacketType.MOVE_VALIDITY:
                break;

            default:
                base.InterpretPacket(toInterpret);
                break;
        }
    }

    protected override void ListenPacketCatch(IOException ioe)
    {
        OnClientDisconnection();
    }

    protected override void ListenPacketCatch(Exception e)
    {
        OnClientDisconnection();
    }


    private void OnClientDisconnection()
    {
        m_stream.Close();
        m_stream = null;

        ChessGameMgr.Instance.EnableAI(true);
    }

    public new void Disconnect()
    {
        try
        {
            connectedClient?.Close();

            base.Disconnect();
        }
        catch (Exception e)
        {
            Debug.LogError("Error during server closing " + e);
        }
        finally
        {
            server.Stop();
        }
    }
    #endregion
}
