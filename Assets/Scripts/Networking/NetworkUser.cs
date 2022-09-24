using System.Collections.Generic;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

using UnityEngine;
using UnityEngine.Events;

public abstract class NetworkUser : MonoBehaviour
{
    #region Variables

    protected int m_port = 33333;
    protected bool m_connected = false;
    protected bool m_listen = false;

    public string pseudo;

    protected NetworkStream m_stream = null;
    public NetworkStream Stream { get { return m_stream; } }

    public UnityEvent<Message> OnChatSentEvent = new UnityEvent<Message>();
    public UnityEvent OnDisconnection = new UnityEvent();


    #endregion

    #region Functions

    public virtual void SendNetMessage(string message) => SendPacket(EPacketType.UNITY_MESSAGE, message);
    public virtual void SendChatMessage(Message message) => SendPacket(EPacketType.CHAT_MESSAGE, message);

    public virtual void SendPacket(EPacketType type, object toSend)
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



    public abstract void ListenPackets();

    protected virtual void ListenPacketCatch(IOException ioe)
    {
        Debug.LogError("Exception catch during packets listening " + ioe);
    }

    protected virtual void ListenPacketCatch(Exception e)
    {
        Debug.LogError("Exception catch during packets listening " + e);
    }

    public virtual void Disconnect()
    {
        m_connected = false;

        m_stream?.Close();
        m_stream = null;

        OnDisconnection?.Invoke();
    }


    #endregion
}
