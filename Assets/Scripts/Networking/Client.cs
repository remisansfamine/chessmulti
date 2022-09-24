using System;
using System.Net;
using System.Net.Sockets;

using UnityEngine;
using UnityEngine.Events;

public class Client : NetworkUser
{

    #region Variables

    private TcpClient m_currClient = null;

    #endregion

    #region MonoBehaviour

    private void OnDestroy()
    {
        if (m_connected) Disconnect();
    }

    #endregion

    #region Functions
    public void Join(string ip, int port)
    {
        try
        {
            m_currClient = new TcpClient(ip, port);
            m_stream = m_currClient.GetStream();

            m_connected = true;

            ListenPackets();
        }
        catch (Exception e)
        {
            Debug.LogError("Error during server connection " + e);
        }
    }

    protected override void ExecuteMovement(Packet toExecute)
    {
        ChessGameMgr.Move move = toExecute.FillObject<ChessGameMgr.Move>();

        ChessGameMgr.Instance.UpdateTurn(move);
    }

    protected override void ExecuteValidity(Packet toExecute)
    {
        if (toExecute.FillObject<bool>()) ChessGameMgr.Instance.UpdateTurn();
        
        else ChessGameMgr.Instance.ResetMove();
    }

    protected override void InterpretPacket(Packet toInterpret)
    {
        switch (toInterpret.header.type)
        {
            case EPacketType.MOVEMENTS:
                ExecuteMovement(toInterpret);
                break;
            case EPacketType.MOVE_VALIDITY:
                ExecuteValidity(toInterpret);
                break;

            default:
                base.InterpretPacket(toInterpret);
                break;
        }
    }

    public new void Disconnect()
    {
        try
        {
            base.Disconnect();
            m_currClient.Close();
        }
        catch (Exception e)
        {
            Debug.LogError("Error during server disconnection " + e);
        }
    }

    #endregion
}
