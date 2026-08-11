// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI && USE_DEEPVOICE

using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

namespace PixelCrushers.DialogueSystem.OpenAIAddon.DeepVoice
{

    public enum DeepVoiceModel
    {
        DeepVoice_Neural,
        DeepVoice_Mono,
        DeepVoice_Multi,
        DeepVoice_Standard
    }

    public enum DeepVoiceMonoMulti
    {
        Jessie, Harry, Glinda, Clyde, Callum, Charlotte, Dave, Fin, Freya, Batman, Andrew, 
        Hailey, Arthur, Anime_Girl, Valentina, Wayne, Jan, Noah, Lily, Lily_Narrator, Ethan, 
        Sophia, Olivia, Ruby, Lucas, John
    };
    public enum DeepVoiceStandard
    {
        Lotte, Maxim, Salli, Geraint, Miguel, Giorgio, Marlene, Ines, Zhiyu, Zeina, Karl, 
        Gwyneth, Lucia, Cristiano, Astrid, Vicki, Mia, Vitoria, Bianca, Chantal, Raveena, 
        Russell, Aditi, Dora, Enrique, Hans, Carmen, Ewa, Maja, Nicole, Filiz, Camila, Jacek, 
        Celine, Ricardo, Mads, Mathieu, Lea, Tatyana, Penelope, Naja, Ruben, Takumi, Mizuki, 
        Carla, Conchita, Jan, Liv, Lupe, Seoyeon
    }

    public enum DeepVoiceNeural
    {
        Olivia, Emma, Amy, Brian, Arthur, Kajal, Aria, Ayanda, Salli, Kimberly, Kendra, 
        Joanna, Ivy, Ruth, Kevin, Matthew, Justin, Joey, Stephen
    }

    public class DeepVoiceAPI : MonoBehaviour
    {

        private static DeepVoiceAPI instance = null;
        public static DeepVoiceAPI Instance
        {
            get
            {
                if (isQuitting) return instance;
                if (instance == null)
                {
                    instance = new GameObject("DeepVoice").AddComponent<DeepVoiceAPI>();
                }
                return instance;
            }
        }

        private static bool isQuitting = false;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStaticVariables()
        {
            instance = null;
            isQuitting = false;
        }
#endif

        private void OnApplicationQuit() => isQuitting = true;

        public static DeepVoiceModel GetDeepVoiceModel(string modelName)
        {
            if (!string.IsNullOrEmpty(modelName) &&
                System.Enum.TryParse<DeepVoiceModel>(modelName, out DeepVoiceModel model))
            {
                return model;
            }
            return DeepVoiceModel.DeepVoice_Mono;
        }

        public static int GetDeepVoiceInt(DeepVoiceModel deepVoiceModel, string voice)
        {
            if (string.IsNullOrEmpty(voice))
            {
                return 0;
            }
            switch (deepVoiceModel)
            {
                case DeepVoiceModel.DeepVoice_Standard:
                    return System.Enum.TryParse<DeepVoiceStandard>(voice, out DeepVoiceStandard standardVoice)
                        ? (int)standardVoice
                        : 0;
                case DeepVoiceModel.DeepVoice_Neural:
                    return System.Enum.TryParse<DeepVoiceNeural>(voice, out DeepVoiceNeural neuralVoice)
                        ? (int)neuralVoice
                        : 0;
                default:
                    return System.Enum.TryParse<DeepVoiceMonoMulti>(voice, out DeepVoiceMonoMulti monoVoice)
                        ? (int)monoVoice
                        : 0;
            }
        }

        public static string DeepVoiceIntToName(DeepVoiceModel deepVoiceModel, int deepVoiceInt)
        {
            switch (deepVoiceModel)
            {
                case DeepVoiceModel.DeepVoice_Standard:
                    return ((DeepVoiceStandard)deepVoiceInt).ToString();
                case DeepVoiceModel.DeepVoice_Neural:
                    return ((DeepVoiceNeural)deepVoiceInt).ToString();
                default:
                    return ((DeepVoiceMonoMulti)deepVoiceInt).ToString();
            }
        }

        public static void GetTextToSpeechRuntime(DeepVoiceModel model, string voice,
            string text, float variability, float clarity,
            System.Action<AudioClip> callback)
        {
            var invoice = PlayerPrefs.GetString("DeepVoice_Invoice");
            if (string.IsNullOrEmpty(invoice)) invoice = PlayerPrefs.GetString("DeepVoicePro_Invoice");
            //var window = DialogueSystemOpenAIWindow.Instance;
            switch (model)
            {
                case DeepVoiceModel.DeepVoice_Standard:
                    Instance.StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text}" + "\",\"model\":\"" + $"{model}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"name\":\"" + $"{voice}" + "\",\"variability\":\"" + $"{variability}" + "\",\"clarity\":\"" + $"{clarity}" + "\"}", callback));
                    break;
                case DeepVoiceModel.DeepVoice_Neural:
                    Instance.StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text}" + "\",\"model\":\"" + $"{model}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"name\":\"" + $"{voice}" + "\",\"variability\":\"" + "0.0" + "\",\"clarity\":\"" + "0.0" + "\"}", callback));
                    break;
                default:
                    Instance.StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text}" + "\",\"model\":\"" + $"{model}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"name\":\"" + $"{voice}" + "\",\"variability\":\"" + $"{variability}" + "\",\"clarity\":\"" + $"{clarity}" + "\"}", callback));
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
                        byte[] bytes= System.Convert.FromBase64String(text);

                        var filePath = Path.Combine(Application.persistentDataPath, $"audio.mp3");
                        filePath = filePath.Replace("/", "\\");
                        File.WriteAllBytes(filePath, bytes);
                        var fileUrl = "file://" + filePath.Replace("\\", "/");
                        UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.UNKNOWN);
                        var wwwOp = www.SendWebRequest();
                        wwwOp.completed += (op2) =>
                        {
                            if (www.result == UnityWebRequest.Result.Success)
                            {
                                audioClip = DownloadHandlerAudioClip.GetContent(www);
                            }
                            File.Delete(filePath);
                            www.Dispose();
                            www = null;
                        };
                    }
                }
            }

            callback?.Invoke(audioClip);

            request.Dispose();
        }


    }

}

#endif
