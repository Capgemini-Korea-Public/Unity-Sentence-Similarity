using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HuggingFace.API;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Rendering;

namespace SentenceSimilarityUnity
{
    public static class SentenceSimilarityModule
    {

    #region HuggingFaceAPI

        public static void MeasureSentenceAccuracyFromAPI(string input, Action<float[]> onMeasureSuccess, Action<string> onMeasureFailure, string[] context)
        {
            
            if (context.Length == 0 || input == "")
            {
                onMeasureFailure?.Invoke("No sentences to detect.");
                Debug.LogWarning("No sentences to detect.");
                return;
            }

            ExecuteModelFromHuggingFaceAPI(input, onMeasureSuccess, onMeasureFailure, context);
        }

        // Sentence Similarity Model Run from HuggingFaceAPI
        private static void ExecuteModelFromHuggingFaceAPI(string input, Action<float[]> onMeasureSuccess, Action<string> onMeasureFailure, string[] context)
        {
            HuggingFaceAPI.SentenceSimilarity(input, onMeasureSuccess, onMeasureFailure, context);
      
        }

  #endregion

        // Sentence Similarity Model Run from Sentis
    #region Sentis

        private static ModelAsset _modelAsset;
        private static TextAsset _vocab;

        private static Worker _modelExecuteWorker; // Worker for executing the model
        private static Worker _scoreOpsWorker; // Worker for scoring operations
        private static Worker _poolingWorker; // Worker for pooling operations

        private static string[] _vocabTokens; // Array of vocabulary tokens

        private const int START_TOKEN = 101; // Start token ID
        private const int END_TOKEN = 102; // End token ID


        public static void MeasureSentenceAccuracyFromSentis(string input, Action<float[]> onMeasureSuccess, Action<string> onMeasureFailure, string[] context)
        {
            if (context.Length == 0 || input == "")
            {
                onMeasureFailure?.Invoke("No sentences to detect.");
                Debug.LogWarning("No sentences to detect.");
                return;
            }

            ExecuteModelFromSentis(input, onMeasureSuccess, onMeasureFailure, context);
        }


        // Asynchronously executes the model with a given sentence and compares it against a list of sentences      
        private static async void ExecuteModelFromSentis(string input, Action<float[]> onMeasureSuccess, Action<string> onMeasureFailure, string[] context)
        {
            try
            {
                DateTime startTime = DateTime.Now; // Timer Start

                if (_vocabTokens == null)
                {
                    _modelAsset = Resources.Load<ModelAsset>("MiniLMv6");
                    _vocab = Resources.Load<TextAsset>("vocab");
                    SplitVocabTokens(_vocab); // Split vocabulary tokens if not already initialized
                }

                var model = ModelLoader.Load(_modelAsset);
                _modelExecuteWorker = new Worker(model, GetBackendType());
                
                List<int> tokens1 = GetTokens(input); // Tokenize the input sentence
                using Tensor<float> embedding1 = await GetEmbeddingAsync(tokens1); // Get embedding for the first sentence

                float[] results = new float[context.Length];
                for (int i = 0; i < context.Length; i++)
                {
                    List<int> tokens2 = GetTokens(context[i]); // Tokenize each comparison sentence
                    using Tensor<float> embedding2 = await GetEmbeddingAsync(tokens2); // Get embedding for the comparison sentence
                    float accuracy = DotScore(embedding1, embedding2); // Calculate similarity score
                    results[i] = accuracy;
                }
                AllWorkerDispose();

                onMeasureSuccess?.Invoke(results);
          
                DateTime endTime = DateTime.Now; // 종료 시간 기록
                TimeSpan duration = endTime - startTime; // 소요 시간 계산
                Debug.Log($"ExecuteModelFromSentis 실행 시간: {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}.{duration.Milliseconds:D3}"); // 로그 출력

            }
            catch (Exception e)
            {
                onMeasureFailure?.Invoke(e.Message);
            }
        }

