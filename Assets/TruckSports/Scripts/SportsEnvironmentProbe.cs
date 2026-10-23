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
            if (Array.IndexOf(arguments, "legacy-time") >= 0)
            {
                yield return LegacyScenes(); Finish(); yield break;
            }
            if (Array.IndexOf(arguments, "legacy-forecasts") >= 0)
            {
                yield return LegacyScenes(true); Finish(); yield break;
            }
            if (Array.IndexOf(arguments, "legacy-courts") >= 0)
            {
                yield return LegacyCourts(); Finish(); yield break;
            }
            int scenesIndex = Array.IndexOf(arguments, "-tsEnvironmentScenes");
            string[] scenes = scenesIndex >= 0 && scenesIndex + 1 < arguments.Length
                ? arguments[scenesIndex + 1].Split(',')
                : new[] { SportsNavigation.Keepaway, SportsNavigation.FieldGoal, SportsNavigation.SkyTow, SportsNavigation.Terrier, SportsNavigation.Snake };
            bool maximumRoster = Array.IndexOf(arguments, "maximum-rosters") >= 0;
            bool allWeather = Array.IndexOf(arguments, "all-weather") >= 0;
            bool weatherTimeMatrix = Array.IndexOf(arguments, "weather-time-matrix") >= 0;
            foreach (string scene in scenes)
            {
                SportsNavigation.LoadSport(scene); yield return null; yield return null;
                var match = FindAnyObjectByType<SportsEventSession>(); var weather = match.GetComponent<SportsChaosWeather>();
                Check(weather != null && weather.matchTime != null, scene + " saved Weather Maker match-time binding");
                if (weather == null || weather.matchTime == null) continue;
                var pickers = match.menus.GetComponentsInChildren<SportsWeatherPicker>(true);
                Check(pickers.Count(picker => picker.timeOfDay) == 1 && pickers.Count(picker => !picker.timeOfDay) == 1, scene + " one actual Heat weather control and one time control");
                var clock = pickers.Single(picker => picker.timeOfDay); var forecast = pickers.Single(picker => !picker.timeOfDay);
                if (maximumRoster)
                {
                    match.setup.count = match.MaximumPlayers;
                    if (match.TeamSport) match.teamA = match.teamB = 4;
                }
                match.AllBots(); SportsCameraOptions.Set(false);
                yield return Capture(scene + "-setup");
                if (scene == SportsNavigation.FieldGoal) yield return MenuNavigation(clock, forecast);
                var cases = allWeather ? forecast.catalog.entries.Select(entry => entry.displayName + "|Night").ToArray() : weatherTimeMatrix || scene == SportsNavigation.FieldGoal
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
        IEnumerator LegacyScenes(bool fullForecasts = false)
        {
            foreach (string scene in fullForecasts ? new[] { "TruckCheeseRolling", "TruckBaseJumping" } : new[] { "TruckCornhole", "TruckCheeseRolling", "TruckBaseJumping" })
            {
                SportsNavigation.LoadSport(scene); yield return null; yield return null;
                var corn = FindAnyObjectByType<LWS.TruckCornhole.CornholeMatch>();
                var cheese = FindAnyObjectByType<LWS.TruckCheeseRolling.CheeseMatch>();
                var jumper = FindAnyObjectByType<LWS.TruckBaseJumping.BaseJumpMatch>();
                var binding = FindAnyObjectByType<SportsMatchTime>();
                Check(binding != null, scene + " saved legacy time binding"); if (binding == null) continue;
                SportsMenuView menus = corn != null ? corn.menus : cheese != null ? cheese.menus : jumper.menus;
                var setup = corn != null ? corn.setup : cheese != null ? cheese.setup : jumper.setup;
                setup.count = 4;
                if (corn != null) corn.AllAI(); else if (cheese != null) cheese.AllAI(); else jumper.AllBots();
                var clock = menus.GetComponentsInChildren<SportsWeatherPicker>(true).Single(picker => picker.timeOfDay);
                var forecast = menus.GetComponentsInChildren<SportsWeatherPicker>(true).SingleOrDefault(picker => !picker.timeOfDay);
                var weather = cheese != null ? cheese.weather.presentation : jumper != null ? jumper.weatherField.presentation : corn.atmosphere;
                if (fullForecasts) Check(forecast != null && forecast.gameObject.activeInHierarchy, scene + " actual independent installed forecast selector");
                Check(clock.gameObject.activeInHierarchy, scene + " actual Heat clock visible");
                yield return Capture(scene + "-legacy-setup");
                string priorId = null;
                foreach (SportsTimeChoice choice in new[] { SportsTimeChoice.Morning, SportsTimeChoice.Day, SportsTimeChoice.Evening, SportsTimeChoice.Night })
                {
                    if (corn != null) corn.ChangeSetup(); else if (cheese != null) cheese.ChangeSetup(); else jumper.SetupScreen();
                    clock.Choose((int)choice);
                    string forecastName = choice == SportsTimeChoice.Morning ? "Snow" : choice == SportsTimeChoice.Day ? "Clear" : choice == SportsTimeChoice.Evening ? "Rain" : "Blizzard";
                    if (fullForecasts) { int at = Array.FindIndex(forecast.catalog.entries, entry => entry.displayName == forecastName); Check(at >= 0, scene + " installed " + forecastName); if (at < 0) continue; forecast.Choose(at + 1); }
                    Check(corn != null ? corn.StartMatch() : cheese != null ? cheese.StartMatch() : jumper.StartMatch(), scene + " actual four-AI start " + choice);
                    yield return new WaitForSeconds(2);
                    string id = corn != null ? corn.MatchId : cheese != null ? cheese.MatchId : jumper.MatchId;
                    Check(!string.IsNullOrEmpty(id) && id != priorId, scene + " fresh rematch identity " + choice); priorId = id;
                    Check(binding.Applied == choice && Mathf.Abs(binding.cycle.TimeOfDay - SportsTimeOfDay.Seconds(choice)) < .01f, scene + " actual vendor time " + choice);
                    Check(!binding.daylightFill.enabled && binding.cycle.Speed == 0 && binding.cycle.NightSpeed == 0, scene + " fixed noon fill superseded by vendor " + choice);
                    Check(RenderSettings.skybox.shader.name == "LWS/TruckSports/Weather Maker Viewport Skybox", scene + " runtime sky owner keeps vendor material " + choice);
                    int revision = binding.Revision; binding.Configure(-458, id);
                    Check(binding.Revision == revision, scene + " attempt seed keeps match time " + choice);
                    if (fullForecasts)
                    {
                        var profile = weather.vendor.LastLocalProfile; int seed = weather.Seed;
                        Check(weather.Description == forecastName.ToUpperInvariant() && profile != null, scene + " actual vendor forecast " + forecastName);
                        if (cheese != null) cheese.weather.Select(seed + 1); else jumper.weatherField.Select((jumper.option + 1) % 3, seed + 997);
                        yield return null;
                        Check(weather.vendor.LastLocalProfile == profile && weather.Seed == seed && binding.Revision == revision, scene + " round refresh retains actual forecast and clock " + choice);
                        var snow = FindAnyObjectByType<SportsSnowSurface>(); Check(snow.weather == weather && snow.Falling == (forecastName == "Snow" || forecastName == "Blizzard"), scene + " snow follows actual forecast " + choice);
                    }
                    var samples = new List<float>(60);
                    for (int frame = 0; frame < 60; frame++) { yield return null; samples.Add(Time.unscaledDeltaTime * 1000); }
                    samples.Sort(); log.AppendLine("FRAME_TIME " + scene + " Legacy-" + choice + " medianMs=" + samples[30].ToString("F2") + " p95Ms=" + samples[56].ToString("F2") + " samples=60; whole-player short-start interval, not isolated GPU/CPU or full-match profiling.");
                    yield return Capture(scene + "-legacy-" + choice);
                }
                if (corn != null) corn.ChangeSetup(); else if (cheese != null) cheese.ChangeSetup(); else jumper.SetupScreen();
                setup.players[0].source = setup.players[1].source = LWS.TruckBowling.BowlingSource.Keyboard;
                SportsCameraOptions.Set(false); SportsTimeOfDay.Select(SportsTimeChoice.Night);
                Check(corn != null ? corn.StartMatch() : cheese != null ? cheese.StartMatch() : jumper.StartMatch(), scene + " two software-keyboard human night views start");
                yield return new WaitForSeconds(1);
                var cameras = corn != null ? corn.courses.Select(course => course.chase.view).ToArray() : cheese != null ? cheese.cameras.Select(rig => rig.view).ToArray() : jumper.views.Select(rig => rig.view).ToArray();
                Check(cameras.Count(view => view.enabled) == 2, scene + " AI cameras OFF leaves two human views");
                yield return Capture(scene + "-legacy-two-software-humans-night");
                if (corn != null) corn.ChangeSetup(); else if (cheese != null) cheese.ChangeSetup(); else jumper.SetupScreen();
            }
            SportsNavigation.FrontEnd(); yield return null; yield return null;
            Check(SceneManager.GetActiveScene().name == SportsNavigation.Front, "legacy clocks return to real sport selection");
        }
        IEnumerator LegacyCourts()
        {
            foreach (string scene in new[] { "TruckHockey", "TruckWallball" })
            {
                yield return SceneManager.LoadSceneAsync(scene); yield return null; yield return null;
                var hockey = FindAnyObjectByType<LWS.TruckHockey.HockeyMatch>(); var wall = FindAnyObjectByType<LWS.TruckWallball.WallballMatch>();
                MonoBehaviour owner = hockey != null ? (MonoBehaviour)hockey : wall;
                var lifecycle = (ISportsWeatherLifecycle)owner; var weather = owner.GetComponent<SportsChaosWeather>(); var binding = weather.matchTime;
                SportsMenuView menus = hockey != null ? (SportsMenuView)hockey.menus : wall.menus;
                if (hockey != null) { hockey.mode = LWS.TruckHockey.HockeyMode.AIVsAI; hockey.Configure(); hockey.menus.RefreshSetup(); }
                else { wall.blueSize = wall.orangeSize = 4; wall.Configure(LWS.TruckWallball.WallballMode.AIVsAI); }
                Check(hockey != null ? hockey.AIOnly : wall.AllAI, scene + " actual maximum roster is entirely AI, independently of menu captions");
                var pickers = menus.GetComponentsInChildren<SportsWeatherPicker>(true); var clock = pickers.Single(picker => picker.timeOfDay); var forecast = pickers.Single(picker => !picker.timeOfDay);
                Check(pickers.Length == 2 && pickers.All(picker => picker.gameObject.activeInHierarchy), scene + " saved independent Heat selectors");
                Check(weather.alternateSession == owner && weather.session == null && FindObjectsByType<SportsChaosWeather>(FindObjectsSortMode.None).Length == 1, scene + " existing legacy match retains authority");
                yield return Capture(scene + "-court-setup"); string priorId = null;
                foreach (string condition in new[] { "Clear|Day", "Rain|Evening", "Snow|Morning", "Blizzard|Night" })
                {
                    string[] choice = condition.Split('|'); int entry = Array.FindIndex(forecast.catalog.entries, value => value.displayName == choice[0]);
                    Check(entry >= 0, scene + " installed " + choice[0]); if (entry < 0) continue;
                    forecast.Choose(entry + 1); clock.Choose((int)Enum.Parse<SportsTimeChoice>(choice[1]));
                    Check(hockey != null ? hockey.StartMatch() : wall.StartMatch(), scene + " actual maximum-AI start " + condition); yield return new WaitForSeconds(2);
                    Check(!string.IsNullOrEmpty(lifecycle.WeatherMatchId) && lifecycle.WeatherMatchId != priorId, scene + " fresh match identity " + condition); priorId = lifecycle.WeatherMatchId;
                    Check(binding.Applied.ToString() == choice[1] && Mathf.Abs(binding.cycle.TimeOfDay - SportsTimeOfDay.Seconds(binding.Applied)) < .01f, scene + " vendor clock " + condition);
                    Check(weather.PresetId == forecast.catalog.entries[entry].id && weather.vendor.LastLocalProfile != null, scene + " real vendor preset " + condition);
                    Check(!binding.daylightFill.enabled && binding.cycle.Speed == 0 && binding.cycle.NightSpeed == 0, scene + " single frozen vendor sun/moon");
                    var beams = owner.GetComponent<SportsNightHeadlights>().beams; bool night = choice[1] == "Night" || choice[1] == "Evening";
                    Check(beams.Length == (hockey != null ? 6 : 8) && beams.All(beam => beam.light.enabled == night), scene + " active legacy truck headlights " + condition);
                    Check(binding.nightLights.Length == 4 && binding.nightLights.All(light => (light.intensity > 0) == night), scene + " scene-owned venue lights " + condition);
                    int revision = binding.Revision; string preset = weather.PresetId;
                    if (hockey != null) hockey.Pause(); else wall.Pause(); yield return new WaitForSecondsRealtime(.1f);
                    if (hockey != null) hockey.Resume(); else wall.Resume(); yield return null;
                    Check(binding.Revision == revision && weather.PresetId == preset, scene + " pause/resume retains selected instance " + condition);
                    var samples = new List<float>(60); for (int frame = 0; frame < 60; frame++) { yield return null; samples.Add(Time.unscaledDeltaTime * 1000); }
                    samples.Sort(); log.AppendLine("FRAME_TIME " + scene + " Court-" + condition.Replace('|', '-') + " medianMs=" + samples[30].ToString("F2") + " p95Ms=" + samples[56].ToString("F2") + " samples=60; short whole-player intervals, not sustained CPU/GPU profiling.");
                    yield return Capture(scene + "-court-" + condition.Replace('|', '-'));
                    if (hockey != null) hockey.ChangeSetup(); else wall.ChangeSetup(); yield return null;
                }
            }
            SportsNavigation.FrontEnd(); yield return null; yield return null;
            Check(SceneManager.GetActiveScene().name == SportsNavigation.Front, "legacy courts return to actual sport selection");
        }
        IEnumerator Capture(string label)
        {
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                Screen.SetResolution(size.x, size.y, false); yield return new WaitForSecondsRealtime(.35f);
                Texture2D texture = null;
                // Windows can commit the resize after the initial end-of-frame,
                // even when Screen.width already reports the requested value.
                // Inspect the real swap-chain capture; never rescale an image or
                // substitute a camera render for actual window evidence.
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    yield return new WaitForEndOfFrame(); texture = ScreenCapture.CaptureScreenshotAsTexture();
                    if (texture.width == size.x && texture.height == size.y) break;
                    log.AppendLine("CAPTURE_RESIZE_RETRY " + label + " requested=" + size + " actual=" + texture.width + "x" + texture.height + " attempt=" + (attempt + 1));
                    Destroy(texture); texture = null; yield return new WaitForSecondsRealtime(.15f);
                }
                Check(texture != null, label + " actual Windows backbuffer " + size.x + "x" + size.y);
                if (texture != null) { File.WriteAllBytes(Path.Combine(folder, label + "-" + size.x + "x" + size.y + ".png"), texture.EncodeToPNG()); Destroy(texture); }
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
