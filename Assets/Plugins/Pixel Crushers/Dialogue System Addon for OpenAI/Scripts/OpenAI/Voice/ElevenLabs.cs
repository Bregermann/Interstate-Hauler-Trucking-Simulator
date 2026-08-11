// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using UnityEngine;
using UnityEngine.Networking;

namespace PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs
{

    /// <summary>
    /// Handles web requests to ElevenLabs API.
    /// </summary>
    public static class ElevenLabs
    {

        public const string ModelListURL = "https://api.elevenlabs.io/v1/models";
        public const string VoiceListURL = "https://api.elevenlabs.io/v1/voices";
        public const string TextToSpeechURL = "https://api.elevenlabs.io/v1/text-to-speech";

        public const string SharedVoiceListURL = "https://api.elevenlabs.io/v1/shared-voices";

        public enum Models
        {
            Monolingual_v1,
            Multilingual_v1,
            Multilingual_v2,
            Turbo_v2,
            Flash_v2,
            Flash_v2_5,
            Eleven_v3
        }

        public static bool IsApiKeyValid(string apiKey)
        {
            return !string.IsNullOrEmpty(apiKey);
        }

        public static string GetDefaultModelId()
        {
            return GetModelId(Models.Monolingual_v1);
        }

        public static string GetModelId(Models model) 
        {
            switch (model)
            {
                default:
                case Models.Monolingual_v1:
                    return "eleven_monolingual_v1";
                case Models.Multilingual_v1:
                    return "eleven_multilingual_v1";
                case Models.Multilingual_v2:
                    return "eleven_multilingual_v2";
                case Models.Turbo_v2:
                    return "eleven_turbo_v2";
                case Models.Flash_v2:
                    return "eleven_flash_v2";
                case Models.Flash_v2_5:
                    return "eleven_flash_v2_5";
                case Models.Eleven_v3:
                    return "eleven_v3";
            }
        }

        /// <summary>
        /// Gets a list of all available voices for a user.
        /// </summary>
        /// <param name="apiKey">ElevenLabs API key.</param>
        /// <param name="callback">List of voices.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation GetVoiceList(string apiKey, Action<VoiceList> callback)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(VoiceListURL + "?token=" + apiKey);
            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;
            webRequest.SetRequestHeader("xi-api-key", apiKey);

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                var text = success ? webRequest.downloadHandler.text : string.Empty;
                if (!success) Debug.Log($"{webRequest.error}\n{webRequest.downloadHandler.text}");
                webRequest.Dispose();
                webRequest = null;

                VoiceList voiceList = null;
                if (!string.IsNullOrEmpty(text))
                {
                    voiceList = JsonUtility.FromJson<VoiceList>(text);
                }
                callback?.Invoke(voiceList);
            };

