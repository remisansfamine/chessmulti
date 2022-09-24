using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Events;


public class Host : NetworkUser
{

    #region Variables

    TcpListener server = null;
    TcpClient connectedClient = null;


    #endregion

    #region MonoBehaviour

    private void OnDestroy()
    {
        if(m_connected) Disconnect();
    }

    #endregion

    #region Functions

    async void WaitPlayer()
    {
        try
        {
            m_connected = true;

            connectedClient = await server.AcceptTcpClientAsync();

            m_stream = connectedClient.GetStream();

            //SetReady();
            //SendNetMessage("SetReady");
        }
        catch (Exception e)
        {
            Debug.LogError("Server seems stopped " + e);

            m_connected = false;
        }


        ListenPackets();
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
