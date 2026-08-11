// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem.DialogueEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Panel to work with voice actors.
    /// </summary>
    public class TextToSpeechPanel : BasePanel
    {

        protected AudioClip audioClip = null;
        protected virtual string Operation => "ElevenLabs";
        protected string modelId;

        protected string openAIKey = EditorPrefs.GetString(DialogueSystemOpenAIWindow.OpenAIKey);

        public TextToSpeechPanel(string apiKey, DialogueDatabase database,
            Asset asset, DialogueEntry entry, Field field)
            : base(apiKey, database, asset, entry, field)
        {
            var model = (ElevenLabs.ElevenLabs.Models)EditorPrefs.GetInt(DialogueSystemOpenAIWindow.ElevenLabsModel, 0);
            modelId = ElevenLabs.ElevenLabs.GetModelId(model);
        }

        ~TextToSpeechPanel()
        {
            DestroyAudioClip();
        }

        protected virtual void RefreshEditor()
        {
            Undo.RecordObject(database, Operation);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DialogueEditorWindow.instance?.Reset();
            DialogueEditorWindow.instance?.Repaint();
            GUIUtility.ExitGUI();
        }

        protected void CloseWindow()
        {
            DialogueSystemOpenAIWindow.Instance.Close();
            GUIUtility.ExitGUI();
            DestroyAudioClip();
        }

        protected void DestroyAudioClip()
        {
            Object.DestroyImmediate(audioClip);
            audioClip = null;
        }

    }
}

#endif
