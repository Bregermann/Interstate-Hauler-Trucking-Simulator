#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class CarnivalScrapHeapNavigationTests
{
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name).GetValue(owner);
    [UnityTest]
    public IEnumerator OriginalFiveActionsHaveConnectedGridNavigationAndScreenSafeCards()
    {
        var lease=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"CarnivalScrapNavigation"});
        var isolation=new GameObject("Private menu input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var front=UnityEngine.Object.FindAnyObjectByType(Sports("SportsFrontEnd")) as Component;front.GetType().GetMethod("Show").Invoke(front,new object[]{4});front.GetType().GetMethod("Focus").Invoke(front,null);yield return null;
            var pages=(GameObject[])Get(front,"pages");var buttons=pages[4].GetComponentsInChildren<Selectable>();Assert.That(buttons.Length,Is.EqualTo(6));
            var visited=new System.Collections.Generic.HashSet<Selectable>();var pending=new System.Collections.Generic.Queue<Selectable>();pending.Enqueue(buttons[0]);
            while(pending.Count>0){var button=pending.Dequeue();if(!visited.Add(button))continue;Assert.That(button.navigation.mode,Is.EqualTo(Navigation.Mode.Explicit));var nav=button.navigation;foreach(var target in new[]{nav.selectOnUp,nav.selectOnDown,nav.selectOnLeft,nav.selectOnRight}){Assert.That(target,Is.Not.Null);Assert.That(buttons,Does.Contain(target));if(!visited.Contains(target))pending.Enqueue(target);}}
            Assert.That(visited.Count,Is.EqualTo(6),"Every original action and Return must be reachable with directional navigation.");
            var canvas=front.GetComponent<Canvas>();var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            var save=Sports("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);
            foreach(int width in new[]{1280,1920})
            {
                Action<Camera> inspect=ui=>{
                    var boxes=buttons.Where(button=>button.GetComponentsInChildren(Sports("SportsMenuAction"),true).Any(item=>(string)Get(item,"action")!="scrapback")).Select(button=>(RectTransform)button.transform.parent).ToArray();
                    Assert.That(boxes.Length,Is.EqualTo(5));var corners=new Vector3[4];foreach(var box in boxes){box.GetWorldCorners(corners);foreach(var point in corners){var pixel=RectTransformUtility.WorldToScreenPoint(ui,point);Assert.That(pixel.x,Is.InRange(0,width));Assert.That(pixel.y,Is.InRange(0,width==1280?720:1080));}}
                };
                save.Invoke(null,new object[]{canvas,cameras,width,width==1280?720:1080,"Logs/TruckSports/PlaytestRepair/CarnivalScrapHeapNative/Preserved-five-sports-"+width+".png",inspect});
            }
            front.GetType().GetMethod("Show").Invoke(front,new object[]{1});yield return null;Assert.That(pages[1].activeSelf,Is.True);Assert.That(pages[4].activeSelf,Is.False);
        }
        finally{UnityEngine.Object.DestroyImmediate(isolation);lease.Dispose();}
    }
}
#endif
