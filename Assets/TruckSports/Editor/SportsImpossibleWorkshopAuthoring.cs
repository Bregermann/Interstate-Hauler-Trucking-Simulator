using System;
using System.Linq;
using PixelCrushers;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace LWS.TruckSports.Editor
{
    public static class SportsImpossibleWorkshopAuthoring
    {
        public static void BindOpenWorkshop()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required for explicit workshop binding.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != "TruckNestBuilding" && scene.name != "TruckSnowmanBuilding" && scene.name != "TruckArchaeology")
                throw new InvalidOperationException("Only the explicitly saved new construction and archaeology workshops are supported.");
            if (scene.path != "Assets/" + scene.name + "/Scenes/" + scene.name + ".unity")
                throw new InvalidOperationException("Explicit saved sport scene required.");
            var session = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
            if (!(session is ISportsWorkshopState)) throw new InvalidOperationException("Mode-owned validated snapshot provider required.");
            var persistence = session.GetComponent<SportsWorkshopPersistence>() ?? session.gameObject.AddComponent<SportsWorkshopPersistence>();
            var storers = session.GetComponents<DiskSavedGameDataStorer>();
            if (storers.Length > 1 || storers.Length == 1 && persistence.storage != null && persistence.storage != storers[0])
                throw new InvalidOperationException("Preserve the existing storage binding for review.");
            persistence.stateProvider = session; persistence.modeId = scene.name;
            persistence.storage = storers.SingleOrDefault() ?? session.gameObject.AddComponent<DiskSavedGameDataStorer>();
            if (!string.IsNullOrEmpty(persistence.storage.customPath))
                throw new InvalidOperationException("A customized storage path must be reviewed, not overwritten.");
            persistence.storage.storeSaveFilesIn = DiskSavedGameDataStorer.BasePath.Custom;
            persistence.storage.encrypt = false;
            // The adapter computes the machine-local Sports directory at Awake.
            // The scene contains no user-specific disk paths or host save slots.
            var page = session.menus.pages[(int)SportsEventScreen.Pause].transform;
            var old = page.GetComponentsInChildren<SportsMenuAction>(true).SingleOrDefault(action => action.action == "nest-save");
            Michsky.UI.Heat.ButtonManager save;
            if (old != null)
            {
                save = old.button; save.gameObject.name = "Save construction workshop";
                UnityEngine.Object.DestroyImmediate(old);
            }
            else
            {
                var existing = page.Find("Save construction workshop");
                save = existing != null ? existing.GetComponent<Michsky.UI.Heat.ButtonManager>() : NewControl(page, "Save construction workshop", "SAVE WORKSHOP");
            }
            Place(save.GetComponent<RectTransform>(), new Vector2(-420, -275), new Vector2(390, 48));
            var loadObject = page.Find("Restore construction workshop");
            var load = loadObject != null ? loadObject.GetComponent<Michsky.UI.Heat.ButtonManager>() : NewControl(page, "Restore construction workshop", "RESTORE WORKSHOP");
            Place(load.GetComponent<RectTransform>(), new Vector2(0, -275), new Vector2(390, 48));
            var newWorkshop = page.GetComponentsInChildren<SportsMenuAction>(true).SingleOrDefault(action => action.action == "nest-new");
            if (newWorkshop != null) Place(newWorkshop.GetComponent<RectTransform>(), new Vector2(420, -275), new Vector2(390, 48));
            else
            {
                Place(save.GetComponent<RectTransform>(), new Vector2(-225, -350), new Vector2(420, 48));
                Place(load.GetComponent<RectTransform>(), new Vector2(225, -350), new Vector2(420, 48));
            }
            Bind(save.onClick, persistence.RequestSave); Bind(load.onClick, persistence.RequestLoad);
            var label = page.Find("Workshop storage status");
            persistence.statusLabel = label != null ? label.GetComponent<TMPro.TMP_Text>() : SportsUIAuthoring.Text("Workshop storage status", page,
                "Release carried materials before saving. Restore checks the same crew and workshop format.", new Vector2(1280, 68), new Vector2(0, newWorkshop == null ? -410 : -345), 22);
            // Explicitly migrate only the former default; preserve designer edits.
            if (newWorkshop == null && persistence.statusLabel.rectTransform.anchoredPosition == new Vector2(0, -345)
                && persistence.statusLabel.rectTransform.sizeDelta == new Vector2(1280, 68))
                persistence.statusLabel.rectTransform.anchoredPosition = new Vector2(0, -410);
            EditorUtility.SetDirty(persistence); EditorUtility.SetDirty(persistence.storage);
            EditorUtility.SetDirty(save); EditorUtility.SetDirty(load);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        static Michsky.UI.Heat.ButtonManager NewControl(Transform page, string name, string title)
        {
            var button = SportsUIAuthoring.Button(page, title, "workshop-owned-storage", Vector2.zero, new Vector2(390, 48));
            button.gameObject.name = name;
            // This production Heat control uses the persistence adapter's guarded
            // callback; it must not also dispatch the unbound mode request event.
            UnityEngine.Object.DestroyImmediate(button.GetComponent<SportsMenuAction>());
            return button;
        }
        static void Place(RectTransform rect, Vector2 position, Vector2 size) { rect.anchoredPosition = position; rect.sizeDelta = size; }
        static void Bind(UnityEvent target, UnityAction action)
        {
            if (target.GetPersistentEventCount() == 0) UnityEventTools.AddPersistentListener(target, action);
            else if (target.GetPersistentEventCount() != 1 || target.GetPersistentTarget(0) != action.Target as UnityEngine.Object || target.GetPersistentMethodName(0) != action.Method.Name)
                throw new InvalidOperationException("Preserve customized workshop button callbacks for review.");
        }
    }
}
