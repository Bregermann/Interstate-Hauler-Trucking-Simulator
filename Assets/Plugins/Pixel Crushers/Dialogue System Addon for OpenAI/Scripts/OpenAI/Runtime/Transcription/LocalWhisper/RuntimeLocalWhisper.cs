// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI && USE_WHISPER

using UnityEngine;
using Whisper;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Add to same GameObject as RuntimeAIConversationSettings to use
    /// local whisper.unity for speech to text transcription.
    /// </summary>
    [RequireComponent(typeof(RuntimeAIConversationSettings))]
    public class RuntimeLocalWhisper : MonoBehaviour
    {

        [Tooltip("If unassigned, a default Whisper Manager will be created on this GameObject at runtime.")]
        public WhisperManager whisperManager;

        protected virtual void Start()
        {
            if (whisperManager == null) whisperManager = gameObject.AddComponent<WhisperManager>();
            var settings = GetComponent<RuntimeAIConversationSettings>();
            settings.TranscriptionService = new LocalWhisperTranscriptionService(whisperManager);
        }

    }
}

#endif
