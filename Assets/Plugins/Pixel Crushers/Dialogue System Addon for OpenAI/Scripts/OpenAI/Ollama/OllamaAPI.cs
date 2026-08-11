// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Handles web requests to OpenAI API.
    /// </summary>
    public static class OllamaAPI
    {

        public const string DefaultOllamaURL = "http://localhost:11434/api/generate";

        public static string currentOllamaURL = DefaultOllamaURL;

        [Serializable]
        public class CompletionData
        {
            public string model;
            public string prompt;
            public string suffix = string.Empty;
        }

        [Serializable]
        public class CompletionResponse
        {
            public string model;
            public string created_at;
            public string response;
            public bool done;
        }

        /// <summary>
        /// Given a prompt, the model will return one or more predicted completions, and can also return the probabilities of alternative tokens at each position.
        /// </summary>
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="model">Model to use.</param>
        /// <param name="temperature">What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic. We generally recommend altering this or top_p but not both.</param>
        /// <param name="top_p">An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered. We generally recommend altering this or temperature but not both.</param>
        /// <param name="frequency_penalty">Number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.</param>
        /// <param name="presence_penalty">Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.</param>
        /// <param name="maxTokens">The maximum number of tokens to generate in the completion. The token count of your prompt plus max_tokens cannot exceed the model's context length.</param>
        /// <param name="prompt">The prompt(s) to generate completions for, encoded as a string, array of strings, array of tokens, or array of token arrays.</param>
        /// <param name="callback">This event handler will be passed the API result.</param>
        public static UnityWebRequestAsyncOperation SubmitCompletionAsync(string model,
            float temperature, float top_p,
            float frequency_penalty, float presence_penalty,
            int maxTokens,
            string prompt, Action<string> callback)
        {
            var completionRequest = new CompletionData();
            completionRequest.model = model;
            completionRequest.prompt = prompt;

            string jsonData = JsonUtility.ToJson(completionRequest);

            // Some models don't allow line breaks, so remove them and sanitize quotes:
            var sanitizedPrompt = prompt.Replace("\"", "\\\"").Replace("\r", ". ").Replace("\n", ". ");
            jsonData = $"{{ \"model\": \"{model}\", \"prompt\": \"{sanitizedPrompt}\", \"stream\": false }}";

            if (DialogueDebug.logInfo) Debug.Log(jsonData);

            UnityWebRequest webRequest = WebRequestUtility.CreateWebRequest("", currentOllamaURL, jsonData);

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                var text = success ? webRequest.downloadHandler.text : string.Empty;
                if (!success) Debug.Log($"{webRequest.error}\n{webRequest.downloadHandler.text}");
                webRequest.Dispose();
                webRequest = null;

                if (!string.IsNullOrEmpty(text))
                {
                    var responseData = JsonUtility.FromJson<CompletionResponse>(text);
                    text = responseData.response.Trim();
                }
                callback?.Invoke(text);
            };

            return asyncOp;
        }
    }

}

#endif
