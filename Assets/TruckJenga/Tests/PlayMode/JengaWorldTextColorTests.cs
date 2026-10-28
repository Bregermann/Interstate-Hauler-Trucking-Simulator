#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class JengaWorldTextColorTests
{
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static int Shared => (int)Sports("SportsDepthTestedWorldText").GetProperty("SharedMaterialCount").GetValue(null);
    static Color[] Frame(Camera camera, RenderTexture target, string name)
    {
        var previous = RenderTexture.active; Texture2D image = null;
        try
        {
            camera.Render(); RenderTexture.active = target;
            image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            string directory = "Logs/TruckSports/PlaytestRepair/WorldTextTintNative";
            Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            return image.GetPixels();
        }
        finally { RenderTexture.active = previous; if (image != null) UnityEngine.Object.DestroyImmediate(image); }
    }
    [UnityTest, Timeout(65000)]
    public IEnumerator ActualAlpha8FontPreservesIndependentAuthoredAndChangingColorsDepthAndOriginalPropertyBlocks()
    {
        IDisposable preferences = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "WorldTint" + Guid.NewGuid().ToString("N") });
        GameObject isolation = new GameObject("Actual world text tint input isolation"), root = null;
        UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        RenderTexture target = null; Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckJenga/Scenes/TruckJenga.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Sports("SportsEventSession"));
            match.GetType().GetMethod("AllBots").Invoke(match, null);
            Assert.That((bool)match.GetType().GetMethod("StartMatch").Invoke(match, null), Is.True);
            match.GetType().GetMethod("Pause").Invoke(match, new object[] { 0, false });
            yield return null; yield return null;
            var source = UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First(item => item.name == "Real container number");
            var bodies = ((Array)match.GetType().GetField("containers").GetValue(match)).Cast<Rigidbody>().ToArray();
            Assert.That(bodies, Has.Length.EqualTo(18));
            var positions = bodies.Select(body => body.position).ToArray(); var velocities = bodies.Select(body => body.linearVelocity).ToArray();
            int baseline = Shared; Assert.That(baseline, Is.EqualTo(1));
            var fontMaterial = source.font.material; var originalFontShader = fontMaterial.shader;
            root = new GameObject("Actual saved font independent color probe"); root.SetActive(false); root.transform.position = new Vector3(0, -15000, 0);
            var labelObject = new GameObject("Actual original Alpha8 atlas label"); labelObject.layer = 30; labelObject.transform.SetParent(root.transform, false);
            var text = labelObject.AddComponent<TextMesh>(); text.font = source.font; text.fontSize = source.fontSize; text.characterSize = source.characterSize;
            text.text = "18"; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = Color.red;
            text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
            var renderer = text.GetComponent<MeshRenderer>(); renderer.sharedMaterial = fontMaterial;
            var originalBlock = new MaterialPropertyBlock(); originalBlock.SetFloat("_OwnedOriginalProbe", 37); renderer.SetPropertyBlock(originalBlock);
            var adapter = (Behaviour)labelObject.AddComponent(Sports("SportsDepthTestedWorldText"));
            adapter.GetType().GetField("worldTextShader").SetValue(adapter, AssetDatabase.LoadAssetAtPath<Shader>("Assets/TruckSports/Resources/Depth Tested World Font.shader"));
            var camera = new GameObject("Bounded native color camera").AddComponent<Camera>(); camera.transform.SetParent(root.transform, false);
            camera.enabled = false; camera.orthographic = true; camera.nearClipPlane = .01f; camera.farClipPlane = 10;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.cullingMask = 1 << 30;
            camera.allowHDR = camera.allowMSAA = false;
            var dataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime", true);
            var data = camera.gameObject.AddComponent(dataType);
            dataType.GetProperty("renderPostProcessing").SetValue(data, false); dataType.GetProperty("volumeLayerMask").SetValue(data, (LayerMask)0);
            target = new RenderTexture(256, 256, 24); target.Create(); camera.targetTexture = target;
            root.SetActive(true); yield return null;
            camera.transform.position = renderer.bounds.center + Vector3.back * 4; camera.transform.rotation = Quaternion.identity;
            camera.orthographicSize = Mathf.Max(.1f, renderer.bounds.extents.magnitude * 1.4f);
            var red = Frame(camera, target, "authored-red");
            Assert.That(red.Count(pixel => pixel.r > .5f && pixel.r > pixel.g * 2 && pixel.r > pixel.b * 2), Is.GreaterThan(50), "Actual authored red must not become white under the installed fixed-function font sample.");
            text.color = Color.blue; yield return null;
            var blue = Frame(camera, target, "runtime-blue");
            Assert.That(blue.Count(pixel => pixel.b > .5f && pixel.b > pixel.r * 2 && pixel.b > pixel.g * 2), Is.GreaterThan(50), "A real TextMesh color change must reach the shared-atlas renderer.");
            Assert.That(Shared, Is.EqualTo(baseline), "Independent label tint must not allocate another atlas material.");
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block); Assert.That(block.GetFloat("_OwnedOriginalProbe"), Is.EqualTo(37));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Quad); wall.name = "Actual opaque color probe occluder"; wall.layer = 30; wall.transform.SetParent(root.transform, false);
            UnityEngine.Object.DestroyImmediate(wall.GetComponent<Collider>()); wall.transform.position = renderer.bounds.center + Vector3.back;
            wall.transform.localScale = Vector3.one * camera.orthographicSize * 3;
            var opaque = new Material(Shader.Find("Universal Render Pipeline/Unlit")); opaque.color = Color.black; wall.GetComponent<MeshRenderer>().sharedMaterial = opaque;
            var hidden = Frame(camera, target, "blue-behind-solid-occluder");
            Assert.That(hidden.Count(pixel => pixel.b > .5f), Is.Zero, "Tint must preserve real depth occlusion.");
            UnityEngine.Object.DestroyImmediate(opaque);
            adapter.enabled = false; renderer.GetPropertyBlock(block);
            Assert.That(renderer.sharedMaterial, Is.SameAs(fontMaterial)); Assert.That(block.GetFloat("_OwnedOriginalProbe"), Is.EqualTo(37));
            Assert.That(block.GetColor("_Color"), Is.EqualTo(Color.clear), "Disable restores the original property block rather than retaining the temporary text tint.");
            Assert.That(fontMaterial.shader, Is.SameAs(originalFontShader)); Assert.That(Shared, Is.EqualTo(baseline));
            CollectionAssert.AreEqual(positions, bodies.Select(body => body.position).ToArray()); CollectionAssert.AreEqual(velocities, bodies.Select(body => body.linearVelocity).ToArray());
            TestContext.WriteLine("WORLD_TINT_NATIVE red/blue actual Alpha8 GPU pixels, opaque occlusion, shared atlas and original renderer block restoration; no physics edits.");
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if (match != null) match.GetType().GetMethod("SetupScreen").Invoke(match, null);
            UnityEngine.Object.DestroyImmediate(isolation); preferences.Dispose();
        }
    }
}
#endif
