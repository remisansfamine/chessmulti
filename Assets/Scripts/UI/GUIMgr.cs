using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/*
 * Simple GUI display : scores and team turn
 */

[RequireComponent(typeof(ChatManager))]
public class GUIMgr : MonoBehaviour
{

    #region singleton
    static GUIMgr instance = null;
    public static GUIMgr Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<GUIMgr>();
            return instance;
        }
    }
    #endregion

    [SerializeField] private PlayerManager playerManager = null;
    private ChatManager   chatManager = null;

    [SerializeField] private Transform whiteToMoveTr = null;
    [SerializeField] private Transform blackToMoveTr = null;
    [SerializeField] private Text whiteScoreText = null;
    [SerializeField] private Text blackScoreText = null;
    [SerializeField] private TMP_InputField inputChatField = null;

    // Use this for initialization
    void Awake()
    {
        chatManager = GetComponent<ChatManager>();

        whiteToMoveTr.gameObject.SetActive(false);
        blackToMoveTr.gameObject.SetActive(false);

        ChessGameMgr.Instance.OnPlayerTurn += DisplayTurn;
        ChessGameMgr.Instance.OnScoreUpdated += UpdateScore;
    }
	
    void DisplayTurn(bool isWhiteMove)
    {
        whiteToMoveTr.gameObject.SetActive(isWhiteMove);
        blackToMoveTr.gameObject.SetActive(!isWhiteMove);
    }

    void UpdateScore(uint whiteScore, uint blackScore)
    {
        whiteScoreText.text = string.Format("White : {0}", whiteScore);
        blackScoreText.text = string.Format("Black : {0}", blackScore);
    }

    public void OnChatSend()
    {
        if(Input.GetKeyDown(KeyCode.Return))
        {
            Message msg = new Message(playerManager.Pseudo, inputChatField.text);
            inputChatField.text = "";

            chatManager.SendChatMessage(msg);
            playerManager.SendPacket(EPacketType.CHAT_MESSAGE, msg);
        }
    }
}
