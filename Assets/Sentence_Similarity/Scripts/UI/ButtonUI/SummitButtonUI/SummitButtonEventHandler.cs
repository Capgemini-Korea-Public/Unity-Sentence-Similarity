using UnityEngine;
using UnityEngine.Serialization;

public class SummitButtonEventHandler : MonoBehaviour
{

    public InputFieldEventHandler inputFieldEventHandler;
    [FormerlySerializedAs("sentenceSimilarity")] [SerializeField] private SentenceSimilarity_API sentenceSimilarityAPI; 
    public void OnClickEvent()
    {
        sentenceSimilarityAPI.DetectSentences(inputFieldEventHandler.GetInputSentence());
    }

}
