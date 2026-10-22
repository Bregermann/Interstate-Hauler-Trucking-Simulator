#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LWS.TruckBowling;
using LWS.TruckSports;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LWS.TruckDockDiving
{
    [DefaultExecutionOrder(2000)] public sealed class DockVenueProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsDockVenue") < 0) return;
            var root = new GameObject("Explicit Dock venue and mixed-source Windows acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<DockVenueProbe>();
        }
        DockMatch match; DockAudiencePresentation audience; string folder; int checks, errors; IDisposable prefs; Keyboard keyboard; Gamepad whole, shared; InputSettings original, injected;
        readonly StringBuilder log = new StringBuilder(); bool overview; int token = -1, neutralFrames; DockPhase priorPhase; readonly bool[] drove = new bool[4];
        Vector3 fixturePosition = new Vector3(56, 39, -23), fixtureFocus = new Vector3(0, 1, 64);
        void Check(bool ok, string message) { checks++; if (!ok) errors++; log.AppendLine((ok ? "PASS " : "FAIL ") + message); File.WriteAllText(Path.Combine(folder, "progress.txt"), log.ToString()); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) { errors++; log.AppendLine("ERROR " + message + "\n" + stack); } }
        void Neutral() { if (keyboard != null && keyboard.added) InputSystem.QueueStateEvent(keyboard, new KeyboardState()); if (whole != null && whole.added) InputSystem.QueueStateEvent(whole, new GamepadState()); if (shared != null && shared.added) InputSystem.QueueStateEvent(shared, new GamepadState()); }
        void Update()
        {
            if (match == null || !match.Running || match.Finished || match.Paused || overview) { if (keyboard != null) Neutral(); return; }
            int current = match.Round * 16 + match.Active;
            if (token != current || match.Phase == DockPhase.Approach && priorPhase != DockPhase.Approach) { token = current; neutralFrames = 3; }
            priorPhase = match.Phase; if (neutralFrames-- > 0) { Neutral(); return; }
            var player = match.setup.players[match.Active]; if (player.source == BowlingSource.AI) { Neutral(); return; }
            // Give the automated humans visibly different real driving inputs.
            // Saturated identical perfect runs can legitimately tie forever;
            // keep the game's existing extra-jump rules unchanged.
            float throttle = match.Phase == DockPhase.Approach ? match.Active == 0 ? 1 : .5f + match.Active * .09f : 0;
            float pressAt = match.Active == 0 ? 2.5f : 1.3f;
            bool action = match.Phase == DockPhase.Approach ? match.edge - (match.truck.body.position.z + 1.4f) <= pressAt : match.Phase == DockPhase.Replay && Time.realtimeSinceStartup % .7f < .2f;
            var keys = new List<Key>(); var one = new GamepadState(); var two = new GamepadState();
            if (player.source == BowlingSource.Keyboard) { if (throttle > 0) keys.Add(player.forward); if (action) keys.Add(match.actionKeys[match.Active]); }
            else if (player.source == BowlingSource.WholeController) { one.rightTrigger = throttle; if (action) one = one.WithButton(GamepadButton.South); }
            else if (player.source == BowlingSource.LeftStick) { two.leftStick = new Vector2(0, throttle); two.leftTrigger = action ? 1 : 0; }
            else { two.rightStick = new Vector2(0, throttle); two.rightTrigger = action ? 1 : 0; }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray())); InputSystem.QueueStateEvent(whole, one); InputSystem.QueueStateEvent(shared, two);
            if (match.Active < 4 && throttle > 0 && !drove[match.Active] && match.ReadDrive(match.Active).y > .5f)
            { drove[match.Active] = true; Check(Enumerable.Range(0, 4).Where(i => i != match.Active).All(i => match.ReadDrive(i).sqrMagnitude < .01f), "actual active source drives alone slot " + match.Active + " · " + player.source); }
        }
        void LateUpdate() { if (!overview || match == null) return; var camera = match.broadcast.view; camera.transform.position = fixturePosition; camera.transform.LookAt(fixtureFocus); camera.fieldOfView = 58; }
        IEnumerator Capture(string name) { yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png")); yield return new WaitForSecondsRealtime(.1f); }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-tsDockVenueOutput"); folder = index >= 0 ? args[index + 1] : SportsEvidencePaths.Folder("DockVenue"); Directory.CreateDirectory(folder);
            prefs = SportsPrefs.IsolateValidation("DockVenueWindows"); original = InputSystem.settings; injected = Instantiate(original); injected.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = injected; keyboard = InputSystem.AddDevice<Keyboard>(); whole = InputSystem.AddDevice<Gamepad>(); shared = InputSystem.AddDevice<Gamepad>(); Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<DockMatch>(SportsNavigation.Dock); yield return null; match = FindAnyObjectByType<DockMatch>(); audience = match.GetComponent<DockAudiencePresentation>();
            Check(audience != null && audience.Spectators == 464 && audience.BakedMeshes == 16, "actual saved venue renders 464 owned spectators using sixteen reused poses");
            Check(audience.variants.Length == 8 && audience.variants.All(v => v.fans.All(f => f.sharedMesh != null && f.GetComponent<Renderer>().sharedMaterials.All(m => m.shader.isSupported))), "all eight clothed character variants have real renderable mesh/material data");
            Check(audience.crowd.nearFans.Length == 24 && audience.variants.All(v => !v.root.gameObject.activeSelf), "24 animated close fans; pose-source rigs retired after baking");
            var water = GameObject.Find("NWH water plane · URP pool surface").GetComponent<Renderer>(); var offset = water.sharedMaterial.mainTextureOffset; yield return new WaitForSeconds(.3f); Check(water.sharedMaterial.mainTextureOffset != offset, "actual pool surface UVs animate through the existing DOTween water adapter");
            var poseIds = audience.crowd.batches.Select(b => b.idle.GetInstanceID()).ToArray();
            overview = true; match.menus.gameObject.SetActive(false); Time.timeScale = 0;
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080), new Vector2Int(1024, 768) }) { Screen.SetResolution(size.x, size.y, false); yield return Capture("labelled-venue-overview-" + size.x + "x" + size.y); }
            Screen.SetResolution(1920, 1080, false); fixturePosition = new Vector3(10, 7, -17); fixtureFocus = new Vector3(0, 2, -12); yield return Capture("labelled-owned-timber-approach");
            overview = false; match.menus.gameObject.SetActive(true); Time.timeScale = 1; Screen.SetResolution(1920, 1080, false);
            match.SetupScreen(); match.setup.count = 8; match.AllBots();
            for (int i = 0; i < 4; i++) { match.setup.players[i].source = i == 0 ? BowlingSource.Keyboard : i == 1 ? BowlingSource.WholeController : i == 2 ? BowlingSource.LeftStick : BowlingSource.RightStick; if (i > 0) Check(match.Assign(i, i == 1 ? whole : shared), "assign distinct whole pad/shared-stick source " + i); }
            Neutral(); yield return null; Check(match.StartMatch(), "four software humans and four AI start the original three-attempt competition");
            match.Pause(); float clock = Time.time; offset = water.sharedMaterial.mainTextureOffset; yield return new WaitForSecondsRealtime(.2f); Check(match.Paused && Time.time == clock && water.sharedMaterial.mainTextureOffset == offset, "pause freezes original simulation and flowing surface");
            InputSystem.RemoveDevice(whole); yield return null; Check(match.Paused && !match.Ready(1), "controller removal retains the paused match and marks its owner unready"); whole = InputSystem.AddDevice<Gamepad>(); Check(match.Assign(1, whole), "explicit replacement preserves whole-controller ownership"); Neutral(); yield return null; match.Resume(); Check(!match.Paused, "resume restores the same mixed-source match"); neutralFrames = 3; Time.timeScale = 2;
            var slang = match.GetComponent<DockSlang>(); var fx = match.GetComponent<SportsWaterInteraction>(); bool speech = false, splash = false; int attempts = 0; float limit = Time.realtimeSinceStartup + 400;
            while (!match.Finished && Time.realtimeSinceStartup < limit)
            {
                if (!speech && slang.group.alpha > .9f && match.Phase == DockPhase.Flight) { speech = true; Check(slang.Speaker == match.Active && slang.owner.text == match.setup.players[match.Active].name, "real airborne slang belongs to the active participant"); Time.timeScale = 0; yield return Capture("actual-owned-airborne-bubble"); Time.timeScale = 2; }
                if (!splash && fx.Entries > 0 && match.Phase == DockPhase.Splash) { splash = true; yield return new WaitForSeconds(.14f); Time.timeScale = 0; yield return Capture("actual-water-entry-and-foam"); Time.timeScale = 2; }
                if (match.CompletedAttempts != attempts) { attempts = match.CompletedAttempts; log.AppendLine("ATTEMPTS=" + attempts + " active=" + match.Active + " best=" + string.Join(",", match.best)); File.WriteAllText(Path.Combine(folder, "progress.txt"), log.ToString()); }
                yield return null;
            }
            Time.timeScale = 1; Neutral(); Check(match.Finished && match.CompletedAttempts >= 24 && match.Launches == match.Locks, "complete mixed human/AI match resolves all real physical launches exactly once");
            Check(drove.All(v => v) && Enumerable.Range(0, 4).All(i => match.best[i] > 0), "keyboard, whole pad and both shared sticks each physically drive and land a valid scored attempt");
            Check(speech && splash && audience.crowd.Reactions > 0, "real speech, water entry and crowd reactions occur during the competition"); yield return Capture("actual-mixed-match-results");
            match.SetupScreen(); match.AllBots(); Check(match.StartMatch() && match.CompletedAttempts == 0 && audience.crowd.batches.Select(b => b.idle.GetInstanceID()).SequenceEqual(poseIds), "rematch restores original rules and reuses the same sixteen crowd meshes"); yield return null; match.SetupScreen(); SportsNavigation.FrontEnd(); yield return null; yield return null;
            Check(FindAnyObjectByType<DockMatch>() == null && FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length == 1 && !Resources.FindObjectsOfTypeAll<Mesh>().Any(m => m.name.StartsWith("Owned runtime Dock audience pose")), "menu return cleans crowd meshes, water flow and original scene with one listener");
            log.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + "; actual composed Windows frames, software source ownership and original NWH/PhysX matches; no human/hardware/listening acceptance."); File.WriteAllText(Path.Combine(folder, "result.txt"), log.ToString()); Application.Quit(errors == 0 ? 0 : 1);
        }
        void OnDestroy() { Time.timeScale = 1; Application.logMessageReceived -= Error; if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); if (whole != null && whole.added) InputSystem.RemoveDevice(whole); if (shared != null && shared.added) InputSystem.RemoveDevice(shared); if (original != null) InputSystem.settings = original; if (injected != null) Destroy(injected); prefs?.Dispose(); }
    }
}
#endif
