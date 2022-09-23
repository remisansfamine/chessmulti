using UnityEngine;
using TMPro;

// 10.2.102.127

public class Lobby : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_inputClientIP;
    [SerializeField] private TMP_InputField m_inputClientPort;
    [SerializeField] private TMP_InputField m_inputHostPort;
    [SerializeField] private TMP_InputField m_pseudonymText;
    [SerializeField] private TMP_Text       m_playersText;
    [SerializeField] private TMP_Text       m_joinInfoText;
    [SerializeField] private PlayerManager  m_playerManager;

    private void Awake()
    {
        m_inputClientIP.text = PlayerPrefs.GetString("Preferences.Client.IP", "");
        m_inputClientPort.text = PlayerPrefs.GetString("Preferences.Client.Port", "33333");

        m_inputHostPort.text = PlayerPrefs.GetString("Preferences.Host.Port", "33333");
    }

    private void OnEnable()
    {
        m_playerManager.OnGameStartEvent.AddListener(OnGameStart);
        m_playerManager.OnPartyReady.AddListener(OnClientConnected);
    }

    private void OnDisable()
    {
        m_playerManager.OnGameStartEvent.RemoveListener(OnGameStart);
        m_playerManager.OnPartyReady.RemoveListener(OnClientConnected);
    }

    private void Update()
    {
            
    }

    public void OnQuit()
    {
        Application.Quit();
    }

    public void OnHost()
    {
        m_playerManager.pseudo = m_pseudonymText.text;

        int port = int.Parse(m_inputHostPort.text);

        Debug.Log($"Hosting server as {m_playerManager.pseudo} at PORT : {m_inputHostPort.text}");

        //  DO host server 
        m_playerManager.Host(port);

        PlayerPrefs.SetString("Preferences.Host.Port", m_inputHostPort.text);
    }

    public void OnHostStop()
    {
        m_playerManager.StopHost();
    }

    public void OnHostStartGame()
    {
        m_playerManager.StartGame();
    }



    public void OnGameStart()
    {
        gameObject.SetActive(false);
    }

    public void OnClientJoin()
    {
        m_playerManager.pseudo = m_pseudonymText.text;

        string IP = m_inputClientIP.text;
        int port = int.Parse(m_inputClientPort.text);

        ChangeText(m_joinInfoText, "Trying to join server :" + IP + "\nAt PORT :" + m_inputClientPort.text, new Color(0.65f, 0.98f, 1.0f));
       
        //  DO join 
        try
        {
            m_playerManager.Join(IP, port);

            PlayerPrefs.SetString("Preferences.Client.IP", m_inputClientIP.text);
            PlayerPrefs.SetString("Preferences.Client.Port", m_inputClientPort.text);
        }
        catch
        {
            ChangeText(m_joinInfoText, "Failed to join server :" + IP + "\nAt PORT :" + m_inputClientPort.text, new Color(1.0f, 0.1f, 0.0f));
        }
    }

    public void OnClientUnjoin()
    {
        m_playerManager.DisconnectFromServer();
    }

    public void OnClientConnected()
    {
        ChangeText(m_joinInfoText, "Successfully connected to " + m_inputClientIP.text + ":" + m_inputClientPort.text, new Color(0.50f, 1.0f, 0.50f));
    }

    private void ChangeText(TMP_Text textUi, string text, Color color)
    {
        textUi.text = text;
        textUi.color = color;
    }
}
