#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CarnivalCompactHudTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length&&method.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(owner,args);
    static object At(object owner,string name,int index)=>((Array)Get(owner,name)).GetValue(index);
    static bool ReadyCargo(object match){var cargo=(int[])Get(Get(match,"rules"),"carried");return Enumerable.Range(0,(int)Get(Get(match,"setup"),"count")).Any(slot=>cargo[slot]>0&&!(bool)Get(At(match,"hoppers",slot),"Open"));}
    static Rect Pixels(RectTransform rect,Camera camera){var corners=new Vector3[4];rect.GetWorldCorners(corners);var p=corners.Select(corner=>RectTransformUtility.WorldToScreenPoint(camera,corner)).ToArray();return Rect.MinMaxRect(p.Min(point=>point.x),p.Min(point=>point.y),p.Max(point=>point.x),p.Max(point=>point.y));}
    static void Within(Rect rect,Rect parent){Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(parent.xMin-2));Assert.That(rect.xMax,Is.LessThanOrEqualTo(parent.xMax+2));Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(parent.yMin-2));Assert.That(rect.yMax,Is.LessThanOrEqualTo(parent.yMax+2));}
    static void Capture(Component match,Component hud,bool volley,string label)
    {
        var menus=(Component)Get(match,"menus");var cameras=(Camera[])Get(match,"cameras");var save=Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
        {
            Action<Camera> inspect=ui=>
            {
                Canvas.ForceUpdateCanvases();var items=(Array)Get(hud,volley?"views":"cards");bool split=(bool)Get(match,"MultiViewHUD");
                for(int view=0;view<items.Length;view++)
                {
                    var item=items.GetValue(view);var rect=(RectTransform)Get(item,"root");if(!rect.gameObject.activeInHierarchy)continue;var bounds=Pixels(rect,ui);Rect pane=!split&&!volley?new Rect(0,0,size.x,size.y):new Rect(cameras[view].rect.x*size.x,cameras[view].rect.y*size.y,cameras[view].rect.width*size.x,cameras[view].rect.height*size.y);Within(bounds,pane);
                    Assert.That(bounds.height,Is.LessThan(size.y*.13f),"Each view must remain a compact score/card, not a huge panel.");
                    var texts=rect.GetComponentsInChildren<Component>().Where(component=>component.GetType().GetProperty("fontSize")!=null).ToArray();
                    foreach(var text in texts){Call(text,"ForceMeshUpdate",false,false);Assert.That(Get(text,"isTextOverflowing"),Is.False,((Component)text).name+" content="+Get(text,"text")+" font="+Get(text,"fontSize")+" rect="+((RectTransform)((Component)text).transform).rect+" preferredHeight="+Get(text,"preferredHeight"));}
                    if(volley)
                    {
                        var teams=(Array)Get(item,"teams");for(int team=0;team<2;team++){var cell=teams.GetValue(team);var name=Pixels(((Component)Get(cell,"name")).GetComponent<RectTransform>(),ui);var points=Pixels(((Component)Get(cell,"points")).GetComponent<RectTransform>(),ui);Within(name,bounds);Within(points,bounds);Assert.That(name.Overlaps(points),Is.False);}
                    }
                    else
                    {
                        var fields=new[]{"name","points","cargo"}.Select(field=>Pixels(((Component)Get(item,field)).GetComponent<RectTransform>(),ui)).ToArray();foreach(var field in fields)Within(field,bounds);for(int i=0;i<fields.Length;i++)for(int j=i+1;j<fields.Length;j++)Assert.That(fields[i].Overlaps(fields[j]),Is.False);
                        var portrait=(UnityEngine.UI.Image)Get(item,"portrait");Assert.That(portrait.sprite,Is.Not.Null);var portraitBounds=Pixels(portrait.rectTransform,ui);Within(portraitBounds,bounds);foreach(var field in fields)Assert.That(portraitBounds.Overlaps(field),Is.False);var prompt=(RectTransform)Get(item,"promptRoot");if(prompt.gameObject.activeInHierarchy)Within(Pixels(prompt,ui),pane);
                    }
                }
                if(!volley)
                {
                    var clocks=(Array)Get(hud,"clocks");Assert.That(clocks.Length,Is.EqualTo(4));
                    for(int view=0;view<clocks.Length;view++)
                    {
                        var clock=clocks.GetValue(view);var rect=(RectTransform)Get(clock,"root");if(!rect.gameObject.activeInHierarchy)continue;var viewport=split?cameras[view].rect:new Rect(0,0,1,1);
                        Within(Pixels(rect,ui),new Rect(viewport.x*size.x,viewport.y*size.y,viewport.width*size.x,viewport.height*size.y));
                        var text=Get(clock,"text");StringAssert.Contains("LEFT",(string)Get(text,"text"));Call(text,"ForceMeshUpdate",false,false);Assert.That(Get(text,"isTextOverflowing"),Is.False,"Actual match clock must remain readable.");
                    }
                }
            };
            save.Invoke(null,new object[]{menus.GetComponent<Canvas>(),cameras,size.x,size.y,"Logs/TruckSports/PlaytestRepair/CarnivalCompactHud/"+(volley?"Volley-":"Hauler-")+label+"-"+size.y+".png",inspect});
        }
    }
    static void AssertData(Component match,Component hud,bool volley)
    {
        var rules=Get(match,"rules");var items=(Array)Get(hud,volley?"views":"cards");
        for(int view=0;view<items.Length;view++)
        {
            var item=items.GetValue(view);if(!((RectTransform)Get(item,"root")).gameObject.activeInHierarchy)continue;
            if(volley)
            {
                for(int team=0;team<2;team++){var cell=At(item,"teams",team);Assert.That(Get(Get(cell,"name"),"text"),Is.EqualTo(Call(match,"TeamName",team)));Assert.That(Get(Get(cell,"points"),"text"),Is.EqualTo(((int[])Get(rules,"points"))[team].ToString()));Assert.That(((UnityEngine.UI.Image)Get(cell,"color")).color,Is.EqualTo((Color)Call(match,"TeamColor",team)));}
            }
            else
            {
                int owner=(int)Get(item,"owner");int expected=(bool)Get(match,"MultiViewHUD")?((int[])Get(Get(Get(match,"drivingViews"),"rally"),"owners"))[view]:view;Assert.That(owner,Is.EqualTo(expected));var player=At(Get(match,"setup"),"players",owner);Assert.That(Get(Get(item,"name"),"text"),Is.EqualTo(Get(player,"name")));Assert.That(Get(Get(item,"points"),"text"),Is.EqualTo(((int[])Get(rules,"points"))[owner].ToString()));Assert.That(Get(Get(item,"cargo"),"text"),Is.EqualTo(((int[])Get(rules,"carried"))[owner]+" / "+Get(rules,"Capacity")+" CARGO"));Assert.That(((UnityEngine.UI.Image)Get(item,"color")).color,Is.EqualTo((Color)Get(player,"color")));Assert.That(Get(Get(item,"prompt"),"text"),Is.EqualTo(Call(match,"DumpContext",owner)??""));
            }
        }
        var menus=Get(match,"menus");Assert.That(Get(Get(menus,"header"),"enabled"),Is.False);Assert.That(Get(Get(menus,"status"),"enabled"),Is.False);Assert.That(Get(Get(menus,"standings"),"enabled"),Is.False);foreach(var text in (Array)Get(menus,"viewTexts"))Assert.That(Get(text,"enabled"),Is.False);
    }

    [UnityTest,Timeout(240000)]public IEnumerator VolleySavedCompactBugUsesRealNativePointsAcrossSharedAndOneToFourOwnedViews()=>Inspect(true);
    [UnityTest,Timeout(300000)]public IEnumerator HaulerSavedCompactCardsUseRealNativeBankAndCargoAcrossSharedAndOwnedViews()=>Inspect(false);
    static IEnumerator Inspect(bool volley)
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"CarnivalCompactHud"});Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);var isolation=new GameObject("Carnival compact HUD original native data and software view owners");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));var pads=new Gamepad[4];Component match=null;
        try
        {
            string mode=volley?"TruckVolleyball":"TruckHungryHaulers";EditorSceneManager.LoadSceneInPlayMode("Assets/"+mode+"/Scenes/"+mode+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType((volley?"LWS.TruckVolleyball.VolleyMatch":"LWS.TruckHungryHaulers.HaulerMatch")+", Assembly-CSharp",true));var setup=Get(match,"setup");if(volley){Set(match,"teamA",2);Set(match,"teamB",2);}Set(setup,"count",4);Call(match,"AllBots");Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});var views=Get(match,"drivingViews");Call(views,"Select",0);
            var hudType=Type.GetType((volley?"LWS.TruckVolleyball.VolleyScorebug":"LWS.TruckHungryHaulers.HaulerCargoHud")+", Assembly-CSharp",true);var hud=((Component)Get(match,"menus")).GetComponent(hudType);Assert.That(hud,Is.Not.Null,"Lead must run the explicit saved mode HUD author hook first.");Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=3;var rules=Get(match,"rules");float until=Time.realtimeSinceStartup+(volley?90:150);
            while(!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<until&&((int)Get(Get(match,"ScoreJournal"),"Count")==0||!volley&&!ReadyCargo(match)))yield return new WaitForFixedUpdate();
            Assert.That(Get(match,"Finished"),Is.False,"Actual gameplay must supply data before the result page.");Assert.That((int)Get(Get(match,"ScoreJournal"),"Count"),Is.GreaterThan(0),"HUD tests may not invent points.");if(!volley)Assert.That(ReadyCargo(match),Is.True,"Actual intake cargo in a closed hopper is required for the read-only contextual dump fixture.");
            ((Behaviour)match).enabled=false;Time.timeScale=0;int receipts=(int)Get(Get(match,"ScoreJournal"),"Count");var actualPoints=((int[])Get(rules,"points")).ToArray();var actualCargo=volley?null:((int[])Get(rules,"carried")).ToArray();Call(hud,"LateUpdate");AssertData(match,hud,volley);Capture(match,hud,volley,"shared-actual-data");Assert.That(Get(hud,volley?"VisibleBugs":"VisibleCards"),Is.EqualTo(volley?1:4));
            for(int i=0;i<4;i++)pads[i]=InputSystem.AddDevice<Gamepad>("Carnival compact HUD software owner "+i);int carrier=volley?3:Enumerable.Range(0,4).First(slot=>actualCargo[slot]>0&&!(bool)Get(At(match,"hoppers",slot),"Open"));
            for(int humans=1;humans<=4;humans++)
            {
                Call(match,"AllBots");var selected=new[]{carrier,3,1,0,2}.Distinct().Take(humans).ToArray();foreach(int slot in selected){var player=At(setup,"players",slot);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"WholeController"));Set(player,"pad",pads[slot]);}
                Call(views,"Select",1);yield return null;Call(hud,"LateUpdate");Assert.That(Get(hud,volley?"VisibleBugs":"VisibleCards"),Is.EqualTo(humans));AssertData(match,hud,volley);Capture(match,hud,volley,humans+"-actual-owner-views");
                if(!volley&&humans==1)
                {
                    // UI context pose fixture only: no rules/cargo/points are
                    // supplied and no ACTION or dump is executed here.
                    var bank=(Transform)At(match,"banks",carrier);var truck=At(match,"trucks",carrier);Call(truck,"Prepare",bank.position+bank.forward*3.7f+Vector3.up*.65f,Quaternion.LookRotation(bank.forward),true);Call(hud,"LateUpdate");Assert.That(Call(match,"CanDump",carrier),Is.True);StringAssert.Contains("SOUTH · DUMP",(string)Call(match,"DumpContext",carrier));AssertData(match,hud,volley);Capture(match,hud,volley,"owned-contextual-dump-pose");
                }
            }
            Call(match,"Pause",0,false);Call(hud,"LateUpdate");Assert.That(Get(hud,volley?"VisibleBugs":"VisibleCards"),Is.Zero);Assert.That(((RectTransform)Get(hud,"root")).gameObject.activeSelf,Is.False);Call(match,"Resume");Time.timeScale=0;Call(hud,"LateUpdate");AssertData(match,hud,volley);Assert.That((int[])Get(rules,"points"),Is.EqualTo(actualPoints));if(!volley)Assert.That((int[])Get(rules,"carried"),Is.EqualTo(actualCargo));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));
            string id=(string)Get(match,"MatchId");Call(match,"SetupScreen");Call(match,"AllBots");Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=0;Call(views,"Select",0);Call(hud,"LateUpdate");Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(id));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That((int[])Get(rules,"points"),Is.All.Zero);if(!volley)Assert.That((int[])Get(rules,"carried"),Is.All.Zero);AssertData(match,hud,volley);Assert.That(((Component)Get(match,"menus")).GetComponents(hudType).Length,Is.EqualTo(1));
            TestContext.WriteLine("ACTUAL_NATIVE_DATA_COMPACT_HUD mode="+mode+" points="+string.Join(",",actualPoints)+" cargo="+(actualCargo==null?"n/a":string.Join(",",actualCargo))+"; actual native rules receipts/intakes before frozen controlled720/1080shared+1–4software-owned viewport captures, correct names/colors/portraits/cargo/points, nonoverlapping compact geometry, contextual dump pose only, pause/rematch and no score mutation. Not human/device/full-match/visual approval.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }
}
#endif
