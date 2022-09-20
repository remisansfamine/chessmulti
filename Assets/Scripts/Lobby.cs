using UnityEngine;
using TMPro;

// 10.2.102.127

public class Lobby : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_inputClientIP;
    [SerializeField] private TMP_InputField m_inputClientPort;
    [SerializeField] private TMP_InputField m_inputHostPort;
    [SerializeField] private TMP_Text       m_playersText;
    [SerializeField] private TMP_Text       m_joinInfoText;
    [SerializeField] private PlayerManager  m_playerManager;

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
        int port = int.Parse(m_inputHostPort.text);

        Debug.Log("Hosting server at PORT :" + m_inputHostPort.text);

        //  DO host server 
        m_playerManager.Host(port);
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
        string IP = m_inputClientIP.text;
        int port = int.Parse(m_inputClientPort.text);

        ChangeText(m_joinInfoText, "Trying to join server :" + IP + "\nAt PORT :" + m_inputClientPort.text, new Color(0.65f, 0.98f, 1.0f));

        //  DO join 
        m_playerManager.Join(IP, port);

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
