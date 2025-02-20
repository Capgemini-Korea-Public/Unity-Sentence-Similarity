using TMPro;
using UnityEngine;
public class ResultInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text sentenceText;
    [SerializeField] private TMP_Text accuracyText;

    public void SetResultText(int rank, string sentence, float accuracy)
    {
        rankText.text = rank.ToString();
        sentenceText.text = sentence;
        accuracyText.text = $"{accuracy * 100:F2}%";
    }
    
}
