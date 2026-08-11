using UnityEngine;
using UnityEditor;

namespace PixelCrushers
{

    public class EncryptedKeyWizard : ScriptableWizard
    {

        [MenuItem("Tools/Pixel Crushers/Dialogue System/Addon for OpenAI/Create Encrypted Key Asset...", priority = 99)]
        private static void CreateWizard()
        {
            DisplayWizard<EncryptedKeyWizard>("Create Encrypted Key", "Create");
        }

        [Tooltip("API Key that you want to encrypt.")]
        [SerializeField] private string key;

        [Tooltip("Password to encrypt & decrypt the key. Record this key somewhere! You cannot recover it from the Encrypted Key asset.")]
        [SerializeField] private string password;

        [Tooltip("You cannnot recover the password from the Encrypted Key asset. Make sure you record it before clicking Create.")]
        [SerializeField] private bool iRecordedThePasswordSomewhere;

        private void OnWizardUpdate()
        {
            isValid = !string.IsNullOrEmpty(key) &&
                !string.IsNullOrEmpty(password) &&
                iRecordedThePasswordSomewhere;
        }

        private void OnWizardCreate()
        {
            var path = EditorUtility.SaveFilePanel("Save Encrypted Key Asset", Application.dataPath, "EncryptedKey", "asset");
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Cancelled Encrypted Key creation.");
                return;
            }
            if (!path.StartsWith(Application.dataPath))
            {
                Debug.LogError("You must select a path inside the Assets folder.");
                return;
            }
            path = path.Substring(Application.dataPath.Length - "Assets".Length);
            var asset = ScriptableObject.CreateInstance<EncryptedKey>();
            asset.data = EncryptionUtility.Encrypt(key, password);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {path}");
        }

        private void OnWizardOtherButton()
        {
            Close();
        }

    }
}
