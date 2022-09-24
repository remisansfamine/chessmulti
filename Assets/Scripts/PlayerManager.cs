using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Events;
using System.IO;

public class PlayerManager : MonoBehaviour
{
    public bool isHost { get; private set; } = false;

    bool enableCommunication = false;

    public bool EnableCommunication
    {
        get { return enableCommunication; }
    }

    public string pseudo = "Player";

    public string Pseudo {
        get{ return pseudo;}
    }

    [SerializeField] PlayerCamera playerCamera;

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
    public UnityEvent OnGameLeaveEvent = new UnityEvent();

    public UnityEvent<Message> OnChatSentEvent = new UnityEvent<Message>();
    
    [SerializeField] private ChessGameMgr chessMgr = null;

    async void WaitPlayer()
    {
        try
        {
            enableCommunication = true;

            connectedClient = await server.AcceptTcpClientAsync();

            stream = connectedClient.GetStream();

            SetReady();
            SendNetMessage("SetReady");
        }
        catch (Exception e)
        {
            Debug.LogError("Server seems stopped " + e);

            enableCommunication = false;
        }


        ListenPackets();
    }

    void StartServer(int port)
    {
        isHost = true;

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

    public void Host(int port)
    {
        StartServer(port);
    }

    public void StopHost()
    {
        enableCommunication = false;

        try
        {
            connectedClient?.Close();
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

    public void SendNetMessage(string message) => SendPacket(EPacketType.UNITY_MESSAGE, message);
    public void SendChatMessage(Message message) => SendPacket(EPacketType.CHAT_MESSAGE, message);

    public void SendPacket(EPacketType type, object toSend)
    {
        if (!enableCommunication) return;
        
        Packet packet = new Packet();

        byte[] bytes = packet.Serialize(type, toSend);

        stream.Write(bytes);
    }

    private void InterpretPacket(Packet toInterpret)
    {
        switch (toInterpret.header.type)
        {
            case EPacketType.MOVEMENTS:
                ChessGameMgr.Move move = toInterpret.FillObject<ChessGameMgr.Move>();

                if (isHost)
                    chessMgr.CheckMove(move);
                else
                    chessMgr.UpdateTurn(move);

                break;
            case EPacketType.MOVE_VALIDITY:
                if (isHost)
                    break;

                bool isValid = toInterpret.FillObject<bool>();
                if (isValid)
                    chessMgr.UpdateTurn();
                else
                    chessMgr.ResetMove();

                break;
            case EPacketType.UNITY_MESSAGE:
                string unity_message = toInterpret.FillObject<string>();
                SendMessage(unity_message);
                break;

            case EPacketType.CHAT_MESSAGE:
                Message chat_message = toInterpret.FillObject<Message>();
                OnChatSentEvent.Invoke(chat_message);
                break;

            case EPacketType.TEAM:
                chessMgr.team = toInterpret.FillObject<ChessGameMgr.EChessTeam>();
                break;

            case EPacketType.TEAM_TURN:
                chessMgr.teamTurn = toInterpret.FillObject<ChessGameMgr.EChessTeam>();
                break;

            case EPacketType.UNDEFINED:
            default:
                break;
        }
    }
    public async void ListenPackets()
    {
        while (enableCommunication)
        {
            if (stream == null)
                continue;
            
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
            catch (IOException e)
            {
                if (isHost)
                {
                    // TODO: Set disconnection state to client
                }
                else
                {
                    DisconnectFromServer();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Exception catch during packets listening " + e);
            }
        }
    }

    public void Join(string ip, int port)
    {
        try
        {
            currClient = new TcpClient(ip, port);
            stream = currClient.GetStream();

            enableCommunication = true;

            ListenPackets();
        }
        catch (Exception e)
        {
            Debug.LogError("Error during server connection " + e);
        }
    }

    public void DisconnectFromServer()
    {
        enableCommunication = false;

        if(!isHost)
        {
            SendNetMessage("OnClientDisconnection");
        }

        try
        {
            stream.Close();
            currClient.Close();
        }
        catch (Exception e) 
        {
            Debug.LogError("Error during server disconnection " + e);
        }

        OnDisconnection();
    }

    public void SetReady() => OnPartyReady.Invoke();

    public void StartGame()
    {
        if (isHost)
        {
            chessMgr.team = ChessGameMgr.EChessTeam.White;

            SendPacket(EPacketType.TEAM, ChessGameMgr.EChessTeam.Black);

            SendNetMessage("StartGame");
        }

        playerCamera.SetCamera(chessMgr.team);

        OnGameStartEvent.Invoke();
    }

    private void OnDisconnection() => OnGameLeaveEvent.Invoke();
}
