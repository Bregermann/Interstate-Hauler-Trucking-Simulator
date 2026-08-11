// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
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
    /// Panel to select an ElevenLabs voice actor.
    /// </summary>
    public class SelectVoicePanel : TextToSpeechPanel
    {

        // ElevenLabs built-in voices:
        private VoiceList voiceList;
        private string[] voiceNames = null;
        private int voiceIndex = -1;

        // ElevenLabs shared voice library:
        // (Code in place in case we decide to add the ability
        //  to add library voices to user's list in-editor.
        //  The ElevenLabs webapp UI is pretty nice, so we
        //  probably won't reinvent the wheel here.)
        private VoiceList sharedVoiceList;
        private string[] sharedVoiceNames = null;
        private int sharedVoiceIndex = -1;
        private VoiceCategory voiceCategory = VoiceCategory.Professional;
        private Gender gender = Gender.Any;
        private Age age = Age.Any;
        private string accent;
        private string search;

        // Preview:
        private string voiceName;
        private string previewText = "Hello, world.";
        private bool isGeneratingAllLines = false;

        // ElevenLabs settings:
        private float stability = 0;
        private bool use_speaker_boost = false;
        private float similarity_boost = 0;
        private float style = 0;

        // OpenAI voices:
        private Voices openAIVoice;
        private bool isOpenAIVoiceSelected;
        private TTSModel ttsModel = TTSModel.GPT_4o_mini_tts;

#if USE_OVERTONE
        private static string[] overtoneVoiceNames = null;
        private int overtoneVoiceIndex = -1;
#endif

#if USE_DEEPVOICE
        private DeepVoiceModel deepVoiceModel = DeepVoiceModel.DeepVoice_Mono;
        private int deepVoiceInt = 0;
        private float variability = 0.3f;
        private float clarity = 0.75f;
#endif

        private static GUIContent HeadingLabel = new GUIContent("Select Voice Actor");
        private static GUIContent GenerateAllLinesLabel = new GUIContent("Generate All Lines", "Generate voice audio for this actor for all dialogue entries that don't already have audio.");
        private static GUIContent ViewMyVoicesLabel = new GUIContent("View My Voices", "Open the MY VOICES panel in ElevenLab's web app.");
        private static GUIContent ViewVoiceLibraryLabel = new GUIContent("View Voice Library", "Open the VOICE LIBRARY panel in ElevenLab's web app. You can use this to add voices to your voice list.");
        private static GUIContent RefreshSharedVoicesLabel = new GUIContent("Refresh", "Refresh shared voices list using settings above.");
        private static GUIContent AccentLabel = new GUIContent("Accent", "Optional accent to search for.");
        private static GUIContent SearchLabel = new GUIContent("Search Term", "Optional search term to search for.");

        protected override string Operation => "Select Voice";
        private Actor Actor => (asset is Actor) ? asset as Actor : null;
        private bool IsElevenLabsVoiceSelected => voiceNames != null && (0 <= voiceIndex && voiceIndex < voiceNames.Length);
        private bool IsElevenLabsSharedVoiceSelected => sharedVoiceNames != null && (0 <= sharedVoiceIndex && sharedVoiceIndex < sharedVoiceNames.Length);

        private static AudioSequencerCommands sequencerCommand = AudioSequencerCommands.None;
        private static string otherSequencerCommand = "";
        private static string path = "Assets";

        private class VoiceGenerationRequest
        {
            public Conversation conversation;
            public DialogueEntry entry;
            public string text;
            public string description;
            public int entryQueueNum;

            public VoiceGenerationRequest(Conversation conversation, DialogueEntry entry, string text, string description, int entryQueueNum)
            {
                this.conversation = conversation;
                this.entry = entry;
                this.text = text;
                this.description = description;
                this.entryQueueNum = entryQueueNum;
            }
        }

        private Queue<VoiceGenerationRequest> voiceGenerationQueue = new Queue<VoiceGenerationRequest>();
        private int totalEntryQueueSize;
        private VoiceGenerationRequest currentJob = null;
        private EntrytagFormat entrytagFormat;

        public SelectVoicePanel(string apiKey, DialogueDatabase database,
            Asset asset, DialogueEntry entry, Field field)
            : base(apiKey, database, asset, entry, field)
        {
            RetrieveOpenAIVoice();
            RefreshElevenLabsBuiltInVoiceList();
            //RefreshElevenLabsSharedVoiceList();
#if USE_OVERTONE
            overtoneVoiceIndex = GetOvertoneVoiceIndex();
#endif
#if USE_DEEPVOICE
            deepVoiceModel = GetDeepVoiceModel();
            deepVoiceInt = GetDeepVoiceInt();
#endif
        }

        ~SelectVoicePanel()
        {
            DestroyAudioClip();
        }

        public override void Draw()
        {
            base.Draw();
            DrawHeading(HeadingLabel, "Select a voice actor for this character.");

            EditorGUI.BeginDisabledGroup(IsAwaitingReply && !isGeneratingAllLines);
            previewText = EditorGUILayout.TextField("Preview Text", previewText);
            DrawSequencerCommandSection();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            DrawOpenAISection();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            DrawElevenLabsSection();
#if USE_OVERTONE
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            DrawOvertoneSection();
#endif
#if USE_DEEPVOICE
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            DrawDeepVoiceSection();
#endif
            EditorGUI.EndDisabledGroup();
            DrawStatus();
            if (isGeneratingAllLines && GUILayout.Button("Cancel")) CancelGenerateAllLines();
        }

        #region OpenAI Voice

        private void RetrieveOpenAIVoice()
        {
            var voice = Actor.LookupValue(DialogueSystemFields.Voice);
            isOpenAIVoiceSelected = Actor.LookupValue(DialogueSystemFields.VoiceID) == "OpenAI";
            System.Enum.TryParse<Voices>(voice, out openAIVoice);
        }

        private void DrawOpenAISection()
        {
            EditorGUILayout.LabelField("OpenAI Voices", EditorStyles.boldLabel);
            ttsModel = (TTSModel)EditorGUILayout.EnumPopup("Model", ttsModel);
            EditorGUI.BeginDisabledGroup(IsAwaitingReply);
            EditorGUILayout.BeginHorizontal();
            openAIVoice = (Voices)EditorGUILayout.EnumPopup("OpenAI Actor", openAIVoice);
            if (GUILayout.Button("Preview", GUILayout.Width(60)))
            {
                PreviewSelectedOpenAIVoice();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Accept"))
            {
                Field.SetValue(Actor.fields, DialogueSystemFields.Voice, openAIVoice.ToString());
                Field.SetValue(Actor.fields, DialogueSystemFields.VoiceID, "OpenAI");
                RefreshEditor();
                CloseWindow();
            }
            if (GUILayout.Button("Generate All Lines"))
            {
                if (EditorUtility.DisplayDialog("Generate All Lines (OpenAI)",
                        "This may take a long time to complete. Proceed?", "OK", "Cancel"))
                {
                    path = EditorUtility.OpenFolderPanel("Generate All Lines", path, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        GenerateAllOpenAILines();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void PreviewSelectedOpenAIVoice()
        {
            DestroyAudioClip();
            IsAwaitingReply = true;
            ProgressText = $"Retrieving preview sample for {openAIVoice}.";
            OpenAI.SubmitVoiceGenerationAsync(openAIKey, ttsModel, openAIVoice,
                VoiceOutputFormat.MP3, 1, previewText, OnReceivedOpenAITextToSpeech);
        }

        private void OnReceivedOpenAITextToSpeech(AudioClip audioClip, byte[] bytes)
        {
            IsAwaitingReply = false;
            if (audioClip == null) return;
            this.audioClip = audioClip;
            Debug.Log($"Playing voice sample for {openAIVoice}.");
            EditorAudioUtility.PlayAudioClip(audioClip);
            Repaint();
        }

        private void GenerateAllOpenAILines()
        {
            GenerateAllLines();
        }

        #endregion

        #region ElevenLabs

        private void DrawElevenLabsSection()
        {
            DrawElevenLabsBuiltInSection();
        }

        private void DrawElevenLabsBuiltInSection()
        {
            if (IsAwaitingReply && !isGeneratingAllLines) return;
            EditorGUILayout.LabelField("ElevenLabs Built-In Voices", EditorStyles.boldLabel);
            if (voiceNames == null)
            {
                EditorGUILayout.HelpBox("Error retrieving voice actor list.", MessageType.Warning);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(IsAwaitingReply);
                EditorGUILayout.BeginHorizontal();
                voiceIndex = EditorGUILayout.Popup("ElevenLabs Actor", voiceIndex, voiceNames);
                if (GUILayout.Button("Preview", GUILayout.Width(60)))
                {
                    PreviewSelectedElevenLabsBuiltInVoice();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(GenerateVoicePanel.ElevenLabsModelLabel, modelId);
            EditorGUI.EndDisabledGroup();
            stability = EditorGUILayout.FloatField(GenerateVoicePanel.StabilityLabel, stability);
            use_speaker_boost = EditorGUILayout.Toggle(GenerateVoicePanel.UseSpeakerBoostLabel, use_speaker_boost);
            similarity_boost = EditorGUILayout.FloatField(GenerateVoicePanel.SimilarityBoostLabel, similarity_boost);
            style = EditorGUILayout.FloatField(GenerateVoicePanel.StyleLabel, style);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(ViewMyVoicesLabel))
            {
                Application.OpenURL("https://elevenlabs.io/app/voice-lab");
            }
            if (GUILayout.Button(ViewVoiceLibraryLabel))
            {
                Application.OpenURL("https://elevenlabs.io/app/voice-library");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!(Actor != null && IsElevenLabsVoiceSelected));
            if (GUILayout.Button("Accept"))
            {
                Field.SetValue(Actor.fields, DialogueSystemFields.Voice, voiceList.voices[voiceIndex].name);
                Field.SetValue(Actor.fields, DialogueSystemFields.VoiceID, voiceList.voices[voiceIndex].voice_id);
                RefreshEditor();
                CloseWindow();
            }
            EditorGUI.EndDisabledGroup();
            DrawGenerateAllLinesButton(voiceIndex != -1);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawElevenLabsSharedVoicesSection()
        {
            if (IsAwaitingReply && !isGeneratingAllLines) return;
            EditorGUILayout.LabelField("ElevenLabs Shared Voice Library", EditorStyles.boldLabel);
            if (sharedVoiceNames == null)
            {
                EditorGUILayout.HelpBox("Error retrieving shared voice list.", MessageType.Warning);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(IsAwaitingReply);
                EditorGUILayout.BeginHorizontal();
                sharedVoiceIndex = EditorGUILayout.Popup("ElevenLabs Actor", sharedVoiceIndex, sharedVoiceNames);

                if (GUILayout.Button("Preview", GUILayout.Width(60)))
                {
                    PreviewSelectedElevenLabsSharedVoice();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
            }
            voiceCategory = (VoiceCategory)EditorGUILayout.EnumPopup("Category", voiceCategory);
            gender = (Gender)EditorGUILayout.EnumPopup("Gender", gender);
            age = (Age)EditorGUILayout.EnumPopup("Age", age);
            accent = EditorGUILayout.TextField(AccentLabel, accent);
            search = EditorGUILayout.TextField(SearchLabel, search);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(RefreshSharedVoicesLabel))
            {
                RefreshElevenLabsSharedVoiceList();
            }
            EditorGUI.BeginDisabledGroup(!(Actor != null && IsElevenLabsSharedVoiceSelected));
            if (GUILayout.Button("Add To My List"))
            {
                // Kept in case we decide to support this.
            }
            EditorGUI.EndDisabledGroup();
            DrawGenerateAllLinesButton(voiceIndex != -1);
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshElevenLabsBuiltInVoiceList()
        {
            IsAwaitingReply = true;
            ProgressText = "Retrieving ElevenLabs built-in voice list.";
            ElevenLabs.ElevenLabs.GetVoiceList(apiKey, OnReceivedElevenLabsBuiltInVoiceList);
        }

        private void RefreshElevenLabsSharedVoiceList()
        {
            IsAwaitingReply = true;
            ProgressText = "Retrieving ElevenLabs Shared Voice Library list.";
            ElevenLabs.ElevenLabs.GetSharedVoiceList(apiKey, voiceCategory, gender, age,
                accent, search, OnReceivedElevenLabsSharedVoiceList);
        }

        private void OnReceivedElevenLabsBuiltInVoiceList(VoiceList voiceList)
        {
            IsAwaitingReply = false;
            this.voiceList = voiceList;
            if (voiceList == null)
            {
                voiceNames = null;
            }
            else
            {
                var currentActorVoiceID = Actor.LookupValue(DialogueSystemFields.VoiceID);
                voiceNames = new string[voiceList.voices.Count];
                for (int i = 0; i < voiceNames.Length; i++)
                {
                    voiceNames[i] = voiceList.voices[i].name;
                    if (voiceList.voices[i].voice_id == currentActorVoiceID) voiceIndex = i;
                }
            }
            Repaint();
        }

        private void OnReceivedElevenLabsSharedVoiceList(VoiceList voiceList)
        {
            IsAwaitingReply = false;
            this.sharedVoiceList = voiceList;
            if (voiceList == null)
            {
                sharedVoiceNames = null;
            }
            else
            {
                var currentActorVoiceID = Actor.LookupValue(DialogueSystemFields.VoiceID);
                sharedVoiceNames = new string[voiceList.voices.Count];
                for (int i = 0; i < sharedVoiceNames.Length; i++)
                {
                    sharedVoiceNames[i] = voiceList.voices[i].name;
                    if (voiceList.voices[i].voice_id == currentActorVoiceID) voiceIndex = i;
                }
            }
            Repaint();
        }

        private void PreviewSelectedElevenLabsBuiltInVoice()
        {
            DestroyAudioClip();
            if (!(0 <= voiceIndex && voiceIndex < voiceList.voices.Count)) return;
            IsAwaitingReply = true;
            ProgressText = $"Retrieving preview sample for {voiceNames[voiceIndex]}.";
            var voiceData = voiceList.voices[voiceIndex];
            ElevenLabs.ElevenLabs.GetTextToSpeech(apiKey, modelId, voiceData.name, voiceData.voice_id, 0, 0, previewText, OnReceivedTextToSpeech);
        }

        private void PreviewSelectedElevenLabsSharedVoice()
        {
            DestroyAudioClip();
            if (!(0 <= sharedVoiceIndex && sharedVoiceIndex < sharedVoiceList.voices.Count)) return;
            IsAwaitingReply = true;
            ProgressText = $"Retrieving preview sample for {sharedVoiceNames[sharedVoiceIndex]}.";
            var voiceData = sharedVoiceList.voices[sharedVoiceIndex];
            ElevenLabs.ElevenLabs.GetTextToSpeech(apiKey, modelId, voiceData.name, voiceData.voice_id, 0, 0, previewText, OnReceivedTextToSpeech);
        }

        #endregion

        #region Shared

        private void OnReceivedTextToSpeech(AudioClip audioClip)
        {
            IsAwaitingReply = false;
            if (audioClip == null) return;
            this.audioClip = audioClip;
            Debug.Log($"Playing voice sample for {voiceNames[voiceIndex]}.");
            EditorAudioUtility.PlayAudioClip(audioClip);
            Repaint();
        }

        #endregion

        #region Generate All Lines

        private void DrawSequencerCommandSection()
        {
            sequencerCommand = (AudioSequencerCommands)EditorGUILayout.EnumPopup(GenerateVoicePanel.SequencerCommandLabel, sequencerCommand);
            if (sequencerCommand == AudioSequencerCommands.Other)
            {
                otherSequencerCommand = EditorGUILayout.TextField("Command", otherSequencerCommand);
            }
        }

        private void DrawGenerateAllLinesButton(bool hasSelectedVoice)
        {
            var needToInputSequencerCommand = sequencerCommand == AudioSequencerCommands.Other && string.IsNullOrEmpty(otherSequencerCommand);
            EditorGUI.BeginDisabledGroup(IsAwaitingReply || needToInputSequencerCommand ||
                Actor == null || !hasSelectedVoice);
            if (GUILayout.Button(GenerateAllLinesLabel))
            {
                if (EditorUtility.DisplayDialog("Generate All Lines",
                    "This may take a long time to complete. Proceed?", "OK", "Cancel"))
                {
                    path = EditorUtility.OpenFolderPanel("Generate All Lines", path, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        GenerateAllLines();
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void GenerateAllLines()
        {
            IsAwaitingReply = true;
            isGeneratingAllLines = true;
            entrytagFormat = GenerateVoicePanel.GetEntrytagFormat();
            voiceGenerationQueue.Clear();
            int actorID = Actor.id;
            int entryQueueNum = 0;
            for (int i = 0; i < database.conversations.Count; i++)
            {
                var conversation = database.conversations[i];
                var conversationTitle = conversation.Title;
                for (int j = 0; j < conversation.dialogueEntries.Count; j++)
                {
                    var dialogueEntry = conversation.dialogueEntries[j];
                    if (dialogueEntry.ActorID != actorID) continue;
                    var fullPath = GetAudioClipFullPath(conversation, dialogueEntry);
                    if (System.IO.File.Exists(fullPath)) continue;
                    var text = dialogueEntry.DialogueText;
                    if (string.IsNullOrEmpty(text)) text = dialogueEntry.MenuText;
                    if (string.IsNullOrEmpty(text)) continue;
                    if (GenerateVoicePanel.CanUseAudioTag(database, dialogueEntry))
                    {
                        text = AITextUtility.PrependAudioTag(text, dialogueEntry);
                    }
                    var description = $"{conversationTitle} entry {dialogueEntry.id}";
                    voiceGenerationQueue.Enqueue(new VoiceGenerationRequest(conversation, dialogueEntry, text, description, entryQueueNum++));
                }
            }
            totalEntryQueueSize = voiceGenerationQueue.Count;
            GenerateNextLine();
        }

        private void GenerateNextLine()
        {
            if (voiceGenerationQueue.Count > 0)
            {
                var job = voiceGenerationQueue.Dequeue();
                currentJob = job;
                float progress = (float)job.entryQueueNum / (float)totalEntryQueueSize;
                if (isOpenAIVoiceSelected)
                {
                    ProgressText = $"[{progress}%] OpenAI: {job.description}: {job.text}";
                    OpenAI.SubmitVoiceGenerationAsync(openAIKey, TTSModel.TTSModel1HD, openAIVoice,
                        VoiceOutputFormat.MP3, 1, job.text, OnReceivedLine);
                }
                else if (voiceIndex != -1)
                {
                    ProgressText = $"[{progress}%] ElevenLabs: {job.description}: {job.text}";
                    var voiceData = voiceList.voices[voiceIndex];
                    ElevenLabs.ElevenLabs.GetTextToSpeech(apiKey, modelId, voiceData.name, voiceData.voice_id, 
                        stability, similarity_boost, use_speaker_boost, style, 
                        job.text, OnReceivedLine);
                }
#if USE_DEEPVOICE
                else if (deepVoiceInt != -1)
                {
                    ProgressText = $"[{progress}%] DeepVoice: {job.description}: {job.text}";
                    var deepVoiceName = DeepVoiceAPI.DeepVoiceIntToName(deepVoiceModel, deepVoiceInt);
                    DeepVoiceAPI.GetTextToSpeech(deepVoiceModel, deepVoiceName,
                        job.text, variability, clarity, OnReceivedLine);
                }
#endif
#if USE_OVERTONE
                else if (overtoneVoiceIndex != -1)
                {
                    ProgressText = $"[{progress}%] Overtone: {job.description}: {job.text}";
                    OnReceivedLine(null);
                }
#endif
                Debug.Log(ProgressText);
            }
            else
            {
                Debug.Log("Finished generating lines for actor.");
                FinishGenerateAllLines();
            }
        }

        private void OnReceivedLine(AudioClip audioClip)
        {
            if (audioClip != null)
            {
                try
                {
                    var filename = SaveAudioClip(audioClip);
                    GenerateVoicePanel.AddSelectedSequencerCommand(sequencerCommand, System.IO.Path.GetFileNameWithoutExtension(filename), database, currentJob.entry);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
            GenerateNextLine();
        }

        private string GetAudioClipFullPath(Conversation conversation, DialogueEntry dialogueEntry)
        {
            var entrytag = database.GetEntrytag(conversation, dialogueEntry, entrytagFormat);
            return $"{path}/{entrytag}.wav";
        }

        private string SaveAudioClip(AudioClip audioClip)
        {
            var fullPath = GetAudioClipFullPath(currentJob.conversation, currentJob.entry);
            var filename = "Assets/" + fullPath.Substring(Application.dataPath.Length);
            Debug.Log($"Saving audio clip to {fullPath}");
            SavWav.Save(fullPath, audioClip);
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

        private void CancelGenerateAllLines()
        {
            Debug.Log("Cancelled generating all lines of voice acting for actor.");
            FinishGenerateAllLines();
        }

        private void FinishGenerateAllLines()
        {
            isGeneratingAllLines = false;
            IsAwaitingReply = false;
            ProgressText = string.Empty;
            voiceGenerationQueue.Clear();
            AssetDatabase.Refresh();
        }


        #endregion

        #region Overtone

#if USE_OVERTONE

        private static GUIContent OvertoneHeading = new GUIContent("Overtone");
        private static GUIContent OvertoneRefreshLabel = new GUIContent("Refresh", "Refresh list of Overtone voices in project.");

        private void DrawOvertoneSection()
        {
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.LabelField("Overtone Voices", EditorStyles.boldLabel);
            if (overtoneVoiceIndex != -1)
            {
                EditorGUILayout.HelpBox("Note: Overtone voices can only be generated at runtime.", MessageType.Info);
            }
            if (overtoneVoiceNames == null)
            {
                RefreshOvertoneVoices();
            }
            overtoneVoiceIndex = EditorGUILayout.Popup("Overtone Voice", overtoneVoiceIndex, overtoneVoiceNames);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(OvertoneRefreshLabel))
            {
                RefreshOvertoneVoices();
            }
            EditorGUI.BeginDisabledGroup(overtoneVoiceIndex == -1);
            if (GUILayout.Button("Accept"))
            {
                Field.SetValue(Actor.fields, DialogueSystemFields.Voice, overtoneVoiceNames[overtoneVoiceIndex]);
                Field.SetValue(Actor.fields, DialogueSystemFields.VoiceID, string.Empty);
                RefreshEditor();
                CloseWindow();
            }
            EditorGUI.EndDisabledGroup();
            // Overtone is currently runtime only: DrawGenerateAllLinesButton(overtoneVoiceIndex != -1);
            EditorGUILayout.EndHorizontal();
        }

        private string[] LoadOvertoneVoices()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Overtone Integration", "Identifying Overtone voices in project. Please wait...", 0);
                var list = new List<string>();
                string[] dirs = System.IO.Directory.GetDirectories(Application.dataPath, "Resources", System.IO.SearchOption.AllDirectories);
                foreach (string dir in dirs)
                {
                    string[] files = System.IO.Directory.GetFiles(dir, "*.config.json", System.IO.SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var text = System.IO.File.ReadAllText(file);
                        if (text.Contains("\"espeak\""))
                        {
                            var voiceName = System.IO.Path.GetFileNameWithoutExtension(file);
                            voiceName = voiceName.Substring(0, voiceName.Length - ".config".Length);
                            list.Add(voiceName);
                        }
                    }
                }
                return list.ToArray();
            }
            catch (System.Exception)
            {
                return new string[0];
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }            
        }

        private void RefreshOvertoneVoices()
        {
            overtoneVoiceNames = LoadOvertoneVoices();
            overtoneVoiceIndex = GetOvertoneVoiceIndex();
        }

        private int GetOvertoneVoiceIndex()
        {
            if (overtoneVoiceNames == null)
            {
                RefreshOvertoneVoices();
                return overtoneVoiceIndex;
            }
            else
            { 
                string actorVoice = Field.LookupValue(Actor.fields, DialogueSystemFields.Voice);
                for (int i = 0; i < overtoneVoiceNames.Length; i++)
                {
                    if (overtoneVoiceNames[i] == actorVoice)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

#endif

        #endregion

        #region DeepVoice

#if USE_DEEPVOICE

        private DeepVoiceModel GetDeepVoiceModel()
        {
            var modelName = Field.LookupValue(Actor.fields, DialogueSystemFields.VoiceID);
            return DeepVoiceAPI.GetDeepVoiceModel(modelName);
        }

        private int GetDeepVoiceInt()
        {
            string actorVoice = Field.LookupValue(Actor.fields, DialogueSystemFields.Voice);
            return DeepVoiceAPI.GetDeepVoiceInt(deepVoiceModel, actorVoice);
        }

        private static GUIContent DeepVoiceHeading = new GUIContent("DeepVoice");

        private void DrawDeepVoiceSection()
        {
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.LabelField("DeepVoice Voices", EditorStyles.boldLabel);
            deepVoiceModel = (DeepVoiceModel)EditorGUILayout.EnumPopup("Model", deepVoiceModel);
            switch (deepVoiceModel)
            {
                case DeepVoiceModel.DeepVoice_Standard:
                    deepVoiceInt = (int)(DeepVoiceStandard)EditorGUILayout.EnumPopup("Voice", (DeepVoiceStandard)deepVoiceInt);
                    break;
                case DeepVoiceModel.DeepVoice_Neural:
                    deepVoiceInt = (int)(DeepVoiceNeural)EditorGUILayout.EnumPopup("Voice", (DeepVoiceNeural)deepVoiceInt);
                    break;
                default:
                    deepVoiceInt = (int)(DeepVoiceMonoMulti)EditorGUILayout.EnumPopup("Voice", (DeepVoiceMonoMulti)deepVoiceInt);
                    break;
            }
            variability = EditorGUILayout.Slider("Variability", variability, 0, 1);
            clarity = EditorGUILayout.Slider("Clarity", clarity, 0, 1);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(deepVoiceInt == -1);
            if (GUILayout.Button("Accept"))
            {
                Field.SetValue(Actor.fields, DialogueSystemFields.VoiceID, deepVoiceModel.ToString());
                Field.SetValue(Actor.fields, DialogueSystemFields.Voice, DeepVoiceAPI.DeepVoiceIntToName(deepVoiceModel, deepVoiceInt));
                RefreshEditor();
                CloseWindow();
            }
            EditorGUI.EndDisabledGroup();
            DrawGenerateAllLinesButton(deepVoiceInt != -1);
            EditorGUILayout.EndHorizontal();
        }

#endif

        #endregion

    }
}

#endif
