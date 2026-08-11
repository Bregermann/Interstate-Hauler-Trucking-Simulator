// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI && USE_DEEPVOICE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.OpenAIAddon.DeepVoice
{

    /// <summary>
    /// Add to same GameObject as RuntimeAIConversationSettings to use
    /// DeepVoice for text to speech.
    /// </summary>
    [RequireComponent(typeof(RuntimeAIConversationSettings))]
    public class RuntimeDeepVoice : MonoBehaviour
    {

        [HelpBox("Note: DeepVoice may return audio formats that Unity is unable to process at runtime. If this happens, no audio will play.", HelpBoxMessageType.Warning)]
        public float variability = 0.3f;
        public float clarity = 0.75f;

        protected virtual void Start()
        {
            var settings = GetComponent<RuntimeAIConversationSettings>();
            settings.VoiceService = new DeepVoiceService(variability, clarity);
        }

    }
}

#endif
