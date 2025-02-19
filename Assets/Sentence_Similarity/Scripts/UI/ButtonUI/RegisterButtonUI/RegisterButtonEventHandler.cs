using UnityEngine;
using UnityEngine.Serialization;

public class RegisterButtonEventHandler : MonoBehaviour
{
    public InputFieldEventHandler inputFieldEventHandler;

    public void OnClickEvent()
    {
        SentenceSimilarityController.Instance.RegisterSentence(inputFieldEventHandler.GetInputSentence());
    }

}
