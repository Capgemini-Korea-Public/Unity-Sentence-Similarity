using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HuggingFace.API;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class SentenceSimilarity : MonoBehaviour
{
    [Header(" Model Information")]
    [SerializeField] private ActivateType activateType;

    [Header("# Base Information")]
    [SerializeField] private List<string> sentenceList;
    [SerializeField] private int maxSentenceCount = 100;
    [SerializeField] private string enteredSentence;
    public List<string> SentenceList => sentenceList;
    public int SentenceCount => sentenceList.Count;
    public string EnteredSentence => enteredSentence;

    [Header("# Sentis Information")]
    [SerializeField] private ModelAsset sentenceSimilarityModel;
    [SerializeField] private TextAsset vocapAsset;

    [Header("Detection Events")]
    [SerializeField] public UnityEvent OnMeasureBeginEvent;
    [SerializeField] public UnityEvent<SimilarityResult[]> OnMeasureSuccessEvent;
    [SerializeField] public UnityEvent OnMeasureFailEvent;

    [Header("Sentence Events")]
    [SerializeField] public UnityEvent<string> OnSentenceRegisterSuccessEvent;
    [SerializeField] public UnityEvent OnSentenceRegisterFailEvent;
    [SerializeField] public UnityEvent OnSentenceDeleteEvent;


    public void MeasureSentenceAccuracy(string sentence)
    {
        if (sentenceList.Count == 0 || sentence == "")
        {
            OnMeasureFailEvent?.Invoke();
            Debug.LogWarning("No sentences to detect.");
            return;
        }
        
        if (activateType == ActivateType.HuggingFaceAPI)
            ExecuteModelFromHuggingFaceAPI(sentence);
        else
            ExecuteModelFromSentis(sentence);
    }

    // Sentence Similarity Model Run from HuggingFaceAPI
    #region HuggingFaceAPI

    private void ExecuteModelFromHuggingFaceAPI(string sentence)
    {
        OnMeasureBeginEvent?.Invoke();
        enteredSentence = sentence;
        HuggingFaceAPI.SentenceSimilarity(enteredSentence, MeasureSuccess, MeasureFailure, sentenceList.ToArray());
    }

  #endregion

    // Sentence Similarity Model Run from Sentis
    #region Sentis

      private Worker modelExecuteWorker; // Worker for executing the model
      private Worker scoreOpsWorker; // Worker for scoring operations
      private Worker poolingWorker; // Worker for pooling operations
  
      private string[] vocabTokens; // Array of vocabulary tokens
      private List<int> tokens1; // Tokenized representation of the first sentence
      private List<int> tokens2; // Tokenized representation of the second sentence
  
      private const int START_TOKEN = 101; // Start token ID
      private const int END_TOKEN = 102; // End token ID
      
    // Asynchronously executes the model with a given sentence and compares it against a list of sentences      
    private async void ExecuteModelFromSentis(string sentence)
    {
        try
        {
            enteredSentence = sentence;
            OnMeasureBeginEvent?.Invoke();
            
            if (vocabTokens == null)
                SplitVocabTokens(); // Split vocabulary tokens if not already initialized

            var model = ModelLoader.Load(sentenceSimilarityModel);
            modelExecuteWorker = new Worker(model, GetBackendType());

            tokens1 = GetTokens(sentence); // Tokenize the input sentence
            using Tensor<float> embedding1 = await GetEmbeddingAsync(tokens1); // Get embedding for the first sentence

            float[] results = new float[sentenceList.Count];
            for (int i = 0; i < sentenceList.Count; i++)
            {
                tokens2 = GetTokens(sentenceList[i]); // Tokenize each comparison sentence
                using Tensor<float> embedding2 = await GetEmbeddingAsync(tokens2); // Get embedding for the comparison sentence
                float accuracy = DotScore(embedding1, embedding2); // Calculate similarity score
                results[i] = accuracy;
            }
            AllWorkerDispose();

            MeasureSuccess(results);
        }
        catch (Exception e)
        {
            MeasureFailure(e.Message);
        }
    }

    // Splits the vocabulary asset into individual tokens
    private void SplitVocabTokens()
    {

        vocabTokens = vocapAsset.text
            .Split(new[] {
                '\n'
            }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    // Calculates the dot product score between two tensors (used for similarity measurement)
    private float DotScore(Tensor<float> tensorA, Tensor<float> tensorB)
    {
        tensorA.CompleteAllPendingOperations(); // Ensure all GPU operations are completed for tensorA
        tensorB.CompleteAllPendingOperations(); // Ensure all GPU operations are completed for tensorB

        float[] dataA = tensorA.DownloadToArray(); // Download tensorA data to CPU-readable array
        float[] dataB = tensorB.DownloadToArray(); // Download tensorB data to CPU-readable array

        FunctionalGraph graph = new FunctionalGraph(); // Create a functional graph for computation

        FunctionalTensor A = Functional.Constant(tensorA.shape, dataA); // Create constant tensor A from dataA
        FunctionalTensor B = Functional.Constant(tensorB.shape, dataB); // Create constant tensor B from dataB

        FunctionalTensor B_Transposed = Functional.Transpose(B, 0, 1); // Transpose B to match matrix multiplication dimensions

        FunctionalTensor C = Functional.MatMul(A, B_Transposed); // Perform matrix multiplication

        Model model = graph.Compile(C); // Compile the computation graph into a model

        scoreOpsWorker?.Dispose();
        scoreOpsWorker = new Worker(model, GetBackendType());
        scoreOpsWorker.Schedule();

        using Tensor<float> result = scoreOpsWorker.PeekOutput() as Tensor<float>;
        if (result != null)
        {
            result.CompleteAllPendingOperations();
            return result[0];
        }

        return 0f;

    }

    // Converts a text input into a list of token IDs based on the vocabulary
    private List<int> GetTokens(string text)
    {
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
                int index = Array.IndexOf(vocabTokens, subword);
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

        Debug.Log("Tokenized sentence = " + s);

        return ids;
    }

    // Asynchronously retrieves the embedding for a given list of tokens.
    private async Task<Tensor<float>> GetEmbeddingAsync(List<int> tokens)
    {
        int tokensCount = tokens.Count;
        using var input_ids = new Tensor<int>(new TensorShape(1, tokensCount), tokens.ToArray());
        using var token_type_ids = new Tensor<int>(new TensorShape(1, tokensCount), new int[tokensCount]);
        
        // Create an attention mask where all tokens are considered.
        int[] mask = new int[tokensCount];
        for (int i = 0; i < mask.Length; i++)
        {
            mask[i] = 1;
        }
        using var attention_mask = new Tensor<int>(new TensorShape(1, tokensCount), mask);

        // Set inputs for the model execution worker.
        modelExecuteWorker.SetInput("input_ids", input_ids);
        modelExecuteWorker.SetInput("attention_mask", attention_mask);
        modelExecuteWorker.SetInput("token_type_ids", token_type_ids);

        // Execute the model asynchronously.
        var executor = modelExecuteWorker.ScheduleIterable();
        while (executor.MoveNext())
            await Task.Yield();

        // Retrieve the output embeddings from the worker.
        using var tokenEmbeddings = modelExecuteWorker.PeekOutput("output") as Tensor<float>;
        if (tokenEmbeddings == null)
        {
            Debug.LogError("tokenEmbeddings is null. Worker execution may have failed.");
            return null;
        }
        
        // Apply mean pooling to the embeddings.
        return await MeanPoolingAsync(tokenEmbeddings, attention_mask);
    }

    // Applies mean pooling to the token embeddings, considering the attention mask.
    private async Task<Tensor<float>> MeanPoolingAsync(Tensor<float> tokenEmbeddings, Tensor<int> attentionMask)
    {
        // 1. Create a functional graph for mean pooling operations.
        FunctionalGraph graph = new FunctionalGraph();

        // 2. Add input nodes for token embeddings and attention mask.
        var tokenEmbeddingsInput = graph.AddInput<float>(tokenEmbeddings.shape);
        var attentionMaskInput = graph.AddInput<int>(attentionMask.shape);

        // 3. Reshape the attention mask to match the embedding dimensions.
        // For example, reshape from [B, L] to [B, L, 1].
        var reshapedMask = Functional.Reshape(attentionMaskInput, attentionMask.shape.Unsqueeze(-1).ToArray());

        // 4. Broadcast the mask to match the shape of token embeddings.
        var expandedMask = Functional.BroadcastTo(reshapedMask, tokenEmbeddings.shape.ToArray());

        // 5. Cast the mask to float type.
        var maskFloat = expandedMask.Float();

        // 6. Perform element-wise multiplication between embeddings and mask.
        var maskedEmbeddings = Functional.Mul(tokenEmbeddingsInput, maskFloat);

        // 7. Sum the masked embeddings along the token dimension (e.g., dim=1).
        var sumEmbeddings = Functional.ReduceSum(maskedEmbeddings, new int[] {
            1
        }, keepdim: false);

        // 8. Sum the mask values along the same dimension for averaging.
        var sumMask = Functional.ReduceSum(maskFloat, new int[] {
            1
        }, keepdim: false);

        // 9. Clip the sum of mask values to avoid division by zero.
        var lowerConst = Functional.Constant(1e-9f); // Lower bound
        var upperConst = Functional.Constant(float.MaxValue); // Upper bound
        var clippedMask = Functional.Max(Functional.Min(sumMask, upperConst), lowerConst);

        // 10. Calculate the mean embeddings by dividing the sum of embeddings by the clipped mask sum.
        var meanEmbeddings = Functional.Div(sumEmbeddings, clippedMask);

        // 11. Calculate the L2 norm of the mean embeddings.
        var squaredMean = Functional.Square(meanEmbeddings);
        var sumSquare = Functional.ReduceSum(squaredMean, new int[] {
            1
        }, keepdim: true);
        var l2Norm = Functional.Sqrt(sumSquare);

        // 12. Normalize the mean embeddings by dividing by their L2 norm.
        var normalizedMean = Functional.Div(meanEmbeddings, l2Norm);

        // 13. Compile the functional graph into a model.
        Model model = graph.Compile(normalizedMean);

        // 14. Create and execute a worker for mean pooling (e.g., using CPU backend).
        poolingWorker?.Dispose();
        poolingWorker = new Worker(model, GetBackendType());
        poolingWorker.SetInput("input_0", tokenEmbeddings);
        poolingWorker.SetInput("input_1", attentionMask);

        poolingWorker.Schedule();
        
        // Retrieve the output from the worker.
        Tensor<float> output = poolingWorker.PeekOutput() as Tensor<float>;
        if (output == null)
        {
            Debug.LogError("Output is null. Worker execution may have failed.");
            return null;
        }

        output.CompleteAllPendingOperations();
        Tensor<float> clonedOutput = await output.ReadbackAndCloneAsync();
        return clonedOutput;
    }

    private BackendType GetBackendType()
    {
        BackendType backend = SystemInfo.supportsComputeShaders ? BackendType.GPUCompute :
            (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3) ? BackendType.GPUPixel : BackendType.CPU;
        return backend;
    }

    private void AllWorkerDispose()
    {
        modelExecuteWorker?.Dispose();
        poolingWorker?.Dispose();
        scoreOpsWorker?.Dispose();
    }
    private void OnDestroy()
    {
        AllWorkerDispose();
        Resources.UnloadUnusedAssets(); // GPU 리소스 강제 해제
    }

  #endregion


    #region Events

    public void RegisterSentence(string sentence)
    {
        if (maxSentenceCount > SentenceCount && !sentenceList.Contains(sentence))
        {
            OnSentenceRegisterSuccessEvent?.Invoke(sentence);
            sentenceList.Add(sentence);
        }
        else
        {
            Debug.LogWarning($"Sentence => {sentence} is not registered");
            OnSentenceRegisterFailEvent?.Invoke();
        }
    }

    public void DeleteSentence(string sentence)
    {
        if (!sentenceList.Contains(sentence))
        {
            Debug.LogError($"Sentence => {sentence} does not exist");
            return;
        }

        sentenceList.Remove(sentence);
        OnSentenceDeleteEvent?.Invoke();
    }

    private void MeasureFailure(string message)
    {
        OnMeasureFailEvent?.Invoke();
        Debug.LogError($"Detect Fail! \n{message}");
    }

    private void MeasureSuccess(float[] accuracy)
    {
        Debug.Log("Sentences Detected");

        SimilarityResult[] results = new SimilarityResult[accuracy.Length];
        for (int i = 0; i < accuracy.Length; i++)
        {
            Debug.Log($"{sentenceList[i]} => {accuracy[i]}");
            results[i].accuracy = accuracy[i];
            results[i].sentence = sentenceList[i];
        }
        Array.Sort(results, (a, b) => b.accuracy.CompareTo(a.accuracy));

        OnMeasureSuccessEvent?.Invoke(results);
    }

  #endregion
}


public enum ActivateType { HuggingFaceAPI, Sentis }

public struct SimilarityResult
{
    public string sentence;
    public float accuracy;
}
