// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    [Serializable]
    public class RuntimeAIResultQuestion
    {
        [SerializeField] private string question;
        [SerializeField] [VariablePopup] private string recordInVariable;

        public string Question => question;
        public string RecordInVariable => recordInVariable;
    }
}
#endif
