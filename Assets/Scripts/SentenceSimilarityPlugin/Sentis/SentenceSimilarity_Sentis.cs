using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Windows;
using File = System.IO.File;

public class SentenceSimilarity_Sentis : MonoBehaviour
{
    [Header("# Play Model")]
    public ModelAsset sentenceSimilarityModel;
    public TextAsset vocapAsset;

    private Worker sentenceSimilarityWorker;
    private Worker poolingWorker;

    string string1 = "That is a happy person"; // similarity = 1

    //Choose a string to comapre string1  to:
    string string2 = "That is a happy dog"; // similarity = 0.695
    //string string2 = "That is a very happy person";   // similarity = 0.943
    //string string2 = "Today is a sunny day";          // similarity = 0.257

    //Special tokens
    const int START_TOKEN = 101;
    const int END_TOKEN = 102;

    public string[] tokens;

    public List<int> tokens1;
    public List<int> tokens2;

    private void Start()
    {
        // 백엔드 선택 로직 개선
        // 백엔드 결정 (GPUCompute, GPUPixel, CPU 등)

        var model = ModelLoader.Load(sentenceSimilarityModel);
        sentenceSimilarityWorker = new Worker(model, GetBackendType());

        tokens = vocapAsset.text
            .Split(new[] {
                '\n'
            }, System.StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        tokens1 = GetTokens(string1);
        tokens2 = GetTokens(string2);

        Tensor<float> embedding1 = GetEmbedding(tokens1);
        Tensor<float> embedding2 = GetEmbedding(tokens2);


        Debug.Log("Similarity Score: " + DotScore(embedding1, embedding2));
        
        embedding1.Dispose();
        embedding2.Dispose();
        sentenceSimilarityWorker.Dispose();
    }

    public float DotScore(Tensor<float> tensorA, Tensor<float> tensorB)
    {
        // 1. Tensor 데이터 추출 (CPU에서 읽기 가능한 상태로 변환)
        tensorA.CompleteAllPendingOperations();
        tensorB.CompleteAllPendingOperations();
        float[] dataA = tensorA.DownloadToArray();
        float[] dataB = tensorB.DownloadToArray();

        // 2. FunctionalGraph 생성
        FunctionalGraph graph = new FunctionalGraph();

        // 3. Functional.Constant 올바른 사용법 
        FunctionalTensor A = Functional.Constant(tensorA.shape, dataA);
        FunctionalTensor B = Functional.Constant(tensorB.shape, dataB);

        // 4. 텐서 전치 추가 (MatMul 차원 조건 충족)
        FunctionalTensor B_Transposed = Functional.Transpose(B, 0, 1);

        // 5. 행렬 곱셈 (1,384) x (384,1) → (1,1)
        FunctionalTensor C = Functional.MatMul(A, B_Transposed);

        // 8. 모델 컴파일
        Model model = graph.Compile(C);

        // 9. Worker 생성 및 동기 실행
        Worker worker = new Worker(model, GetBackendType());
        worker.Schedule(); // Schedule() → Execute()로 수정

        // 10. 결과 추출
        using Tensor<float> result = worker.PeekOutput() as Tensor<float>;
        result.CompleteAllPendingOperations();
        
        poolingWorker.Dispose();
        worker.Dispose();
        
        return result[0];
    }
    
    private List<int> GetTokens(string text)
    {
        //split over whitespace
        string[] words = text.ToLower().Split(null);

        var ids = new List<int> {
            START_TOKEN
        };

        string s = "";

        foreach (var word in words)
        {
            int start = 0;
            for (int i = word.Length; i >= 0; i--)
            {
                string subword = start == 0 ? word.Substring(start, i) : "##" + word.Substring(start, i - start);
                int index = System.Array.IndexOf(tokens, subword);
                if (index >= 0)
                {
                    ids.Add(index);
                    s += subword + " ";
                    if (i == word.Length) break;
                    start = i;
                    i = word.Length + 1;
                }
            }
        }

        ids.Add(END_TOKEN);

        Debug.Log("Tokenized sentece = " + s);

        return ids;
    }

    private Tensor<float> GetEmbedding(List<int> tokens)
    {
        int N = tokens.Count;
        using var input_ids = new Tensor<int>(new TensorShape(1, N), tokens.ToArray());
        using var token_type_ids = new Tensor<int>(new TensorShape(1, N), new int[N]);
        int[] mask = new int[N];
        for (int i = 0; i < mask.Length; i++)
        {
            mask[i] = 1;
        }
        using var attention_mask = new Tensor<int>(new TensorShape(1, N), mask);

        sentenceSimilarityWorker.SetInput("input_ids", input_ids);
        sentenceSimilarityWorker.SetInput("attention_mask", attention_mask);
        sentenceSimilarityWorker.SetInput("token_type_ids", token_type_ids);
        sentenceSimilarityWorker.Schedule();

        using var tokenEmbeddings = sentenceSimilarityWorker.PeekOutput("output") as Tensor<float>;
        return MeanPooling(tokenEmbeddings, attention_mask);
    }

    private Tensor<float> MeanPooling(Tensor<float> tokenEmbeddings, Tensor<int> attentionMask)
    {
        // 1. FunctionalGraph 생성 및 입력 등록
        FunctionalGraph graph = new FunctionalGraph();
        var tokenEmbeddingsInput = graph.AddInput<float>(tokenEmbeddings.shape);
        var attentionMaskInput = graph.AddInput<int>(attentionMask.shape);

        // 2. attentionMask를 마지막 차원에 Unsqueeze
        // 예를 들어, Unsqueeze(-1)은 [B, L] -> [B, L, 1]로 변경합니다.
        var reshapedMask = Functional.Reshape(attentionMaskInput, attentionMask.shape.Unsqueeze(-1).ToArray());

        // 3. tokenEmbeddings와 같은 shape으로 Broadcast
        var expandedMask = Functional.BroadcastTo(reshapedMask, tokenEmbeddings.shape.ToArray());

        // 4. mask를 float으로 캐스팅
        var maskFloat = expandedMask.Float();

        // 5. tokenEmbeddings와 mask의 element-wise 곱셈
        var maskedEmbeddings = Functional.Mul(tokenEmbeddingsInput, maskFloat);

        // 6. 토큰 차원(예: dim=1)으로 합산하여 임베딩 합 계산
        var sumEmbeddings = Functional.ReduceSum(maskedEmbeddings, new int[] {
            1
        }, keepdim: false);

        // 7. mask도 같은 방식으로 합산 (평균 계산용)
        var sumMask = Functional.ReduceSum(maskFloat, new int[] {
            1
        }, keepdim: false);

        // 8. sumMask에 대해 clip: 스칼라 상수를 사용하여 broadcast 처리
        var lowerConst = Functional.Constant(1e-9f);
        var upperConst = Functional.Constant(float.MaxValue);
        var clippedMask = Functional.Max(Functional.Min(sumMask, upperConst), lowerConst);

        // 9. 평균 임베딩 계산
        var meanEmbeddings = Functional.Div(sumEmbeddings, clippedMask);

        // 10. L2 norm 계산: norm = sqrt(ReduceSum(Square(meanEmbeddings), dim=1, keepdim=true))
        var squaredMean = Functional.Square(meanEmbeddings);
        var sumSquare = Functional.ReduceSum(squaredMean, new int[] {
            1
        }, keepdim: true);
        var l2Norm = Functional.Sqrt(sumSquare);

        // 11. 정규화: meanEmbeddings / l2Norm
        var normalizedMean = Functional.Div(meanEmbeddings, l2Norm);

        // 12. FunctionalGraph 컴파일
        Model model = graph.Compile(normalizedMean);

        // 13. Worker 생성 및 실행 (예: CPU 백엔드 사용)
        poolingWorker = new Worker(model, GetBackendType());
        poolingWorker.SetInput("input_0", tokenEmbeddings);
        poolingWorker.SetInput("input_1", attentionMask);
        poolingWorker.Schedule();

        Tensor<float> output = poolingWorker.PeekOutput() as Tensor<float>;
        output.CompleteAllPendingOperations();
        
        return output;
    }

    private BackendType GetBackendType()
    {
        BackendType backend = SystemInfo.supportsComputeShaders ? BackendType.GPUCompute :
            (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3) ? BackendType.GPUPixel : BackendType.CPU;
        return backend;
    }

}
