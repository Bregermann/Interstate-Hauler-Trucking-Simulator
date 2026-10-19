#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
namespace LWS.TruckSports
{
    public sealed class SportsMenuPresentationProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
        {
            if (!UnityEditor.SessionState.GetBool("Sports.MenuPresentation.Probe", false)) return;
            UnityEditor.SessionState.SetBool("Sports.MenuPresentation.Probe", false);
            var root = new GameObject("Explicit collection presentation and navigation acceptance"); DontDestroyOnLoad(root);
            root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<SportsMenuPresentationProbe>();
        }
        IDisposable preferences; Keyboard keyboard; int checks, errors; string folder;
        readonly StringBuilder report = new StringBuilder();
        void Awake() { preferences = SportsPrefs.IsolateValidation("MenuPresentation"); SportsParticipantRegistry.ForgetCache(); }
        void Check(bool value, string label) { checks++; if (!value) errors++; report.AppendLine((value ? "PASS " : "FAIL ") + label); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) { errors++; report.AppendLine("ERROR " + message + "\n" + stack); } }
        IEnumerator Start()
        {
            folder = Path.GetFullPath("Logs/TruckSports/EssentialRelease/MenuPresentation"); Directory.CreateDirectory(folder);
            File.WriteAllText(folder + "/result.txt", "RUNNING " + DateTime.UtcNow.ToString("o")); Application.logMessageReceived += Error;
            keyboard = InputSystem.AddDevice<Keyboard>("Menu presentation injected keyboard");
            yield return null; yield return null;
            var front = FindAnyObjectByType<SportsFrontEnd>(); front.Action("play"); yield return new WaitForSecondsRealtime(.4f);
            foreach (var card in front.catalogue.cards)
            {
                var icon = card.transform.Find("Custom sport icon")?.GetComponent<Image>();
                var frame = card.transform.Find("Rounded gameplay frame"); var picture = frame?.Find("Sport preview")?.GetComponent<Image>();
                Check(icon != null && icon.sprite != null && picture != null && picture.sprite != null && frame.GetComponent<Mask>() != null,
                    card.name + " has an original icon and rounded real-gameplay screenshot");
                Check(card.GetComponent<SportsCardMotion>() != null && card.GetComponentsInChildren<SportsMenuAction>(true).Length > 0,
                    card.name + " preserves playable actions and shared focus animation");
            }
            var canvas = front.GetComponent<Canvas>();
            var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude).Where(c => c.enabled && c.targetTexture == null).ToArray();
            for (int page = 0; page < front.catalogue.Pages; page++)
            {
                if (page > 0) front.catalogue.Step(1); yield return new WaitForSecondsRealtime(.25f);
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
                    SportsControlledCapture.Save(canvas, cameras, size.x, size.y, folder + "/catalogue-" + page + "-" + size.x + "x" + size.y + ".png");
                foreach (var card in front.catalogue.cards) if (card.activeInHierarchy)
                {
                    var title = card.transform.Find("Sport").GetComponent<TMP_Text>(); title.ForceMeshUpdate();
                    var description = card.transform.Find("Description").GetComponent<TMP_Text>(); description.ForceMeshUpdate();
                    Check(!title.isTextTruncated && !description.isTextTruncated, card.name + " title and description are not truncated");
                }
            }
            front.catalogue.Step(-front.catalogue.Pages); yield return new WaitForSecondsRealtime(.2f);
            var first = front.catalogue.cards[0]; var play = first.GetComponentsInChildren<SportsMenuAction>().First();
            EventSystem.current.SetSelectedGameObject(null);EventSystem.current.SetSelectedGameObject(play.gameObject); yield return new WaitForSecondsRealtime(.25f);
            Check(first.transform.localScale.x > 1.01f, "keyboard/controller selection smoothly raises its whole card");
            var selected = EventSystem.current.currentSelectedGameObject;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); yield return new WaitForSecondsRealtime(.12f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(.2f);
            Check(EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject != selected,
                "injected keyboard navigation moves selection between real menu controls");
            foreach (string key in new[] { "kickball", "danceoff", "limbo", "skytow", "trailerplug", "farefight", "snake" })
            {
                front.Action(key); yield return null; yield return null; yield return new WaitForSecondsRealtime(.2f);
                Check(FindAnyObjectByType<SportsEventSession>() != null && FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length == 1,
                    key + " menu action loads its actual saved setup with one listener");
                SportsNavigation.FrontEnd(); yield return null; yield return null; yield return new WaitForSecondsRealtime(.2f);
                front = FindAnyObjectByType<SportsFrontEnd>(); front.Action("play");
            }
            report.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + " · native rendered UI and injected keyboard; no physical controller/human playtest.");
            File.WriteAllText(folder + "/result.txt", report.ToString()); Application.logMessageReceived -= Error;
            UnityEditor.EditorApplication.isPlaying = false;
        }
        void OnDestroy() { Application.logMessageReceived -= Error; if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); preferences?.Dispose(); SportsParticipantRegistry.ForgetCache(); }
    }
}
#endif
