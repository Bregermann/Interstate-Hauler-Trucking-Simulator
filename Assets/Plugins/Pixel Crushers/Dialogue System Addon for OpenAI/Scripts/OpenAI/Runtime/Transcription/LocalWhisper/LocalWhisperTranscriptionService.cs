// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI && USE_WHISPER

using System;
using UnityEngine;
using Whisper;
using Whisper.Utils;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Locally-installed Whisper speech to text transcription service.
    /// You must first install Macoron's whisper.unity package:
    /// GitHub page: https://github.com/Macoron/whisper.unity
    /// Installation: Package Manager > + > Add package from git URL...
    ///     https://github.com/Macoron/whisper.unity.git?path=/Packages/com.whisper.unity
    /// If this is useful, consider donating to Marocon: https://ko-fi.com/macoron
    /// 
    /// Then install at least one Whisper model from:
    ///     https://huggingface.co/ggerganov/whisper.cpp/tree/main
    /// and put it in Assets/StreamingAssets/Whisper. (Start with ggml-tiny.bin)
    /// 
    /// </summary>
    public class LocalWhisperTranscriptionService : ITranscriptionService
    {

        /// <summary>
        /// Name of the service, used in logging.
        /// </summary>
        public string Name { get; } = "whisper.unity";

        public WhisperManager Manager { get; protected set; }

        public LocalWhisperTranscriptionService(WhisperManager manager)
        {
            Manager = manager;
        }

        /// <summary>
        /// Transcribe a recorded audio clip to text and pass the text to a callback function.
        /// </summary>
        public async void TranscribeSpeech(AudioClip audioClip, Action<string> callback)
        {
            var result = await Manager.GetTextAsync(audioClip);
            callback?.Invoke(result != null ? result.Result : null);
        }

    }
}

#endif
