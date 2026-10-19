#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckBowling;
namespace LWS.TruckSports
{
    // Explicit Editor acceptance: starts real saved modes and captures their
    // actual rendered opening. This is neither a full-match nor Windows test.
    public sealed class SportsMenuCaptureProbe : MonoBehaviour
    {
        [Serializable] public sealed class Manifest { public string[] paths; }
        [Serializable] public sealed class Entry
        { public string scene, path, authority, result, screenshot720, screenshot1080; public int exceptions, listeners, cameras; }
        [Serializable] public sealed class Receipt { public string evidence; public Entry[] scenes; public int errors; }
        readonly List<Entry> entries = new List<Entry>();
        readonly List<string> problems = new List<string>();
        IDisposable preferences;
        string folder; Entry current;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
        {
            if (!UnityEditor.SessionState.GetBool("Sports.MenuCapture.Run", false)) return;
            UnityEditor.SessionState.SetBool("Sports.MenuCapture.Run", false);
            var root = new GameObject("Explicit collection saved-scene opening capture"); DontDestroyOnLoad(root);
            root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<SportsMenuCaptureProbe>();
        }
        void Awake() { preferences = SportsPrefs.IsolateValidation("MenuCapture"); SportsParticipantRegistry.ForgetCache(); }
        void Error(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            if (current != null) current.exceptions++;
            problems.Add((current?.scene ?? "transition") + ": " + message + "\n" + stack); Save();
        }
        void Save()
        {
            if (folder == null) return;
            File.WriteAllText(folder + "/receipt.json", JsonUtility.ToJson(new Receipt {
                evidence = "Native saved scene launch, short real AI simulation and Camera.Render/UI composite. Not full-match, human, hardware, Windows or performance acceptance.",
                scenes = entries.ToArray(), errors = problems.Count }, true));
            File.WriteAllLines(folder + "/errors.txt", problems);
        }
        IEnumerator Start()
        {
            folder = Path.GetFullPath("Logs/TruckSports/EssentialRelease/"+UnityEditor.SessionState.GetString("Sports.MenuCapture.Batch","CollectionOpenings")); Directory.CreateDirectory(folder);
            File.WriteAllText(folder + "/result.txt", "RUNNING " + DateTime.UtcNow.ToString("o"));
            Application.logMessageReceived += Error;
            var manifest = JsonUtility.FromJson<Manifest>(UnityEditor.SessionState.GetString("Sports.MenuCapture.Paths", ""));
            foreach (string path in manifest.paths)
            {
                string scene = Path.GetFileNameWithoutExtension(path); if (scene == "TruckSports") continue;
                current = new Entry { scene = scene, path = path, result = "LOADING" }; entries.Add(current); Save();
                Time.timeScale = 1;
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null; yield return new WaitForSecondsRealtime(.15f);
                Component authority = null; Canvas canvas = null; bool started = false;
                try
                {
                    var eventMatch = FindAnyObjectByType<SportsEventSession>();
                    if (eventMatch != null)
                    {
                        authority = eventMatch;
                        eventMatch.setup.count = Mathf.Clamp(4, eventMatch.MinimumPlayers, eventMatch.MaximumPlayers);
                        if (eventMatch.TeamSport) { eventMatch.teamA = eventMatch.teamB = eventMatch.MaximumPlayers >= 12 ? 3 : 2; }
                        eventMatch.NormalizeSetup(); eventMatch.AllBots(); started = eventMatch.StartMatch(); canvas = eventMatch.menus.GetComponent<Canvas>();
                    }
                    else
                    {
                        authority = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude).FirstOrDefault(c =>
                            c.GetType().Namespace != null && c.GetType().Namespace.StartsWith("LWS.Truck") && c.GetType().GetMethod("StartMatch") != null);
                        if (authority == null) throw new InvalidOperationException("No saved sport authority with StartMatch found.");
                        PrepareLegacy(authority); var method = authority.GetType().GetMethod("StartMatch");
                        var result = method.Invoke(authority, method.GetParameters().Select(p => p.DefaultValue).ToArray());
                        started = !(result is bool value) || value;
                        var menu = (Component)Field(authority, "menus"); canvas = menu.GetComponent<Canvas>();
                        if(canvas==null)canvas=menu.GetComponentInParent<Canvas>();if(canvas==null)canvas=menu.GetComponentInChildren<Canvas>(true);
                        if(canvas==null&&Field(menu,"pages") is Array pages)foreach(var page in pages){if(page is GameObject root){canvas=root.GetComponentInParent<Canvas>();if(canvas!=null)break;}}
                        if(canvas==null)throw new InvalidOperationException("Saved legacy menu pages have no parent canvas.");
                    }
                    current.authority = authority.GetType().FullName;
                    if (!started) throw new InvalidOperationException("AI configuration did not start the saved scene.");
                    current.result = "STARTED";
                }
                catch (Exception failure) { current.result = "FAILED START"; problems.Add(scene + ": " + failure); }
                if (started)
                {
                    Time.timeScale = scene == "TruckDanceOff" ? 1 : 3;
                    yield return new WaitForSecondsRealtime(scene == "TruckDanceOff" ? 3.5f : 2.2f);
                    try
                    {
                        var cameras = authority is SportsEventSession session ? session.cameras : FindObjectsByType<Camera>(FindObjectsInactive.Exclude)
                            .Where(c => c.enabled && c.targetTexture == null && c.cameraType == CameraType.Game).ToArray();
                        current.cameras = cameras.Count(c => c != null && c.enabled);
                        current.listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length;
                        if (current.cameras == 0 || current.listeners != 1) throw new InvalidOperationException("Expected visible gameplay cameras and exactly one listener.");
                        current.screenshot720 = folder + "/" + scene + "-1280x720.png";
                        current.screenshot1080 = folder + "/" + scene + "-1920x1080.png";
                        SportsControlledCapture.Save(canvas, cameras, 1280, 720, current.screenshot720);
                        SportsControlledCapture.Save(canvas, cameras, 1920, 1080, current.screenshot1080);
                        current.result = current.exceptions == 0 ? "OPENING CAPTURED" : "RUNTIME ERRORS";
                    }
                    catch (Exception failure) { current.result = "FAILED CAPTURE"; problems.Add(scene + ": " + failure); }
                }
                Save(); yield return null;
            }
            current = null; Time.timeScale = 1; SportsNavigation.FrontEnd(); yield return null; yield return null;
            Save(); File.WriteAllText(folder + "/result.txt", "SCENES=" + entries.Count + " ERRORS=" + problems.Count + " " + DateTime.UtcNow.ToString("o"));
            Application.logMessageReceived -= Error; UnityEditor.EditorApplication.isPlaying = false;
        }
        static object Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        static void AIArray(object owner, string name, string member)
        {
            if (!(Field(owner, name) is Array players)) return;
            foreach (var player in players) { var field = player.GetType().GetField(member); if (field != null) field.SetValue(player, Enum.Parse(field.FieldType, "AI")); }
        }
        static void PrepareLegacy(Component owner)
        {
            string type = owner.GetType().Name;
            var mode = owner.GetType().GetField("mode");
            if (mode != null && mode.FieldType.IsEnum && Enum.GetNames(mode.FieldType).Contains("AIVsAI")) mode.SetValue(owner, Enum.Parse(mode.FieldType, "AIVsAI"));
            AIArray(owner, "players", type == "BilliardsSession" ? "control" : "source");
            if (owner is BowlingMatch bowling) { bowling.PresetAI(4, BowlingMode.Simultaneous); return; }
            var allAI = owner.GetType().GetMethod("AllAI");
            if (allAI != null) { var setup = Field(owner, "setup"); setup?.GetType().GetField("count")?.SetValue(setup, 4); allAI.Invoke(owner, null); }
        }
        void OnDestroy() { Application.logMessageReceived -= Error; preferences?.Dispose(); SportsParticipantRegistry.ForgetCache(); Time.timeScale = 1; }
    }
}
#endif
