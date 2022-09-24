using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCamera m_playerCamera;
    public NetworkUser networkUser = null;

    public UnityEvent OnGameStartEvent = new UnityEvent();

    private bool isHost = false;

    void Awake()
    {
        m_playerCamera = GetComponent<PlayerCamera>();
    }

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

        return networkUser as T;
    }
}
