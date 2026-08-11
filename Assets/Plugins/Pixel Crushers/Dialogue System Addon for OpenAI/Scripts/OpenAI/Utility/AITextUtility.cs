// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Globalization;
using System.Text.RegularExpressions;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Utility methods for OpenAI addon.
    /// </summary>
    public static class AITextUtility
    {

        /// <summary>
        /// Gets English language name from a language code. 
        /// If can't determine English name, returns language code itself.
        /// </summary>
        public static string DetermineLanguage(string languageCode)
        {
            try
            {
                var cultureInfo = new CultureInfo(languageCode);
                return (cultureInfo == null || string.IsNullOrEmpty(cultureInfo.Name)) ? languageCode : cultureInfo.EnglishName;
            }
            catch (CultureNotFoundException)
            {
                return languageCode;
            }
        }

        public static string ExtractSpeaker(ref string line, DialogueDatabase database)
        {
            var speaker = string.Empty;

            // Match **speaker**: text
            var pattern = @"^\*\*[^\*]+:\*\* ";
            var match = Regex.Match(line, pattern);
            if (match.Success)
            {
                speaker = match.Value.Substring(2, match.Value.Length - 6);
                line = RemoveSurroundingQuotes(line.Substring(match.Value.Length));
                return speaker;
            }

            // Match <speaker>: text or <speaker> <says>: text
            var pos = line.IndexOf(":");
            if (pos != -1)
            {
                speaker = RemoveLastWord(line.Substring(0, pos));
                line = RemoveSurroundingQuotes(line.Substring(pos + 1));
                return speaker;
            }

            // Otherwise if line ends with a quote character,
            // grab text from first quote to end.
            if (line.EndsWith("\""))
            {
                var firstQuotePos = line.IndexOf("\"");
                speaker = RemoveLastWord(line.Substring(0, firstQuotePos));
                line = RemoveSurroundingQuotes(line.Substring(firstQuotePos));
                return speaker;
            }
            if (line.EndsWith("'"))
            {
                var firstQuotePos = line.IndexOf("'");
                speaker = RemoveLastWord(line.Substring(0, firstQuotePos));
                line = RemoveSurroundingQuotes(line.Substring(firstQuotePos));
                return speaker;
            }

            // Otherwise look for <speaker> says, text
            if (line.Contains(" says, "))
            {
                var saysPos = line.IndexOf(" says, ");
                speaker = line.Substring(0, saysPos).Trim();
                line = RemoveSurroundingQuotes(line.Substring(pos + " says, ".Length));
                return speaker;
            }

            // Fallback:
            return speaker;
        }

        public static string RemoveLastWord(string line)
        {
            var lastSpacePos = line.LastIndexOf(' ');
            if (lastSpacePos != -1) return line.Substring(0, lastSpacePos).Trim();
            return line.Trim();
        }

        /// <summary>
        /// Removes "speaker:", "speaker says:", or surrounding quotes around lines.
        /// </summary>
        public static string RemoveSpeaker(string speaker, string line)
        {
            // Match <speaker>: text
            if (line.StartsWith($"{speaker}:"))
            {
                return RemoveSurroundingQuotes(line.Substring(speaker.Length + 2));
            }

            // Match **speaker**: text
            if (line.StartsWith($"**{speaker}**:"))
            {
                return RemoveSurroundingQuotes(line.Substring(speaker.Length + 6));
            }

            // Match <speaker> <says>: text
            // where <says> could be any word and text could have surrounding quotes.
            var pattern = $"^{speaker} \\w+[:,] ";
            var match = Regex.Match(line, pattern);
            if (match.Success)
            {
                return RemoveSurroundingQuotes(line.Substring(match.Value.Length));
            }

            // Otherwise if line ends with a quote character,
            // grab text from first quote to end.
            if (line.EndsWith("\""))
            {
                var pos = line.IndexOf('"');
                return RemoveSurroundingQuotes(line.Substring(pos));
            }
            if (line.EndsWith("'"))
            {
                var pos = line.IndexOf(" '");
                return RemoveSurroundingQuotes(line.Substring(pos + 1));
            }

            // Otherwise look for <speaker> says, text
            if (line.Contains(" says, "))
            {
                var pos = line.IndexOf(" says, ");
                return RemoveSurroundingQuotes(line.Substring(pos + " says, ".Length));
            }

            // Fallback:
            return line;
        }

        /// <summary>
        /// Removes double quotes around string if present.
        /// </summary>
        public static string RemoveSurroundingQuotes(string text)
        {
            if (text.StartsWith("\"") || text.StartsWith("'"))
            {
                return text.Substring(1, text.Length - 2);
            }
            else
            {
                return text;
            }
        }

        public static string DoubleQuotesToSingle(string text)
        {
            return text.Replace('"', '\'');
        }

        public static string GetTranslationText(string text, string language)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = text.Trim();
            // Example: The translation of drawer is: cajón
            if (text.StartsWith("The translation of "))
            {
                var translationPos = text.IndexOf(" is: ");
                if (translationPos >= 0)
                {
                    text = text.Substring(translationPos + " is: ".Length);
                }
                else
                {
                    translationPos = text.IndexOf(" is ");
                    if (translationPos >= 0)
                    {
                        text = text.Substring(translationPos + " is ".Length);
                    }
                }
            }
            else
            {
                // Example: "Drawer" in Spanish is "Cajón".
                var inLanguageIs = $" in {language} is ";
                var inLanguageIsPos = text.IndexOf(inLanguageIs);
                if (inLanguageIsPos >= 0)
                {
                    text = text.Substring(inLanguageIsPos + inLanguageIs.Length);
                    if (text.EndsWith(".")) text = text.Substring(0, text.Length - 1);
                }
                else if (text.EndsWith("\"") || text.EndsWith("\"."))
                {
                    // Example: "Drawer" is "Cajón"
                    var lastQuotePos = text.LastIndexOf("\"");
                    var translationPos = text.Substring(0, lastQuotePos).LastIndexOf("\"");
                    if (translationPos >= 0)
                    {
                        text = text.Substring(translationPos);
                    }
                }
            }
            return RemoveSurroundingQuotes(text);
        }

        /// <summary>
        /// If dialogue entry has Audio Tag field, prepends it to text.
        /// </summary>
        public static string PrependAudioTag(string text, DialogueEntry dialogueEntry)
        {
            var audioTag = Field.LookupValue(dialogueEntry.fields, DialogueSystemFields.AudioTag);
            if (!string.IsNullOrEmpty(audioTag)) text = $"[{audioTag}] {text}";
            return text;
        }

    }

}

#endif
