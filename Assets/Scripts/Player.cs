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
            ChessGameMgr.Instance.team = ChessGameMgr.EChessTeam.White;

            networkUser.SendPacket(EPacketType.TEAM, ChessGameMgr.EChessTeam.Black);

            networkUser.SendNetMessage("StartGame");
        }

        m_playerCamera.SetCamera(ChessGameMgr.Instance.team);

        OnGameStartEvent.Invoke();
    }


    public T SetNetworkState<T>() where T : NetworkUser
    {
        isHost = typeof(T) == typeof(Host);
        networkUser = gameObject.AddComponent<T>();

        networkUser.OnDisconnection.AddListener(OnDisconnected);

        return networkUser as T;
    }

    private void OnPause() => OnGamePausedEvent.Invoke();
    private void OnResume() => OnGameResumedEvent.Invoke();
    private void OnDisconnected() => OnGameLeaveEvent.Invoke();
}
