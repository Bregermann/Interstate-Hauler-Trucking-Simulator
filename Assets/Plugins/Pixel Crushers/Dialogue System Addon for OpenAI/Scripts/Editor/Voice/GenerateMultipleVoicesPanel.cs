// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Collections.Generic;
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

    /// <summary>
    /// Panel to generate voiceover for a dialogue entry using an ElevenLabs voice actor.
    /// </summary>
    public class GenerateMultipleVoicesPanel : TextToSpeechPanel
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
        private List<DialogueEntry> nodes;
        private int currentNodeIndex;
        private string folder;
        private bool cancel;

#if USE_DEEPVOICE
        private float variability = 0.3f;
        private float clarity = 0.75f;
#endif

        private static string lastFilename;
        private static AudioSequencerCommands sequencerCommand = AudioSequencerCommands.None;
        private static string otherSequencerCommand = "";

        private static GUIContent HeadingLabel = new GUIContent("Generate Voiceover");
        public static GUIContent SequencerCommandLabel = new GUIContent("Sequencer Command", "Add sequencer command to dialogue entries' Sequence fields.");
        public static GUIContent ElevenLabsModelLabel = new GUIContent("ElevenLabs Model", "Set in Welcome Window.");
        public static GUIContent AudioTagLabel = new GUIContent("Audio Tag (v3 only)", "Currently used only by ElevenLabs v3 model.");
        public static GUIContent StabilityLabel = new GUIContent("Stability", "Determines how stable the voice is and the randomness between each generation. Lower values introduce broader emotional range for the voice. Higher values can result in a monotonous voice with limited emotion.");
        public static GUIContent UseSpeakerBoostLabel = new GUIContent("Speaker Boost", "This setting boosts the similarity to the original speaker. Using this setting requires a slightly higher computational load, which in turn increases latency.");
        public static GUIContent SimilarityBoostLabel = new GUIContent("Similarity", "Determines how closely the AI should adhere to the original voice when attempting to replicate it.");
        public static GUIContent StyleLabel = new GUIContent("Style", "Determines the style exaggeration of the voice. This setting attempts to amplify the style of the original speaker. It does consume additional computational resources and might increase latency if set to anything other than 0.");
        public static GUIContent GenerateAndSaveLabel = new GUIContent("Generate & Save", "Generate all audio clips and save them in a specified folder.");

        protected override string Operation => "Generate Voiceover";

        public GenerateMultipleVoicesPanel(string apiKey, DialogueDatabase database,
            Asset asset)
            : base(apiKey, database, asset, null, null)
        {
            var selection = (DialogueEditorWindow.instance != null) ? DialogueEditorWindow.instance.GetMultinodeSelection() : null;
            if (selection == null || selection.nodes == null || selection.nodes.Count == 0)
            {
                nodes = null;
                entry = null;
            }
            else
            {
                nodes = new List<DialogueEntry>(selection.nodes);
                entry = nodes[0];
            }
            textForVoiceover = (entry != null) ? entry.DialogueText : string.Empty;
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

        ~GenerateMultipleVoicesPanel()
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
            DrawHeading(HeadingLabel, "Generate voiceover for the selected dialogue entries using the actors' selected voices.");
            DrawGenerateButton();
            DrawStatus();
        }

        protected override void DrawStatus()
        {
            base.DrawStatus();
            if (IsAwaitingReply)
            {
                if (GUILayout.Button("Cancel"))
                {
                    cancel = true;
                }
            }
        }

        private void DrawGenerateButton()
        {
            EditorGUI.BeginDisabledGroup(true);
            if (nodes == null || nodes.Count == 0)
            {
                EditorGUILayout.LabelField("Dialogue Editor is closed or no nodes selected.");
            }
            else
            {
                EditorGUILayout.IntField("# Entries", nodes.Count);
            }
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
            if (GUILayout.Button(GenerateAndSaveLabel))
            {
                GenerateAudio();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void GenerateAudio()
        {
            folder = EditorUtility.SaveFolderPanel("Generate Audio Clips", folder, "");
            if (string.IsNullOrEmpty(folder)) return;
            DestroyAudioClip();
            currentNodeIndex = 0;
            GenerateAudioForNode();
        }

        private void GenerateAudioForNode()
        {
            IsAwaitingReply = true;
            entry = nodes[currentNodeIndex];
            textForVoiceover = entry.DialogueText;
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
            if (cancel) return;
            if (audioClip == null)
            {
                Debug.LogWarning($"Stopping. No audio clip generated for entry {entry.id}: '{entry.DialogueText}'");
                return;
            }
            this.audioClip = audioClip;
            Debug.Log($"Playing: {textForVoiceover}");
            EditorAudioUtility.PlayAudioClip(audioClip);
            var filename = SaveAudioClip();
            AddSelectedSequencerCommand(sequencerCommand, System.IO.Path.GetFileNameWithoutExtension(filename), database, entry);
            SaveDatabaseChanges();
            RefreshEditor();
            Repaint();
            currentNodeIndex++;
            if (currentNodeIndex < nodes.Count)
            {
                GenerateAudioForNode();
            }
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
            var filename = database.GetEntrytag(conversation, entry, GetEntrytagFormat());
            // if (isLocalizationField) filename += "_" + field.title;
            var path = $"{folder}/{filename}";
            lastFilename = filename;
            EditorPrefs.SetString(DialogueSystemOpenAIWindow.ElevenLabsLastFilename, lastFilename);
            Debug.Log($"Saving audio clip to {filename}");
            SavWav.Save(path, audioClip);
            AssetDatabase.Refresh();
            var localPath = "Assets" + path.Substring(Application.dataPath.Length) + ".wav";
            AssetDatabase.ImportAsset(localPath);

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
        }

    }
}

#endif
