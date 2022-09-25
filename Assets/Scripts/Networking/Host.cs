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

    Dictionary<NetworkStream, ChessGameMgr.EChessTeam> clientsDatas = new Dictionary<NetworkStream, ChessGameMgr.EChessTeam>();

    protected List<NetworkStream> m_clientStreams = new List<NetworkStream>();

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
    }

    public override void SendPacket(EPacketType type, object toSend)
    {
        foreach(NetworkStream stream in m_clientStreams)
        {
            stream?.Write(Packet.SerializePacket(type, toSend));
        }
    }


    public async void WaitPlayer()
    {
        try
        {
            connectedClient = await server.AcceptTcpClientAsync();

            NetworkStream newStream = connectedClient.GetStream();
            m_clientStreams.Add(newStream);

        }
        catch (Exception e)
        {
            Debug.LogError("Server seems stopped " + e);
        }
        finally
        {
            if (!HasPlayer()) ChessGameMgr.Instance.EnableAI(false);

            foreach(NetworkStream stream in m_clientStreams)
            {
                ListeClientPackets(stream);
            }
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

    public async void ListeClientPackets(NetworkStream stream)
    {
        while (stream != null)
        {
            int headerSize = Packet.PacketSize();

            byte[] headerBytes = new byte[headerSize];

            try
            {
                await stream.ReadAsync(headerBytes);

                Packet packet = Packet.DeserializeHeader(headerBytes);

                packet.datas = new byte[packet.header.size];
                await stream.ReadAsync(packet.datas);

                InterpretPacket(packet);
            }
            catch (IOException ioe)
            {
                ListenPacketCatch(ioe, stream);
                return;
            }
            catch (Exception e)
            {
                ListenPacketCatch(e, stream);
                return;
            }
        }
    }

    protected override void ExecuteMovement(Packet toExecute)
    {
        ChessGameMgr.Move move = toExecute.FillObject<ChessGameMgr.Move>();

        ChessGameMgr.Instance.CheckMove(move);
    }

    protected void ExecuteTeamInfo(Packet toExecute, NetworkStream stream)
    {
        clientsDatas.Add(stream, toExecute.FillObject<ChessGameMgr.EChessTeam>());
    }

    protected void InterpretPacket(Packet toInterpret, NetworkStream stream)
    {
        switch (toInterpret.header.type)
        {
            case EPacketType.MOVEMENTS:
                ExecuteMovement(toInterpret);
                break;
            case EPacketType.MOVE_VALIDITY:
                break;

            case EPacketType.TEAM_INFO:
                ExecuteTeamInfo(toInterpret, stream);
                break;
            default:
                base.InterpretPacket(toInterpret);
                break;
        }
    }


    protected void ListenPacketCatch(IOException ioe, NetworkStream stream)
    {
        OnClientDisconnection(stream);
    }

    protected void ListenPacketCatch(Exception e, NetworkStream stream)
    {
        OnClientDisconnection(stream);
    }


    private void OnClientDisconnection(NetworkStream stream)
    {
        if(clientsDatas[stream] != ChessGameMgr.EChessTeam.None)
        {
            ChessGameMgr.Instance.EnableAI(true);
        }

        clientsDatas.Remove(stream);

        stream?.Close();

        m_clientStreams.Remove(stream);

        ChessGameMgr.Instance.EnableAI(true);
    }

    public new void Disconnect()
    {
        try
        {
            foreach (NetworkStream stream in m_clientStreams) stream?.Close();

            m_clientStreams.Clear();

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

    public bool HasPlayer()
    {
        foreach(var element in clientsDatas)
        {
            if(element.Value == ChessGameMgr.EChessTeam.White ||
               element.Value == ChessGameMgr.EChessTeam.Black)
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}
