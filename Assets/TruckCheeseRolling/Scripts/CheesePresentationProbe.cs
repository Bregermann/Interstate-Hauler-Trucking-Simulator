#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using LWS.TruckBowling;
using LWS.TruckSports;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LWS.TruckCheeseRolling
{
    public sealed class CheesePresentationProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsCheesePresentation") < 0) return;
            var root = new GameObject("Explicit Cheese presentation Windows acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<CheesePresentationProbe>();
        }
        CheeseMatch m; string folder; int checks, errors; IDisposable prefs; Keyboard keys; Gamepad whole, shared; InputSettings original, injected; readonly StringBuilder log = new StringBuilder();
        void Check(bool ok, string what) { checks++; if (!ok) errors++; log.AppendLine((ok ? "PASS " : "FAIL ") + what); File.WriteAllText(Path.Combine(folder, "progress.txt"), log.ToString()); }
        void Error(string message, string stack, LogType kind) { if (kind == LogType.Error || kind == LogType.Exception) { errors++; log.AppendLine("ERROR " + message + "\n" + stack); } }
        IEnumerator Capture(string name) { yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png")); yield return new WaitForSecondsRealtime(.15f); }
        bool Inside(RectTransform r, Rect area) { var corners = new Vector3[4]; r.GetWorldCorners(corners); return corners.All(p => p.x >= area.xMin * Screen.width - 1 && p.x <= area.xMax * Screen.width + 1 && p.y >= area.yMin * Screen.height - 1 && p.y <= area.yMax * Screen.height + 1); }
        void Neutral() { InputSystem.QueueStateEvent(keys, new KeyboardState()); InputSystem.QueueStateEvent(whole, new GamepadState()); InputSystem.QueueStateEvent(shared, new GamepadState()); }
        void Configure(int humans, bool aiViews)
        {
            m.ChangeSetup(); m.setup.count = 4; m.AllAI(); SportsCameraOptions.Set(aiViews); m.fixedSeed = true; m.seed = 8137;
            for (int i = 0; i < humans; i++) { m.setup.players[i].source = i == 0 ? BowlingSource.Keyboard : i == 1 ? BowlingSource.WholeController : i == 2 ? BowlingSource.LeftStick : BowlingSource.RightStick; if (i > 0) m.Assign(i, i == 1 ? whole : shared); }
        }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-tsCheesePresentationOutput"); folder = index >= 0 ? args[index + 1] : SportsEvidencePaths.Folder("CheesePresentation"); Directory.CreateDirectory(folder);
            prefs = SportsPrefs.IsolateValidation("CheesePresentationWindows"); original = InputSystem.settings; injected = Instantiate(original); injected.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = injected; keys = InputSystem.AddDevice<Keyboard>(); whole = InputSystem.AddDevice<Gamepad>(); shared = InputSystem.AddDevice<Gamepad>(); Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<CheeseMatch>(SportsNavigation.Cheese); yield return null; m = FindAnyObjectByType<CheeseMatch>(); var hud = m.menus.presentation;
            Check(hud != null && m.trucks.Length == 16 && m.explosions.Length == 8, "saved HUD and original sixteen-truck/eight-explosion scene load");
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080), new Vector2Int(1024, 768) })
            {
                Screen.SetResolution(size.x, size.y, false); yield return new WaitForSecondsRealtime(.2f);
                foreach (int humans in new[] { 0, 1, 2, 3, 4 }) foreach (bool aiViews in new[] { false, true })
                {
                    Configure(humans, aiViews); Neutral(); yield return null; Check(m.StartMatch(), "mixed-source start humans=" + humans + " AI cameras=" + aiViews + " " + size);
                    Time.timeScale = 0; yield return null; yield return null; hud.Refresh(); Canvas.ForceUpdateCanvases();
                    int expected = aiViews ? 4 : Mathf.Max(1, humans); Check(m.cameras.Count(c => c.view.enabled) == expected, "only selected participant cameras occupy " + expected + " viewports");
                    for (int i = 0; i < 4; i++) if (m.cameras[i].view.enabled)
                    {
                        var v = hud.views[i]; Rect area = m.cameras[i].view.rect;
                        Check(Inside(v.card, area) && Inside(v.prompt.rectTransform, area) && v.opponents.All(o => Inside((RectTransform)o.root.transform, area)) && Inside(v.markerBack, area), "all new panels inside owner viewport " + i + " " + size);
                        v.catches.ForceMeshUpdate(); Check(v.name.text == m.setup.players[i].name && v.catches.text == m.totals[i] + "/5" && v.catches.textInfo.characterInfo.Any(c => c.isVisible), "owner name and actual catches have rendered glyphs " + i);
                    }
                    if (!aiViews || humans == 1) yield return Capture("layout-" + humans + "humans-ai" + aiViews + "-" + size.x + "x" + size.y);
                }
            }
            Screen.SetResolution(1920, 1080, false); Configure(0, false); Neutral(); yield return null; m.StartMatch();
            float limit = Time.realtimeSinceStartup + 400; bool airborne = false, raceCapture = false, explosionCapture = false; int lastRound = 0; Time.timeScale = 3;
            while (!m.Finished && Time.realtimeSinceStartup < limit)
            {
                if (m.Live && !raceCapture && m.RaceTime > 3) { raceCapture = true; Time.timeScale = 1; yield return Capture("actual-downhill-race"); Time.timeScale = 3; }
                airborne |= m.trucks.Where(t => t.Scoring).Any(t => t.body.linearVelocity.y > 1);
                if (!explosionCapture && m.explosions.Any(f => f.isPlaying && f.GetComponentsInChildren<ParticleSystem>().Any(s => s != f && s.particleCount > 0))) { explosionCapture = true; Time.timeScale = 0; yield return Capture("actual-race-explosion"); Time.timeScale = 3; }
                if (m.Round != lastRound) { lastRound = m.Round; log.AppendLine("ROUND " + lastRound + " catches=" + m.Catches + " totals=" + string.Join(",", m.totals) + " explosions=" + m.ExplosionCount); File.WriteAllText(Path.Combine(folder, "progress.txt"), log.ToString()); }
                yield return null;
            }
            Time.timeScale = 1; Check(m.Finished && m.Catches >= 5 && m.totals.Max() == 5, "original autonomous physical first-to-five match completes"); Check(raceCapture && airborne, "original mountain race and earned airborne motion observed");
            Check(m.ExplosionCount > 0 && explosionCapture, "natural sheep impacts invoke layered VFX through original explosion pool"); hud.Results(); yield return Capture("actual-five-catch-results");
            Check(hud.resultCards.All(c => c.activeSelf) && hud.resultScores.Select((t, i) => t.text == m.totals[i] + "/5").All(v => v), "graphical results use original authoritative catch totals");
            Configure(0, false); m.seed = 8143; m.StartMatch(); Time.timeScale = 1; float snowyDeadline = Time.realtimeSinceStartup + 12;
            while (!m.Live && Time.realtimeSinceStartup < snowyDeadline) yield return null;
            yield return new WaitForSeconds(2); Check(m.Live && m.weather.Kind == CheeseWeatherKind.Blizzard, "original seeded blizzard applies during a real new race"); Time.timeScale = 0; yield return Capture("seeded-blizzard-hud-1920x1080");
            Check(hud.views[0].markerBack.gameObject.activeSelf && hud.views[0].markerBack.GetComponent<UnityEngine.UI.Image>().color.a > .5f, "outlined cream locator retains opaque dark backing in blizzard");
            // Explicit close-up visual fixtures do not award points or pretend
            // to be natural race events. Original cats/colliders stay in place.
            m.ChangeSetup(); var cat = FindObjectsByType<CheeseCatPresentation>(FindObjectsSortMode.None).First(); var cam = m.cameras[0].view; var oldPosition = cam.transform.position; var oldRotation = cam.transform.rotation; m.cameras[0].enabled = false;
            m.menus.gameObject.SetActive(false); cam.transform.position = cat.transform.position + new Vector3(3, 1.3f, 3); cam.transform.LookAt(cat.transform.position + Vector3.up * .35f); cat.animator.Play("Base Layer.Scratch", 0, .35f); cat.animator.Update(0); Time.timeScale = 0; yield return Capture("labelled-owned-cat-animation-closeup");
            var pose = cat.animator.GetComponentsInChildren<Transform>().Select(t => t.localRotation).ToArray(); yield return new WaitForSecondsRealtime(.2f); Check(cat.animator.GetComponentsInChildren<Transform>().Where((t, i) => t.localRotation != pose[i]).Count() == 0, "owned cat animation respects scaled pause");
            var burst = m.explosions[0]; burst.transform.position = cat.transform.position + Vector3.right * 8 + Vector3.up * 3; cam.transform.position = burst.transform.position + new Vector3(23, 12, 28); cam.transform.LookAt(burst.transform.position + Vector3.up * 4); Time.timeScale = 1; burst.Play(true); yield return new WaitForSeconds(.2f); Time.timeScale = 0; yield return Capture("labelled-owned-explosion-visual-fixture"); burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Check(m.explosions.All(f => f.GetComponent<CheeseExplosionPresentation>().visual.GetComponentsInChildren<Light>(true).Length == 0), "pooled examples cannot leave permanent demo lights in the weather world");
            cam.transform.SetPositionAndRotation(oldPosition, oldRotation); m.cameras[0].enabled = true; m.menus.gameObject.SetActive(true); Time.timeScale = 1; Configure(0, false); m.StartMatch(); yield return null;
            Check(m.totals.All(t => t == 0) && m.Catches == 0 && m.ExplosionCount == 0 && m.explosions.All(f => !f.isPlaying), "rematch clears catches and pooled effects without startup burst");
            m.Pause(); Check(m.Paused && Time.timeScale == 0, "original pause remains authoritative"); yield return Capture("pause"); m.Resume(); Check(!m.Paused && Time.timeScale == 1, "resume restores original race lifecycle"); m.ChangeSetup(); yield return Capture("setup-after-rematch");
            SportsNavigation.FrontEnd(); yield return null; yield return null; Check(FindAnyObjectByType<CheeseMatch>() == null && FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length == 1, "menu return cleans Cheese presentation and retains one listener");
            log.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + "; actual Windows composed frames and original physical Cheese simulation; software input only, no human/hardware/listening acceptance."); File.WriteAllText(Path.Combine(folder, "result.txt"), log.ToString()); Application.Quit(errors == 0 ? 0 : 1);
        }
        void OnDestroy() { Time.timeScale = 1; Application.logMessageReceived -= Error; if (keys != null && keys.added) InputSystem.RemoveDevice(keys); if (whole != null && whole.added) InputSystem.RemoveDevice(whole); if (shared != null && shared.added) InputSystem.RemoveDevice(shared); if (original != null) InputSystem.settings = original; if (injected != null) Destroy(injected); prefs?.Dispose(); }
    }
}
#endif
