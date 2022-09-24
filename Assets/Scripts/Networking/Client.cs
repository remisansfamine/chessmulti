using System;
using System.IO;
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
        Disconnect();
    }

    #endregion

    #region Functions

    public override void ListenPackets() => ListenServer();

    public async void ListenServer()
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

    public void Join(string ip, int port)
    {
        try
        {
            m_currClient = new TcpClient(ip, port);
            m_stream = m_currClient.GetStream();

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

    protected new void ListenPacketCatch(IOException ioe)
    {
        base.ListenPacketCatch(ioe);

        Disconnect();
    }

    protected new void ListenPacketCatch(Exception e)
    {
        base.ListenPacketCatch(e);

        Disconnect();
    }

    public new void Disconnect()
    {
        try
        {
            m_currClient.Close();
            base.Disconnect();
        }
        catch (Exception e)
        {
            Debug.LogError("Error during server disconnection " + e);
        }
    }

    #endregion
}
