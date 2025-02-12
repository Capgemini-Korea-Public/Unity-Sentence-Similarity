using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SentenceListUI : MonoBehaviour
{
    [Header("# Sentence List UI Components")]
    [SerializeField] private RectTransform sentenceContentRect;

    [Header("# Sentence Prefab")]
    [SerializeField] private GameObject sentencePrefab;
    
    [Header("# Sentence Similarity Plugin")]
    [SerializeField] private SentenceSimilarity sentenceSimilarity;

    [Header("# Sentence Information")]
    [SerializeField] private int currentSentenceIndex;
    private Queue<SentenceInfo> deactiveSentenceUIQueue = new Queue<SentenceInfo>();

    private void Start()
    {
        sentenceSimilarity.sentenceRegisterSuccessEvent.AddListener(RegisterSentence);
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
        sentenceSimilarity.DeleteSentence(sentenceInfo.Sentence);
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
