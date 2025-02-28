using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SentenceSimilarityUnity
{
    internal static class StopListHelper
    {
        public static HashSet<string> ParseFromPath(string? stopWordsPath)
        {
            var stopWords = new HashSet<string>(StringComparer.Ordinal);

            // stopWordsPath가 비어 있으면 기본 경로에서 읽기, 그렇지 않으면 지정된 경로에서 읽기
            foreach (var line in string.IsNullOrWhiteSpace(stopWordsPath)
                ? ReadDefaultStopListLine()
                : File.ReadAllLines(stopWordsPath))
            {
                ReadOnlySpan<char> normalizedLine = line.AsSpan().Trim();

                if (normalizedLine.Length == 0 || normalizedLine[0] == '#') continue;

                var splitter = new StringSplitter(normalizedLine, ' ');

                while (splitter.TryGetNext(out var word))
                {
                    stopWords.Add(word.ToString());
                }
            }

            return stopWords;
        }

        private static IEnumerable<string> ReadDefaultStopListLine()
        {
            // Resources 폴더에서 SmartStoplist.txt 파일 로드
            TextAsset stopListAsset = Resources.Load<TextAsset>("SmartStoplist");
            if (stopListAsset == null)
            {
                Debug.LogError("SmartStoplist.txt not found in Resources folder.");
                yield break; // 파일이 없으면 빈 결과를 반환
            }

            // 텍스트 내용을 줄 단위로 분리
            string stopListText = stopListAsset.text;
            string[] lines = stopListText.Split('\n');

            // 각 줄을 반환
            foreach (var line in lines)
            {
                yield return line;
            }
        }
    }
}
