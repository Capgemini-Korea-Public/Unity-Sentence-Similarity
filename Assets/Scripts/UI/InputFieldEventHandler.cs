using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldEventHandler : MonoBehaviour
{
    [Header("Input Field Settings")]
    [SerializeField] private TMP_InputField myInputField; 
    
    [field:SerializeField]public string InputString { get; private set; } 
    private void Awake()
    {
        myInputField.onValueChanged.AddListener(HandleValueChanged);
    }
    
    private void HandleValueChanged(string newText)
    {
        InputString = newText;
    }
}
