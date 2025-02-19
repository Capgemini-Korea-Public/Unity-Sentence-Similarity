using UnityEngine;
using UnityEngine.Serialization;
public class BaseScrollViewUI : MonoBehaviour
{
    [Header("# Scroll View UI Components")]
    [SerializeField] protected RectTransform sentenceContentRect;

    [Header("# Content Prefab")]
    [SerializeField] protected GameObject sentencePrefab;
    
    protected SentenceSimilarityController sentenceSimilarityController => SentenceSimilarityController.Instance;
}
