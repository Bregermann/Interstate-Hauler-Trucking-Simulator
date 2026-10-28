using System;
using System.Linq;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckCamping.Editor
{
    // Authoring only. Normal Play Mode and builds never rewrite designer UI.
    public static class CampingActivityPromptAuthoring
    {
        public const string PlateName = "Wilderness activity contrast plate";
        public static void Install(TMP_Text label)
        {
            if (label == null || label.transform.parent == null) throw new InvalidOperationException("Existing wilderness activity reminder required.");
            var text = label.rectTransform;
            if (label.color == new Color(.8f, .92f, .81f)) label.color = new Color(1, .94f, .8f);
            var prior = label.transform.parent.Find(PlateName);
            if (prior != null)
            {
                if (prior.GetComponent<UnityEngine.UI.Image>() == null || prior.GetComponent<SportsBroadcastPlate>() == null)
                    throw new InvalidOperationException("Preserve customized wilderness activity plate for review.");
                return;
            }
            var plate = SportsUIAuthoring.Rect(PlateName, text.parent, text.sizeDelta + new Vector2(32, 12), text.anchoredPosition);
            var rect = (RectTransform)plate.transform;
            rect.anchorMin = text.anchorMin; rect.anchorMax = text.anchorMax; rect.pivot = text.pivot;
            rect.localRotation = text.localRotation; rect.localScale = text.localScale;
            var image = plate.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.012f, .035f, .045f, .92f); image.raycastTarget = false;
            plate.AddComponent<SportsBroadcastPlate>().cut = 10;
            rect.SetSiblingIndex(text.GetSiblingIndex());
        }
        [MenuItem("Truck Sports/Impossible Games/Repair Existing Wilderness Activity Prompt Contrast")]
        public static void RepairExisting()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Preserve open unsaved scenes before explicit presentation repair.");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string mode in new[] { "TruckCamping", "TruckPetPals" })
                {
                    var scene = EditorSceneManager.OpenScene("Assets/" + mode + "/Scenes/" + mode + ".unity");
                    var match = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CampingMatch>(true)).Single();
                    var label = match.menus.hud.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Cooperative wilderness activity reminder");
                    Install(label); CampingAuthoring.ConfigureSnowAtmosphere(match); EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save existing wilderness prompt presentation: " + mode);
                }
            }
            finally
            {
                if (previous.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // Batch entry can legitimately have no loaded scene.
            }
        }
    }
}
