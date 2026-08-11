// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    // Add to dialogue UI.
    public class RuntimeAIConversationSettings : MonoBehaviour
    {

        public static RuntimeAIConversationSettings Instance { get; private set; }

        [Header("OpenAI Settings")]
        [Tooltip("We strongly recommend only providing the OpenAI API key here for internal testing. Do not distribute builds with plain text keys. Retrieve the key from a server, or assign Encrypted API Key as a better alternative to plain text.")]
        [SerializeField] private string apiKey;
        [Tooltip("If you can't retrieve the OpenAI API key from a secure server, you can assign an Encrypted Key here for better security than plain text.")]
        [SerializeField] private EncryptedKey encryptedApiKey;
        [Tooltip("Password to decrypt the OpenAI API key.")]
        [SerializeField] private string encryptedApiKeyPassword;
        [SerializeField] private TextModelName textModelName = Model.DefaultTextModelName;
        [Tooltip("Fine-Tuned Model is only applicable if Text Model Name is set to Fine-Tuned.")]
        [SerializeField] private string fineTunedModelName;
        [Tooltip("Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic. We generally recommend altering this or top_p but not both.")]
        [SerializeField] private float temperature = 0.4f;
        [Tooltip("An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered. We generally recommend altering this or temperature but not both.")]
        [SerializeField][Range(0, 1)] private float top_p = 1f;
        [Tooltip("The total length of input tokens and generated tokens is limited by the model's context length.")]
        [SerializeField] private int maxTokens = 4097;
        [Tooltip("Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.")]
        [SerializeField][Range(-2, 2)] private float frequencyPenalty = 0;
        [Tooltip("Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.")]
        [SerializeField][Range(-2, 2)] private float presencePenalty = 0;
        [Tooltip("Use OpenAI for text to speech voice generation.")]
        [SerializeField] private bool useOpenAIVoiceGeneration = true;
        [Tooltip("If using OpenAI for text to speech, use this model.")]
        [ShowIf("useOpenAIVoiceGeneration")][SerializeField] private TTSModel textToSpeechModel = TTSModel.GPT_4o_mini_tts;
        [Tooltip("To transcribe microphone input, use this model.")]
        [SerializeField] private STTModel speechToTextModel = STTModel.GPT_4o_mini_transcribe;

        [Header("ElevenLabs Settings")]
        [Tooltip("If you want to generate text to speech, set your ElevenLabs API key here for internal testing. Use an Encrypted Key or retrieve the key from a secure server for release builds.")]
        [SerializeField] private string elevenLabsApiKey;
        [Tooltip("If you can't retrieve the ElevenLabs API key from a secure server, you can assign an Encrypted Key here for better security than plain text.")]
        [SerializeField] private EncryptedKey encryptedElevenLabsKey;
        [Tooltip("Password to decrypt the ElevenLabs API key.")]
        [SerializeField] private string encryptedElevenLabsKeyPassword;
        [SerializeField] private ElevenLabs.ElevenLabs.Models elevenLabsModel;

        [Header("UI Elements")]
        [Tooltip("Runtime conversations show this icon while waiting for OpenAI responses.")]
        [SerializeField] private GameObject waitingIcon;
        [Tooltip("Input field used for freeform text input conversations.")]
        [SerializeField] private StandardUIInputField chatInputField;
        [Tooltip("This button ends freeform text input conversations.")]
        [SerializeField] private Button goodbyeButton;
        [Space]
        [Tooltip("Shows generated images when playing CYOA (Choose Your Own Adventure) conversations.")]
        [SerializeField] private Image image;
        [SerializeField] private ImageSizes imageSize = ImageSizes.Auto;
        [Space]
        [Tooltip("If you want to allow speech input, this button starts recording user's speech.")]
        [SerializeField] private Button recordButton;
        [Tooltip("This button stops recording and submits it to OpenAI for text transcription.")]
        [SerializeField] private Button submitRecordingButton;
        [Tooltip("Hold record button down to record, release to submit without having to click Submit Recording Button.")]
        [SerializeField] private bool holdRecordButtonToRecord = false;
        [Tooltip("Listen for speech input without having to click any buttons.")]
        [SerializeField] private bool buttonlessRecording = false;
        [Tooltip("Use this Audio Source as intermediary to perform buttonless recording.\nTip: Assign mixer group with volume set to -80 dB.\nTip: If in the Dialogue Manager's hierarchy, add another Audio Source to the main Dialogue Manager GameObject for general Audio()/AudioWait() commands.")]
        [ShowIf("buttonlessRecording")][SerializeField] private AudioSource recordingAudioSource;
        [Tooltip("Record this many samples to detect speech in buttonless recording. 64 is usually sufficient, but you can increase if needed.")]
        [ShowIf("buttonlessRecording")][SerializeField] private int numRecordingSamples = 64;
        [Tooltip("Detect speech if buttonless recording level is above this value.")]
        [ShowIf("buttonlessRecording")][SerializeField] private float minRecordingLevel = 0.001f;
        [Tooltip("Detect that player has stopped speaking when this much silence (in seconds) has passed.")]
        [ShowIf("buttonlessRecording")][SerializeField] private float minStoppedSpeakingDuration = 2;
        [Tooltip("If assigned, runtime conversations will show this icon while recording speech.")]
        [SerializeField] private GameObject recordingIcon;
        [Tooltip("Optional dropdown for microphone input selection.")]
        [SerializeField] private UIDropdownField microphoneDevicesDropdown;
        [Tooltip("If recording audio, record up to this many seconds.")]
        [SerializeField] private int maxRecordingLength = 10;
        [Tooltip("If recording audio, record at this frequency.")]
        [SerializeField] private int recordingFrequency = 44100;
        [Tooltip("If enabled, microphone will start listening immediately (during NPC speech). If disabled, microphone waits until NPC finishes talking.")]
        [ShowIf("buttonlessRecording")][SerializeField] private bool listenDuringNPCSpeech = false;

        public bool ListenDuringNPCSpeech { get => listenDuringNPCSpeech; set => listenDuringNPCSpeech = value; }

        public GameObject WaitingIcon { get => waitingIcon; set => waitingIcon = value; }
        public StandardUIInputField ChatInputField { get => chatInputField; set => chatInputField = value; }
        public Button GoodbyeButton { get => goodbyeButton; set => goodbyeButton = value; }
        public Image Image { get => image; set => image = value; }
        public Button RecordButton { get => recordButton; set => recordButton = value; }
        public Button SubmitRecordingButton { get => submitRecordingButton; set => submitRecordingButton = value; }
        public bool HoldRecordButtonToRecord { get => holdRecordButtonToRecord; set => holdRecordButtonToRecord = value; }

        public bool ButtonlessRecording { get => buttonlessRecording; set => buttonlessRecording = value; }
        public AudioSource RecordingAudioSource { get => recordingAudioSource; set => recordingAudioSource = value; }
        public int NumRecordingSamples { get => numRecordingSamples; set => numRecordingSamples = value; }
        public float MinRecordingLevel { get => minRecordingLevel; set => minRecordingLevel = value; }
        public float MinStoppedSpeakingDuration { get => minStoppedSpeakingDuration; set => minStoppedSpeakingDuration = value; }

        public GameObject RecordingIcon { get => recordingIcon; set => recordingIcon = value; }
        public UIDropdownField MicrophoneDevicesDropdown { get => microphoneDevicesDropdown; set => microphoneDevicesDropdown = value; }
        public int MaxRecordingLength { get => maxRecordingLength; set => maxRecordingLength = value; }
        public int RecordingFrequency { get => recordingFrequency; set => recordingFrequency = value; }

        public string APIKey { get => apiKey; set => apiKey = value; }
        public Model Model => GetModel();
        public bool IsChatModel => Model.ModelType == ModelType.Chat;
        public bool IsReasoningModel => Model.ModelType == ModelType.Reasoning;
        public TTSModel TTSModel { get => textToSpeechModel; set => textToSpeechModel = value; }
        public STTModel STTModel { get => speechToTextModel; set => speechToTextModel = value; }
        public float Temperature { get => temperature; set => temperature = value; }
        public float TopP { get => top_p; set => top_p = value; }
        public int MaxTokens { get => maxTokens; set => maxTokens = value; }
        public float FrequencyPenalty { get => frequencyPenalty; set => frequencyPenalty = value; }
        public float PresencePenalty { get => presencePenalty; set => presencePenalty = value; }
        public bool UseOpenAIVoiceGeneration { get => useOpenAIVoiceGeneration; set => useOpenAIVoiceGeneration = value; }
        public string ElevenLabsApiKey { get => elevenLabsApiKey; set => elevenLabsApiKey = value; }
        public ElevenLabs.ElevenLabs.Models ElevenLabsModel { get => elevenLabsModel; set => elevenLabsModel = value; }
        public string ElevenLabsModelId => ElevenLabs.ElevenLabs.GetModelId(elevenLabsModel);

        /// <summary>
        /// To use an alternate TTS voice service instead of OpenAI or ElevenLabs, assign to this property.
        /// </summary>
        public IVoiceService VoiceService { get; set; } = null;

        /// <summary>
        /// To use an alternate STT transcription service instead of OpenAI, assign to this property.
        /// </summary>
        public ITranscriptionService TranscriptionService { get; set; } = null;

        public string ImageSizeString
        {
            get
            {
                switch (imageSize)
                {
                    default:
                    case ImageSizes.Auto: return "auto";
                    case ImageSizes.Size1024x1024: return "1024x1024";
                    case ImageSizes.Size1024x1536: return "1024x1536";
                    case ImageSizes.Size1536x1024: return "1536x1024";
                }
            }
        }

        public int ImageWidth
        {
            get
            {
                switch (imageSize)
                {
                    default:
                    case ImageSizes.Auto: return 1024;
                    case ImageSizes.Size1024x1024: return 1024;
                    case ImageSizes.Size1024x1536: return 1024;
                    case ImageSizes.Size1536x1024: return 1536;
                }
            }
        }

        public int ImageHeight
        {
            get
            {
                switch (imageSize)
                {
                    default:
                    case ImageSizes.Auto: return 1024;
                    case ImageSizes.Size1024x1024: return 1024;
                    case ImageSizes.Size1024x1536: return 1536;
                    case ImageSizes.Size1536x1024: return 1024;
                }
            }
        }

        private Model fineTunedModel = null;

        protected virtual Model GetModel()
        {
            if (textModelName == TextModelName.FineTune)
            {
                if (fineTunedModel == null)
                {
                    fineTunedModel = new Model(fineTunedModelName, ModelType.Chat, MaxTokens);
                }
                return fineTunedModel;
            }
            else
            {
                return OpenAI.NameToModel(textModelName);
            }
        }

        protected virtual void Awake()
        {
            Instance = this;
            HideExtraUIElements();
            CheckEncryptedKeys();
        }

        protected virtual void Start()
        {
            var dialogueUI = GetComponent<StandardDialogueUI>();
            if (dialogueUI == null) return;
            if (dialogueUI.conversationUIElements.mainPanel != null)
            {
                dialogueUI.conversationUIElements.mainPanel.onClose.AddListener(HideExtraUIElements);
            }
        }

        protected virtual void CheckEncryptedKeys()
        {
            CheckEncryptedAPIKey();
            CheckEncryptedElevenLabsKey();
        }

        protected virtual void CheckEncryptedAPIKey()
        {
            if (encryptedApiKey != null)
            {
                if (EncryptionUtility.TryDecrypt(encryptedApiKey.data, encryptedApiKeyPassword, out var key))
                {
                    apiKey = key;
                }
                else
                {
                    Debug.LogError("Unable to decrypt OpenAI API key.", encryptedApiKey);
                }
            }
        }

        protected virtual void CheckEncryptedElevenLabsKey()
        {
            if (encryptedElevenLabsKey != null)
            {
                if (EncryptionUtility.TryDecrypt(encryptedElevenLabsKey.data, encryptedElevenLabsKeyPassword, out var key))
                {
                    elevenLabsApiKey = key;
                }
                else
                {
                    Debug.LogError("Unable to decrypt ElevenLabs API key.", encryptedElevenLabsKey);
                }
            }
        }

        protected virtual void HideExtraUIElements()
        {
            if (waitingIcon != null) waitingIcon.SetActive(false);
            if (goodbyeButton != null) goodbyeButton.gameObject.SetActive(false);
        }

    }
}

#endif

