using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResultUIEventHandler : BaseScrollViewUI
{
    [SerializeField] private TMP_Text commandText;

    private List<ResultInfo> resultTextList = new List<ResultInfo>();
   
    public void DisplaySimilarityResults(SimilarityResult[] similarityResults)
    {
        commandText.text = $"Entered Command : {sentenceSimilarityAPI.EnteredSentence}";
        
        int resultCount = similarityResults.Length;
        int deactiveResultTextCount = resultTextList.Count;

        for (int i = 0; i < deactiveResultTextCount; i++)
        {
            if (i >= resultCount) return;
            resultTextList[i].gameObject.SetActive(true);
            resultTextList[i].SetResultText(i + 1, similarityResults[i].sentence, similarityResults[i].accuracy);
        }

        for (int i = deactiveResultTextCount; i < resultCount; i++)
        {
            ResultInfo instantiateText = Instantiate(sentencePrefab, sentenceContentRect).GetComponent<ResultInfo>();
            resultTextList.Add(instantiateText);
            instantiateText.SetResultText(i + 1, similarityResults[i].sentence, similarityResults[i].accuracy);          
        }
    }
    

    public void DisableResults()
    {
        commandText.text = $"Entered...";

        foreach (var resultText in resultTextList)
        {
            resultText.gameObject.SetActive(false);
        }
    }
}