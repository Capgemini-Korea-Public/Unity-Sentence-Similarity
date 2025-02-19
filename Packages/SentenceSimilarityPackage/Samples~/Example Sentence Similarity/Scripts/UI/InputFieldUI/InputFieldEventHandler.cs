using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldEventHandler : MonoBehaviour
{
    [Header("Input Field Settings")]
    [SerializeField] private TMP_InputField myInputField;
    
    public string GetInputSentence()
    {
        string inputString = myInputField.text; 
        myInputField.text = "";
        return inputString;
    }
    
}
