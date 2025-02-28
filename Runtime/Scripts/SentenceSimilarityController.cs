using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

    [Header("# Measure Events")]
    [SerializeField] public UnityEvent OnMeasureBeginEvent;
    [SerializeField] public UnityEvent<SimilarityResult[]> OnMeasureSuccessEvent;
    [SerializeField] public UnityEvent OnMeasureFailEvent;

    [Header("# Register Events")]
    [SerializeField] public UnityEvent<string> OnSentenceRegisterSuccessEvent;
    [SerializeField] public UnityEvent OnSentenceRegisterFailEvent;
    [SerializeField] public UnityEvent OnSentenceDeleteEvent;

    [Header("# Preprocessing & Postprocessing")]
    [SerializeField] private List<string> filteredSentenceList = new List<string>();
    [SerializeField] private float keywordAlpha = 0.15f;
    [SerializeField] private float similarityThreshold = 0.7f;
    private Dictionary<string, float> weightDictionary = new Dictionary<string, float>();

    private bool isExecute;
    private SentenceSaveSystem saveSystem;
    private Rake rakeAlgorithm;
    
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

        rakeAlgorithm = new Rake();
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

        weightDictionary.Clear();
        filteredSentenceList.Clear();

        // Keyword Extractor 
        string positiveSentence = StringUtility.RemoveNegativeParts(input);
        var rakeResults = rakeAlgorithm.Run(positiveSentence);
        foreach (var rakeResult in rakeResults)
        {
            Debug.Log($"result: {rakeResult.Key} => {rakeResult.Value}");
            var processedKey = StringUtility.NormalizeText(rakeResult.Key);
            var findSentences = sentenceList.FindAll(s => StringUtility.NormalizeText(s).Contains(processedKey));
            foreach (var sentence in findSentences)
            {
                // Rake 점수 가중치 더하기
                if (!weightDictionary.TryAdd(sentence, (float)rakeResult.Value))
                    weightDictionary[sentence] += (float)rakeResult.Value;
                filteredSentenceList.Add(sentence);
            }
        }

        if (filteredSentenceList.Count == 0)
        {
            filteredSentenceList = new List<string>(SentenceList);
        }

        OnMeasureBeginEvent?.Invoke();
        isExecute = true;
        enteredSentence = input;

        if (activateType == ActivateType.HuggingFaceAPI)
            SentenceSimilarityModule.MeasureSentenceAccuracyFromAPI
                (positiveSentence, MeasureSuccess, MeasureFailure, filteredSentenceList.ToArray());
        else
            SentenceSimilarityModule.MeasureSentenceAccuracyFromSentis
                (positiveSentence, MeasureSuccess, MeasureFailure, filteredSentenceList.ToArray());
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

        // 가중치와 유사도를 조합한 최종 점수 계산을 위한 변수들
        float maxWeight = 0f; 
        float maxAccuracy = 0f;

        // 최대값 찾기(정규화 목적)
        for (int i = 0; i < accuracy.Length; i++)
        {
            string sentence = filteredSentenceList[i];
            float weight = weightDictionary.ContainsKey(sentence) ? weightDictionary[sentence] : 0f;

            maxWeight = Mathf.Max(maxWeight, weight);
            maxAccuracy = Mathf.Max(maxAccuracy, accuracy[i]);
        }

        // 가중치와 정확도를 조합하여 최종 점수 계산
        for (int i = 0; i < accuracy.Length; i++)
        {
            string sentence = filteredSentenceList[i];
            float similarityScore = accuracy[i];
            float rawWeight = weightDictionary.ContainsKey(sentence) ? weightDictionary[sentence] : 0f;

            // 가중치와 유사도 점수 정규화 (0~1 범위로)
            float normalizedWeight = maxWeight > 0 ? rawWeight / maxWeight : 0;
            float normalizedAccuracy = maxAccuracy > 0 ? similarityScore / maxAccuracy : 0;
            normalizedAccuracy = Mathf.Max(normalizedAccuracy, 0f);

            // 가중치와 유사도를a 결합한 최종 점수 계산
            float combinedScore = (normalizedAccuracy * (1 - keywordAlpha)) + (normalizedWeight * keywordAlpha);
            results[i].sentence = sentence;
            results[i].accuracy = combinedScore;
        }

        // 최종 점수에 따라 결과 정렬
        Array.Sort(results, (a, b) => b.accuracy.CompareTo(a.accuracy));

        if (results[0].accuracy >= similarityThreshold)
        {
            OnMeasureSuccessEvent?.Invoke(results);
        }
        else
        {
            MeasureFailure("Not match accuracy detected.");
        }

        isExecute = false;
    }

#endregion

    [ContextMenu("Delete All Sentences")]
    private void DeleteAllSentenceData()
    {
        if (saveSystem == null)
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
