# Sentence Similarity from Unity

![Unity](https://img.shields.io/badge/Unity-2023.2+-black.svg?style=flat&logo=unity) ![License](https://img.shields.io/badge/License-MIT-blue.svg?style=flat) 

Welcome to **Sentence Similarity from Unity** – a powerful Unity module that simplifies the use of AI models to infer the most similar sentence to a given input. Perfect for creating intelligent NPCs, enhancing dialogue systems, or powering text-based features in your game, this tool supports two execution options: **Sentis** for local inference and **HuggingFace** for robust API-driven models.

---

## Key Features

- **Multiple Execution Options**: 
  - **Sentis (Local)**: Perform on-device processing for privacy and offline capabilities.
  - **HuggingFace (API)**: Leverage cloud-powered AI models for high accuracy and scalability.

---

## Installation

### 1. HuggingFace API Setup
![HuggingFace Setup](https://github.com/user-attachments/assets/f5dabc08-fc79-402b-9c64-3d868e290b9b)

Follow the detailed guide to set up the HuggingFace API **`(complete up to Step 3)`**
- **[HuggingFace API Installation Tutorial](https://thomassimonini.substack.com/p/building-a-smart-robot-ai-using-hugging)**

### 2. Unity Sentence Similarity Module Installation
![Package Manager](https://github.com/user-attachments/assets/ea7df365-e492-4732-8934-eba837176f73)

1. Open Unity Editor and navigate to **Window → Package Manager**.  
   ![Add Package](https://github.com/user-attachments/assets/a5769f40-dd93-4753-9806-14bc72f9a7f7)  
2. Click the `+` button and select **Add package from git URL**.  
   ![Git URL](https://github.com/user-attachments/assets/85bb23e0-784b-4619-aa43-5ce684187198)  
3. Enter the following URL and install
```csharp
https://github.com/Capgemini-Korea-Public/Unity-Sentence-Similarity.git
```
## Usage

Add the `SentenceSimilarityController` component to a GameObject in your Unity scene to start measuring sentence similarities. Below is an example of how to set it up and use it:

### Example Setup
![image](https://github.com/user-attachments/assets/e0a8a4d3-5788-4af3-9dd6-5d432fbf1e5c)

1. Attach the `SentenceSimilarityController` script to a GameObject.
2. Configure the component in the Inspector
- Set the **Activate Type** to either `HuggingFaceAPI` or `Sentis`.
- Populate the **Sentence List** with sentences to compare against.
- Assign Unity Events for success, failure, and other actions (e.g `OnMeasureSuccessEvent`).
3. Check out the sample scene for an exact example!

### Sample Code
```csharp
using SentenceSimilarityUnity;
using UnityEngine;

public class SentenceSimilarityExample : MonoBehaviour
{
 void Start()
 {
     // Access the singleton instance
     SentenceSimilarityController controller = SentenceSimilarityController.Instance;

     // Register some sentences to compare against
     controller.RegisterSentence("Hello, how are you?");
     controller.RegisterSentence("Hi, what's up?");
     controller.RegisterSentence("Greetings, friend!");

     // Measure similarity with an input sentence
     controller.MeasureSentenceAccuracy("Hey, how's it going?");
 }

 // Callback for successful similarity measurement
 public void OnSimilarityMeasured(SimilarityResult[] results)
 {
     foreach (var result in results)
     {
         Debug.Log($"Sentence: '{result.sentence}' | Similarity: {result.accuracy}");
     }
 }
}
```

3. In the Inspector, assign the OnSimilarityMeasured method to the OnMeasureSuccessEvent to handle the results. The results are sorted by similarity score in descending order.

---

## How It Works

- **RegisterSentence**: Adds a sentence to the comparison list (up to a predefined maximum count).
- **MeasureSentenceAccuracy**: Compares an input sentence against registered sentences using the selected model (`Sentis` or `HuggingFace`).
- **Events**: Use Unity Events like `OnMeasureSuccessEvent` or `OnMeasureFailureEvent` to handle results or errors.

---

## Contact
Have questions or need help? Feel free to reach out at `twotwo12345678@gmail.com`. <br/>
We’d love to hear from you!

---
**Start building smarter games with Unity today!**
