// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    #region Text Generation

    public enum TextModelName
    {
        GPT5_6_Sol, GPT5_6_Terra, GPT5_6_Luna,
        GPT5_5, GPT5_5_Pro,
        GPT5_4, GPT5_4_Pro, GPT5_4_mini, GPT5_4_nano,
        GPT5_2, GPT5_2_pro,
        GPT5_1,
        GPT5, GPT5_mini, GPT5_nano,
        GPT_4_1,
        o4_mini, o3, o3_mini, o1_mini, o1, o1_pro,
        FineTune,
        Llama3_2, Llama3_3, CustomOllama
    }

    [Serializable]
    public class CompletionRequest
    {
        public string model;
        public string prompt;
        public float temperature;
        public float top_p;
        public float frequency_penalty;
        public float presence_penalty;
        public int max_tokens;

        public CompletionRequest(string modelName, string prompt, 
            float temperature, float top_p, 
            float frequency_penalty, float presence_penalty,
            int maxTokens)
        {
            this.model = modelName;
            this.prompt = prompt;
            this.temperature = temperature;
            this.max_tokens = maxTokens;
            this.top_p = top_p;
            this.frequency_penalty = frequency_penalty;
            this.presence_penalty = presence_penalty;
        }

        public CompletionRequest(string modelName, string prompt,
            float temperature, 
            int maxTokens)
        {
            this.model = modelName;
            this.prompt = prompt;
            this.temperature = temperature;
            this.max_tokens = maxTokens;
            this.top_p = 1;
            this.frequency_penalty = 0;
            this.presence_penalty = 0;
        }
    }

    [Serializable]
    public class CompletionResponse
    {
        public string id;
        public string @object;
        public int created;
        public string model;
        public Choice[] choices;
        public Usage usage;
    }

    [Serializable]
    public class Choice
    {
        public string text;
        public int index;
        public object logprobs;
        public string finish_reason;
    }

    [Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }


    [Serializable]
    public class EditRequest
    {
        public string model;
        public string input;
        public string instruction;
        public float temperature;
        public float top_p;
        public float frequency_penalty;
        public float presence_penalty;
        public int max_tokens;

        public EditRequest(string modelName, string input, string instruction, 
            float temperature, float top_p,
            float frequency_penalty, float presence_penalty, 
            int maxTokens)
        {
            this.model = modelName;
            this.input = input;
            this.instruction = instruction;
            this.temperature = temperature;
            this.max_tokens = maxTokens;
            this.top_p = top_p;
            this.frequency_penalty = frequency_penalty;
            this.presence_penalty = presence_penalty;
        }

        public EditRequest(string modelName, string input, string instruction,
            float temperature, 
            int maxTokens)
        {
            this.model = modelName;
            this.input = input;
            this.instruction = instruction;
            this.temperature = temperature;
            this.max_tokens = maxTokens;
            this.top_p = 1;
            this.frequency_penalty = 0;
            this.presence_penalty = 0;
        }
    }

    [Serializable]
    public class EditResponse
    {
        public string @object;
        public int created;
        public Choice[] choices;
        public Usage usage;
    }

    [Serializable]
    public class ReasoningRequest
    {
        public string model;
        public string reasoning_effort = "medium";
        public List<ChatMessage> messages;

        public ReasoningRequest(string modelName, List<ChatMessage> messages)
        {
            this.model = modelName;
            this.messages = messages;
        }
    }

    [Serializable]
    public class ChatRequest
    {
        public string model;
        public List<ChatMessage> messages;
        public float temperature;
        public float top_p;
        public float frequency_penalty;
        public float presence_penalty;
        public int max_tokens;

        public ChatRequest(string modelName, List<ChatMessage> messages, 
            float temperature, float top_p,
            float frequency_penalty, float presence_penalty, 
            int maxTokens)
        {
            this.model = modelName;
            this.messages = messages;
            this.temperature = temperature;
            this.max_tokens = maxTokens;
            this.top_p = top_p;
            this.frequency_penalty = frequency_penalty;
            this.presence_penalty = presence_penalty;
        }

        public ChatRequest(string modelName, List<ChatMessage> messages,
            float temperature, int maxTokens)
        {
            this.model = modelName;
            this.messages = messages;
            this.temperature = temperature;
            this.max_tokens = maxTokens;
            this.top_p = 1;
            this.frequency_penalty = 0;
            this.presence_penalty = 0;
        }
    }

    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [Serializable]
    public class ChatResponse
    {
        public string id;
        public string @object;
        public int created;
        public string model;
        public ChatChoice[] choices;
        public Usage usage;
    }

    [Serializable]
    public class ChatChoice
    {
        public ChatMessage message;
        public string finish_reason;
        public int index;
    }

    #endregion

    #region Image Generation

    public enum ImageSizes { Auto, Size1024x1024, Size1536x1024, Size1024x1536 }

    [Serializable]
    public class ImageGenerationRequest
    {
        public string prompt;
        public string model;
        public int n; // Number of images to generate.
        public string size; 
        [NonSerialized] public string response_format;
        public string user;

        public ImageGenerationRequest(string prompt, int n, string size, string response_format, string user)
        {
            this.prompt = prompt;
            this.model = Model.ImageGenerationModel;
            this.n = n;
            this.size = size;
            this.response_format = response_format;
            this.user = user;
        }
    }

    [Serializable]
    public class ImageEditRequest
    {
        public string image;
        public string mask;
        public string prompt;
        public int n; // Number of images to generate.
        public string size; // Must be 256x256, 512x512, or 1024x1024.
        public string response_format;
        public string user;

        public ImageEditRequest(string image, string mask, string prompt, int n, string size, string response_format, string user)
        {
            this.image = image;
            this.mask = mask;
            this.prompt = prompt;
            this.n = n;
            this.size = size;
            this.response_format = response_format;
            this.user = user;
        }
    }

    [Serializable]
    public class ImagesResponse
    {
        public int created;
        public string size;
        public List<ImageResult> data;
    }

    [Serializable]
    public class ImageResult
    {
        public string url;
        public string b64_json;
    }

    #endregion

    #region Audio

    public enum AudioResponseFormat { Json, Text, SRT, Verbose_Json, VTT }

    [Serializable]
    public class AudioTranscriptionResponse
    {
        public string text;
    }

    public enum STTModel { GPT_4o_transcribe, GPT_4o_mini_transcribe, whisper_1 }

    public enum TTSModel { GPT_4o_mini_tts, TTSModel1, TTSModel1HD }

    public enum Voices { Alloy, Echo, Fable, Onyx, Nova, Shimmer }

    public enum VoiceOutputFormat { MP3, WAV } // Opus, AAC, FLAC, PCM not supported.

    [Serializable] 
    public class AudioSpeechRequest
    {
        public string model;
        public string input;
        public string voice;
        public string response_format;
        public float speed;

        public AudioSpeechRequest(string model, string input, string voice, string responseFormat, float speed)
        {
            this.model = model;
            this.input = input;
            this.voice = voice;
            this.response_format = responseFormat;
            this.speed = speed;
        }
    }

    #endregion

}

#endif
