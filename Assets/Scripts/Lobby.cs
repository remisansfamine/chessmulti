using UnityEngine;
using TMPro;

public class Lobby : MonoBehaviour
{
    private PlayerManager playerManager;

    [SerializeField] private TMP_InputField m_inputClientIP;
    [SerializeField] private TMP_InputField m_inputClientPort;
    [SerializeField] private TMP_InputField m_inputHostPort;
    [SerializeField] private TMP_Text       m_playersText;
    [SerializeField] private PlayerManager  m_playerManager;

    private void Update()
    {
            
    }

    public void OnHost()
    {
        int port = int.Parse(m_inputHostPort.text);

        //  DO host server 
        m_playerManager.Host(port);
    }

    public void OnHostStartGame()
    {
   
    }

    public void OnClientJoin()
    {
        string IP = m_inputClientIP.text;
        int port = int.Parse(m_inputClientPort.text);

        //  DO join 
        m_playerManager.Join(IP, port);
    }
}
