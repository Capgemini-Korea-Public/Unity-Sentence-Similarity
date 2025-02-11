using HuggingFace.API;
using UnityEngine;

public class SentenceSimilarity : MonoBehaviour
{
    [SerializeField] private string content;
    
    [SerializeField] private string[] sentenceArray;

    [ContextMenu("Detect")]
    public void DetectSentences()
    {
        HuggingFaceAPI.SentenceSimilarity(content,DetectionSuccess, DetectionFailure ,sentenceArray);
    }
    private void DetectionFailure(string message)
    {
        Debug.LogError($"Detect Fail! \n{message}");
    }
    
    private void DetectionSuccess(float[] sentences)
    {
        Debug.Log("Sentences Detected");
        for (int i = 0; i < sentences.Length; i++)
        {
            Debug.Log($"{sentenceArray[i]} => {sentences[i]}");
        }
    }
}
