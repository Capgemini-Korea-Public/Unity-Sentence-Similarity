using System.Collections.Generic;
using HuggingFace.API;
using UnityEngine;
using UnityEngine.Events;

public class SentenceSimilarity : MonoBehaviour
{
    [Header("# Base Information")]
    [SerializeField] private List<string> sentenceList;
    [SerializeField] private int maxSentenceCount;
    public List<string> SentenceList => sentenceList;
    public int SentenceCount => sentenceList.Count;
    
    [Header("Detection Events")]
    [SerializeField] public UnityEvent detectBeginEvent;
    [SerializeField] public UnityEvent detectSuccessEvent;
    [SerializeField] public UnityEvent detectFailEvent;
    
    [Header("Sentence Events")]
    [SerializeField] public UnityEvent<string> sentenceRegisterSuccessEvent;
    [SerializeField] public UnityEvent sentenceRegisterFailEvent;
    [SerializeField] public UnityEvent sentenceDeleteEvent;
    public void DetectSentences(string sentence)
    {
        if (sentenceList.Count == 0)
        {
            detectFailEvent?.Invoke();
            Debug.LogWarning("No sentences to detect.");
            return;
        }
        
        detectBeginEvent?.Invoke();
        HuggingFaceAPI.SentenceSimilarity(sentence ,DetectionSuccess, DetectionFailure ,sentenceList.ToArray());
    }
    
    private void DetectionFailure(string message)
    {
        detectFailEvent?.Invoke();
        Debug.LogError($"Detect Fail! \n{message}");
    }
    
    private void DetectionSuccess(float[] sentences)
    {
        detectSuccessEvent?.Invoke();
        Debug.Log("Sentences Detected");
        for (int i = 0; i < sentences.Length; i++)
        {
            Debug.Log($"{sentenceList[i]} => {sentences[i]}");
        }
    }

    public void RegisterSentence(string sentence)
    {
        if (maxSentenceCount > SentenceCount && !sentenceList.Contains(sentence))
        {
            sentenceRegisterSuccessEvent?.Invoke(sentence);
            sentenceList.Add(sentence);   
        }
        else
        {
            sentenceRegisterFailEvent?.Invoke();
        }
    }

    public void DeleteSentence(string sentence)
    {
        if (!sentenceList.Contains(sentence)) return;
        
        sentenceList.Remove(sentence);
        sentenceDeleteEvent?.Invoke();
    }
}
