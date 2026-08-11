// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

namespace PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs
{
    public enum Gender { Any, Male, Female, Neutral }

    public static class ElevenLabsGenderUtility
    {
        public static string GetGenderString(Gender gender)
        {
            return gender.ToString().ToLower();
        }
    }
}
#endif
