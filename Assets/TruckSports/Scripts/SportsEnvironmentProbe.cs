#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DigitalRuby.WeatherMaker;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports
{
    // Explicit development acceptance only. Captures the real Windows backbuffer and UI.
    public sealed class SportsEnvironmentProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsEnvironment") < 0) return;
            var root = new GameObject("Explicit environment acceptance"); DontDestroyOnLoad(root);
            if (!SportsProbeInputIsolation.Active) root.AddComponent<SportsProbeInputIsolation>();
            root.AddComponent<SportsEnvironmentProbe>();
        }
        readonly StringBuilder log = new StringBuilder();
        string folder; int checks, errors; bool complete;
        IDisposable preferences; Keyboard keyboard; Gamepad menuPad;
        void Check(bool pass, string message)
        {
            checks++; if (!pass) errors++; log.AppendLine((pass ? "PASS " : "FAIL ") + message);
            File.WriteAllText(Path.Combine(folder, "progress.txt"), log.ToString());
        }
        void Error(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;
            errors++; log.AppendLine("ERROR " + message); File.WriteAllText(Path.Combine(folder, "progress.txt"), log.ToString());
        }
        IEnumerator Start()
        {
            var arguments = Environment.GetCommandLineArgs(); int index = Array.IndexOf(arguments, "-tsEnvironmentOutput");
            if (index < 0 || index + 1 == arguments.Length) throw new ArgumentException("Provide -tsEnvironmentOutput.");
            folder = Path.GetFullPath(arguments[index + 1]); Directory.CreateDirectory(folder);
            preferences = SportsPrefs.IsolateValidation("EnvironmentWindows"); Application.logMessageReceived += Error;
            keyboard = InputSystem.AddDevice<Keyboard>();
            if (Array.IndexOf(arguments, "verify-time-persistence") >= 0)
            {
                Check(SportsTimeOfDay.Selection == SportsTimeChoice.Night, "saved independent time preference survives a fresh Windows process");
                Finish(); yield break;
            }
            foreach (string scene in new[] { SportsNavigation.Keepaway, SportsNavigation.FieldGoal, SportsNavigation.SkyTow, SportsNavigation.Terrier, SportsNavigation.Snake })
            {
                SportsNavigation.LoadSport(scene); yield return null; yield return null;
                var match = FindAnyObjectByType<SportsEventSession>(); var weather = match.GetComponent<SportsChaosWeather>();
                Check(weather != null && weather.matchTime != null, scene + " saved Weather Maker match-time binding");
                if (weather == null || weather.matchTime == null) continue;
                var pickers = match.menus.GetComponentsInChildren<SportsWeatherPicker>(true);
                Check(pickers.Count(picker => picker.timeOfDay) == 1 && pickers.Count(picker => !picker.timeOfDay) == 1, scene + " one actual Heat weather control and one time control");
                var clock = pickers.Single(picker => picker.timeOfDay); var forecast = pickers.Single(picker => !picker.timeOfDay);
                match.AllBots(); SportsCameraOptions.Set(false);
                yield return Capture(scene + "-setup");
                if (scene == SportsNavigation.FieldGoal) yield return MenuNavigation(clock, forecast);
                var cases = scene == SportsNavigation.FieldGoal
                    ? new[] { "Clear|Morning", "Clear|Day", "Clear|Evening", "Clear|Night", "Rain|Evening", "Thunderstorm|Night", "Snow|Morning", "Blizzard|Night", "Hurricane|Day" }
                    : new[] { "Clear|Day", "Clear|Night" };
                foreach (string combination in cases)
                {
                    match.SetupScreen(); string[] choice = combination.Split('|');
                    clock.Choose((int)Enum.Parse<SportsTimeChoice>(choice[1]));
                    int entry = Array.FindIndex(forecast.catalog.entries, item => item.displayName == choice[0]);
                    Check(entry >= 0, scene + " installed forecast " + choice[0]); if (entry < 0) continue;
                    forecast.Choose(entry + 1);
                    Check(match.StartMatch(), scene + " starts " + combination);
                    yield return new WaitForSeconds(choice[0] == "Hurricane" ? 9 : 2);
                    var binding = weather.matchTime;
                    Check(binding.Applied.ToString() == choice[1] && Mathf.Abs(binding.cycle.TimeOfDay - SportsTimeOfDay.Seconds(binding.Applied)) < .01f, scene + " real vendor clock " + combination);
                    Check(!binding.daylightFill.enabled && binding.cycle.Speed == 0 && binding.cycle.NightSpeed == 0, scene + " fixed fill off and short-match time frozen");
                    Check(weather.PresetId == forecast.catalog.entries[entry].id, scene + " selected weather independently matches installed profile");
                    int revision = binding.Revision; binding.Configure(-7654, match.MatchId);
                    Check(binding.Revision == revision, scene + " heat/attempt seed does not reroll active match time");
                    var lights = weather.vendor.GetComponentsInChildren<Light>(true).Where(light => light.type == LightType.Directional).ToArray();
                    log.AppendLine("LIGHTS " + scene + " " + combination + " " + string.Join("; ", lights.Select(light => light.name + " intensity=" + light.intensity + " rotation=" + light.transform.eulerAngles)) + " ambient=" + RenderSettings.ambientLight);
                    var samples = new List<float>(60);
                    for (int frame = 0; frame < 60; frame++) { yield return null; samples.Add(Time.unscaledDeltaTime * 1000); }
                    samples.Sort(); log.AppendLine("FRAME_TIME " + scene + " " + combination + " medianMs=" + samples[30].ToString("F2") + " p95Ms=" + samples[56].ToString("F2") + " samples=60; whole-player frame interval, not isolated GPU/CPU profiling.");
                    yield return Capture(scene + "-" + combination.Replace('|', '-'));
                    if (choice[0] == "Hurricane")
                    {
                        Check(weather.precipitation.SnowScript.Intensity < .001f && weather.precipitation.SnowScript.GetComponentsInChildren<ParticleSystem>(true).Sum(system => system.particleCount) == 0, "prior Blizzard flakes have actually retired; Hurricane is emitting rain");
                        Check(weather.precipitation.RainScript.Intensity > .1f, "Hurricane has actual rain emission");
                    }
                }
                if (scene == SportsNavigation.SkyTow)
                {
                    match.SetupScreen(); match.AllBots();
                    match.setup.players[0].source = LWS.TruckBowling.BowlingSource.Keyboard; match.setup.players[1].source = LWS.TruckBowling.BowlingSource.Keyboard;
                    SportsTimeOfDay.Select(SportsTimeChoice.Night); Check(match.StartMatch(), "two software-keyboard humans start Arctic night"); yield return new WaitForSeconds(2);
                    var cameras = match.cameras.Where(camera => camera.enabled).ToArray(); Check(cameras.Length == 2, "two independent human views retain all screen space at night");
                    Check(cameras.All(camera => weather.vendor.AllowCameras.Contains(camera)), "both night viewports share the same vendor weather world");
                    yield return Capture(scene + "-two-software-humans-night");
                }
                match.SetupScreen();
            }
            SportsTimeOfDay.Select(SportsTimeChoice.Night); SportsNavigation.FrontEnd(); yield return null; yield return null;
            Check(SceneManager.GetActiveScene().name == SportsNavigation.Front, "returns to the real sport-selection menu after repeated weather/time matches");
            Finish();
        }
        IEnumerator Capture(string label)
        {
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                Screen.SetResolution(size.x, size.y, false); yield return new WaitForSecondsRealtime(.35f); yield return new WaitForEndOfFrame();
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                Check(texture.width == size.x && texture.height == size.y, label + " actual Windows backbuffer " + size.x + "x" + size.y);
                File.WriteAllBytes(Path.Combine(folder, label + "-" + size.x + "x" + size.y + ".png"), texture.EncodeToPNG()); Destroy(texture);
            }
        }
        IEnumerator MenuNavigation(SportsWeatherPicker clock, SportsWeatherPicker forecast)
        {
            menuPad = InputSystem.AddDevice<Gamepad>(); string originalWeather = forecast.catalog.Selection;
            clock.Choose((int)SportsTimeChoice.Day); clock.dropdown.SetDropdownIndex((int)SportsTimeChoice.Day);
            EventSystem.current.SetSelectedGameObject(clock.gameObject);
            InputSystem.QueueStateEvent(menuPad, new GamepadState().WithButton(GamepadButton.South)); yield return null; yield return null;
            InputSystem.QueueStateEvent(menuPad, new GamepadState()); yield return new WaitForSecondsRealtime(.25f);
            Check(clock.dropdown.isOn, "software gamepad South opens the real Heat time dropdown");
            yield return Capture("FieldGoal-time-dropdown-open");
            InputSystem.QueueStateEvent(menuPad, new GamepadState().WithButton(GamepadButton.DpadDown)); yield return null; yield return null;
            InputSystem.QueueStateEvent(menuPad, new GamepadState()); yield return null;
            InputSystem.QueueStateEvent(menuPad, new GamepadState().WithButton(GamepadButton.South)); yield return null; yield return null;
            InputSystem.QueueStateEvent(menuPad, new GamepadState()); yield return new WaitForSecondsRealtime(.25f);
            Check(!clock.dropdown.isOn && SportsTimeOfDay.Selection == SportsTimeChoice.Evening, "software gamepad selects Evening and closes the Heat dropdown");
            Check(forecast.catalog.Selection == originalWeather, "changing time through real menu input leaves weather selection unchanged");
            Check(EventSystem.current.currentSelectedGameObject == clock.gameObject, "focus returns to the time selector after selection");
            var expected = clock.GetComponent<UnityEngine.UI.Selectable>().FindSelectableOnUp();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab, Key.LeftShift)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            Check(expected != null && EventSystem.current.currentSelectedGameObject == expected.gameObject, "software keyboard Shift-Tab leaves the time selector using established menu navigation");
            InputSystem.RemoveDevice(menuPad); menuPad = null;
        }
        void Finish()
        {
            complete = true;
            log.AppendLine("Actual saved scenes and rendered Windows backbuffer; automatic match starts and software sources, not human/controller/listening acceptance. Frame samples include the complete player workload.");
            log.AppendLine("CHECKS=" + checks + " ERRORS=" + errors); File.WriteAllText(Path.Combine(folder, "result.txt"), log.ToString()); Application.Quit(errors == 0 ? 0 : 2);
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= Error; preferences?.Dispose(); if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (menuPad != null && menuPad.added) InputSystem.RemoveDevice(menuPad);
            if (!complete && folder != null) File.WriteAllText(Path.Combine(folder, "interrupted.txt"), log.ToString());
        }
    }
}
#endif
