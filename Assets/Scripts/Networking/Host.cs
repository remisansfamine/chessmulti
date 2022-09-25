using UnityEngine;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Events;


public enum EUserState
{
    PLAYER,
    SPECTATOR
}

public class Host : NetworkUser
{
    private class ClientInfo
    {
        public NetworkStream stream;
        public TcpClient tcp;

        public bool verified = false;
    }

    #region Variables

    TcpListener server = null;

    Dictionary<ClientInfo, string> clientNames = new Dictionary<ClientInfo, string>();

    private List<ClientInfo> m_clients = new List<ClientInfo>();

    [SerializeField] private uint maxClients = 5;
    public bool acceptClients;
    private int currentOpponent = -1;

    #endregion

    #region MonoBehaviour


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
    public void SendPacketToOne(EPacketType type, object toSend, NetworkStream stream)
    {
        byte[] serializedObject = Packet.SerializePacket(type, toSend);

        stream?.Write(serializedObject);
    }

    public async void WaitPlayer()
    {
        acceptClients = true;

        for (uint i = 0; i < maxClients && acceptClients; i++)
        {
            bool added = false;
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
                    added = true;
                    m_clients.Add(client);
                }
                else
                {
                    client.tcp.Close();
                }
            }
            catch (IOException e)
            {
                break;
            }
            catch (Exception e)
            {
                break;
            }
            finally
            {
                if (added && m_clients.Count > 0)
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

                InterpretPacket(packet, client);
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

    private void ExecuteVerification(Packet toExecute, ClientInfo client)
    {
        string pseudo = toExecute.FillObject<string>();
        clientNames.Add(client, pseudo);

        Debug.Log(pseudo + " Joined");
        client.verified = true;
    }

    private void ExecuteHostChatMessage(Packet toExecute, ClientInfo client)
    {
        Message chat_message = toExecute.FillObject<Message>();
        OnChatSentEvent.Invoke(chat_message);

        //  Send this chat message to everyone beside the sender
        foreach(ClientInfo c in m_clients)
        {
            if (c == client) continue;

            SendPacketToOne(EPacketType.CHAT_MESSAGE, chat_message, c.stream);
        }
    }

    private void InterpretPacket(Packet toInterpret, ClientInfo client)
    {
        switch (toInterpret.header.type)
        {
            case EPacketType.MOVEMENTS:
                ExecuteMovement(toInterpret);
                break;
            case EPacketType.MOVE_VALIDITY:
                break;
            case EPacketType.VERIFICATION:
                ExecuteVerification(toInterpret, client);
                break;
            case EPacketType.CHAT_MESSAGE:
                ExecuteHostChatMessage(toInterpret, client);
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
        if(currentOpponent != -1 && currentOpponent < m_clients.Count && m_clients[currentOpponent] == client)
        {
            currentOpponent = -1;
            ChessGameMgr.Instance.EnableAI(true);
        }

        clientNames.Remove(client);

        m_clients.Remove(client);
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
            //Debug.LogError("Error during server closing " + e);
        }
        finally
        {
            server.Stop();
        }

        Destroy(this);
    }

    public bool HasClients()
    {
        return m_clients.Count > 0;
    }

    public bool HasPlayer()
    {
        return currentOpponent != -1;
    }

    public bool AreClientVerified()
    {
        if (!HasClients()) return true;

        bool verify = true;

        foreach (var element in m_clients)
        {
            verify &= element.verified;
        }

        return verify;
    }


    public List<string> GetClientPseudo()
    {
        List<string> pseudos = new List<string>();

        foreach (string name in clientNames.Values)
        {
            pseudos.Add(name);
        }

        return pseudos;
    }

    public void SetOpponentInClients(int index)
    {
        if (index == currentOpponent) return;

        if(currentOpponent >= 0)
        {
            SendPacketToOne(EPacketType.STATE_SWITCH, EUserState.SPECTATOR, m_clients[currentOpponent].stream);

            if (index >= 0)
            {
                SendPacketToOne(EPacketType.STATE_SWITCH, EUserState.PLAYER, m_clients[index].stream);
                ChessGameMgr.Instance.EnableAI(false);
            }
            else ChessGameMgr.Instance.EnableAI(true);
        }
        else // Is cuurently AI
        {
            if (index >= 0)
            {
                ChessGameMgr.Instance.EnableAI(false);

                SendPacketToOne(EPacketType.STATE_SWITCH, EUserState.PLAYER, m_clients[index].stream);
            }
        }

        currentOpponent = index;
    }

    #endregion
}
