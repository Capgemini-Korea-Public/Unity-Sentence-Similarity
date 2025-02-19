using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SentenceListUIEventHandler : BaseScrollViewUI
{
    [Header("# Sentence Information")]
    [SerializeField] private int currentSentenceIndex;
    private Queue<SentenceInfo> deactiveSentenceUIQueue = new Queue<SentenceInfo>();

    
    private void Start()
    {
        sentenceSimilarityController.OnSentenceRegisterSuccessEvent.AddListener(RegisterSentence);

        for (int i = 0; i < sentenceSimilarityController.SentenceCount; i++)
        {
            RegisterSentence(sentenceSimilarityController.SentenceList[i]);
        }
    }
    
    private void RegisterSentence(string sentence)
    {
        SentenceInfo activeSentence = GetSentenceUI();
        currentSentenceIndex++;
        
        activeSentence.ActiveCommandUI(sentence);
    }

    public void DeleteSentence(SentenceInfo sentenceInfo)
    {
        sentenceInfo.gameObject.SetActive(false);
        currentSentenceIndex--;
        deactiveSentenceUIQueue.Enqueue(sentenceInfo);
        sentenceSimilarityController.DeleteSentence(sentenceInfo.Sentence);
    }

    private SentenceInfo GetSentenceUI()
    {
        if (deactiveSentenceUIQueue.Count > 0)
        {
            SentenceInfo activeSentence = deactiveSentenceUIQueue.Dequeue();
            activeSentence.gameObject.SetActive(true);
            return activeSentence;
        }

        SentenceInfo sentenceInfo = Instantiate(sentencePrefab, sentenceContentRect).GetComponent<SentenceInfo>();
        sentenceInfo.SentenceInfoInit(this);
        return sentenceInfo;
    }
    
}