using System;
using System.Collections.Generic;
using UnityEngine;
public class SentenceTokenizer
{
    private Dictionary<string, int> vocab;

    public SentenceTokenizer(TextAsset vocabFile)
    {
        vocab = new Dictionary<string, int>();
        string[] lines = vocabFile.text.Split('\n');
        for(int i=0; i<lines.Length; i++)
            vocab[lines[i].Trim()] = i; // [6][8] 참조
    }
    
    public int[] Encode(string sentence)
    {
        List<int> tokens = new List<int>();
    
        // 1. [CLS] 토큰 추가
        tokens.Add(vocab["[CLS]"]); 
    
        // 2. 소문자 변환 및 분리
        string[] words = sentence.ToLower()
            .Split(new[] { ' ', '.', ',', '!', '?' }, 
                StringSplitOptions.RemoveEmptyEntries);

        foreach(string word in words)
        {
            // 3. 단어 → ID 매핑
            if(vocab.TryGetValue(word, out int id))
                tokens.Add(id);
            else
                tokens.Add(vocab["[UNK]"]); // 알 수 없는 단어
        }
    
        // 4. [SEP] 토큰 및 패딩
        tokens.Add(vocab["[SEP]"]);
        return PadSequence(tokens, 128); // [2][6] 참조
    }

    private int[] PadSequence(List<int> tokens, int maxLength)
    {
        int[] padded = new int[maxLength];
        Array.Fill(padded, 0);
        int len = Mathf.Min(tokens.Count, maxLength);
        Array.Copy(tokens.ToArray(), padded, len);
        return padded;
    }

}
