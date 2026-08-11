// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI && USE_DEEPVOICE

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.OpenAIAddon.DeepVoice
{

    /// <summary>
    /// IVoiceService implementation that uses DeepVoice.
    /// </summary>
    public class DeepVoiceService : IVoiceService
    {

        public string Name => "DeepVoice";
        public string SequencerCommand => "GenerateDeepVoice()";

        public float Variability { get; set; }
        public float Clarity { get; set; }

        public DeepVoiceService(float variability, float clarity)
        {
            Variability = variability;
            Clarity = clarity;
        }

        public void GenerateTextToSpeech(string voiceName, string voiceID,
            string text, Action<AudioClip> callback)
        {
            if (string.IsNullOrEmpty(voiceName)) return;
            DeepVoiceAPI.GetTextToSpeechRuntime(DeepVoiceAPI.GetDeepVoiceModel(voiceID), 
                voiceName, text, Variability, Clarity, callback);
        }

    }
}

#endif
