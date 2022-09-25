using UnityEngine;
using TMPro;
using System.Collections.Generic;

// 10.2.102.127

public class Lobby : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_inputClientIP;
    [SerializeField] private TMP_InputField m_inputClientPort;
    [SerializeField] private TMP_InputField m_inputHostPort;
    [SerializeField] private TMP_InputField m_pseudonymText;
    [SerializeField] private TMP_Text       m_joinInfoText;
    [SerializeField] private TMP_Dropdown   m_opponentDropdown;
    [SerializeField] private Player  m_player;

    private void Awake()
    {
        m_pseudonymText.text = PlayerPrefs.GetString("Preferences.Pseudonym", "Player");

        m_inputClientIP.text = PlayerPrefs.GetString("Preferences.Client.IP", "");
        m_inputClientPort.text = PlayerPrefs.GetString("Preferences.Client.Port", "33333");
        m_inputHostPort.text = PlayerPrefs.GetString("Preferences.Host.Port", "33333");
    }

    private void OnEnable()
    {
        m_player.OnGameStartEvent.AddListener(OnGameStart);
    }

    private void OnDisable()
    {
        m_player.OnGameStartEvent.RemoveListener(OnGameStart);
    }

    private void Update()
    {
        UpdateDropdown();
    }

    public void OnOtherPlayerDisconnection()
    {
        Debug.Log("Other player disconnected");
    }

    public void OnMainMenu()
    {
        ChessGameMgr.Instance.StopGame();

        if (m_player.isHost)
            OnHostStop();
        else
            OnClientUnjoin();
    }

    public void OnQuit()
    {
        Application.Quit();
    }

    public void OnHost()
    {
        int port = int.Parse(m_inputHostPort.text);

        //  DO host server 
        Host host = m_player.SetNetworkState<Host>();
        host.pseudo = m_pseudonymText.text;
        host.OpenServer(port);

        PlayerPrefs.SetString("Preferences.Host.Port", m_inputHostPort.text);
        PlayerPrefs.SetString("Preferences.Pseudonym", m_pseudonymText.text);
    }

    public void OnHostStop()
    {
        Host host = m_player.networkUser as Host;

        if (host) host.Disconnect();
    }

    public void OnHostStartGame()
    {
        m_player.StartGame();
    }

    public void OnGameStart()
    {
        gameObject.SetActive(false);
    }

    public void OnClientJoin()
    {
        Client client = m_player.SetNetworkState<Client>();

        client.pseudo = m_pseudonymText.text;

        string IP = m_inputClientIP.text;
        int port = int.Parse(m_inputClientPort.text);

        ChangeText(m_joinInfoText, "Trying to join server :" + IP + "\nAt PORT :" + m_inputClientPort.text, new Color(0.65f, 0.98f, 1.0f));
       
        //  DO join 
        try
        {
            client.Join(IP, port);
        }
        catch
        {
            ChangeText(m_joinInfoText, "Failed to join server :" + IP + "\nAt PORT :" + m_inputClientPort.text, new Color(1.0f, 0.1f, 0.0f));
        }
        finally
        {
            PlayerPrefs.SetString("Preferences.Client.IP", m_inputClientIP.text);
            PlayerPrefs.SetString("Preferences.Client.Port", m_inputClientPort.text);

            PlayerPrefs.SetString("Preferences.Pseudonym", m_pseudonymText.text);

            ChangeText(m_joinInfoText, "Successfully connected to " + m_inputClientIP.text + ":" + m_inputClientPort.text, new Color(0.50f, 1.0f, 0.50f));
        }
    }

    /*public void OnSpectateJoin()
    {
        Client client = m_player.SetNetworkState<Client>();
        client.pseudo = m_pseudonymText.text;

        string IP = m_inputClientIP.text;
        int port = int.Parse(m_inputClientPort.text);

        ChangeText(m_joinInfoText, "Trying to join server :" + IP + "\nAt PORT :" + m_inputClientPort.text, new Color(0.65f, 0.98f, 1.0f));

        //  DO join 
        try
        {
            client.Join(IP, port);
        }
        catch
        {
            ChangeText(m_joinInfoText, "Failed to join server :" + IP + "\nAt PORT :" + m_inputClientPort.text, new Color(1.0f, 0.1f, 0.0f));
        }
        finally
        {
            PlayerPrefs.SetString("Preferences.Client.IP", m_inputClientIP.text);
            PlayerPrefs.SetString("Preferences.Client.Port", m_inputClientPort.text);

            PlayerPrefs.SetString("Preferences.Pseudonym", m_pseudonymText.text);

            ChangeText(m_joinInfoText, "Successfully connected to " + m_inputClientIP.text + ":" + m_inputClientPort.text, new Color(0.50f, 1.0f, 0.50f));
        }
    }*/

    public void OnClientUnjoin()
    {
        Client client = m_player.networkUser as Client;
        client.Disconnect();
    }

    private void ChangeText(TMP_Text textUi, string text, Color color)
    {
        textUi.text = text;
        textUi.color = color;
    }

    public void OnSwitchTeam(int team)
    {
        if (team == 0) ChessGameMgr.Instance.team = ChessGameMgr.EChessTeam.White;
        if (team == 1) ChessGameMgr.Instance.team = ChessGameMgr.EChessTeam.Black;
    }

    public void OnOpponentSwitch(int index)
    {
        Host host = m_player.networkUser as Host;

        host.SetOpponentInClients(index - 1);
    }


    public void UpdateDropdown()
    {
        if (m_opponentDropdown.IsActive())
        {
            Host host = m_player.networkUser as Host;

            if (!host) return;

            List<string> options = host.GetClientPseudo();

            if (m_opponentDropdown.options.Count == options.Count + 1) return;

            m_opponentDropdown.ClearOptions();

            options.Insert(0, "AI");

            m_opponentDropdown.AddOptions(options);
        }
    }
}
