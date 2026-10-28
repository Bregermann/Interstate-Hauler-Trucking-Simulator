#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class JengaWorldFontLifetimeTests
{
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static int Count => (int)Runtime("SportsDepthTestedWorldText").GetProperty("SharedMaterialCount").GetValue(null);

    [UnityTest, Timeout(65000)]
    public IEnumerator ActualSavedLabelsShareOneAtlasRestoreOnDisableAndReleaseAfterRepeatedSceneUnload()
    {
        var preferences = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "JengaFontLifetime07" });
        var isolation = new GameObject("World font lifetime input isolation");
        UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        try
        {
            // Exercise actual saved scene enable/unload twice; no fixture-created
            // materials and no gameplay or scoring state changes.
            for (int round = 0; round < 2; round++)
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckJenga/Scenes/TruckJenga.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var type = Runtime("SportsDepthTestedWorldText");
                var labels = UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.None).Cast<Behaviour>().ToArray();
                Assert.That(labels.Length, Is.GreaterThanOrEqualTo(18), "Actual saved container-number adapters must be present.");
                var label = labels.First(item => item.name == "Real container number");
                var text = label.GetComponent<TextMesh>(); var renderer = label.GetComponent<MeshRenderer>();
                var fontMaterial = text.font.material; var shared = renderer.sharedMaterial;
                Assert.That(Count, Is.EqualTo(1), "One real original font atlas must not allocate a material per label.");
                Assert.That(labels.All(item => item.GetComponent<MeshRenderer>().sharedMaterial == shared), Is.True);
                Assert.That(shared.shader.name, Is.EqualTo("LWS/Truck Sports/Depth Tested World Font"));
                Assert.That(shared.mainTexture, Is.SameAs(fontMaterial.mainTexture));
                label.enabled = false;
                Assert.That(renderer.sharedMaterial, Is.SameAs(fontMaterial), "Disable restores the authored font material.");
                Assert.That(Count, Is.EqualTo(1), "Other real labels retain their shared material.");
                label.enabled = true;
                Assert.That(renderer.sharedMaterial, Is.SameAs(shared));
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                Assert.That(Count, Is.Zero, "Scene exit must release the shared material and cache ownership.");
                Assert.That(shared == null, Is.True, "The released runtime material must actually be destroyed.");
                Assert.That(fontMaterial != null && fontMaterial.shader.name == "GUI/Text Shader", Is.True, "Original shared font asset remains unchanged.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(isolation); preferences.Dispose();
        }
    }
}
#endif
