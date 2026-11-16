#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace LWS.TruckSports
{
    // One opt-in Windows acceptance pass through production UI/input/rules.
    // Normal play never installs this component. It never injects scores/results.
    public sealed class SportsGameNightWindowsProbe : MonoBehaviour
    {
        [Serializable] sealed class Result { public string status = "running", purpose = "two actual matches", stage, error, report, telemetry; public int completedMatches; public bool softwareControllers = true; }
        [Serializable] sealed class SavedProfiles { public string scope; public SportsParticipant[] profiles; }
        readonly Result result = new Result();
        Gamepad host, second;
        SportsGameNightRuntime night;
        IDisposable preferences;
        string folder;
        double deadline;
        bool finished;
        InputSettings.BackgroundBehavior previousBackground;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("-tsGameNight") && !args.Contains("-tsGameNightNativeExit")) return;
            var root = new GameObject("Focused Windows game-night acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsGameNightWindowsProbe>();
        }
        void Check(bool value, string message) { if (value) return; result.error = message; throw new InvalidOperationException(message); }
        void Update() { if (!finished && deadline > 0 && Time.realtimeSinceStartupAsDouble > deadline) Finish(false, "Windows game-night deadline exceeded at " + result.stage); }
        IEnumerator Press(Gamepad pad, GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button)); yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null;
        }
        IEnumerator Move(Gamepad pad, Vector2 direction)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = direction }); yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null;
        }
        void Capture(string name)
        {
            var texture = ScreenCapture.CaptureScreenshotAsTexture(); Check(texture != null, "Screenshot unavailable: " + name);
            Check(name.EndsWith("720p") ? texture.width == 1280 && texture.height == 720 : texture.width == 1920 && texture.height == 1080, "Screenshot resolution differs from its label: " + name);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG()); Destroy(texture);
        }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tsGameNightOut");
            folder = at >= 0 && at + 1 < args.Length ? Path.GetFullPath(args[at + 1]) : Path.Combine(Application.persistentDataPath, "TruckSports/Validation/WindowsGameNight");
            if (args.Contains("-tsGameNightNativeExit"))
            {
                Directory.CreateDirectory(folder); Application.runInBackground = true;
                yield return new WaitForSecondsRealtime(2);
                var plainFront = FindAnyObjectByType<SportsFrontEnd>();
                Check(plainFront != null && !SportsProbeInputIsolation.Active, "Plain launch must use the real menu without development input isolation.");
                result.status = "quit requested"; result.purpose = "ordinary menu quit / no software-input isolation";
                result.softwareControllers = false;
                File.WriteAllText(Path.Combine(folder, "ordinary-quit.json"), JsonUtility.ToJson(result, true));
                plainFront.Action("quit"); yield break;
            }
            Directory.CreateDirectory(folder); deadline = Time.realtimeSinceStartupAsDouble + 480;
            Application.runInBackground = true; Screen.SetResolution(1280, 720, false);
            previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            bool reload = args.Contains("-tsGameNightReload");
            string savedPath = Path.Combine(folder, "saved-profiles.json");
            var saved = reload ? JsonUtility.FromJson<SavedProfiles>(File.ReadAllText(savedPath)) : new SavedProfiles { scope = "FoundationWindows" + Guid.NewGuid().ToString("N") };
            preferences = SportsPrefs.IsolateValidation(saved.scope); SportsParticipantRegistry.ForgetCache();
            // Automated input isolation deliberately skips normal auto-install.
            if (!reload && FindAnyObjectByType<SportsSessionTelemetry>() == null) gameObject.AddComponent<SportsSessionTelemetry>();
            host = InputSystem.AddDevice<Gamepad>("Windows game-night host"); second = InputSystem.AddDevice<Gamepad>("Windows game-night second player");
            bool success = false;
            try
            {
                if (reload)
                {
                    result.stage = "relaunch profile / statistics persistence";
                    foreach (var expected in saved.profiles)
                    {
                        var actual = SportsParticipantRegistry.Find(expected.id);
                        Check(actual != null && JsonUtility.ToJson(actual) == JsonUtility.ToJson(expected), "Saved profile or statistics changed after executable relaunch: " + expected.id);
                    }
                    Check(saved.profiles.Length == 2 && saved.profiles.All(p => p.statistics.matches == 2), "Both participants must retain two actual match results.");
                    result.purpose = "executable relaunch persistence";
                    yield return null; yield return null; yield return null;
                    var savedFront = FindAnyObjectByType<SportsFrontEnd>(); savedFront.Show(0); savedFront.Focus();
                    yield return Press(host, GamepadButton.South); yield return Press(host, GamepadButton.South); yield return Press(host, GamepadButton.North);
                    yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("persisted-statistics-720p");
                    result.completedMatches = 2; success = true; yield break;
                }
                result.stage = "controller title / join"; yield return null; yield return null; yield return null;
                Screen.SetResolution(1280, 720, false);
                var front = FindAnyObjectByType<SportsFrontEnd>(); Check(front != null, "Build did not launch the Truck Sports front end."); front.Show(0); front.Focus();
                yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("title-720p");
                yield return Press(host, GamepadButton.South); night = SportsGameNightRuntime.Instance;
                Check(night != null && night.State != null && night.State.HumanCount == 1, "Title Confirm did not open and join game night. Focus=" + Application.isFocused + "; padEnabled=" + host.enabled + "; selected=" + EventSystem.current?.currentSelectedGameObject?.name + "; phase=" + night?.State?.Phase);
                yield return Press(host, GamepadButton.South);
                yield return Press(second, GamepadButton.South); yield return Press(second, GamepadButton.West);
                Check(SportsControllerKeyboard.OwnerPad == second, "New profile keyboard has the wrong owner.");
                yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("keyboard-720p");
                yield return Move(second, Vector2.down); yield return Move(second, Vector2.down); yield return Press(second, GamepadButton.South);
                yield return Move(second, Vector2.down); yield return Move(second, Vector2.right); yield return Move(second, Vector2.right); yield return Press(second, GamepadButton.South);
                yield return Move(second, Vector2.up); yield return Move(second, Vector2.up); yield return Press(second, GamepadButton.South); yield return Press(second, GamepadButton.Start);
                Check(night.Profile(1)?.displayName == "ace", "Controller-created Windows profile was not committed.");
                yield return Press(host, GamepadButton.South); yield return Press(second, GamepadButton.South);
                yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("lobby-720p");
                if (args.Contains("-tsGameNightVisual"))
                {
                    result.purpose = "lobby visual retake only"; result.stage = "title canvas hidden under game-night lobby";
                    Check(!front.GetComponentInParent<Canvas>().enabled, "Title canvas still draws over the game-night lobby.");
                    yield return Press(host, GamepadButton.Start);
                    Check(night.State.Phase == SportsGameNightPhase.Selection && front.GetComponentInParent<Canvas>().enabled, "Sport selection did not restore its saved canvas.");
                    yield return null; yield return null;
                    var catalogue = front.catalogue; Check(catalogue.CategoryControl != null, "The controller category filter is missing.");
                    EventSystem.current.SetSelectedGameObject(catalogue.CategoryControl); yield return Press(host, GamepadButton.South);
                    Check(catalogue.CurrentCategory != SportsCatalogue.Category.All && catalogue.FilteredCount > 0 && catalogue.FilteredCount < catalogue.cards.Length, "Controller category selection did not filter the existing roster.");
                    var selectedCategory = catalogue.CurrentCategory; yield return Press(second, GamepadButton.South);
                    Check(catalogue.CurrentCategory == selectedCategory, "A nonhost controller changed the category.");
                    yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("category-filter-720p");
                    yield return Press(host, GamepadButton.East);
                    Check(night.State.Phase == SportsGameNightPhase.Lobby && night.LobbyVisible && !front.GetComponentInParent<Canvas>().enabled, "Controller Back did not return sport selection to the lobby.");
                    result.stage += "; host-only category filter / controller Back passed";
                    success = true; yield break;
                }
                foreach (string scene in args.Contains("-tsGameNightStartState") ? new[] { SportsNavigation.Whack } : new[] { SportsNavigation.DanceOff, SportsNavigation.Whack })
                {
                    result.stage = "select / configure " + scene;
                    yield return Press(host, GamepadButton.Start); Check(night.State.Phase == SportsGameNightPhase.Selection, "Host sport selection failed.");
                    front = FindAnyObjectByType<SportsFrontEnd>(); int page = front.Page;
                    yield return Press(second, GamepadButton.South); Check(front.Page == page && night.State.Phase == SportsGameNightPhase.Selection, "A nonhost controller operated sport selection.");
                    yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("selection-" + scene + "-720p");
                    // Select the real registered menu action; controller navigation
                    // and physical hardware are reported separately from this focus aid.
                    string action = scene == SportsNavigation.DanceOff ? "danceoff" : "whackamole";
                    front.Action(action); yield return null; yield return null; yield return null; yield return null;
                    var session = FindAnyObjectByType<SportsEventSession>(); Check(session != null && session.gameObject.scene.name == scene, "Registered sport action did not load " + scene);
                    session.setup.count = 4; session.option = 0; session.Save();
                    yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("setup-" + scene + "-720p");
                    yield return Press(host, GamepadButton.Start); yield return Press(second, GamepadButton.Start);
                    var start = session.menus.GetComponentsInChildren<SportsMenuAction>(true).FirstOrDefault(a => a.action == "start" && a.gameObject.activeInHierarchy);
                    Check(start != null, "A visible controller Start button is missing.");
                    EventSystem.current.SetSelectedGameObject(start.button.gameObject); yield return Press(host, GamepadButton.South);
                    Check(session.Running, "Controller Confirm did not start the configured match.");
                    result.stage = "actual match " + scene;
                    if (scene == SportsNavigation.Whack)
                    {
                        InputSystem.QueueStateEvent(host, new GamepadState { rightTrigger = .65f, leftStick = new Vector2(.35f, 0) });
                        yield return new WaitForSecondsRealtime(1);
                        Check(session.commands[0].sqrMagnitude > .05f && session.commands[1].sqrMagnitude < .001f, "Driving input leaked across human players.");
                        InputSystem.QueueStateEvent(host, new GamepadState());
                    }
                    yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("gameplay-" + scene + "-720p");
                    if (args.Contains("-tsGameNightStartState"))
                    {
                        result.purpose = "five-second startup-state check only";
                        var whack = (LWS.TruckWhackAMole.WhackMatch)session; float before = whack.rules.Clock;
                        yield return new WaitForSecondsRealtime(5);
                        result.stage = "running=" + session.Running + "; paused=" + session.Paused + "; screen=" + session.menus.Screen + "; timeScale=" + Time.timeScale + "; ready=" + session.Ready(0) + "/" + session.Ready(1) + "; clock=" + before + " -> " + whack.rules.Clock;
                        yield return new WaitForEndOfFrame(); Capture("startup-state-720p");
                        Check(session.Live && Time.timeScale > 0 && whack.rules.Clock > before + 1, "Startup simulation did not advance: " + result.stage);
                        SportsFocusPolicy.InstallScene().SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);
                        yield return null; yield return null;
                        Check(session.Paused && Time.timeScale == 0, "Existing focus policy did not pause human play.");
                        yield return Press(host, GamepadButton.Start);
                        Check(session.Live && Time.timeScale == 1, "Host controller did not resume the focus-paused match.");
                        result.stage += "; simulated focus pause / controller resume passed";
                        success = true; yield break;
                    }
                    while (!session.Finished) { Check(Time.realtimeSinceStartupAsDouble < deadline, "Sport did not reach its normal result: " + scene); yield return null; }
                    yield return null; yield return null; Check(night.State.Phase == SportsGameNightPhase.Results, "Official results did not reach the game-night owner.");
                    yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("results-" + scene + "-720p");
                    var back = session.menus.GetComponentsInChildren<SportsMenuAction>(true).FirstOrDefault(a => a.action == "front" && a.gameObject.activeInHierarchy);
                    Check(back != null, "A visible controller Return button is missing.");
                    EventSystem.current.SetSelectedGameObject(back.button.gameObject); yield return Press(host, GamepadButton.South);
                    yield return null; yield return null; Check(night.State.Phase == SportsGameNightPhase.Lobby, "Controller return did not restore the lobby.");
                    if (scene == SportsNavigation.DanceOff) { yield return Press(host, GamepadButton.South); yield return Press(second, GamepadButton.South); }
                }
                result.stage = "statistics / final report";
                yield return Press(second, GamepadButton.North); yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("statistics-720p");
                yield return Move(second, Vector2.right); yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("sport-records-720p"); yield return Press(second, GamepadButton.East);
                yield return Press(host, GamepadButton.Select); yield return Move(host, Vector2.right); yield return Press(host, GamepadButton.South);
                Check(night.State.Phase == SportsGameNightPhase.Ended, "Controller end-night confirmation failed.");
                yield return Press(host, GamepadButton.West); Screen.SetResolution(1920, 1080, false); yield return new WaitForSecondsRealtime(.3f); yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame(); Capture("final-standings-1080p");
                result.completedMatches = night.Report.Contents.completedMatches; Check(result.completedMatches == 2, "Both actual matches must be recorded.");
                result.report = night.Report.JsonPath; result.telemetry = SportsSessionTelemetry.CurrentFile;
                Check(File.Exists(result.report) && File.Exists(night.Report.SummaryPath), "Final reports are missing.");
                Check(!string.IsNullOrEmpty(result.telemetry) && File.Exists(result.telemetry), "The game-night telemetry stream is missing.");
                saved.profiles = new[] { night.Profile(0), night.Profile(1) };
                File.WriteAllText(savedPath, JsonUtility.ToJson(saved, true)); success = true;
            }
            finally { Finish(success, result.error); }
        }
        void Finish(bool success, string error)
        {
            if (finished) return; finished = true; result.status = success ? "passed" : "failed"; result.error = error;
            var args = Environment.GetCommandLineArgs();
            SportsSessionTelemetry.FlushNow(); File.WriteAllText(Path.Combine(folder, args.Contains("-tsGameNightReload") ? "windows-relaunch.json" : args.Contains("-tsGameNightVisual") ? "windows-visual.json" : args.Contains("-tsGameNightStartState") ? "windows-start-state.json" : "windows-game-night.json"), JsonUtility.ToJson(result, true));
            if (host != null && host.added) InputSystem.RemoveDevice(host); if (second != null && second.added) InputSystem.RemoveDevice(second);
            InputSystem.settings.backgroundBehavior = previousBackground;
            preferences?.Dispose(); SportsParticipantRegistry.ForgetCache(); Application.Quit(success ? 0 : 2);
        }
    }
}
#endif
