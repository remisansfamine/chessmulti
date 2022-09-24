using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;

using UnityEngine;
using UnityEngine.Events;

public abstract class NetworkUser : MonoBehaviour
{
    #region Variables

    protected int m_port = 33333;
    protected bool m_connected = false;

    public string pseudo;

    protected NetworkStream m_stream = null;

    public UnityEvent<Message> OnChatSentEvent = new UnityEvent<Message>();
    public UnityEvent OnDisconnection = new UnityEvent();


    #endregion

    #region Functions

    public void SendNetMessage(string message) => SendPacket(EPacketType.UNITY_MESSAGE, message);
    public void SendChatMessage(Message message) => SendPacket(EPacketType.CHAT_MESSAGE, message);

    public void SendPacket(EPacketType type, object toSend)
    {
        if (m_connected) m_stream.Write(Packet.SerializePacket(type, toSend));
    }

    protected abstract void ExecuteMovement(Packet toExecute);
    protected virtual void ExecuteValidity(Packet toExecute) {}

    protected void ExecuteUnityMessage(Packet toExecute)
    {
        string unity_message = toExecute.FillObject<string>();
        SendMessage(unity_message);
    }
    protected void ExecuteChatMessage(Packet toExecute)
    {
        Message chat_message = toExecute.FillObject<Message>();
        OnChatSentEvent.Invoke(chat_message);
    }
    protected void ExecuteTeam(Packet toExecute)
    {
        ChessGameMgr.Instance.team = toExecute.FillObject<ChessGameMgr.EChessTeam>();
    }

    protected void ExecuteTeamTurn(Packet toExecute)
    {
        ChessGameMgr.Instance.teamTurn = toExecute.FillObject<ChessGameMgr.EChessTeam>();
    }

    protected virtual void InterpretPacket(Packet toInterpret)
    {
        switch (toInterpret.header.type)
        {
            case EPacketType.UNITY_MESSAGE:
                ExecuteUnityMessage(toInterpret);
                break;

            case EPacketType.CHAT_MESSAGE:
                ExecuteChatMessage(toInterpret);
                break;

            case EPacketType.TEAM:
                ExecuteTeam(toInterpret);
                break;

            case EPacketType.TEAM_TURN:
                ExecuteTeamTurn(toInterpret);
                break;

            case EPacketType.UNDEFINED:
            default:
                break;
        }
    }



    public async void ListenPackets()
    {
        while (m_connected)
        {
            if (m_stream == null)
                continue;

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
            catch (Exception e)
            {
                Debug.LogError("Exception catch during packets listening " + e);

                /*if (isHost)
                {
                    // TODO: Set disconnection state to client
                }
                else
                {
                    DisconnectFromServer();
                }*/
            }
        }
    }

    public virtual void Disconnect()
    {
        m_connected = false;

        m_stream.Close();

        OnDisconnection.Invoke();
    }


    #endregion
}
