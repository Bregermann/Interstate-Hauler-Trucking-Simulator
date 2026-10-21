using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckKickball.Editor
{
    public static class KickballRepairAuthoring
    {
        public static void Upgrade()
        {
            var scene=EditorSceneManager.OpenScene(KickballAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<KickballMatch>();
            if(match.homeRunCard==null)
            {
                var card=SportsUIAuthoring.Rect("Sealed home run celebration",match.menus.hud.transform,new Vector2(800,180),Vector2.zero).GetComponent<RectTransform>();
                card.anchorMin=card.anchorMax=new Vector2(.5f,.72f);card.pivot=new Vector2(.5f,.5f);
                card.gameObject.AddComponent<UnityEngine.UI.Image>().color=SportsBroadcastCanvas.Ink;card.gameObject.AddComponent<SportsBroadcastPlate>().cut=24;
                var text=SportsUIAuthoring.Text("Home run and credited runs",card,"HOME RUN!",new Vector2(750,160),Vector2.zero,88);
                var theme=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");text.font=theme.headingFont;text.color=SportsBroadcastCanvas.Cream;text.richText=true;text.raycastTarget=false;
                match.homeRunCard=card;match.homeRunBanner=text;card.gameObject.SetActive(false);
                EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            Debug.Log("KICKBALL_HOME_RUN_PRESENTATION_BOUND: existing scene/HUD, shared fonts/plate, DOTween and actual crowd reaction");
        }
    }
}
