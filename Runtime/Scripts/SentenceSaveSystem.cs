using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;

public class SentenceSaveSystem
{
    private string path;

    public SentenceSaveSystem()
    {
        path = Application.persistentDataPath + "/SentenceSaveSystem.txt";
    }

    public async Task SaveSentencesAsync(List<string> sentences)
    {
        try
        {
            await File.WriteAllLinesAsync(path, sentences);
            Debug.Log("Sentences saved");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Save Error: " + e.Message);
        }
    }

    public List<string> LoadSentences()
    {
        if (!File.Exists(path))
            return new List<string>();
        
        return new List<string>(File.ReadAllLines(path));
    }
    
    public void DeleteAllData()
    {
        try
        {
            if(path == "")  
                path = Application.persistentDataPath + "/SentenceSaveSystem.txt";
            
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("Deleted all sentences");
            }
            else
            {
                Debug.Log("File does not exist");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Delete Error : " + e.Message);
        }
    }

}
