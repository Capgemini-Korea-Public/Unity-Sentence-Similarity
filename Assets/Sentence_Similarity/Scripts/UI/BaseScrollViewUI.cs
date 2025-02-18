﻿using UnityEngine;
using UnityEngine.Serialization;
public class BaseScrollViewUI : MonoBehaviour
{
    [Header("# Scroll View UI Components")]
    [SerializeField] protected RectTransform sentenceContentRect;

    [Header("# Content Prefab")]
    [SerializeField] protected GameObject sentencePrefab;
    
    [FormerlySerializedAs("sentenceSimilarityAPI")]
    [Header("# Sentence Similarity Plugin")]
    [SerializeField] protected SentenceSimilarity sentenceSimilarity;

}
