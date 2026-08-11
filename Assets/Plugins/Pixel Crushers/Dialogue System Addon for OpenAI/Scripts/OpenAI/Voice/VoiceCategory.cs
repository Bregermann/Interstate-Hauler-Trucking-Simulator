// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

namespace PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs
{
    public enum VoiceCategory { Professional, HighQuality, Famous, Generated, Cloned }

    public static class ElevenLabsVoiceCategoryUtility
    {
        public static string GetCategoryString(VoiceCategory category)
        {
            switch (category)
            {
                case VoiceCategory.HighQuality:
                    return "high_quality";
                default:
                    return category.ToString().ToLower();
            }
        }
    }

}
#endif
