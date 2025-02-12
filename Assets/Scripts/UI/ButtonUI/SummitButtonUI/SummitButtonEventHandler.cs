using UnityEngine;

public class SummitButtonEventHandler : MonoBehaviour
{

    public InputFieldEventHandler inputFieldEventHandler;
    [SerializeField] private SentenceSimilarity sentenceSimilarity; 
    public void OnClickEvent()
    {
        sentenceSimilarity.DetectSentences(inputFieldEventHandler.GetInputSentence());
    }

}
