using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    // Adds an entry to the existing Your Crew cards. Does not regenerate front
    // pages, logos, sport selections or designer-edited customizer content.
    public static class SportsTruckCustomizerEntryAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Install Your Crew Entries")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save hand-edited open scenes before explicit authoring.");
            if(Resources.Load<GameObject>(SportsTruckCustomizerAuthoring.ResourcePath)==null||Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog")==null)throw new InvalidOperationException("Author and validate the reusable body catalog and Heat shop first.");
            var scene=EditorSceneManager.OpenScene(SportsBuilder.FrontScene);
            var menu=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsParticipantsMenu>(true)).Single();
            var page=menu.front.pages[menu.page];var existing=page.GetComponentsInChildren<SportsMenuAction>(true).Count(action=>action.action.StartsWith("peoplecustom",StringComparison.Ordinal));
            if(existing==4){Debug.Log("TRUCK_CUSTOMIZER_ENTRIES_PRESERVED existing designer-edited four entries retained.");return;}
            if(existing!=0)throw new InvalidOperationException("Partial customizer entries exist; inspect them before authoring.");
            for(int i=0;i<4;i++)
            {
                var card=page.transform.Find("Driver card "+(i+1)) as RectTransform;if(card==null)throw new InvalidOperationException("Original Your Crew card is missing.");
                card.sizeDelta=new Vector2(660,245);card.anchoredPosition=new Vector2(i%2==0?-350:350,i<2?150:-135);
                var name=(RectTransform)menu.names[i].transform;name.anchoredPosition+=Vector2.up*20;
                var details=menu.details[i].rectTransform;details.anchoredPosition+=Vector2.up*20;
                foreach(var button in new[]{menu.profiles[i],menu.colors[i],menu.kinds[i]})((RectTransform)button.transform).anchoredPosition+=Vector2.up*12;
                var newDriver=card.GetComponentsInChildren<SportsMenuAction>(true).Single(action=>action.action=="peoplenew"+i);((RectTransform)newDriver.transform).anchoredPosition+=Vector2.up*20;
                var custom=SportsUIAuthoring.Button(card,"CUSTOMIZE TRUCK","peoplecustom"+i,new Vector2(0,-101),new Vector2(600,36));
                custom.normalTextObj.font=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").bodyFont;custom.normalTextObj.fontSize=19;custom.normalTextObj.fontSizeMax=19;custom.normalTextObj.fontSizeMin=17;
            }
            menu.notice.rectTransform.anchoredPosition=new Vector2(0,-308);menu.notice.rectTransform.sizeDelta=new Vector2(1280,78);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("TRUCK_CUSTOMIZER_CREW_ENTRIES_SAVED four named-profile entries; original sport cards, names, colors and controller assignments retained.");
        }
    }
}
