#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Read-only diagnostic: images and observed pixel differences are evidence, not a gameplay pass.
public sealed class SnakeWorldSnowCoverageDiagnosticsTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [Explicit("Controlled whole-world snow diagnostic, not match acceptance.")]
    [UnityTest, Timeout(180000)]
    public IEnumerator InspectDistantSavedRoadSamplesWithoutMovingTheCoverageAnchor()
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SnakeWorldSnowAudit" });
        var isolation = new GameObject("Isolated distant-road snow audit"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null; GameObject cameraObject = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnake/Scenes/TruckSnake.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession")).Cast<Component>().Single(); Call(match, "AllBots");
            var weather = match.GetComponent(Runtime("SportsChaosWeather")); var catalog = Get(weather, "presets"); var snow = ((Array)Get(catalog, "entries")).Cast<object>().Single(item => (string)Get(item, "displayName") == "Snow"); Call(catalog, "Select", Get(snow, "id"));
            Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), "Day") }); Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSeconds(6);
            var bridge = match.GetComponent(Runtime("SportsSnowSurface")); var surface = Get(bridge, "surface"); var adapter = Get(bridge, "adapter"); var coverage = (Component)Get(adapter, "_coverageComponent");
            var world = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSnake.SnakeWorld, Assembly-CSharp", true)).Cast<Component>().Single();
            var graph = Get(world, "graph"); var samples = ((IEnumerable)Get(graph, "edges")).Cast<object>().SelectMany(edge => ((IEnumerable)Get(edge, "samples")).Cast<object>()).ToArray(); Assert.That(samples.Length, Is.GreaterThan(0));
            var original = ((Camera[])Get(match, "cameras")).First(item => item.enabled);
            cameraObject = new GameObject("Read-only distant-road diagnostic camera"); var camera = cameraObject.AddComponent<Camera>(); camera.CopyFrom(original); camera.enabled = false; camera.tag = "Untagged"; camera.rect = new Rect(0,0,1,1); camera.orthographic = true; camera.orthographicSize = 38; camera.nearClipPlane = .1f; camera.farClipPlane = 500;
            var dataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime", true); var sourceData = original.GetComponent(dataType); var data = cameraObject.AddComponent(dataType); Call(data, "SetRenderer", (int)Get(sourceData, "m_RendererIndex"));
            var report = new StringBuilder(); report.AppendLine("Diagnostic only; no whole-match, human or split-screen acceptance.");
            report.AppendLine("world=" + Get(world, "playableBounds") + " usableSquareMetres=" + Get(world, "verifiedUsableSquareMetres") + " roadSamples=" + samples.Length);
            report.AppendLine("areaSize=" + Get(coverage, "areaSize") + " areaDepth=" + Get(coverage, "areaDepth") + " anchor=" + coverage.transform.position + " follow=" + ((Transform)Get(coverage, "followTarget")).name + " followPosition=" + ((Transform)Get(coverage, "followTarget")).position);
            var materials = ((IDictionary)Get(bridge, "runtimeRoads")).Values.Cast<Material>().ToArray(); var amounts = materials.Select(material => material.GetFloat("_CoverageAmount")).ToArray();
            var requested = new[] { Vector3.zero, new Vector3(1200,0,0), new Vector3(-1200,0,0), new Vector3(0,0,900), new Vector3(0,0,-900), new Vector3(1100,0,800), new Vector3(-1100,0,-800) };
            Time.timeScale = 0; string output = "Logs/TruckSports/PlaytestRepair/SnakeWorldSnowAudit"; Directory.CreateDirectory(output);
            for (int index = 0; index < requested.Length; index++)
            {
                var point = samples.OrderBy(sample => Vector3.ProjectOnPlane((Vector3)Get(sample, "position") - requested[index], Vector3.up).sqrMagnitude).First(); var position = (Vector3)Get(point, "position");
                camera.transform.SetPositionAndRotation(position + Vector3.up * 120, Quaternion.Euler(90,0,0)); var applied = Read(camera,320,180); Texture2D bare = null;
                try
                {
                    foreach (var material in materials) material.SetFloat("_CoverageAmount",0); bare = Read(camera,320,180);
                    int brighter = applied.GetPixels().Zip(bare.GetPixels(), (a,b) => a.grayscale - b.grayscale > .04f).Count(value => value);
                    report.AppendLine("sample=" + index + " road=" + Get(point,"roadId") + " position=" + position + " brighterRoadPixels=" + brighter + "/57600 anchor=" + coverage.transform.position);
                }
                finally { for(int i=0;i<materials.Length;i++) materials[i].SetFloat("_CoverageAmount",amounts[i]); UnityEngine.Object.DestroyImmediate(applied); if(bare!=null)UnityEngine.Object.DestroyImmediate(bare); }
                var image=Read(camera,1280,720); File.WriteAllBytes(output + "/sample-" + index + "-snow.png",image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
            }
            File.WriteAllText(output + "/observations.txt",report.ToString()); TestContext.WriteLine(report.ToString());
        }
        finally { Time.timeScale=1; if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject); if(match!=null)Call(match,"SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
    }
    static Texture2D Read(Camera camera,int width,int height)
    {
        var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);var prior=camera.targetTexture;var active=RenderTexture.active;
        try {camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();return image;}
        finally {camera.targetTexture=prior;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
    }
}
#endif
