using UnityEngine;
using UnityEngine.Serialization;

public class SummitButtonEventHandler : MonoBehaviour
{

    public InputFieldEventHandler inputFieldEventHandler;
    [SerializeField] private SentenceSimilarity sentenceSimilarity; 
    public void OnClickEvent()
    {
        sentenceSimilarity.MeasureSentenceAccuracy(inputFieldEventHandler.GetInputSentence());
    }

}
