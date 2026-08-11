// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Panel for managing list of fine-tuned models.
    /// </summary>
    public class FineTunedModelsPanel : BasePanel
    {

        private static GUIContent FineTunedHeadingLabel = new GUIContent("Fine-Tuned & Custom Models");
        private static GUIContent CustomOllamaHeadingLabel = new GUIContent("Ollama Model");
        private static GUIContent CustomOllamaNameLabel = new GUIContent("Ollama Model", "Specify the name of a locally-installed Ollama model.");
        private static GUIContent CustomOllamaURLLabel = new GUIContent("Ollama URL", "URL to communicate with Ollama.");

        private ReorderableList reorderableList;

        public FineTunedModelsPanel(string apiKey, DialogueDatabase database)
            : base(apiKey, database, null, null, null)
        {
        }

        public override void Draw()
        {
            base.Draw();
            DrawFineTunedSection();
            DrawCustomOllamaSection();
        }

        private void DrawFineTunedSection()
        {
            DrawHeading(FineTunedHeadingLabel, "Add your fine-tuned models here.\nYou can create fine-tuned models on OpenAI's website.");
            if (GUILayout.Button("OpenAI Fine-Tuning", labelHyperlinkStyle))
            {
                Application.OpenURL("https://platform.openai.com/finetune");
            }
            if (reorderableList == null)
            {
                reorderableList = new ReorderableList(DialogueSystemOpenAIWindow.fineTunedModelInfo.models,
                    typeof(string), true, true, true, true);
                reorderableList.drawHeaderCallback += OnDrawHeader;
                reorderableList.drawElementCallback += OnDrawElement;
            }
            reorderableList.DoLayoutList();
        }

        private void OnDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Your Fine-Tuned Models");
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (!(0 <= index && index < DialogueSystemOpenAIWindow.fineTunedModelInfo.models.Count)) return;
            DialogueSystemOpenAIWindow.fineTunedModelInfo.models[index] =
                EditorGUI.TextField(rect, DialogueSystemOpenAIWindow.fineTunedModelInfo.models[index]);
        }

        private void DrawCustomOllamaSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(CustomOllamaHeadingLabel, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("If you use Ollama, you can specify a model such as 'deepseek-v3' here.", MessageType.None);
            EditorGUI.BeginChangeCheck();
            DialogueSystemOpenAIWindow.customOllamaName = EditorGUILayout.TextField(CustomOllamaNameLabel,
                DialogueSystemOpenAIWindow.customOllamaName);
            if (EditorGUI.EndChangeCheck())
            {
                Model.CustomOllamaModel.Name = DialogueSystemOpenAIWindow.customOllamaName;
            }
            OllamaAPI.currentOllamaURL = EditorGUILayout.TextField(CustomOllamaURLLabel,
                OllamaAPI.currentOllamaURL);
        }

    }
}

#endif
