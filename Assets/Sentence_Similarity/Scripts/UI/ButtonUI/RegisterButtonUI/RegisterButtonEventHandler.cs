using UnityEngine;
using UnityEngine.Serialization;

public class RegisterButtonEventHandler : MonoBehaviour
{
    public InputFieldEventHandler inputFieldEventHandler;
    [SerializeField] private SentenceSimilarity sentenceSimilarity; 
    
    public void OnClickEvent()
    {
        sentenceSimilarity.RegisterSentence(inputFieldEventHandler.GetInputSentence());
    }

}
