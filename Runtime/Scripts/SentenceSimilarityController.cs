using System;
using System.Collections.Generic;
using SentenceSimilarityUnity;
using UnityEngine;
using UnityEngine.Events;


[AddComponentMenu("SentenceSimilarity/Sentence Similarity Controller")]
public class SentenceSimilarityController : MonoBehaviour
{
    private static SentenceSimilarityController instance;
    public static SentenceSimilarityController Instance => instance;
    
    [Header(" Model Information")]
    [SerializeField] private ActivateType activateType;

    [Header("# Base Information")]
    [SerializeField] private List<string> sentenceList;
    [SerializeField] private int maxSentenceCount = 100;
    [SerializeField] private string enteredSentence;
    public List<string> SentenceList => sentenceList;
    public int SentenceCount => sentenceList.Count;
    public string EnteredSentence => enteredSentence;

    [Header("Measure Events")]
    [SerializeField] public UnityEvent OnMeasureBeginEvent;
    [SerializeField] public UnityEvent<SimilarityResult[]> OnMeasureSuccessEvent;
    [SerializeField] public UnityEvent OnMeasureFailEvent;

    [Header("Register Events")]
    [SerializeField] public UnityEvent<string> OnSentenceRegisterSuccessEvent;
    [SerializeField] public UnityEvent OnSentenceRegisterFailEvent;
    [SerializeField] public UnityEvent OnSentenceDeleteEvent;

    private bool isExecute;
    private SentenceSaveSystem saveSystem;
    // Set Singleton Object
    private void Awake()
    {
        if (instance == null) 
        {
            instance = this; 
        }
        else
        {
            Destroy(gameObject);
        }
        
        saveSystem = new SentenceSaveSystem();
        sentenceList = saveSystem.LoadSentences();
    }
    
    public void MeasureSentenceAccuracy(string input)
    {
        if (isExecute)
        {
            MeasureFailure("Model is executing."); 
            return;
        }
        if (sentenceList.Count == 0 || input == "")
        {
            MeasureFailure("No sentences to detect.");
            return;
        }

        OnMeasureBeginEvent?.Invoke();
        isExecute = true;
        enteredSentence = input;
        
        if (activateType == ActivateType.HuggingFaceAPI)
            SentenceSimilarityModule.MeasureSentenceAccuracyFromAPI(input, MeasureSuccess, MeasureFailure, sentenceList.ToArray());
        else
            SentenceSimilarityModule.MeasureSentenceAccuracyFromSentis(input, MeasureSuccess, MeasureFailure, sentenceList.ToArray());
    }

    
    #region Events

    public async void RegisterSentence(string input)
    {
        if (maxSentenceCount > SentenceCount && !sentenceList.Contains(input))
        {
            OnSentenceRegisterSuccessEvent?.Invoke(input);
            sentenceList.Add(input);
            await saveSystem.SaveSentencesAsync(sentenceList);
        }
        else
        {
            Debug.LogWarning($"Sentence => {input} is not registered");
            OnSentenceRegisterFailEvent?.Invoke();
        }
    }

    public async void DeleteSentence(string input)
    {
        if (!sentenceList.Contains(input))
        {
            Debug.LogError($"Sentence => {input} does not exist");
            return;
        }

        sentenceList.Remove(input);
        OnSentenceDeleteEvent?.Invoke();
        await saveSystem.SaveSentencesAsync(sentenceList);
    }

    private void MeasureFailure(string message)
    {
        OnMeasureFailEvent?.Invoke();
        Debug.LogError($"Detect Fail! \n{message}");
    }

    private void MeasureSuccess(float[] accuracy)
    {
        Debug.Log("Sentences Detected");

        SimilarityResult[] results = new SimilarityResult[accuracy.Length];
        for (int i = 0; i < accuracy.Length; i++)
        {
            Debug.Log($"{sentenceList[i]} => {accuracy[i]}");
            results[i].accuracy = accuracy[i];
            results[i].sentence = sentenceList[i];
        }
        Array.Sort(results, (a, b) => b.accuracy.CompareTo(a.accuracy));

        OnMeasureSuccessEvent?.Invoke(results);
        isExecute = false;
    }
#endregion

    [ContextMenu("Delete All Sentences")]
    private void DeleteAllSentenceData()
    {
        if(saveSystem == null) 
            saveSystem = new SentenceSaveSystem();
        
        saveSystem.DeleteAllData();
    }
}


public enum ActivateType { HuggingFaceAPI, Sentis }

public struct SimilarityResult
{
    public string sentence;
    public float accuracy;
}
