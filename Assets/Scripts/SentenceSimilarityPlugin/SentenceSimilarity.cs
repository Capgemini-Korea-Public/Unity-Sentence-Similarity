using System;
using System.Collections.Generic;
using HuggingFace.API;
using UnityEngine;
using UnityEngine.Events;

public struct SimilarityResult
{
    public string sentence;
    public float accuracy;
}

public class SentenceSimilarity : MonoBehaviour
{
    [Header("# Base Information")]
    [SerializeField] private List<string> sentenceList;
    [SerializeField] private int maxSentenceCount;
    [SerializeField] private string enteredSentence;
    public List<string> SentenceList => sentenceList;
    public int SentenceCount => sentenceList.Count;
    public string EnteredSentence => enteredSentence;
    
    [Header("Detection Events")]
    [SerializeField] public UnityEvent detectBeginEvent;
    [SerializeField] public UnityEvent<SimilarityResult[]> detectSuccessEvent;
    [SerializeField] public UnityEvent detectFailEvent;
    
    [Header("Sentence Events")]
    [SerializeField] public UnityEvent<string> sentenceRegisterSuccessEvent;
    [SerializeField] public UnityEvent sentenceRegisterFailEvent;
    [SerializeField] public UnityEvent sentenceDeleteEvent;

    public void DetectSentences(string sentence)
    {
        if (sentenceList.Count == 0 || sentence == "")
        {
            detectFailEvent?.Invoke();
            Debug.LogWarning("No sentences to detect.");
            return;
        }
        
        detectBeginEvent?.Invoke();
        enteredSentence = sentence;
        HuggingFaceAPI.SentenceSimilarity(enteredSentence ,DetectionSuccess, DetectionFailure ,sentenceList.ToArray());
    }
    
    private void DetectionFailure(string message)
    {
        detectFailEvent?.Invoke();
        Debug.LogError($"Detect Fail! \n{message}");
    }
    
    private void DetectionSuccess(float[] accuracy)
    {
        Debug.Log("Sentences Detected");
        
        SimilarityResult [] results = new SimilarityResult[accuracy.Length];
        for (int i = 0; i < accuracy.Length; i++)
        {
            Debug.Log($"{sentenceList[i]} => {accuracy[i]}");
            results[i].accuracy = accuracy[i];
            results[i].sentence = sentenceList[i];
        }
        Array.Sort(results, (a, b) => b.accuracy.CompareTo(a.accuracy));
        
        detectSuccessEvent?.Invoke(results);
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
            Debug.LogWarning($"Sentence => {sentence} is not registered");
            sentenceRegisterFailEvent?.Invoke();
        }
    }

    public void DeleteSentence(string sentence)
    {
        if (!sentenceList.Contains(sentence))
        {
            Debug.LogError($"Sentence => {sentence} does not exist");
            return;
        }
        
        sentenceList.Remove(sentence);
        sentenceDeleteEvent?.Invoke();
    }
}

