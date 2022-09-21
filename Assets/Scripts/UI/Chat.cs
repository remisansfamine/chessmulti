using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Chat : MonoBehaviour
{
    [SerializeField] private int m_maxMessage = 100;

    [SerializeField] GameObject panel, textObject;

    private List<ChatMessage> m_messages;

    public void SendChatMessage(Message msg)
    {
        if(m_messages.Count > m_maxMessage)
        {
            Destroy(m_messages[0].msgTextObject);
            m_messages.RemoveAt(0);
        }

        ChatMessage newChatMessage = new ChatMessage(msg, Instantiate(textObject, panel.transform));

        m_messages.Add(newChatMessage);
    }
}

[Serializable]
public class ChatMessage
{
    //private string m_text;
    public GameObject msgTextObject;

    public ChatMessage(Message msg, GameObject go)
    {
        //m_text = msg;

        if(go.TryGetComponent(out TMP_Text textComp))
        {
            textComp.text = msg.GetComputedMessage();
        }
    }
}

public class Message
{
    private string m_pseudo;
    private string m_hour;
    private string m_message;

    public Message(string pseudo, string message)
    {
        m_pseudo  = pseudo;
        m_message = message;

        m_hour = DateTime.UtcNow.ToString("HH:mm:ss"); ;
    }

    public string GetComputedMessage()
    {
        return "[" + m_hour + "] " + m_pseudo + " : " + m_message;
    }
}