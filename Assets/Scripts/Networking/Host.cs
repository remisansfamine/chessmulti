using UnityEngine;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Events;


public class Host : NetworkUser
{
    private class ClientInfo
    {
        public NetworkStream stream;
        public TcpClient tcp;
    }

    #region Variables

    TcpListener server = null;

    Dictionary<NetworkStream, ChessGameMgr.EChessTeam> clientsDatas = new Dictionary<NetworkStream, ChessGameMgr.EChessTeam>();

    private List<ClientInfo> m_clients = new List<ClientInfo>();

    [SerializeField] private uint maxClients = 5;
    public bool acceptClients;

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
        byte[] serializedObject = Packet.SerializePacket(type, toSend);

        foreach(ClientInfo client in m_clients)
            client.stream?.Write(serializedObject);
    }

    public async void WaitPlayer()
    {
        acceptClients = true;

        for (uint i = 0; i < maxClients && acceptClients; i++)
        {
            try
            {
                ClientInfo client = new ClientInfo();

                client.tcp = await server.AcceptTcpClientAsync();

                if (!acceptClients)
                {
                    client.tcp.Close();
                    break;
                }

                client.stream = client.tcp?.GetStream();

                if (client.stream != null)
                {
                    m_clients.Add(client);
                }
                else
                {
                    client.tcp.Close();
                }
            }
            catch (IOException e)
            {
                Debug.LogError("Server seems stopped " + e);
            }
            catch (Exception e)
            {
                Debug.LogError("Server seems stopped " + e);
            }
            finally
            {
                if (m_clients.Count > 0)
                {
                    ListeClientPackets(m_clients[m_clients.Count - 1]);
                }
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

    private async void ListeClientPackets(ClientInfo client)
    {
        while (client != null)
        {
            int headerSize = Packet.PacketSize();

            byte[] headerBytes = new byte[headerSize];

            try
            {
                await client.stream.ReadAsync(headerBytes);

                Packet packet = Packet.DeserializeHeader(headerBytes);

                packet.datas = new byte[packet.header.size];
                await client.stream.ReadAsync(packet.datas);

                InterpretPacket(packet, client.stream);
            }
            catch (IOException ioe)
            {
                ListenPacketCatch(ioe, client);
                return;
            }
            catch (Exception e)
            {
                ListenPacketCatch(e, client);
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


    private void ListenPacketCatch(IOException ioe, ClientInfo client)
    {
        OnClientDisconnection(client);
    }

    private void ListenPacketCatch(Exception e, ClientInfo client)
    {
        OnClientDisconnection(client);
    }


    private void OnClientDisconnection(ClientInfo client)
    {
        clientsDatas.Remove(client.stream);

        m_clients.Remove(client);

        if (!HasPlayer())
        {
            ChessGameMgr.Instance.EnableAI(true);
        }
    }

    public new void Disconnect()
    {
        try
        {
            foreach (ClientInfo client in m_clients)
                client.tcp?.Close();

            m_clients.Clear();

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

    public bool HasClients()
    {
        return m_clients.Count > 0;
    }

    public bool HasPlayer()
    {
        if (!HasClients()) return false;

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