        // Splits the vocabulary asset into individual tokens
        private static void SplitVocabTokens(TextAsset vocap)
        {

            _vocabTokens = vocap.text
                .Split(new[] {
                    '\n'
                }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
        }

        // Calculates the dot product score between two tensors (used for similarity measurement)
        private static float DotScore(Tensor<float> tensorA, Tensor<float> tensorB)
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

            _scoreOpsWorker?.Dispose();
            _scoreOpsWorker = new Worker(model, GetBackendType());
            _scoreOpsWorker.Schedule();

            using Tensor<float> result = _scoreOpsWorker.PeekOutput() as Tensor<float>;
            if (result != null)
            {
                result.CompleteAllPendingOperations();
                return result[0];
            }

            return 0f;

        }

        // Converts a text input into a list of token IDs based on the vocabulary
        private static List<int> GetTokens(string text)
        {

            string[] words = text.ToLower().Split(null);

            var ids = new List<int> {
                START_TOKEN
            };

            var sb = new StringBuilder();

            foreach (var word in words)
            {
                int start = 0;
                for (int i = word.Length; i >= 0; i--)
                {
                    string subword = start == 0 ? word.Substring(start, i) : "##" + word.Substring(start, i - start);
                    int index = Array.IndexOf(_vocabTokens, subword);
                    if (index >= 0)
                    {
                        ids.Add(index);
                        sb.Append(subword).Append(' ');
                        if (i == word.Length) break;
                        start = i;
                        i = word.Length + 1;
                    }
                }
            }

            ids.Add(END_TOKEN);

            Debug.Log($"Tokenized sentence = {sb.ToString().Trim()}");

            return ids;
        }


        // Asynchronously retrieves the embedding for a given list of tokens.
        private static async Task<Tensor<float>> GetEmbeddingAsync(List<int> tokens)
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
            _modelExecuteWorker.SetInput("input_ids", input_ids);
            _modelExecuteWorker.SetInput("attention_mask", attention_mask);
            _modelExecuteWorker.SetInput("token_type_ids", token_type_ids);

            // Execute the model asynchronously.
            var executor = _modelExecuteWorker.ScheduleIterable();
            while (executor.MoveNext())
                await Task.Yield();

            // Retrieve the output embeddings from the worker.
            using var tokenEmbeddings = _modelExecuteWorker.PeekOutput("output") as Tensor<float>;
            if (tokenEmbeddings == null)
            {
                Debug.LogError("tokenEmbeddings is null. Worker execution may have failed.");
                return null;
            }

            // Apply mean pooling to the embeddings.
            return await MeanPoolingAsync(tokenEmbeddings, attention_mask);
        }

        // Applies mean pooling to the token embeddings, considering the attention mask.
        private static async Task<Tensor<float>> MeanPoolingAsync(Tensor<float> tokenEmbeddings, Tensor<int> attentionMask)
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
            _poolingWorker?.Dispose();
            _poolingWorker = new Worker(model, GetBackendType());
            _poolingWorker.SetInput("input_0", tokenEmbeddings);
            _poolingWorker.SetInput("input_1", attentionMask);

            _poolingWorker.Schedule();

            // Retrieve the output from the worker.
            Tensor<float> output = _poolingWorker.PeekOutput() as Tensor<float>;
            if (output == null)
            {
                Debug.LogError("Output is null. Worker execution may have failed.");
                return null;
            }

            output.CompleteAllPendingOperations();
            Tensor<float> clonedOutput = await output.ReadbackAndCloneAsync();
            return clonedOutput;
        }

        private static BackendType GetBackendType()
        {
            BackendType backend = SystemInfo.supportsComputeShaders ? BackendType.GPUCompute :
                (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3) ? BackendType.GPUPixel : BackendType.CPU;
            return backend;
        }

        private static void AllWorkerDispose()
        {
            _modelExecuteWorker?.Dispose();
            _poolingWorker?.Dispose();
            _scoreOpsWorker?.Dispose();
        }

  #endregion

    }


}