using UnityEngine;
using UnityEngine.Serialization;

public class SummitButtonEventHandler : MonoBehaviour
{

    public InputFieldEventHandler inputFieldEventHandler;

    public void OnClickEvent()
    {
        SentenceSimilarityController.Instance.MeasureSentenceAccuracy(inputFieldEventHandler.GetInputSentence());
    }

}
