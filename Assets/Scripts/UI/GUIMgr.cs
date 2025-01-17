﻿using UnityEngine;
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

    //[SerializeField] private PlayerManager playerManager = null;
    [SerializeField] private Player player = null;
    private ChatManager   chatManager = null;

    [SerializeField] private Transform whiteToMoveTr = null;
    [SerializeField] private Transform blackToMoveTr = null;
    [SerializeField] private Text whiteScoreText = null;
    [SerializeField] private Text blackScoreText = null;
    [SerializeField] private TMP_InputField inputChatField = null;
    [SerializeField] private GameObject pauseMenu = null;

    // Use this for initialization
    void Awake()
    {
        chatManager = GetComponent<ChatManager>();
        whiteToMoveTr.gameObject.SetActive(false);
        blackToMoveTr.gameObject.SetActive(false);

        ChessGameMgr.Instance.OnPlayerTurn += DisplayTurn;
        ChessGameMgr.Instance.OnScoreUpdated += UpdateScore;
    }

    private void OnEnable()
    {
        player.networkUser.OnChatSentEvent.AddListener(chatManager.SendChatMessage);
    }
    private void OnDisable()
    {
        player.networkUser.OnChatSentEvent.RemoveListener(chatManager.SendChatMessage);
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
            if (inputChatField.text.Length == 0) return;

            Message msg = new Message(player.networkUser.pseudo, inputChatField.text);
            inputChatField.text = "";

            chatManager.SendChatMessage(msg);
            player.networkUser.SendPacket(EPacketType.CHAT_MESSAGE, msg);
        }
    }


    void Update()
    {
        if (!pauseMenu)
            return;

        if (Input.GetButtonDown("Pause"))
        {
            bool isPaused = pauseMenu.activeSelf;

            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        player.networkUser.SendNetMessage("OnResume");
        pauseMenu.SetActive(false);
    }
    public void Pause()
    {
        player.networkUser.SendNetMessage("OnPause");
        pauseMenu.SetActive(true);
    }
}
