using UnityEngine;
using UnityEngine.Serialization;

public class RegisterButtonEventHandler : MonoBehaviour
{
    public InputFieldEventHandler inputFieldEventHandler;
    [FormerlySerializedAs("sentenceSimilarity")] [SerializeField] private SentenceSimilarity_API sentenceSimilarityAPI; 
    
    public void OnClickEvent()
    {
        sentenceSimilarityAPI.RegisterSentence(inputFieldEventHandler.GetInputSentence());
    }

}
