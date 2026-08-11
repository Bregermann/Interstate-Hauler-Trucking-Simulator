// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

namespace PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs
{
    public enum Age { Any, Young, MiddleAged, Old }

    public static class ElevenLabsAgeUtility
    {
        public static string GetAgeString(Age age)
        {
            switch (age)
            {
                case Age.MiddleAged:
                    return "middle_aged";
                default:
                    return age.ToString().ToLower();
            }
        }
    }
}
#endif
