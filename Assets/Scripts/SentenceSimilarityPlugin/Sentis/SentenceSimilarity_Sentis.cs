using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Rendering;

public class SentenceSimilarity_Sentis : MonoBehaviour
{
    [Header("# Play Model")]
    public ModelAsset sentenceSimilarityModel;
    public TextAsset vocapAsset;

    private Worker sentenceSimilarityWorker;
    private SentenceTokenizer sentenceTokenizer;

    private void Awake()
    {
        // 어휘 파일 기반으로 토크나이저 초기화
        sentenceTokenizer = new SentenceTokenizer(vocapAsset);
    }

    private void Start()
    {
        // 백엔드 결정 (GPUCompute, GPUPixel, CPU 등)
        BackendType backend = SystemInfo.supportsComputeShaders ? BackendType.GPUCompute :
            (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3) ? BackendType.GPUPixel : BackendType.CPU;

        // 모델 로드 및 워커 생성
        var model = ModelLoader.Load(sentenceSimilarityModel);
        sentenceSimilarityWorker = new Worker(model, backend);
    }

    [ContextMenu("Test")]
    public void Test()
    {
        StartSentenceSimilarity("Test");
    }
    
    public void StartSentenceSimilarity(string sentence)
    {
        // (1) input_ids: 문장 토큰화 및 패딩
        int[] tokens = sentenceTokenizer.Encode(sentence); // 길이 128
        var input_ids = new Tensor<int>(new TensorShape(1, tokens.Length), tokens);

        // (2) attention_mask: 실제 토큰이 있는 부분은 1, 패딩(0)은 0
        int[] maskArray = new int[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
            maskArray[i] = (tokens[i] == 0) ? 0 : 1;
        var attention_mask = new Tensor<int>(new TensorShape(1, tokens.Length), maskArray);

        // (3) token_type_ids: 단일 문장이라면 전부 0
        int[] tokenTypes = new int[tokens.Length];
        var token_type_ids = new Tensor<int>(new TensorShape(1, tokens.Length), tokenTypes);

        // (4) 이름 기반으로 각 입력을 설정
        sentenceSimilarityWorker.SetInput("input_ids",      input_ids);
        sentenceSimilarityWorker.SetInput("attention_mask", attention_mask);
        sentenceSimilarityWorker.SetInput("token_type_ids", token_type_ids);

        // (5) 비동기로 추론 스케줄 → 코루틴에서 결과 대기
        sentenceSimilarityWorker.Schedule();
        StartCoroutine(WaitForInference());
    }

    private IEnumerator WaitForInference()
    {
        // (6) 출력이 준비될 때까지 PeekOutput()으로 폴링
        Tensor<float> output = null;
        while (output == null)
        {
            output = sentenceSimilarityWorker.PeekOutput() as Tensor<float>;
            yield return null; 
        }

        // 추론 완료
        Debug.Log("Inference complete!");
        Debug.Log($"Output shape: {output.shape}");

        // TODO: output 데이터를 후처리하거나 UI에 반영
    }
}
