// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI && USE_DEEPVOICE

using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using System.Text;

namespace PixelCrushers.DialogueSystem.OpenAIAddon.DeepVoice
{

    public static class DeepVoiceEditorAPI
    {

        public static void GetTextToSpeech(DeepVoiceModel model, string voice,
            string text, float variability, float clarity,
            System.Action<AudioClip> callback)
        {
            var invoice = PlayerPrefs.GetString("DeepVoice_Invoice");
            if (string.IsNullOrEmpty(invoice)) invoice = PlayerPrefs.GetString("DeepVoicePro_Invoice");
            var window = DialogueSystemOpenAIWindow.Instance;
            switch (model)
            {
                case DeepVoiceModel.DeepVoice_Standard:
                    window.StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text}" + "\",\"model\":\"" + $"{model}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"name\":\"" + $"{voice}" + "\",\"variability\":\"" + $"{variability}" + "\",\"clarity\":\"" + $"{clarity}" + "\"}", callback));
                    break;
                case DeepVoiceModel.DeepVoice_Neural:
                    window.StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text}" + "\",\"model\":\"" + $"{model}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"name\":\"" + $"{voice}" + "\",\"variability\":\"" + "0.0" + "\",\"clarity\":\"" + "0.0" + "\"}", callback));
                    break;
                default:
                    window.StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text}" + "\",\"model\":\"" + $"{model}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"name\":\"" + $"{voice}" + "\",\"variability\":\"" + $"{variability}" + "\",\"clarity\":\"" + $"{clarity}" + "\"}", callback));
                    break;
            }
        }

        private static IEnumerator Post(string url, string bodyJsonString, System.Action<AudioClip> callback)
        {
            // Note: Adapted code from DeepVoice since it doesn't expose API methods.
            AudioClip audioClip = null;
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("From DeepVoice: There was an error in generating the voice. Please check your DeepVoice invoice/order number and try again or check the documentation for more information.");
                if (request.responseCode == 400)
                {
                    Debug.Log("From DeepVoice: Error in text field: Please check your prompt for quotes (\"\") and line breaks at the end of the prompt. There could also be special formatting in your text. Please remove any special formatting by pasting as plain text in a notepad and then pasting the text here. Inclusion of any special formatting or illegal characters will result in an error such as this. For best results, please use a combination of letters, periods and commas and make sure there are no line breaks in between or at the end. If you must use quotes or line breaks, please prepend them with a backslash. Please do not press enter in the text field before clicking on generate.");
                }
            }
            else
            {
                if (request.responseCode == 400)
                {
                    Debug.Log("From DeepVoice: Error in text field: Please check your prompt for quotes (\"\") and line breaks at the end of the prompt. There could also be special formatting in your text. Please remove any special formatting by pasting as plain text in a notepad and then pasting the text here. Inclusion of any special formatting or illegal characters will result in an error such as this. For best results, please use a combination of letters, periods and commas and make sure there are no line breaks in between or at the end. If you must use quotes or line breaks, please prepend them with a backslash. Please do not press enter in the text field before clicking on generate.");
                }
                if (request.downloadHandler.text == "Invalid Response")
                    Debug.Log("From DeepVoice: Invalid DeepVoice Invoice/Order Number. Please check your invoice/order number and try again.");
                else if (request.downloadHandler.text == "Limit Reached")
                    Debug.Log("From DeepVoice: It seems that you may have reached the limit. To check your character usage, please click on the Status button. Please wait until 30th/31st of the month to get a renewed character count. Thank you for using DeepVoice.");
                else
                {
                    var text = request.downloadHandler.text;
                    if (text == null)
                    {
                        Debug.LogError("DeepVoice returned nothing.");
                    }
                    else if (text.StartsWith("{\"detail\":"))
                    {
                        Debug.LogError($"Deepvoice returned: {text}");
                    }
                    else
                    {
                        byte[] soundBytes = System.Convert.FromBase64String(text);
                        var fullPath = $"{Application.dataPath}/DeepVoiceTemp.mp3";
                        var assetPath = $"Assets/DeepVoiceTemp.mp3";
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                            File.Delete($"{fullPath}.meta");
                            AssetDatabase.Refresh();
                        }
                        File.WriteAllBytes(fullPath, soundBytes);
                        AssetDatabase.Refresh();
                        audioClip = (AudioClip)AssetDatabase.LoadMainAssetAtPath(assetPath);
                    }
                }
            }

            request.Dispose();

            callback(audioClip);
        }


    }

}

#endif
