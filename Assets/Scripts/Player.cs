using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCamera m_playerCamera;
    public NetworkUser networkUser = null;

    public UnityEvent OnGameStartEvent = new UnityEvent();
    public UnityEvent OnGamePausedEvent = new UnityEvent();
    public UnityEvent OnGameResumedEvent = new UnityEvent();
    public UnityEvent OnGameLeaveEvent = new UnityEvent();

    public bool isHost { get; private set; } = false;

    public void StartGame()
    {
        if (isHost)
        {
            Host host = networkUser as Host;

            ChessGameMgr.Instance.team = ChessGameMgr.EChessTeam.White;


            if (!host.HasClients())
            {
                ChessGameMgr.Instance.EnableAI(true);
            }
            else
            {
                host.SendPacket(EPacketType.TEAM_SWITCH, ChessGameMgr.EChessTeam.Black);

                host.SendNetMessage("StartGame");
            }
        }
        else
        {
            networkUser.SendPacket(EPacketType.TEAM_INFO, ChessGameMgr.Instance.team);
        }

        m_playerCamera.SetCamera(ChessGameMgr.Instance.team);

        OnGameStartEvent.Invoke();
    }

    public T SetNetworkState<T>() where T : NetworkUser
    {
        isHost = typeof(T) == typeof(Host);
        networkUser = gameObject.AddComponent<T>();
        networkUser.player = this;

        networkUser.OnDisconnection.AddListener(OnDisconnected);

        return networkUser as T;
    }

    private void OnPause() => OnGamePausedEvent.Invoke();
    private void OnResume() => OnGameResumedEvent.Invoke();
    private void OnDisconnected() => OnGameLeaveEvent.Invoke();
}
