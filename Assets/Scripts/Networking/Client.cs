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

    protected NetworkStream m_stream = null;

    public bool isPlayer;

    #endregion

    #region MonoBehaviour

    private void OnDestroy()
    {
        Disconnect();
    }

    #endregion

    #region Functions

    public override void SendPacket(EPacketType type, object toSend)
    {
        m_stream?.Write(Packet.SerializePacket(type, toSend));
    }

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

            ListenServer();
        }
        catch (Exception e)
        {
            Debug.LogError("Error during server connection " + e);
        }
        finally
        {
            SendPacket(EPacketType.VERIFICATION, pseudo);
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
    protected void ExecuteStateSwitch(Packet toExecute)
    {
        if (toExecute.FillObject<EUserState>() == EUserState.PLAYER) isPlayer = true;
        else isPlayer = false;
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
            case EPacketType.TEAM_SWITCH:
                if (isPlayer) 
                    ExecuteTeam(toInterpret);
                else
                    ChessGameMgr.Instance.team = ChessGameMgr.EChessTeam.None;
                break;
            case EPacketType.SPECTATORS_MOVEMENTS:
                if (isPlayer) break;
                ExecuteMovement(toInterpret); 
                break;
            case EPacketType.STATE_SWITCH:
                ExecuteStateSwitch(toInterpret);
                break;
            default:
                base.InterpretPacket(toInterpret);
                break;
        }
    }

    protected void ListenPacketCatch(IOException ioe)
    {
        Disconnect();
    }

    protected void ListenPacketCatch(Exception e)
    {
        Disconnect();
    }

    public new void Disconnect()
    {
        try
        {
            m_stream?.Close();
            m_stream = null;

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
