// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem.DialogueEditor;
#if USE_ADDRESSABLES
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
#endif
#if USE_DEEPVOICE
using PixelCrushers.DialogueSystem.OpenAIAddon.DeepVoice;
#endif
using PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    public enum AudioSequencerCommands { None, AudioWait, SALSA, Other }

    /// <summary>
    /// Panel to generate voiceover for a dialogue entry using an ElevenLabs voice actor.
    /// </summary>
    public class GenerateVoicePanel : TextToSpeechPanel
    {

        private string textForVoiceover;
        private string audioTag;
        private Actor actor;
        private string voiceName;
        private string voiceID;
        private bool isLocalizationField;
        private TTSModel ttsModel = TTSModel.GPT_4o_mini_tts;

        private float stability = 0.5f;
        private bool use_speaker_boost = false;
        private float similarity_boost = 0.5f;
        private float style = 0;

#if USE_DEEPVOICE
        private float variability = 0.3f;
        private float clarity = 0.75f;
#endif

        private static string lastFilename;
        private static AudioSequencerCommands sequencerCommand = AudioSequencerCommands.None;
        private static string otherSequencerCommand = "";

        private static GUIContent HeadingLabel = new GUIContent("Generate Voiceover");
        public static GUIContent SequencerCommandLabel = new GUIContent("Sequencer Command", "Add sequencer command to dialogue entry's Sequence field.");
        public static GUIContent ElevenLabsModelLabel = new GUIContent("ElevenLabs Model", "Set in Welcome Window.");
        public static GUIContent AudioTagLabel = new GUIContent("Audio Tag (v3 only)", "Currently used only by ElevenLabs v3 model.");
        public static GUIContent StabilityLabel = new GUIContent("Stability", "Determines how stable the voice is and the randomness between each generation. Lower values introduce broader emotional range for the voice. Higher values can result in a monotonous voice with limited emotion.");
        public static GUIContent UseSpeakerBoostLabel = new GUIContent("Speaker Boost", "This setting boosts the similarity to the original speaker. Using this setting requires a slightly higher computational load, which in turn increases latency.");
        public static GUIContent SimilarityBoostLabel  = new GUIContent("Similarity", "Determines how closely the AI should adhere to the original voice when attempting to replicate it.");
        public static GUIContent StyleLabel = new GUIContent("Style", "Determines the style exaggeration of the voice. This setting attempts to amplify the style of the original speaker. It does consume additional computational resources and might increase latency if set to anything other than 0.");

        protected override string Operation => "Generate Voiceover";

        public GenerateVoicePanel(string apiKey, DialogueDatabase database,
            Asset asset, DialogueEntry entry, Field field)
            : base(apiKey, database, asset, entry, field)
        {
            textForVoiceover = (field != null && field.type == FieldType.Localization) ? field.value
                : (entry != null) ? entry.DialogueText : string.Empty;
            audioTag = string.Empty;
            actor = (entry != null) ? database.GetActor(entry.ActorID) : null;
            if (entry != null)
            {
                audioTag = Field.LookupValue(entry.fields, DialogueSystemFields.AudioTag);
                textForVoiceover = AITextUtility.PrependAudioTag(textForVoiceover, entry);
            }
            voiceName = (actor != null) ? actor.LookupValue(DialogueSystemFields.Voice) : null;
            voiceID = (actor != null) ? actor.LookupValue(DialogueSystemFields.VoiceID) : null;
            lastFilename = EditorPrefs.GetString(DialogueSystemOpenAIWindow.ElevenLabsLastFilename);
            isLocalizationField = field != null && field.type == FieldType.Localization;
        }

        ~GenerateVoicePanel()
        {
            DestroyAudioClip();
        }

        public static bool CanUseAudioTag(DialogueDatabase database, DialogueEntry entry)
        {
            var actor = (entry != null) ? database.GetActor(entry.ActorID) : null;
            return CanUseAudioTag(actor);
        }

        public static bool CanUseAudioTag(Actor actor)
        {
            if (actor == null) return false;
            var voiceID = actor.LookupValue(DialogueSystemFields.VoiceID);
            if (voiceID == "OpenAI")
            {
                return false;
            }

#if USE_DEEPVOICE
            if (voiceID.StartsWith("DeepVoice"))
            {
                return false;
            }
#endif

            var model = (ElevenLabs.ElevenLabs.Models)EditorPrefs.GetInt(DialogueSystemOpenAIWindow.ElevenLabsModel, 0);
            return model == ElevenLabs.ElevenLabs.Models.Eleven_v3;
        }

        public override void Draw()
        {
            base.Draw();
            DrawHeading(HeadingLabel, "Generate voiceover for this line using the actor's selected voice.");
            if (actor == null)
            {
                EditorGUILayout.LabelField("Assign an actor to this dialogue entry first.");
            }
            else if (string.IsNullOrEmpty(voiceID))
            {
                EditorGUILayout.LabelField("Select a voice for this actor first.");
                if (GUILayout.Button("Select Voice"))
                {
                    DialogueSystemOpenAIWindow.Open(AIRequestType.SelectVoice, database, actor, null, null);
                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                DrawGenerateButton();
                DrawPreviewButton();
                DrawAcceptButton();
            }
            DrawStatus();
        }

        private void DrawGenerateButton()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("Dialogue Text");
            EditorGUILayout.TextArea(textForVoiceover);
            if (!string.IsNullOrEmpty(audioTag)) EditorGUILayout.TextField(AudioTagLabel, audioTag);
            EditorGUILayout.TextField("Voice", voiceName);
            EditorGUI.EndDisabledGroup();

#if USE_DEEPVOICE
            variability = EditorGUILayout.Slider("Variability", variability, 0, 1);
            clarity = EditorGUILayout.Slider("Clarity", clarity, 0, 1);
#endif

#if USE_DEEPVOICE
            if (voiceID.StartsWith("DeepVoice"))
            {
                // Don't show ElevenLabs info if using DeepVoice.
            }
            else
#endif
            if (voiceID == "OpenAI")
            {
                ttsModel = (TTSModel)EditorGUILayout.EnumPopup("Model", ttsModel);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(ElevenLabsModelLabel, modelId);
                EditorGUI.EndDisabledGroup();
                stability = EditorGUILayout.FloatField(StabilityLabel, stability);
                use_speaker_boost = EditorGUILayout.Toggle(UseSpeakerBoostLabel, use_speaker_boost);
                similarity_boost = EditorGUILayout.FloatField(SimilarityBoostLabel, similarity_boost);
                style = EditorGUILayout.FloatField(StyleLabel, style);
            }

            EditorGUI.BeginDisabledGroup(isLocalizationField);
            sequencerCommand = (AudioSequencerCommands)EditorGUILayout.EnumPopup(SequencerCommandLabel, sequencerCommand);
            if (sequencerCommand == AudioSequencerCommands.Other)
            {
                otherSequencerCommand = EditorGUILayout.TextField("Command", otherSequencerCommand);
            }
            var needToInputSequencerCommand = sequencerCommand == AudioSequencerCommands.Other && string.IsNullOrEmpty(otherSequencerCommand);
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(IsAwaitingReply || needToInputSequencerCommand);
            if (GUILayout.Button("Generate"))
            {
                GenerateAudio();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawPreviewButton()
        {
            EditorGUI.BeginDisabledGroup(audioClip == null || IsAwaitingReply);
            if (GUILayout.Button("Preview"))
            {
                EditorAudioUtility.PlayAudioClip(audioClip);
            }
            EditorGUI.EndDisabledGroup();
        }

        private void GenerateAudio()
        {
            DestroyAudioClip();
            IsAwaitingReply = true;
            ProgressText = $"Generating voiceover: {textForVoiceover}";

            if (voiceID == "OpenAI")
            {
                var openAIVoice = System.Enum.Parse<Voices>(voiceName);
                OpenAI.SubmitVoiceGenerationAsync(openAIKey, ttsModel, openAIVoice,
                    VoiceOutputFormat.MP3, 1, textForVoiceover, OnReceivedTextToSpeech);
                return;
            }

#if USE_DEEPVOICE
            if (voiceID.StartsWith("DeepVoice"))
            {
                Debug.Log($"Generating DeepVoice voiceover for: {textForVoiceover}");
                DeepVoiceAPI.GetTextToSpeech(DeepVoiceAPI.GetDeepVoiceModel(voiceID), voiceName, 
                    textForVoiceover, variability, clarity, OnReceivedTextToSpeech);
                return;
            }
#endif
            Debug.Log($"Generating voiceover for: {textForVoiceover}");
            ElevenLabs.ElevenLabs.GetTextToSpeech(apiKey, modelId, voiceName, voiceID,
                stability, similarity_boost, use_speaker_boost, style,
                textForVoiceover, OnReceivedTextToSpeech);
        }

        private void OnReceivedTextToSpeech(AudioClip audioClip)
        {
            IsAwaitingReply = false;
            if (audioClip == null) return;
            this.audioClip = audioClip;
            Debug.Log($"Playing: {textForVoiceover}");
            EditorAudioUtility.PlayAudioClip(audioClip);
            Repaint();
        }

        private void DrawAcceptButton()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(audioClip == null || IsAwaitingReply);
            if (GUILayout.Button("Accept"))
            {
                var filename = SaveAudioClip();
                if (!string.IsNullOrEmpty(filename))
                {
                    if (!isLocalizationField)
                    {
                        AddSelectedSequencerCommand(sequencerCommand, System.IO.Path.GetFileNameWithoutExtension(filename), database, entry);
                    }
                    SaveDatabaseChanges();
                    RefreshEditor();
                    CloseWindow();
                }
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Cancel"))
            {
                CloseWindow();
            }
            EditorGUILayout.EndHorizontal();
        }

        public static void AddSelectedSequencerCommand(AudioSequencerCommands sequencerCommand, string entrytag, DialogueDatabase database, DialogueEntry entry)
        {
            switch (sequencerCommand)
            {
                case AudioSequencerCommands.AudioWait:
                case AudioSequencerCommands.SALSA:
                    AddSequencerCommand(sequencerCommand.ToString(), entrytag, database, entry);
                    break;
                case AudioSequencerCommands.Other:
                    AddSequencerCommand(otherSequencerCommand, entrytag, database, entry);
                    break;
            }
        }

        public static void AddSequencerCommand(string command, string entrytag, DialogueDatabase database, DialogueEntry entry)
        {
            var sequence = entry.Sequence;
            if (!(string.IsNullOrEmpty(sequence) || sequence.EndsWith(";")))
            {
                sequence += ";\n";
            }
            sequence += $"{command}({entrytag})";
            entry.Sequence = sequence;
            PrefabUtility.RecordPrefabInstancePropertyModifications(database);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssetIfDirty(database);
            AssetDatabase.Refresh();
            Debug.Log($"Sequence: {entry.Sequence}");
        }

        private string SaveAudioClip()
        {
            if (audioClip == null) return string.Empty;
            var conversation = database.GetConversation(entry.conversationID);
            var defaultName = database.GetEntrytag(conversation, entry, GetEntrytagFormat());
            if (isLocalizationField) defaultName += "_" + field.title;
            var path = EditorAudioUtility.GetPathForSaveInFile(lastFilename);
#if USE_ADDRESSABLES
            var title = "Save Audio Clip";
#else
            var title = "Save Audio Clip in Resources Folder";
#endif
            var filename = EditorUtility.SaveFilePanelInProject(title, defaultName, "wav", "", path);
            if (string.IsNullOrEmpty(filename)) return string.Empty;
            lastFilename = filename;
            EditorPrefs.SetString(DialogueSystemOpenAIWindow.ElevenLabsLastFilename, lastFilename);
            // Remove extra Assets/ & extension:
            var fullPath = Application.dataPath + "/" + filename.Substring("Assets/".Length); 
            Debug.Log($"Saving audio clip to {filename}");
            SavWav.Save(fullPath, audioClip);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(filename);

#if USE_ADDRESSABLES
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings != null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<AudioClip>(filename);
                string assetPath = AssetDatabase.GetAssetPath(asset);
                string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
                settings.CreateAssetReference(assetGUID);
                AddressableAssetEntry addressableEntry = settings.FindAssetEntry(assetGUID);
                if (addressableEntry != null)
                {
                    addressableEntry.address = System.IO.Path.GetFileNameWithoutExtension(filename);
                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                    AssetDatabase.SaveAssets();
                }
            }
#endif

            return filename;
        }

        public static EntrytagFormat GetEntrytagFormat()
        {
            var dialogueManager = GameObjectUtility.FindFirstObjectByType<DialogueSystemController>();
            if (dialogueManager != null)
            {
                return dialogueManager.displaySettings.cameraSettings.entrytagFormat;
            }
            else
            {
                return EntrytagFormat.ActorName_ConversationID_EntryID;
            }
        }

        private void SaveDatabaseChanges()
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DialogueEditorWindow.instance?.Repaint();
        }

        protected override void RefreshEditor()
        {
            Undo.RecordObject(database, Operation);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DialogueEditorWindow.instance?.Reset();
            DialogueEditorWindow.OpenDialogueEntry(database, entry.conversationID, entry.id);
            DialogueEditorWindow.instance?.Repaint();
            GUIUtility.ExitGUI();
        }

    }
}

#endif
