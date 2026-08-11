#if USE_OPENAI

using UnityEngine;
using PixelCrushers.DialogueSystem.OpenAIAddon;
using PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    using OpenAI = PixelCrushers.DialogueSystem.OpenAIAddon.OpenAI;

    /// <summary>
    /// Sequencer command: GenerateVoice()
    /// Generates and plays voice audio through OpenAI or ElevenLabs.
    /// If unable to generate audio, delays for the value of {{end}}.
    /// </summary>
    public class SequencerCommandGenerateVoice : SequencerCommand
    {
        private AudioClip audioClip = null;

        protected virtual void Awake()
        {
            var speakerInfo = DialogueManager.currentConversationState.subtitle.speakerInfo;
            var dialogueText = DialogueManager.currentConversationState.subtitle.formattedText.text;
            var actor = DialogueManager.masterDatabase.GetActor(speakerInfo.id);
            var voiceName = (actor != null) ? actor.LookupValue(DialogueSystemFields.Voice) : null;
            var voiceID = (actor != null) ? actor.LookupValue(DialogueSystemFields.VoiceID) : null;

            // Stop after delay (unless invoke is cancelled by successful ElevenLabs call):
            InvokeStopAfterDelay();

            if (RuntimeAIConversationSettings.Instance == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"Dialogue System: Sequencer: GenerateVoice(): No Runtime AI Conversation Settings component in scene. Not playing audio.");
            }
            else if (string.IsNullOrEmpty(voiceName) || string.IsNullOrEmpty(voiceID))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"Dialogue System: Sequencer: GenerateVoice(): No voice has been selected for {speakerInfo.nameInDatabase}. Not playing audio.");
            }
            else if (voiceID == "OpenAI")
            {
                CancelInvoke();
                var openAIVoice = System.Enum.Parse<Voices>(voiceName);
                OpenAI.SubmitVoiceGenerationAsync(RuntimeAIConversationSettings.Instance.APIKey, 
                    TTSModel.TTSModel1HD, openAIVoice,
                    VoiceOutputFormat.MP3, 1, dialogueText, OnReceivedOpenAITextToSpeech);
            }
            else if (string.IsNullOrEmpty(RuntimeAIConversationSettings.Instance.ElevenLabsApiKey))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"Dialogue System: Sequencer: GenerateVoice(): ElevenLabs API key is not set on Runtime AI Conversation Settings component. Not playing audio.");
            }
            else
            {
                CancelInvoke();
                ElevenLabs.GetTextToSpeech(RuntimeAIConversationSettings.Instance.ElevenLabsApiKey,
                    RuntimeAIConversationSettings.Instance.ElevenLabsModelId,
                    voiceName, voiceID, 0, 0, dialogueText, OnReceivedTextToSpeech);
            }
        }

        protected virtual void InvokeStopAfterDelay()
        {
            Invoke(nameof(Stop), ConversationView.GetDefaultSubtitleDurationInSeconds(DialogueManager.currentConversationState.subtitle.formattedText.text));
        }

        protected virtual void OnReceivedOpenAITextToSpeech(AudioClip audioClip, byte[] bytes)
        {
            OnReceivedTextToSpeech(audioClip);
        }

        protected virtual void OnReceivedTextToSpeech(AudioClip audioClip)
        {
            this.audioClip = audioClip;
            if (audioClip == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"Dialogue System: Sequencer: GenerateVoice(): ElevenLabs did not return a valid audio clip. Not playing audio.");
                InvokeStopAfterDelay();
            }
            else
            {
                var audioSource = SequencerTools.GetAudioSource(speaker);
                if (audioSource == null)
                {
                    if (DialogueDebug.logWarnings) Debug.LogWarning($"Dialogue System: Sequencer: GenerateVoice(): Unable to get or create an AudioSource on {speaker}. Not playing audio.");
                    InvokeStopAfterDelay();
                }
                else if (isPlaying) // Player may have skipped ahead.
                {
                    audioSource.clip = audioClip;
                    audioSource.Play();
                    Invoke(nameof(Stop), audioClip.length);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            Destroy(audioClip);
            audioClip = null;
        }

    }
}

#endif
