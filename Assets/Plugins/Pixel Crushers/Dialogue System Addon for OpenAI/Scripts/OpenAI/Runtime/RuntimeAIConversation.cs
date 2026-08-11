// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    public enum RuntimeAIConversationMode { FreeformTextInput, ResponseMenu, Bark, CYOA }

    public class RuntimeAIConversation : MonoBehaviour
    {

        [ActorPopup(true)]
        [SerializeField] private string actor;

        [ActorPopup(false)]
        [SerializeField] private string conversant;

        [Tooltip("Topic of conversation and background info. May contain [var=variable] and [lua(code)] tags. Can also include 'Also involve <extra-actors>.'")]
        [TextArea]
        [SerializeField] private string topic;

        [Tooltip("Setting used for image generation. May contain [var=variable] and [lua(code)] tags. Can also include 'Also involve <extra-actors>.'")]
        [TextArea]
        [FormerlySerializedAs("setting")]
        [SerializeField] private string imageSetting;

        [Tooltip("Include Description fields of Locations in dialogue database in prompt.")]
        [SerializeField] private bool includeLocationDescriptions;

        [Tooltip("Optional guidance to send to OpenAI.")]
        [SerializeField] private string assistantPrompt = "Keep lines of dialogue succinct.";

        [Tooltip("Type of runtime conversation to play.")]
        [SerializeField] private RuntimeAIConversationMode mode;

        [Tooltip("Label to display in text input prompt.")]
        [SerializeField] private string textInputPrompt = "";

        [Tooltip("Max characters allowed in text input.")]
        [SerializeField] private int maxTextInputLength = 100;

        [Tooltip("If nonzero, summarize conversation up to this point to reduce token count.")]
        [SerializeField] private int summarizeAfterMessageCount = 0;

        [Tooltip("Optional. Only valid when Mode is Freeform Text Input. Questions to answer at the end of the freeform conversation.")]
        [SerializeField] private List<RuntimeAIResultQuestion> freeformConversationResultQuestions = new List<RuntimeAIResultQuestion>();

        private DialogueDatabase database = null;
        private List<ChatMessage> messages;
        private bool approachedMaxTokens;
        private int approximateTokenCount;

        protected bool isWaitingForInput = false;

        protected const int ApproximateCharactersPerToken = 4;
        protected const int TokenBufferAmount = 100; // Stop conversation before tokens get this amount close to max tokens.

        public string Actor { get => actor; set => actor = value; }
        public string Conversant { get => conversant; set => conversant = value; }
        public string Topic { get => topic; set => topic = value; }
        public string Setting { get => imageSetting; set => imageSetting = value; }
        public string AssistantPrompt { get => assistantPrompt; set => assistantPrompt = value; }
        public RuntimeAIConversationMode Mode { get => mode; set => mode = value; }
        public string TextInputPrompt { get => textInputPrompt; set => textInputPrompt = value; }
        public int MaxTextInputLength { get => maxTextInputLength; set => maxTextInputLength = value; }

        protected List<ChatMessage> Messages { get => messages; set => messages = value; }
        protected int ApproximateTokenCount { get => approximateTokenCount; set => approximateTokenCount = value; }
        protected bool ApproachedMaxTokens { get => approachedMaxTokens; set => approachedMaxTokens = value; }
        protected List<RuntimeAIResultQuestion> FreeformConversationResultQuestions => freeformConversationResultQuestions;
        protected RuntimeAIConversationSettings Settings { get; set; }
        protected StandardDialogueUI DialogueUI { get; set; }
        protected DialogueDatabase Database { get => database; set => database = value; }
        protected CharacterInfo SpeakerInfo { get; set; }
        protected CharacterInfo ListenerInfo { get; set; }

        protected virtual int ConversationID => 9999;
        protected virtual string ConversationTitle => "AI Conversation";

        protected bool IsOllamaModel => Settings.Model.IsOllamaModel;
        protected string ServiceName => Settings.Model.ServiceName;

        protected IVoiceService VoiceService => Settings.VoiceService;
        protected bool IsElevenLabsEnabled => !string.IsNullOrEmpty(Settings.ElevenLabsApiKey);
        protected bool IsBuiltInVoiceEnabled => Settings.UseOpenAIVoiceGeneration || IsElevenLabsEnabled;
        protected bool IsVoiceEnabled => IsBuiltInVoiceEnabled || VoiceService != null;
        protected virtual string VoiceServiceName => (VoiceService != null) ? VoiceService.Name
            : IsBuiltInVoiceEnabled ? "ElevenLabs"
            : "No Voice Service";

        protected ITranscriptionService TranscriptionService => Settings.TranscriptionService;

        protected string CurrentLineText { get; set; }
        protected AudioClip CurrentAudioClip { get; set; }
        protected Sprite CurrentSprite { get; set; }
        protected bool EndConversationOnReceivedLine { get; set; } = false;

        protected AcceptedTextDelegate acceptedTextHandler = null;
        protected AudioClip recordedClip;
        protected int currentQuestionIndex;

        // For buttonless recording:
        protected float currentAverageVolume;
        protected float[] clipSampleData = null;
        protected string microphoneDeviceName;
        protected Coroutine monitorRecordingCoroutine = null;
        protected float timeLastSpoke;

        /// <summary>
        /// Invoked when a freeform text input conversation or CYOA story has ended.
        /// </summary>
        public System.Action freeformTextInputConversationEnded = null;

        #region Entrypoints

        protected virtual IEnumerator StartRecordingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (Settings.ButtonlessRecording && Settings.RecordingAudioSource != null)
            {
                StartRecording();
            }
        }
        protected virtual IEnumerator StartStoryTextInputAfterAudio(float audioLength)
        {
            yield return new WaitForSeconds(audioLength);
            StartTextInput(OnAcceptedStoryTextInput);
        }

        protected virtual bool NeedsOpenAIKey()
        {
            return !(OpenAI.IsApiKeyValid(Settings.APIKey) || IsOllamaModel);
        }

        protected virtual bool IsUsingChatModel()
        {
            return Settings.Model.ModelType == ModelType.Chat || Settings.Model.ModelType == ModelType.Reasoning;
        }

        public virtual void Play()
        {
            if (!RetrieveSettings())
            {
                Debug.LogError("Dialogue System: Scene is missing a RuntimeAIConversationSettings component.");
            }
            else if (NeedsOpenAIKey())
            {
                Debug.LogError("Dialogue System: You must first set an OpenAI API key on the RuntimeAIConversationSettings component.", Settings);
            }
            else
            {
                switch (mode)
                {
                    case RuntimeAIConversationMode.FreeformTextInput:
                        StartFreeformTextInputConversation();
                        break;
                    case RuntimeAIConversationMode.ResponseMenu:
                        StartResponseMenuConversation();
                        break;
                    case RuntimeAIConversationMode.Bark:
                        StartBark();
                        break;
                    case RuntimeAIConversationMode.CYOA:
                        StartCYOA();
                        break;
                }
            }
        }

        protected virtual void OnConversationEnd(Transform actor)
        {
            if (Settings != null && Settings.GoodbyeButton != null) Settings.GoodbyeButton.onClick.RemoveListener(OnClickedGoodbye);
            ResetTextInputButtons();
            freeformTextInputConversationEnded?.Invoke();
            if (mode == RuntimeAIConversationMode.FreeformTextInput) RecordFreeformConversationResults();
            DestroyTemporaryDatabase();
        }

        protected virtual void DestroyTemporaryDatabase()
        {
            if (database == null) return;
            DialogueManager.RemoveDatabase(database);
            Destroy(database);
            database = null;
        }

        #endregion

        #region Text Input Shared Methods

        protected virtual void SetupTextInputButtons()
        {
            if (Settings.GoodbyeButton != null)
            {
                Settings.GoodbyeButton.onClick.AddListener(OnClickedGoodbye);
            }
            if (Settings.MicrophoneDevicesDropdown != null)
            {
                Settings.MicrophoneDevicesDropdown.ClearOptions();
#if !UNITY_WEBGL
                foreach (var device in Microphone.devices)
                {
                    Settings.MicrophoneDevicesDropdown.AddOption(device);
                }
#endif
            }
            if (Settings.RecordButton != null)
            {
                if (Settings.HoldRecordButtonToRecord)
                {
                    // If hold to record is set, add event trigger entries to handle pointer down/up events:
                    var eventTrigger = Settings.RecordButton.GetComponent<UnityEngine.EventSystems.EventTrigger>() ??
                        Settings.RecordButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    var downEntry = eventTrigger.triggers.Find(x => x.eventID == UnityEngine.EventSystems.EventTriggerType.PointerDown);
                    if (downEntry == null)
                    {
                        downEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
                        downEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
                        eventTrigger.triggers.Add(downEntry);
                    }
                    downEntry.callback.AddListener((data) => { StartRecording(); });

                    var upEntry = eventTrigger.triggers.Find(x => x.eventID == UnityEngine.EventSystems.EventTriggerType.PointerUp);
                    if (upEntry == null)
                    {
                        upEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
                        upEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
                        eventTrigger.triggers.Add(upEntry);
                    }
                    upEntry.callback.AddListener((data) => { StopRecordingAndSubmit(); });
                }
                else
                {
                    // Otherwise regular click starts recording:
                    Settings.RecordButton.onClick.AddListener(StartRecording);
                }
            }
            if (Settings.SubmitRecordingButton != null)
            {
                Settings.SubmitRecordingButton.onClick.AddListener(StopRecordingAndSubmit);
            }
            SetRecordingButtons(false);
        }

        protected virtual void ResetTextInputButtons()
        {
            isWaitingForInput = false;
            if (monitorRecordingCoroutine != null)
            {
                StopCoroutine(monitorRecordingCoroutine);
                monitorRecordingCoroutine = null;
            }
#if !UNITY_WEBGL
            if (Microphone.IsRecording(microphoneDeviceName))
            {
                Microphone.End(microphoneDeviceName);
            }
#endif
            if (Settings.RecordingAudioSource != null)
            {
                Settings.RecordingAudioSource.Stop();
            }

            if (Settings.GoodbyeButton != null)
            {
                Settings.GoodbyeButton.onClick.RemoveListener(OnClickedGoodbye);
            }
            if (Settings.RecordButton != null)
            {
                Settings.RecordButton.onClick.RemoveListener(StartRecording);
            }
            if (Settings.SubmitRecordingButton != null)
            {
                Settings.SubmitRecordingButton.onClick.RemoveListener(StopRecordingAndSubmit);
            }
            SetRecordingButtons(false);
        }

        protected virtual void SetRecordingButtons(bool value)
        {
            if (Settings.MicrophoneDevicesDropdown != null) Settings.MicrophoneDevicesDropdown.enabled = value;
            if (Settings.RecordButton != null) Settings.RecordButton.enabled = value;
            if (Settings.SubmitRecordingButton != null) Settings.SubmitRecordingButton.enabled = value;
            if (value == true && Settings.ButtonlessRecording && Settings.RecordingAudioSource != null)
            {
                StartRecording();
            }
        }

        protected virtual void StartTextInput(AcceptedTextDelegate onAcceptedTextInput)
        {
            Settings.ChatInputField.StartTextInput(TextInputPrompt, "", MaxTextInputLength, onAcceptedTextInput);
            SetRecordingButtons(true);
            acceptedTextHandler = onAcceptedTextInput;
        }



        protected virtual void StartRecording()
        {
#if UNITY_WEBGL
    recordedClip = null;
#else
            var microphoneDeviceName = (Microphone.devices.Length >= 1)
                ? (Settings.MicrophoneDevicesDropdown != null && 0 <= Settings.MicrophoneDevicesDropdown.value && Settings.MicrophoneDevicesDropdown.value < Microphone.devices.Length)
                    ? Microphone.devices[Settings.MicrophoneDevicesDropdown.value]
                    : Microphone.devices[0]
                : "";
            SetRecordingIcon(true);
            recordedClip = Microphone.Start(microphoneDeviceName, false, Settings.MaxRecordingLength, Settings.RecordingFrequency);
            if (Settings.ButtonlessRecording && Settings.RecordingAudioSource != null)
            {
                isWaitingForInput = true;
                monitorRecordingCoroutine = StartCoroutine(MonitorRecording());
            }
#endif
        }

        protected virtual void SetRecordingIcon(bool value)
        {
            if (Settings.RecordingIcon == null) return;
            Settings.RecordingIcon.SetActive(value);
        }

        protected virtual IEnumerator MonitorRecording()
        {
#if UNITY_WEBGL
            if (DialogueDebug.LogInfo) Debug.Log("Dialogue System: Microphone not supported in WebGL. Not monitoring microphone input.", this);
            yield break;
#else
            if (DialogueDebug.LogInfo) Debug.Log("Dialogue System: Monitoring microphone input...", this);
            while (!(Microphone.GetPosition(null) > 0))
            {
                yield return null;
            }
            if (clipSampleData == null) clipSampleData = new float[Settings.NumRecordingSamples];
            Settings.RecordingAudioSource.clip = recordedClip;
            Settings.RecordingAudioSource.Play();

            timeLastSpoke = float.MaxValue;
            bool isSpeaking = false;
            bool hasSpokenAtAll = false;
            float recordingStartTime = Time.time;

            while (isWaitingForInput)
            {
                // Check if we've reached max recording length
                if (Time.time - recordingStartTime >= Settings.MaxRecordingLength - 0.5f)
                {
                    if (!hasSpokenAtAll)
                    {
                        // User hasn't spoken yet, restart recording
                        if (DialogueDebug.LogInfo) Debug.Log("Dialogue System: Max recording length reached without input. Restarting microphone...", this);
                        RestartRecording();
                        yield break;
                    }
                }

                Settings.RecordingAudioSource.GetSpectrumData(clipSampleData, 0, FFTWindow.Rectangular);
                currentAverageVolume = Average(clipSampleData);

                if (currentAverageVolume > Settings.MinRecordingLevel)
                {
                    if (!isSpeaking && DialogueDebug.LogInfo) Debug.Log("Dialogue System: Player has started speaking.", this);
                    isSpeaking = true;
                    hasSpokenAtAll = true;
                    timeLastSpoke = Time.time;
                }
                else if (isSpeaking && Time.time > (timeLastSpoke + Settings.MinStoppedSpeakingDuration))
                {
                    if (DialogueDebug.LogInfo) Debug.Log("Dialogue System: Player has stopped speaking.", this);
                    isSpeaking = false;
                    isWaitingForInput = false;
                    StopRecordingAndSubmit();
                }
                yield return null;
            }
#endif
        }

        protected virtual void RestartRecording()
        {
            if (monitorRecordingCoroutine != null)
            {
                StopCoroutine(monitorRecordingCoroutine);
                monitorRecordingCoroutine = null;
            }
#if !UNITY_WEBGL
            Microphone.End(microphoneDeviceName);
#endif
            if (Settings.RecordingAudioSource != null)
            {
                Settings.RecordingAudioSource.Stop();
            }

            // Start fresh recording
            StartRecording();
        }

        // Return the average volume of audio sample data.
        private float Average(float[] clipSampleData)
        {
            float total = 0;
            foreach (var x in clipSampleData)
            {
                total += x;
            }
            return total / clipSampleData.Length;
        }

        protected virtual void StopRecordingAndSubmit()
        {
            isWaitingForInput = false;
            if (monitorRecordingCoroutine != null)
            {
                if (DialogueDebug.LogInfo) Debug.Log("Dialogue System: Stopping monitoring microphone input.", this);
                StopCoroutine(monitorRecordingCoroutine);
                monitorRecordingCoroutine = null;
            }
            SetRecordingIcon(false);
#if !UNITY_WEBGL
            Microphone.End(microphoneDeviceName);
#endif
            if (Settings.RecordingAudioSource != null)
            {
                Settings.RecordingAudioSource.Stop();
            }
            if (recordedClip == null) return;
            SetWaitingIcon(true);
            SetRecordingButtons(false);
            TranscribeRecordedAudio();
        }

        protected virtual void TranscribeRecordedAudio()
        {
            if (TranscriptionService != null)
            {
                TranscriptionService.TranscribeSpeech(recordedClip, OnReceivedTranscription);
            }
            else
            {
                if (DialogueDebug.LogInfo) Debug.Log($"Dialogue System: Sending to OpenAI: Transcribe recorded audio.");
                OpenAI.SubmitAudioTranscriptionAsync(Settings.APIKey, recordedClip, string.Empty,
                    Settings.STTModel, AudioResponseFormat.Json, 0,
                    string.Empty, OnReceivedTranscription);
            }
        }

        protected virtual void OnReceivedTranscription(string s)
        {
            SetWaitingIcon(false);
            if (DialogueDebug.LogInfo) Debug.Log($"Dialogue System: Received transcription: [{s}]");
            Settings.ChatInputField.inputField.text = s;
            acceptedTextHandler(s);
        }

        protected virtual void SetGoodbyeButton(bool value)
        {
            Settings.GoodbyeButton.gameObject.SetActive(value);
        }

        protected virtual void OnClickedGoodbye()
        {
            DialogueManager.instance.isAlternateConversationActive = false;
            SetGoodbyeButton(false);
            Settings.ChatInputField.CancelTextInput();
            DialogueUI.Close();
            OnConversationEnd(CharacterInfo.GetRegisteredActorTransform(actor));
            var actorTransform = CharacterInfo.GetRegisteredActorTransform(actor);
            if (actorTransform == null) actorTransform = DialogueManager.instance.transform;
            InformParticipants<Transform>(DialogueSystemMessages.OnConversationEnd, actorTransform);
        }

        protected virtual void InformParticipants<T>(string message, T parameter)
        {
            DialogueManager.instance.BroadcastMessage(message, parameter, SendMessageOptions.DontRequireReceiver);
            if (SpeakerInfo != null && SpeakerInfo.transform != null && SpeakerInfo.transform != DialogueManager.instance.transform)
            {
                SpeakerInfo.transform.BroadcastMessage(message, parameter, SendMessageOptions.DontRequireReceiver);
            }
            if (ListenerInfo != null && ListenerInfo.transform != null && ListenerInfo.transform != SpeakerInfo.transform && ListenerInfo.transform != DialogueManager.instance.transform)
            {
                ListenerInfo.transform.BroadcastMessage(message, parameter, SendMessageOptions.DontRequireReceiver);
            }
        }

