#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckBowling;
using LWS.TruckSports;

namespace LWS.TruckKickball
{
    // Explicit development acceptance only. Uses real scene, input routing and PhysX.
    public sealed class KickballWaitingRepairProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsKickballWaiting") < 0) return;
            var root = new GameObject("Owned Kickball waiting acceptance"); DontDestroyOnLoad(root);
            root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<KickballWaitingRepairProbe>();
        }
        public int Checks { get; private set; }
        public int Errors { get; private set; }
        public bool Complete { get; private set; }
        public string Folder;
        KickballMatch match; Gamepad whole, shared; Keyboard keys; Mouse mouse;
        InputSettings oldSettings, settings; IDisposable preferences;
        readonly StringBuilder log = new StringBuilder(); float deadline;
        void Check(bool value, string message)
        {
            Checks++; if (!value) Errors++;
            log.AppendLine((value ? "PASS " : "FAIL ") + message);
            File.WriteAllText(Path.Combine(Folder, "progress.txt"), log.ToString());
        }
        void Error(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;
            Errors++; log.AppendLine("ERROR " + message + "\n" + stack);
        }
        IEnumerator Start() { if (!Application.isEditor) yield return Run(); }
        public IEnumerator Run()
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tsKickballOutput");
            Folder = Folder ?? (at >= 0 ? args[at + 1] : SportsEvidencePaths.Folder("PlaytestRepair/KickballWaiting"));
            Directory.CreateDirectory(Folder); deadline = Time.realtimeSinceStartup + 180;
            preferences = SportsPrefs.IsolateValidation("KickballWaitingRepair"); Application.logMessageReceived += Error;
            oldSettings = InputSystem.settings; settings = Instantiate(oldSettings);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = settings;
