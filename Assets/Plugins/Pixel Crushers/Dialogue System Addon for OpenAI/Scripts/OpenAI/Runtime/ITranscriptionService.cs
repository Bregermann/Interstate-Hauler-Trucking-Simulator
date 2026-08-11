// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Interface that provides speech to text transcription service.
    /// </summary>
    public interface ITranscriptionService
    {

        /// <summary>
        /// Name of the service, used in logging.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Transcribe a recorded audio clip to text and pass the text to a callback function.
        /// </summary>
        public void TranscribeSpeech(AudioClip recordedClip, Action<string> callback);

    }
}

#endif
