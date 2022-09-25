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

            ChessGameMgr.EChessTeam otherTeam = (ChessGameMgr.Instance.team == ChessGameMgr.EChessTeam.White) ? ChessGameMgr.EChessTeam.Black : ChessGameMgr.EChessTeam.White;

            host.acceptClients = false;

            if (host.HasClients())
            {
                host.SendPacket(EPacketType.TEAM_SWITCH, otherTeam);

                host.SendNetMessage("StartGame");

                //while (!host.AreClientVerified())
                {
                }
                VerifyPlayers(host);
            }
            else
            {
                ChessGameMgr.Instance.EnableAI(true);
                InitGame();
            }
        }
        else
        {
            UserInfo info = new UserInfo();

            info.pseudo = networkUser.name;
            info.state = EUserState.SPECTATOR;

            networkUser.SendPacket(EPacketType.VERIFICATION, networkUser.name);

            InitGame();
        }
    }

    private void InitGame()
    {
        m_playerCamera.SetCamera(ChessGameMgr.Instance.team);

        OnGameStartEvent.Invoke();

        ChessGameMgr.Instance.StartGame();
    }

    async void VerifyPlayers(Host host)
    {
        await System.Threading.Tasks.Task.Delay(500);

        while(!host.AreClientVerified());


        if (!host.HasPlayer())
        {
            ChessGameMgr.Instance.EnableAI(true);
        }
        InitGame();
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