            return asyncOp;
        }

        /// <summary>
        /// Gets a list of all available voices for a user.
        /// </summary>
        /// <param name="apiKey">ElevenLabs API key.</param>
        /// <param name="callback">List of voices.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation GetSharedVoiceList(string apiKey, 
            VoiceCategory category, Gender gender, Age age,
            string accent, string search,
            Action<VoiceList> callback)
        {
            var url = SharedVoiceListURL + "?token=" + apiKey;
            url += "&page_size=100";
            url += $"&category={ElevenLabsVoiceCategoryUtility.GetCategoryString(category)}";
            if (gender != Gender.Any) url += $"&gender={ElevenLabsGenderUtility.GetGenderString(gender)}";
            if (age != Age.Any) url += $"&age={ElevenLabsAgeUtility.GetAgeString(age)}";
            if (!string.IsNullOrEmpty(accent)) url += $"&accent={accent}";
            if (!string.IsNullOrEmpty(search)) url += $"&search={search}";

            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;
            webRequest.SetRequestHeader("xi-api-key", apiKey);
            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                var text = success ? webRequest.downloadHandler.text : string.Empty;
                if (!success) Debug.Log($"{webRequest.error}\n{webRequest.downloadHandler.text}");
                webRequest.Dispose();
                webRequest = null;

                VoiceList voiceList = null;
                if (!string.IsNullOrEmpty(text))
                {
                    voiceList = JsonUtility.FromJson<VoiceList>(text);
                }
                callback?.Invoke(voiceList);
            };

            return asyncOp;
        }

        /// <summary>
        /// Converts text into speech using a voice of your choice and returns audio.
        /// </summary>
        /// <param name="apiKey">ElevenLabs API key.</param>
        /// <param name="model_id">ElevenLabs model.</param>
        /// <param name="voice_name">Name of voice to use.</param>
        /// <param name="voice_id">ID of voice to use.</param>
        /// <param name="stability">Determines how stable the voice is and the randomness between each generation. Lower values introduce broader emotional range for the voice. Higher values can result in a monotonous voice with limited emotion.</param>
        /// <param name="similarity_boost">Determines how closely the AI should adhere to the original voice when attempting to replicate it.</param>
        /// <param name="text">Text to convert to speech audio.</param>
        /// <param name="callback">Resulting audio clip.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation GetTextToSpeech(string apiKey,
            string model_id, string voice_name, string voice_id,
            float stability, float similarity_boost, string text, Action<AudioClip> callback)
        {
            return GetTextToSpeech(apiKey, model_id, voice_name, voice_id, stability, similarity_boost, 
                use_speaker_boost:false, style:0, text, callback);
        }

        /// <summary>
        /// Converts text into speech using a voice of your choice and returns audio.
        /// </summary>
        /// <param name="apiKey">ElevenLabs API key.</param>
        /// <param name="model_id">ElevenLabs model.</param>
        /// <param name="voice_name">Name of voice to use.</param>
        /// <param name="voice_id">ID of voice to use.</param>
        /// <param name="stability">Determines how stable the voice is and the randomness between each generation. Lower values introduce broader emotional range for the voice. Higher values can result in a monotonous voice with limited emotion.</param>
        /// <param name="similarity_boost">Determines how closely the AI should adhere to the original voice when attempting to replicate it.</param>
        /// <param name="use_speaker_boost">This setting boosts the similarity to the original speaker. Using this setting requires a slightly higher computational load, which in turn increases latency.</param>
        /// <param name="style">Determines the style exaggeration of the voice. This setting attempts to amplify the style of the original speaker. It does consume additional computational resources and might increase latency if set to anything other than 0.</param>
        /// <param name="text">Text to convert to speech audio.</param>
        /// <param name="callback">Resulting audio clip.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation GetTextToSpeech(string apiKey, 
            string model_id, string voice_name, string voice_id,
            float stability, float similarity_boost, bool use_speaker_boost, float style,
            string text, Action<AudioClip> callback)
        {
            var url = $"{TextToSpeechURL}/{voice_id}";

            Debug.Log(url);

            var ttsRequest = new TextToSpeechRequest(model_id, text, new VoiceSettings(stability, similarity_boost));
            string jsonData = JsonUtility.ToJson(ttsRequest);

            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);

#if UNITY_2022_2_OR_NEWER
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm(url, jsonData);
#else
            UnityWebRequest webRequest = UnityWebRequest.Post(url, jsonData);
#endif
            webRequest.uploadHandler.Dispose();
            webRequest.uploadHandler = new UploadHandlerRaw(postData);
            webRequest.downloadHandler.Dispose();
            webRequest.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("xi-api-key", apiKey);

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                AudioClip audioClip = null;
                var success = webRequest.result == UnityWebRequest.Result.Success;
                if (success)
                {
                    audioClip = (webRequest.downloadHandler as DownloadHandlerAudioClip).audioClip;
                }
                else
                {
                    Debug.Log($"{webRequest.error}");
                }
                webRequest.Dispose();
                webRequest = null;

                callback?.Invoke(audioClip);
            };

            return asyncOp;
        }

    }
}

#endif