#if UNITY_EDITOR
            // The native runner has no focused Game View. Route only this
            // temporary settings clone to it, like the existing cab-look probe.
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            whole = InputSystem.AddDevice<Gamepad>(); shared = InputSystem.AddDevice<Gamepad>(); keys = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            yield return SportsProbeScene.Ensure<KickballMatch>(SportsNavigation.Kickball); yield return null;
            match = FindAnyObjectByType<KickballMatch>(); match.AllBots(); match.option = 3;
            foreach (var preset in match.weatherPresets.entries) if (preset.displayName == "Clear") { match.weatherPresets.Select(preset.id); break; }
            SportsCameraOptions.Set(false);
            SportsPrefs.SetInt(KickballAIPlayback.Preference, 0);
            match.setup.players[1].source = BowlingSource.WholeController; match.setup.players[1].pad = whole;
            match.setup.players[2].source = BowlingSource.Keyboard;
            var cluster = BowlingSetup.Defaults()[1]; var keyboardPlayer = match.setup.players[2];
            keyboardPlayer.forward = cluster.forward; keyboardPlayer.back = cluster.back;
            keyboardPlayer.left = cluster.left; keyboardPlayer.right = cluster.right; match.actionKeys[2] = Key.RightShift;
            match.setup.players[3].source = BowlingSource.LeftStick; match.setup.players[3].pad = shared;
            match.setup.players[4].source = BowlingSource.RightStick; match.setup.players[4].pad = shared;
            match.RefreshExtraUI();
            Check(match.aiPlayback != null && match.paceChoice != null && match.pauseViewChoice != null && match.pausePaceChoice != null,
                "saved scene binds selectable setup/pause pacing and view controls");
            if (!Application.isEditor) yield return Capture("setup");
            while (match.ViewMode != 2) match.ExtraAction("view");
            Check(match.StartMatch(), "four software humans and eight AI start using existing ownership validation");
            yield return new WaitForSecondsRealtime(.25f);
            Check(match.views.Humans == 4 && match.WaitingToBat(1) && match.WaitingToBat(2) && match.WaitingToBat(3) && match.WaitingToBat(4),
                "all four assigned humans are waiting behind the actual AI opening kicker");
            Check(!match.aiPlayback.Accelerating && Time.timeScale == 1, "normal pacing is the default");
            var bodies = new Vector3[4]; for (int v = 0; v < 4; v++) bodies[v] = match.agents[match.views.owners[v]].truck.body.position;
            InputSystem.QueueStateEvent(whole, new GamepadState { rightStick = Vector2.right });
            yield return new WaitForSecondsRealtime(.3f);
            Check(Mathf.Abs(match.presentation.looks[0].Yaw) > 3 && Mathf.Abs(match.presentation.looks[1].Yaw) < .1f,
                "whole pad pans only its own waiting cab");
            Neutral(); yield return new WaitForSecondsRealtime(.15f);
            InputSystem.QueueStateEvent(keys, new KeyboardState(keyboardPlayer.right));
            yield return new WaitForSecondsRealtime(.3f);
            log.AppendLine("KEYBOARD fixture current=" + Keyboard.current?.name + " enabled=" + keys.enabled + " key=" + keyboardPlayer.right + " held=" + keys[keyboardPlayer.right].isPressed + " drive=" + match.ReadDrive(2) + " owner=" + match.presentation.looks[1].Owner + " yaw=" + match.presentation.looks[1].Yaw);
            Check(Mathf.Abs(match.presentation.looks[1].Yaw) > 3, "second keyboard participant looks with their own assigned key cluster");
            Neutral(); yield return new WaitForSecondsRealtime(.15f);
            float beforeMouse = match.presentation.looks[1].Yaw, otherMouse = match.presentation.looks[0].Yaw;
            for (int frame = 0; frame < 5; frame++) { InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2, delta = new Vector2(12, 0) }); yield return null; }
            Check(match.presentation.looks[1].Yaw > beforeMouse + 1 && Mathf.Abs(match.presentation.looks[0].Yaw - otherMouse) < .1f,
                "first keyboard owns mouse look even when a controller occupies the first viewport");
            Neutral(); yield return new WaitForSecondsRealtime(.15f);
            InputSystem.QueueStateEvent(shared, new GamepadState { leftStick = Vector2.left });
            yield return new WaitForSecondsRealtime(.3f);
            Check(match.presentation.looks[2].Yaw < -3 && Mathf.Abs(match.presentation.looks[3].Yaw) < .1f,
                "left shared stick cannot pan the right participant's camera");
            Neutral(); yield return new WaitForSecondsRealtime(.15f);
            InputSystem.QueueStateEvent(shared, new GamepadState { rightStick = Vector2.right });
            yield return new WaitForSecondsRealtime(.3f);
            Check(match.presentation.looks[3].Yaw > 3, "right shared stick pans its own waiting camera");
            bool parked = true; for (int v = 0; v < 4; v++) parked &= Vector3.Distance(bodies[v], match.agents[match.views.owners[v]].truck.body.position) < .02f;
            Check(parked, "look input never drives the four benched trucks"); Neutral();
            int compact = 0; foreach (var rect in match.menus.hud.GetComponentsInChildren<RectTransform>()) if (rect.name == "Compact owned viewport score") compact++;
            Check(compact == 4 && !match.menus.viewTexts[0].gameObject.activeSelf, "four owned compact cards replace the old multiline split-screen strips");
            if (!Application.isEditor) yield return Capture("four-waiting-cabs");
            // The shipped option, not a probe-only time multiplier.
            match.ExtraAction("aipace"); yield return null;
            Check(KickballAIPlayback.Enabled && match.aiPlayback.Accelerating && Time.timeScale == 2,
                "selectable fast-forward runs genuine AI-only play at 2x");
            Check(SportsPrefs.GetInt(KickballAIPlayback.Preference) == 1 && match.pausePaceChoice.text.Contains("2×"),
                "preference persists and setup/pause captions agree");
            match.Pause(1); yield return new WaitForSecondsRealtime(.15f); float clock = match.Clock;
            yield return new WaitForSecondsRealtime(.15f);
            Check(match.Paused && Time.timeScale == 0 && !match.aiPlayback.Accelerating && match.Clock == clock,
                "pause releases pacing without unpausing simulation");
            if (!Application.isEditor) yield return Capture("pause");
            // Pause view selection is a real menu action, not a separate camera.
            while (match.ViewMode != 1) match.ExtraAction("view");
            match.Resume(); yield return new WaitForSecondsRealtime(.2f);
            InputSystem.QueueStateEvent(shared, new GamepadState { leftStick = Vector2.left });
            yield return new WaitForSecondsRealtime(.3f);
            Check(match.presentation.looks[2].Yaw < -3 && match.cameras.Length == 4,
                "waiting chase view supports independent look after a pause camera change"); Neutral();
            while (match.rules.Batter == 0 && Time.realtimeSinceStartup < deadline - 70) yield return null;
            Check(match.rules.Batter != 0 && match.rules.CompletedPlays > 0 && match.rules.Pitches > 0,
                "actual AI pitches and contacts advance to the next kicker without injected results");
            yield return null;
            Check(!match.WaitingToBat(1) && !match.aiPlayback.Accelerating && Time.timeScale == 1,
                "fast-forward stops automatically before the human batting turn");
            var guard = new GameObject("Explicit ordinary shared-stick cab contract").AddComponent<SportsCabLook>();
            var guarded = guard.Apply(Quaternion.identity, match.setup.players[3], 3, false, true, 1, false);
            Check(guarded == Quaternion.identity && guard.Yaw == 0, "ordinary shared-stick cab controls retain the existing no-free-look driving contract"); Destroy(guard.gameObject);
            // Shared-pad protection remains the normal driving contract.
            while (match.ViewMode != 2) match.ExtraAction("view"); yield return null;
            match.Pause(1); InputSystem.RemoveDevice(whole); yield return null;
            Check(match.Paused && Time.timeScale == 0, "disconnect during pause cannot restart accelerated play");
            whole = InputSystem.AddDevice<Gamepad>();
            Check(match.Assign(1, whole), "explicit replacement restores the same participant"); match.Resume(); yield return null;
            Check(match.Ready(1) && !match.Paused && Time.timeScale == 1, "reconnect preserves the human turn at normal speed");
            match.SetupScreen(); yield return null;
            Check(Time.timeScale == 1 && !match.aiPlayback.Accelerating, "setup restores normal time and releases speed ownership");
            match.AllBots(); Check(match.StartMatch(), "AI-only rematch starts with the retained pacing selection"); yield return null;
            Check(match.rules.Scores[0] == 0 && match.rules.Scores[1] == 0 && match.rules.CompletedPlays == 0 && match.aiPlayback.Accelerating,
                "rematch resets scores/plays while restoring chosen AI pacing");
            match.ExtraAction("aipace"); yield return null;
            Check(Time.timeScale == 1 && !match.aiPlayback.Accelerating && !KickballAIPlayback.Enabled,
                "normal playback can be restored during a match");
            match.SetupScreen(); match.AllBots();
            match.setup.players[1].source = BowlingSource.WholeController; match.setup.players[1].pad = whole;
            match.setup.players[6].source = BowlingSource.Keyboard;
            match.ExtraAction("aipace"); Check(match.StartMatch(), "mixed opposing human teams start through normal source validation"); yield return null;
            Check(match.WaitingToBat(1) && !match.WaitingToBat(6) && !match.aiPlayback.Accelerating && Time.timeScale == 1,
                "a waiting human cannot fast-forward another human's active fielding role");
            match.SetupScreen(); Complete = true;
            log.AppendLine("CHECKS=" + Checks + " ERRORS=" + Errors + "; synthetic device acceptance, not physical-controller or listening acceptance");
            File.WriteAllText(Path.Combine(Folder, "result.txt"), log.ToString());
            if (!Application.isEditor) Application.Quit(Errors == 0 ? 0 : 1);
        }
        void Neutral()
        {
            if (whole != null && whole.added) InputSystem.QueueStateEvent(whole, new GamepadState());
            if (shared != null && shared.added) InputSystem.QueueStateEvent(shared, new GamepadState());
            if (keys != null && keys.added) InputSystem.QueueStateEvent(keys, new KeyboardState());
            if (mouse != null && mouse.added) InputSystem.QueueStateEvent(mouse, new MouseState());
        }
        IEnumerator Capture(string label)
        {
            foreach (int width in new[] { 1280, 1920 })
            {
                Screen.SetResolution(width, width * 9 / 16, false); yield return new WaitForSecondsRealtime(.2f);
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(Folder, label + "-" + width + ".png"));
                yield return new WaitForSecondsRealtime(.1f);
            }
        }
        void Update()
        {
            if (Application.isEditor || Complete || deadline <= 0 || Time.realtimeSinceStartup <= deadline) return;
            Check(false, "waiting-camera/pacing acceptance watchdog expired"); Complete = true;
            File.WriteAllText(Path.Combine(Folder, "result.txt"), log + "CHECKS=" + Checks + " ERRORS=" + Errors); Application.Quit(1);
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= Error;
            if (match != null) match.SetupScreen();
            if (whole != null && whole.added) InputSystem.RemoveDevice(whole);
            if (shared != null && shared.added) InputSystem.RemoveDevice(shared);
            if (keys != null && keys.added) InputSystem.RemoveDevice(keys);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (oldSettings != null) InputSystem.settings = oldSettings;
            if (settings != null) Destroy(settings); preferences?.Dispose(); Time.timeScale = 1;
        }
    }
}
#endif
