#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ImpossibleBroadcastDiagnosisTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static string PathOf(Transform item) => item.parent == null ? item.name : PathOf(item.parent) + "/" + item.name;

    [Serializable] sealed class CameraFacts
    {
        public string scene, label, position, rotation, primaryBounds, configuration;
        public bool orthographic;
        public float orthoSize, fov, projectedHeight;
        public string[] sightHits, nearbyMeshes;
    }

    [UnityTest, Timeout(150000)]
    public IEnumerator RecordYogaAndBullCameraOccludersWithoutChangingTheirPresentation()
    {
        var lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "ImpossibleCameraDiagnosis" + Guid.NewGuid().ToString("N") });
        GameObject isolation = new GameObject("Impossible camera diagnostic original input isolation");
        UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        Component match = null;
        string folder = Path.GetFullPath("Logs/TruckSports/Impossible07/BroadcastDiagnosisV1"); Directory.CreateDirectory(folder);
        try
        {
            foreach (string name in new[] { "TruckYoga", "TruckMechanicalBull" })
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                match = (Component)UnityEngine.Object.FindAnyObjectByType(Sports("SportsEventSession"));
                Assert.That(match, Is.Not.Null); Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                float end = Time.realtimeSinceStartup + 6;
                while (Time.realtimeSinceStartup < end) yield return null;
                var cameras = (Camera[])Get(match, "cameras");
                foreach (Camera camera in cameras.Where(item => item.enabled))
                {
                    var framing = camera.GetComponent(Sports("SportsBroadcastFraming"));
                    Assert.That(framing, Is.Not.Null);
                    var bounds = (Bounds)Get(framing, "PrimaryBounds");
                    var direction = bounds.center - camera.transform.position;
                    var facts = new CameraFacts
                    {
                        scene = name, label = "unchanged-natural-camera-diagnostic",
                        position = camera.transform.position.ToString("R"), rotation = camera.transform.eulerAngles.ToString("R"),
                        primaryBounds = bounds.ToString("R"), configuration = JsonUtility.ToJson(Get(framing, "Configuration")),
                        orthographic = camera.orthographic, orthoSize = camera.orthographicSize, fov = camera.fieldOfView,
                        projectedHeight = (float)Get(framing, "ProjectedTruckHeight"),
                        sightHits = Physics.RaycastAll(camera.transform.position, direction.normalized, direction.magnitude, ~0, QueryTriggerInteraction.Ignore)
                            .OrderBy(hit => hit.distance).Take(12).Select(hit => PathOf(hit.collider.transform) + " distance=" + hit.distance.ToString("R") + " point=" + hit.point.ToString("R")).ToArray(),
                        nearbyMeshes = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                            .Where(mesh => mesh.enabled && mesh.gameObject.activeInHierarchy)
                            .OrderBy(mesh => Vector3.Distance(mesh.bounds.ClosestPoint(camera.transform.position), camera.transform.position)).Take(18)
                            .Select(mesh => PathOf(mesh.transform) + " bounds=" + mesh.bounds.ToString("R") + " containsCamera=" + mesh.bounds.Contains(camera.transform.position)).ToArray()
                    };
                    File.WriteAllText(Path.Combine(folder, name + "-camera.json"), JsonUtility.ToJson(facts, true));
                    TestContext.WriteLine("IMPOSSIBLE_CAMERA_DIAG " + JsonUtility.ToJson(facts));
                }
                var save = Sports("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5);
                foreach (int width in new[] { 1280, 1920 })
                    save.Invoke(null, new object[] { match, width, width == 1280 ? 720 : 1080, Path.Combine(folder, name + "-" + width + "-controlled.png"), null });
                Call(match, "SetupScreen"); match = null;
            }
            TestContext.WriteLine("DIAGNOSTIC ONLY: camera values and controlled native composites recorded; no camera acceptance or Windows/human verification inferred.");
        }
        finally
        {
            Time.timeScale = 1; if (match != null) Call(match, "SetupScreen");
            UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose();
        }
    }
}
#endif
