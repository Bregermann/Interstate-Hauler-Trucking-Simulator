using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckKickball.Editor
{
    public static class KickballWaitingAuthoring
    {
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(KickballAuthoring.Scene);
            var match = Object.FindAnyObjectByType<KickballMatch>();
            if (match.waitingPresentationVersion >= 2)
            { Debug.Log("KICKBALL_WAITING_ALREADY_BOUND: preserving authored choices"); return; }
            if (match.aiPlayback == null) match.aiPlayback = match.gameObject.AddComponent<KickballAIPlayback>();
            match.aiPlayback.match = match;
            var view = match.viewChoice.transform.parent.GetComponent<RectTransform>();
            view.anchoredPosition = new Vector2(-345, -245); view.sizeDelta = new Vector2(650, 45);
            match.viewChoice.rectTransform.sizeDelta = new Vector2(632, 37);
            if (match.paceChoice == null) match.paceChoice = SportsUIAuthoring.Button(match.menus.pages[0].transform, KickballAIPlayback.Label, "aipace", new Vector2(345, -245), new Vector2(650, 45)).normalTextObj;
            var pause = match.menus.pages[3].transform;
            if (match.pauseViewChoice == null) match.pauseViewChoice = SportsUIAuthoring.Button(pause, "CAMERA · BROADCAST", "view", Vector2.zero).normalTextObj;
            if (match.pausePaceChoice == null) match.pausePaceChoice = SportsUIAuthoring.Button(pause, KickballAIPlayback.Label, "aipace", Vector2.zero).normalTextObj;
            string[] order = { "resume", "view", "aipace", "help", "settings", "restart", "front" };
            foreach (var action in pause.GetComponentsInChildren<SportsMenuAction>(true))
                for (int i = 0; i < order.Length; i++) if (action.action == order[i])
                {
                    var rect = action.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(0, 200 - i * 82); rect.sizeDelta = new Vector2(680, 62);
                    action.transform.SetAsLastSibling();
                }
            var obsolete = match.menus.pages[0].transform.Find("Waiting camera and pacing guidance");
            if (obsolete != null) Object.DestroyImmediate(obsolete.gameObject);
            match.menus.setupInfo.rectTransform.anchoredPosition = new Vector2(0, -310);
            match.menus.setupInfo.rectTransform.sizeDelta = new Vector2(1320, 78);
            match.menus.setupInfo.fontSizeMin = 18; match.menus.setupInfo.fontSizeMax = 20;
            var hint = match.menus.pages[0].transform.Find("Weather selection explanation").GetComponent<TMPro.TMP_Text>();
            hint.text = "Wind affects play · Random rolls again on rematch";
            hint.rectTransform.anchoredPosition = new Vector2(-360, 215); hint.rectTransform.sizeDelta = new Vector2(630, 22);
            hint.fontSizeMin = 17; hint.fontSizeMax = 18;
            var picker = match.menus.pages[0].GetComponentInChildren<SportsWeatherPicker>(true);
            picker.dropdown.headerText.rectTransform.sizeDelta = new Vector2(550, 45);
            match.menus.GetComponent<SportsBroadcastCanvas>()?.Apply();
            match.waitingPresentationVersion = 2;
            match.RefreshExtraUI(); EditorUtility.SetDirty(match);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("KICKBALL_WAITING_BOUND: existing scene, Heat buttons, Cinemachine look, owned idle inputs, scoped 2x pacing");
        }
    }
}
