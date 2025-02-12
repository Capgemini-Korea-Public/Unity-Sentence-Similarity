using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SentenceInfo : MonoBehaviour
{
    [SerializeField] private string sentence;
    public string Sentence => sentence;

    [SerializeField] private Button deleteButton;
    [SerializeField] private TMP_Text commandText;
    
    private SentenceListUI sentenceListUI;
    private void Awake()
    {
        deleteButton.onClick.AddListener(DeleteCommand);
    }

    private void DeleteCommand()
    {
        sentenceListUI.DeleteSentence(this);
    }

    public void SentenceInfoInit(SentenceListUI sentenceListUI)
    {
        this.sentenceListUI = sentenceListUI; 
    }
    
    public void ActiveCommandUI(string sentence)
    {
        this.sentence = sentence;
        commandText.text = this.sentence;
    }
    
    
}
