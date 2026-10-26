using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckWallball.Editor
{
    // Explicit additive authoring. Normal loads, rematches and builds do not
    // regenerate the original court, menus, cameras or participant controls.
    public static class WallballPresentationAuthoring
    {
        [MenuItem("Truck Sports/Wallball/Install Confirmed Team Awards and Results")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring Wallball presentation.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before the explicit Wallball presentation upgrade.");
            SportsScorePresentationAuthoring.InstallWallballAwards();
            SportsResultsPresentationAuthoring.InstallWallballResults();
            var scene = EditorSceneManager.OpenScene(WallballAuthoring.Scene, OpenSceneMode.Single);
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WallballMatch>(true)).Single();
            var menus = match.menus; var page = menus.pages[(int)WallballScreen.Result];
            // Retain the existing title animation and the same three actions.
            // Reserve the upper margin for the confirmed final team point.
            Place(menus.resultTitle, new Vector2(0, 400), new Vector2(1350, 64));
            var subtitle = page.transform.Find("Subtitle") as RectTransform;
            if (subtitle == null) throw new InvalidOperationException("Existing Wallball result subtitle is required.");
            Place(subtitle, new Vector2(0, 330), new Vector2(1320, 40));
            string[] keys = { "rematch", "setup", "front" };
            for (int i = 0; i < keys.Length; i++)
            {
                var action = page.GetComponentsInChildren<SportsMenuAction>(true).Single(a => a.action == keys[i]);
                var rect = (RectTransform)action.transform;
                Place(rect, new Vector2(0, -225 - i * 85), rect.sizeDelta);
            }
            var award = menus.GetComponentsInChildren<SportsScorePresentation>(true).Single();
            var results = page.GetComponentsInChildren<SportsResultsPresentation>(true).Single();
            if (award.session != null || award.alternateSession != match || results.source.wallball != match || results.source.session != null || results.source.cornhole != null)
                throw new InvalidOperationException("Wallball's saved adapters must observe only the original Wallball authority.");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("WALLBALL_TEAM_PRESENTATION_SAVED original team scoring, court, controls, serve alternation and menu actions retained; shared Heat point receipts and read-only ranked results added.");
        }
        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; EditorUtility.SetDirty(rect);
        }
    }
}