#endregion

        #region Voice Shared Methods

        private void InvokeTextToSpeech(string voiceName, string voiceID,
            string text, Action<AudioClip> callback)
        {
            if (VoiceService != null)
            {
                VoiceService.GenerateTextToSpeech(voiceName, voiceID, text, callback);
            }
            else if (voiceID == "OpenAI")
            {
                var openAIVoice = System.Enum.Parse<Voices>(voiceName);
                OpenAI.SubmitVoiceGenerationAsync(Settings.APIKey, TTSModel.TTSModel1HD, openAIVoice,
                    VoiceOutputFormat.MP3, 1, text, callback);

            }
            else
            {
                ElevenLabs.ElevenLabs.GetTextToSpeech(Settings.ElevenLabsApiKey,
                    Settings.ElevenLabsModelId, voiceName, voiceID, 0, 0,
                    text, callback);
            }
        }

        #endregion

        #region UI

        protected virtual bool RetrieveSettings()
        {
            if (Settings != null) return true;
            DialogueUI = DialogueManager.standardDialogueUI;
            if (DialogueUI == null) return false;
            Settings = DialogueUI.GetComponent<RuntimeAIConversationSettings>();
            if (Settings == null) return false;
            return true;
        }

        protected virtual void SetWaitingIcon(bool value)
        {
            if (!RetrieveSettings() || Settings.WaitingIcon == null) return;
            Settings.WaitingIcon.SetActive(value);
        }

        #endregion

        #region Freeform Text Input Conversation

        protected virtual void StartFreeformTextInputConversation()
        {
            if (!(Settings.IsChatModel || Settings.IsReasoningModel))
            {
                Debug.LogWarning("Dialogue System: Freeform text input conversations require a reasoning or chat model. Change the RuntimeAIConversationSettings component's Model dropdown.");
                return;
            }
            DialogueManager.instance.isAlternateConversationActive = true;
            SetupTextInputButtons();
            SetGoodbyeButton(false);
            DialogueUI.Open();
            SetWaitingIcon(true);

            // All subtitle lines are spoken by conversant (NPC):
            SpeakerInfo = GetCharacterInfo(conversant);
            ListenerInfo = GetCharacterInfo(actor);

            var background = GetLocationDescriptions() +
                AIConversationUtility.GetActorDescriptions(DialogueManager.masterDatabase, actor, conversant, "");
            background = FormattedText.ParseCode(background);
            var prompt = $"Write an initial line of dialogue in a fictional context spoken by {conversant} to {actor} {topic}. Reply only in first person.";
            prompt = FormattedText.ParseCode(prompt);
            if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Sending to {ServiceName}: {prompt}", this);

            messages = new List<ChatMessage>
            {
                new ChatMessage("user", background),
                new ChatMessage("user", prompt)
            };
            approximateTokenCount = prompt.Length / ApproximateCharactersPerToken;
            approachedMaxTokens = false;
            EndConversationOnReceivedLine = false;
            if (!string.IsNullOrEmpty(assistantPrompt))
            {
                messages.Add(new ChatMessage("assistant", assistantPrompt));
                approximateTokenCount += assistantPrompt.Length / ApproximateCharactersPerToken;
            }
            SubmitChatAsync(Settings.APIKey, Settings.Model,
                Settings.Temperature, Settings.TopP,
                Settings.FrequencyPenalty, Settings.PresencePenalty,
                Settings.MaxTokens,
                messages, OnReceivedLine);

            // Send OnConversationStart message:
            var actorTransform = CharacterInfo.GetRegisteredActorTransform(actor);
            if (actorTransform == null) actorTransform = DialogueManager.instance.transform;
            InformParticipants<Transform>(DialogueSystemMessages.OnConversationStart, actorTransform);
        }

        protected virtual void SubmitChatAsync(string apiKey, Model model,
            float temperature, float top_p,
            float frequency_penalty, float presence_penalty,
            int maxTokens,
            List<ChatMessage> messages, Action<string> callback)
        {
            if (IsOllamaModel)
            {
                var prompt = string.Empty;
                messages.ForEach(message => prompt += $"{message.content} ");
                OllamaAPI.SubmitCompletionAsync(Settings.Model.Name,
                    temperature, top_p,
                    frequency_penalty, presence_penalty,
                    maxTokens, prompt, callback);
            }
            else
            {
                OpenAI.SubmitChatAsync(Settings.APIKey, Settings.Model,
                    Settings.Temperature, Settings.TopP,
                    Settings.FrequencyPenalty, Settings.PresencePenalty,
                    Settings.MaxTokens,
                    messages, callback);
            }

        }

        protected virtual string GetLocationDescriptions()
        {
            return includeLocationDescriptions ? AIConversationUtility.GetLocationDescriptions(DialogueManager.masterDatabase) : string.Empty;
        }

        protected virtual CharacterInfo GetCharacterInfo(string actorName)
        {
            var actor = DialogueManager.masterDatabase.GetActor(actorName);
            var actorID = (actor != null) ? actor.id : 2; // 2 is usually NPC.
            var nameInDatabase = (actor != null) ? actor.Name : actorName;
            var actorTransform = CharacterInfo.GetRegisteredActorTransform(actorName);
            var characterInfo = new CharacterInfo(actorID, nameInDatabase, actorTransform, CharacterType.NPC, actor.GetPortraitSprite());
            return characterInfo;
        }

        protected virtual void OnReceivedLine(string line)
        {
            if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Received from {ServiceName}: {line}", this);

            if (string.IsNullOrEmpty(line))
            {
                OnClickedGoodbye();
            }
            else
            {
                approximateTokenCount += line.Length / ApproximateCharactersPerToken;
                line = RemoveConversant(line);
                messages.Add(new ChatMessage("user", $"{conversant} says: {line}"));
                var needToSummarize = summarizeAfterMessageCount > 0 &&
                    messages.Count > summarizeAfterMessageCount;
                if (needToSummarize)
                {
                    SummarizeConversation(line);
                }
                else
                {
                    DisplayLine(line);
                }
            }
        }

        protected virtual void SummarizeConversation(string line)
        {
            SetWaitingIcon(true);
            CurrentLineText = line;
            messages.Add(new ChatMessage("assistant", "Summarize the preceding content."));
            SubmitChatAsync(Settings.APIKey, Settings.Model,
                Settings.Temperature, Settings.TopP,
                Settings.FrequencyPenalty, Settings.PresencePenalty,
                Settings.MaxTokens,
                messages, OnReceivedSummary);
        }

        protected virtual void OnReceivedSummary(string summary)
        {
            SetWaitingIcon(false);
            var newPrompt = $"Emulate dialogue in a fictional context spoken by {conversant} to {actor} {topic}. Reply only in first person. {summary}";
            if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Summarizing conversation and refreshing prompt: {newPrompt}");
            messages.Clear();
            messages.Add(new ChatMessage("user", newPrompt));
            DisplayLine(CurrentLineText);
        }

        protected virtual void DisplayLine(string line)
        {
            if (IsVoiceEnabled)
            {
                GenerateVoice(line);
            }
            else
            {
                ShowSubtitle(line, null);
            }
        }

        protected virtual string RemoveConversant(string line)
        {
            return AITextUtility.RemoveSpeaker(conversant, line);
        }

        protected virtual void GenerateVoice(string line)
        {
            CurrentLineText = line;
            var actor = DialogueManager.masterDatabase.GetActor(SpeakerInfo.id);
            var voiceName = (actor != null) ? actor.LookupValue(DialogueSystemFields.Voice) : null;
            var voiceID = (actor != null) ? actor.LookupValue(DialogueSystemFields.VoiceID) : null;
            if (string.IsNullOrEmpty(voiceName) && string.IsNullOrEmpty(voiceID))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"Dialogue System: No {VoiceServiceName} voice has been selected for {SpeakerInfo.nameInDatabase}. Not playing audio.");
                ShowSubtitle(line, null);
            }
            else
            {
                InvokeTextToSpeech(voiceName, voiceID, line, OnReceivedTextToSpeech);
            }
        }

        protected virtual void OnReceivedTextToSpeech(AudioClip audioClip)
        {
            ShowSubtitle(CurrentLineText, audioClip);
            PlayAudio(SpeakerInfo.transform, audioClip);
        }

        protected virtual void PlayAudio(Transform speaker, AudioClip audioClip)
        {
            var audioSource = SequencerTools.GetAudioSource(speaker);
            if (audioSource == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"Dialogue System: Unable to get or create an AudioSource on {speaker}. Not playing audio.");
            }
            else
            {
                Destroy(audioSource.clip);
                audioSource.clip = audioClip;
                audioSource.Play();
            }
        }

        protected virtual void ShowSubtitle(string line, AudioClip audioClip)
        {
            SetWaitingIcon(false);
            Destroy(CurrentAudioClip);
            CurrentAudioClip = audioClip;
            var sequence = (audioClip != null) ? $"Delay({audioClip.length}); {{default}}" : "";
            var dialogueText = line;
            var formattedText = FormattedText.Parse(dialogueText);
            var subtitle = new Subtitle(SpeakerInfo, ListenerInfo, formattedText, sequence, "", null);
            InformParticipants<Subtitle>(DialogueSystemMessages.OnConversationLineEarly, subtitle);
            InformParticipants<Subtitle>(DialogueSystemMessages.OnConversationLine, subtitle);
            DialogueUI.ShowSubtitle(subtitle);
            SetGoodbyeButton(true);

            if (EndConversationOnReceivedLine)
            {
                var duration = (audioClip != null) ? audioClip.length : ConversationView.GetDefaultSubtitleDurationInSeconds(line);
                Invoke(nameof(OnClickedGoodbye), duration);
            }
            else if (approachedMaxTokens)
            {
                Settings.ChatInputField.Close();
            }
            else
            {
                // Delay starting text input until after audio finishes (if setting is disabled)
                if (audioClip != null && Settings.ButtonlessRecording && !Settings.ListenDuringNPCSpeech)
                {
                    StartCoroutine(StartTextInputAfterAudio(audioClip.length));
                }
                else
                {
                    StartTextInput(OnAcceptedTextInput);
                }
            }
        }


        protected virtual IEnumerator StartTextInputAfterAudio(float audioLength)
        {
            yield return new WaitForSeconds(audioLength);
            StartTextInput(OnAcceptedTextInput);
        }


        protected virtual void OnAcceptedTextInput(string text)
        {
            SetWaitingIcon(true);
            approximateTokenCount += text.Length / ApproximateCharactersPerToken;
            if (approximateTokenCount < Settings.MaxTokens - TokenBufferAmount)
            {
                if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Sending to {ServiceName}: Reply to {text}", this);
                messages.Add(new ChatMessage("user", $"Reply to {actor}'s reply: \"{text}\". Reply only in first person."));
            }
            else
            {
                approachedMaxTokens = true;
                if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Sending to {ServiceName}: Wrap up conversation. (Approaching max tokens.)", this);
                messages.Add(new ChatMessage("user", $"Say a final line of dialogue. Reply only in first person."));
            }
            SubmitChatAsync(Settings.APIKey, Settings.Model,
                Settings.Temperature, Settings.TopP,
                Settings.FrequencyPenalty, Settings.PresencePenalty,
                Settings.MaxTokens,
                messages, OnReceivedLine);
        }

        #region Record Freeform Conversation Results

        protected virtual void RecordFreeformConversationResults()
        {
            if (freeformConversationResultQuestions == null ||
                freeformConversationResultQuestions.Count == 0) return;

            // Replace prompt since we're going to prompt at the end of messages instead:
            var promptReplacementText = $"This is a conversation spoken by {conversant} to {actor} {topic}.";
            messages[1].content = promptReplacementText;
            messages.Add(new ChatMessage("user", "")); // Spot for our question.

            currentQuestionIndex = 0;
            AskNextEndOfConversationQuestion();
        }

        protected virtual void AskNextEndOfConversationQuestion()
        {
            // Check if we're done:
            if (currentQuestionIndex >= FreeformConversationResultQuestions.Count) return;

            var question = FreeformConversationResultQuestions[currentQuestionIndex];
            if (question == null) return;
            if (string.IsNullOrEmpty(question.Question))
            {
                Debug.LogWarning("A freeform conversation result question is blank. Not asking OpenAI.");
            }
            else
            {
                var variable = FindVariable(question.RecordInVariable);
                if (variable == null)
                {
                    Debug.LogWarning($"The specified variable doesn't exist to record the answer to freeform conversation result question '{question.Question}'");
                }
                else
                {
                    var prompt = $"{question.Question}. ";
                    messages[messages.Count - 1].content = prompt;
                    if (DialogueDebug.LogInfo) Debug.Log($"Asking at end of conversation: {prompt}");
                    SubmitChatAsync(Settings.APIKey, Settings.Model,
                        Settings.Temperature, Settings.TopP,
                        Settings.FrequencyPenalty, Settings.PresencePenalty,
                        Settings.MaxTokens,
                        messages, OnReceivedEndOfConversationAnswer);
                }
            }
        }

        private Variable FindVariable(string variableName)
        {
            return DialogueManager.masterDatabase.variables.Find(x => x.Name == variableName);
        }

        private string GetVariableTypePrompt(FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.Boolean:
                    return "Reply only true or false.";
                case FieldType.Number:
                    return "Reply only with a numeric value.";
                case FieldType.Actor:
                    return "Reply only with the person's name.";
                case FieldType.Item:
                    return "Reply only with the name of the item.";
                case FieldType.Location:
                default:
                    return string.Empty;
            }
        }

        private void OnReceivedEndOfConversationAnswer(string answer)
        {
            var question = FreeformConversationResultQuestions[currentQuestionIndex];
            var variable = FindVariable(question.RecordInVariable);
            if (variable == null)
            {
                Debug.LogWarning($"Received answer \"{answer}\" to \"{question.Question}\" but can't access variable {question.RecordInVariable} - not recording.");
                return;
            }
            if (DialogueDebug.LogInfo) Debug.Log($"Recording answer \"{answer}\" to \"{question.Question}\" in variable {question.RecordInVariable}.");
            switch (variable.Type)
            {
                case FieldType.Boolean:
                    DialogueLua.SetVariable(question.RecordInVariable, GetAnswerBooleanValue(answer));
                    break;
                case FieldType.Number:
                    DialogueLua.SetVariable(question.RecordInVariable, GetAnswerFloatValue(answer));
                    break;
                case FieldType.Actor:
                    DialogueLua.SetVariable(variable.Name, GetAnswerActorValue(answer));
                    break;
                case FieldType.Item:
                    DialogueLua.SetVariable(variable.Name, GetAnswerItemValue(answer));
                    break;
                case FieldType.Location:
                    DialogueLua.SetVariable(variable.Name, GetAnswerLocationValue(answer));
                    break;
                default:
                    DialogueLua.SetVariable(variable.Name, answer);
                    break;
            }
            currentQuestionIndex++;
            AskNextEndOfConversationQuestion();
        }

        private bool GetAnswerBooleanValue(string answer)
        {
            answer = AITextUtility.RemoveSurroundingQuotes(answer);
            if (answer.StartsWith("true", StringComparison.OrdinalIgnoreCase) ||
                answer.StartsWith("yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (answer.StartsWith("false", StringComparison.OrdinalIgnoreCase) ||
                answer.StartsWith("no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else
            {
                Debug.LogWarning($"Don't know how to interpret \"{answer}\" as a Boolean value.");
                return false;
            }
        }

        private float GetAnswerFloatValue(string answer)
        {
            answer = AITextUtility.RemoveSurroundingQuotes(answer);
            if (float.TryParse(answer, out var f))
            {
                return f;
            }
            else
            {
                Debug.LogWarning($"Don't know how to interpret \"{answer}\" as a numeric value.");
                return 0;
            }
        }

        private float GetAnswerActorValue(string answer)
        {
            answer = AITextUtility.RemoveSurroundingQuotes(answer);
            var actor = DialogueManager.masterDatabase.GetActor(answer);
            if (actor == null)
            {
                Debug.LogWarning($"Unable to associate answer \"{answer}\" with an actor in the database.");
                return -1;
            }
            else
            {
                return actor.id;
            }
        }

        private float GetAnswerItemValue(string answer)
        {
            answer = AITextUtility.RemoveSurroundingQuotes(answer);
            var item = DialogueManager.masterDatabase.GetItem(answer);
            if (item == null)
            {
                Debug.LogWarning($"Unable to associate answer \"{answer}\" with an item/quest in the database.");
                return -1;
            }
            else
            {
                return item.id;
            }
        }

        private float GetAnswerLocationValue(string answer)
        {
            answer = AITextUtility.RemoveSurroundingQuotes(answer);
            var location = DialogueManager.masterDatabase.GetLocation(answer);
            if (location == null)
            {
                Debug.LogWarning($"Unable to associate answer \"{answer}\" with a location in the database.");
                return -1;
            }
            else
            {
                return location.id;
            }
        }

        #endregion

        #endregion

        #region Response Menu Conversation

        protected virtual void StartResponseMenuConversation()
        {
            DialogueUI.Open();
            SetWaitingIcon(true);

            var prompt = GetLocationDescriptions() +
                AIConversationUtility.GetActorDescriptions(DialogueManager.masterDatabase, actor, conversant, "") +
                $"Write a dialogue between {conversant} and {actor} {topic}.";
            prompt = FormattedText.ParseCode(prompt);
            if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Sending to {ServiceName}: {prompt}", this);

            if (IsUsingChatModel())
            {
                messages = new List<ChatMessage>
                {
                    new ChatMessage("user", prompt)
                };
                if (!string.IsNullOrEmpty(assistantPrompt)) messages.Add(new ChatMessage("assistant", assistantPrompt));
                SubmitChatAsync(Settings.APIKey, Settings.Model,
                    Settings.Temperature, Settings.TopP,
                    Settings.FrequencyPenalty, Settings.PresencePenalty,
                    Settings.MaxTokens,
                    messages, OnReceivedConversation);
            }
            else
            {
                OpenAI.SubmitCompletionAsync(Settings.APIKey, Settings.Model,
                    Settings.Temperature, Settings.TopP,
                    Settings.FrequencyPenalty, Settings.PresencePenalty,
                    Settings.MaxTokens,
                    prompt, OnReceivedConversation);
            }
        }

        protected virtual void OnReceivedConversation(string fullConversationText)
        {
            if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Received from OpenAI: {fullConversationText}", this);
            SetWaitingIcon(false);

            // Create database:
            var database = ScriptableObject.CreateInstance<DialogueDatabase>();

            // Sync actors:
            foreach (var actor in DialogueManager.masterDatabase.actors)
            {
                database.actors.Add(new Actor(actor));
            }

            // Create a template, which provides helper methods for creating database content:
            var template = Template.FromDefault();

            // Create conversation:
            var conversation = AIConversationUtility.CreateConversation(database, template, ConversationTitle,
                actor, conversant, fullConversationText, ConversationID);

            // Add audio:
            if (IsVoiceEnabled)
            {
                var command = (VoiceService != null) ? VoiceService.SequencerCommand
                    : IsBuiltInVoiceEnabled ? "GenerateVoice()"
                    : "None()";
                foreach (var entry in conversation.dialogueEntries)
                {
                    if (string.IsNullOrEmpty(entry.DialogueText)) continue;
                    entry.Sequence = command;
                }
            }

            // Add to database and start conversation:
            DialogueManager.AddDatabase(database);
            DialogueManager.StartConversation(ConversationTitle);
        }

        #endregion

        #region Bark

        protected virtual void StartBark()
        {
            SpeakerInfo = GetCharacterInfo(actor);
            ListenerInfo = GetCharacterInfo(conversant);

            var prompt = GetLocationDescriptions() +
                AIConversationUtility.GetActorDescriptions(DialogueManager.masterDatabase, actor, conversant, "") +
                (string.IsNullOrEmpty(conversant)
                    ? $"Write a bark spoken by {actor} {topic}."
                    : $"Write a bark spoken by {actor} to {conversant} {topic}.");
            prompt = FormattedText.ParseCode(prompt);
            if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Sending to {ServiceName}: {prompt}", this);

            if (IsUsingChatModel())
            {
                messages = new List<ChatMessage>
                {
                    new ChatMessage("user", prompt)
                };
                if (!string.IsNullOrEmpty(assistantPrompt)) messages.Add(new ChatMessage("assistant", assistantPrompt));
                SubmitChatAsync(Settings.APIKey, Settings.Model,
                    Settings.Temperature, Settings.TopP,
                    Settings.FrequencyPenalty, Settings.PresencePenalty,
                    Settings.MaxTokens,
                    messages, OnReceiveBark);
            }
            else
            {
                OpenAI.SubmitCompletionAsync(Settings.APIKey, Settings.Model,
                    Settings.Temperature, Settings.TopP,
                    Settings.FrequencyPenalty, Settings.PresencePenalty,
                    Settings.MaxTokens,
                    prompt, OnReceiveBark);
            }
        }

        protected virtual void OnReceiveBark(string fullBarkText)
        {
            if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Received from OpenAI: {fullBarkText}", this);
            var line = fullBarkText.Trim();

            if (IsVoiceEnabled)
            {
                CurrentLineText = line;
                var actor = DialogueManager.masterDatabase.GetActor(SpeakerInfo.id);
                var voiceName = (actor != null) ? actor.LookupValue(DialogueSystemFields.Voice) : null;
                var voiceID = (actor != null) ? actor.LookupValue(DialogueSystemFields.VoiceID) : null;
                if (string.IsNullOrEmpty(voiceName) && string.IsNullOrEmpty(voiceID))
                {
                    if (DialogueDebug.logWarnings) Debug.LogWarning($"Dialogue System: No {VoiceServiceName} voice has been selected for {SpeakerInfo.nameInDatabase}. Not playing audio for bark.");
                    DisplayBark(line);
                }
                else
                {
                    InvokeTextToSpeech(voiceName, voiceID, line, OnReceivedBarkTextToSpeech);
                }
            }
            else
            {
                DisplayBark(line);
            }
        }

        protected virtual void OnReceivedBarkTextToSpeech(AudioClip audioClip)
        {
            DisplayBark(CurrentLineText);
            PlayAudio(SpeakerInfo.transform, audioClip);
        }


        protected virtual void DisplayBark(string line)
        { 
            var barker = CharacterInfo.GetRegisteredActorTransform(actor) ?? transform;
            DialogueManager.BarkString(line, barker);
        }

        #endregion

        #region CYOA

        protected virtual void StartCYOA()
        {
            if (!(Settings.IsChatModel || Settings.IsReasoningModel))
            {
                Debug.LogWarning("Dialogue System: Freeform text input conversations require a chat model such as GPT-3.5. Change the RuntimeAIConversationSettings component's Model dropdown.");
                return;
            }
            DialogueManager.instance.isAlternateConversationActive = true;
            SetupTextInputButtons();
            SetGoodbyeButton(false);
            DialogueUI.Open();
            SetWaitingIcon(true);

            // All subtitle lines (story) are spoken by conversant (NPC):
            SpeakerInfo = GetCharacterInfo(conversant);
            ListenerInfo = GetCharacterInfo(actor);

            CurrentLineText = string.Empty;
            Destroy(CurrentAudioClip); CurrentAudioClip = null;
            Destroy(CurrentSprite); CurrentSprite = null;
            if (Settings.Image != null) Settings.Image.enabled = false;

            var prompt =
                "I want you to act as a text based adventure game. I will type commands and you will " +
                "reply with a description of what the player character sees. I want you to only reply " +
                "with the game output and nothing else. Do not write explanations. " +
                $"{topic}.";
            prompt = FormattedText.ParseCode(prompt);
            if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Sending to {ServiceName}: {prompt}", this);

            messages = new List<ChatMessage>
            {
                new ChatMessage("user", prompt)
            };
            approximateTokenCount = prompt.Length / ApproximateCharactersPerToken;
            approachedMaxTokens = false;
            if (!string.IsNullOrEmpty(assistantPrompt))
            {
                messages.Add(new ChatMessage("assistant", assistantPrompt));
                approximateTokenCount += assistantPrompt.Length / ApproximateCharactersPerToken;
            }
            SubmitChatAsync(Settings.APIKey, Settings.Model,
                Settings.Temperature, Settings.TopP,
                Settings.FrequencyPenalty, Settings.PresencePenalty,
                Settings.MaxTokens,
                messages, OnReceivedStoryDescription);

        }

        protected virtual void OnReceivedStoryDescription(string line)
        {
            if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Received from OpenAI: {line}", this);

            if (string.IsNullOrEmpty(line))
            {
                OnClickedGoodbye();
            }
            else
            {
                CurrentLineText = line;
                approximateTokenCount += line.Length / ApproximateCharactersPerToken;
                line = RemoveConversant(line);
                messages.Add(new ChatMessage("user", line));
                if (IsVoiceEnabled)
                {
                    GenerateStoryVoice(line);
                }
                else if (Settings.Image != null)
                {
                    GenerateStoryImage(line, null);
                }
                else
                {
                    ShowStoryDescription(line, null);
                }
            }
        }

        protected virtual void GenerateStoryVoice(string line)
        {
            var actor = DialogueManager.masterDatabase.GetActor(SpeakerInfo.id);
            var voiceName = (actor != null) ? actor.LookupValue(DialogueSystemFields.Voice) : null;
            var voiceID = (actor != null) ? actor.LookupValue(DialogueSystemFields.VoiceID) : null;
            if (string.IsNullOrEmpty(voiceName) || string.IsNullOrEmpty(voiceID))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"Dialogue System: No {VoiceServiceName} voice has been selected for {SpeakerInfo.nameInDatabase}. Not playing audio.");
                if (Settings.Image != null)
                {
                    GenerateStoryImage(line, null);
                }
                else
                {
                    ShowStoryDescription(line, null);
                }
            }
            else
            {
                InvokeTextToSpeech(voiceName, voiceID, line, OnReceivedStoryVoice);
            }
        }

        protected virtual void OnReceivedStoryVoice(AudioClip audioClip)
        {
            Destroy(CurrentAudioClip);
            CurrentAudioClip = audioClip;
            if (Settings.Image != null)
            {
                GenerateStoryImage(CurrentLineText, audioClip);
            }
            else
            {
                ShowStoryDescription(CurrentLineText, audioClip);
            }
        }

        protected virtual void GenerateStoryImage(string line, AudioClip audioClip)
        {
            var prompt = $"{FormattedText.ParseCode(Setting)} {line}";
            if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Sending to OpenAI: {prompt}", this);
            OpenAI.SubmitImageGenerationAsync(Settings.APIKey, 1, Settings.ImageSizeString, OpenAI.ResponseFormatB64JSON,
                user: "", prompt, ReceiveStoryImages);
        }

        private void ReceiveStoryImages(Vector2Int size, List<string> b64_jsons)
        {
            if (b64_jsons == null || b64_jsons.Count == 0 || string.IsNullOrEmpty(b64_jsons[0]))
            {
                Debug.LogWarning($"Received no images from OpenAI.");
                Settings.Image.enabled = false;
                ShowStoryDescription(CurrentLineText, null);
                return;
            }
            var b64_json = b64_jsons[0];
            byte[] bytes = System.Convert.FromBase64String(b64_json);
            var texture2D = new Texture2D(size.x, size.y);
            texture2D.LoadImage(bytes);
            var sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), (Vector2.one / 0.5f), 100);
            Destroy(CurrentSprite);
            CurrentSprite = sprite;
            Settings.Image.enabled = true;
            Settings.Image.sprite = sprite;
            ShowStoryDescription(CurrentLineText, CurrentAudioClip);
        }

        protected virtual void ShowStoryDescription(string line, AudioClip audioClip)
        {
            SetWaitingIcon(false);
            var sequence = (audioClip != null) ? $"Delay({audioClip.length}); {{default}}" : "";
            var dialogueText = line;
            var formattedText = FormattedText.Parse(dialogueText);
            var subtitle = new Subtitle(SpeakerInfo, ListenerInfo, formattedText, sequence, "", null);
            InformParticipants<Subtitle>(DialogueSystemMessages.OnConversationLine, subtitle);
            DialogueUI.ShowSubtitle(subtitle);
            PlayAudio(SpeakerInfo.transform, audioClip);
            SetGoodbyeButton(true);

            if (approachedMaxTokens)
            {
                Settings.ChatInputField.Close();
            }
            else
            {
                // Delay starting text input until after audio finishes (if setting is disabled)
                if (audioClip != null && Settings.ButtonlessRecording && !Settings.ListenDuringNPCSpeech)
                {
                    StartCoroutine(StartStoryTextInputAfterAudio(audioClip.length));
                }
                else
                {
                    StartTextInput(OnAcceptedStoryTextInput);
                }
            }
        }

        protected virtual void OnAcceptedStoryTextInput(string text)
        {
            SetWaitingIcon(true);
            approximateTokenCount += text.Length / ApproximateCharactersPerToken;
            if (approximateTokenCount < Settings.MaxTokens - TokenBufferAmount)
            {
                if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Sending to {ServiceName}: Reply to {text}", this);
                messages.Add(new ChatMessage("user", $"Reply to {actor}'s reply: {text}"));
            }
            else
            {
                approachedMaxTokens = true;
                if (DialogueDebug.logInfo) Debug.Log($"Dialogue System: Sending to {ServiceName}: Wrap up conversation. (Approaching max tokens.)", this);
                messages.Add(new ChatMessage("user", $"Say a final line of dialogue."));
            }
            SubmitChatAsync(Settings.APIKey, Settings.Model,
                Settings.Temperature, Settings.TopP,
                Settings.FrequencyPenalty, Settings.PresencePenalty,
                Settings.MaxTokens,
                messages, OnReceivedStoryDescription);
        }

        public virtual void EndStory()
        {
            OnClickedGoodbye();
        }

        #endregion

    }

}

#endif
