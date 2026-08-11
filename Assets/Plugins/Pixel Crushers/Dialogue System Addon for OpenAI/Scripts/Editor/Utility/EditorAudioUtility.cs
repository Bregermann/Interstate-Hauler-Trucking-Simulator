// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs
{

    public static class EditorAudioUtility
    {

        /// <summary>
        /// Plays an audio clip in the editor.
        /// </summary>
        public static void PlayAudioClip(AudioClip clip)
        {
            var unityEditorAssembly = typeof(AudioImporter).Assembly;
            var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public);
            method.Invoke(null,  new object[] { clip, 0, false } );
        }

        public static string GetPathForSaveInFile(string lastFilename)
        {
            if (!string.IsNullOrEmpty(lastFilename))
            {
                var lastFolder = System.IO.Path.GetDirectoryName(lastFilename).Replace("\\", "/");
                return (lastFolder == "Assets")
                    ? Application.dataPath
                    : lastFolder.StartsWith("Assets/")
                        ? Application.dataPath + "/" + lastFolder.Substring("Assets/".Length)
                        : "Assets";
            }
            return "Assets";
        }

    }
}

#endif
