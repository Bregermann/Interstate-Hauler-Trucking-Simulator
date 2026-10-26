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
using UnityEngine.UI;

public sealed class CarnivalContextHudTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static Component Find(Scene scene,string name)=>scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Component>(true)).Single(component=>component!=null&&component.GetType().Name==name);
    static Rect Pixels(RectTransform rect,Camera camera){var corners=new Vector3[4];rect.GetWorldCorners(corners);var p=corners.Select(c=>RectTransformUtility.WorldToScreenPoint(camera,c)).ToArray();return Rect.MinMaxRect(p.Min(v=>v.x),p.Min(v=>v.y),p.Max(v=>v.x),p.Max(v=>v.y));}
    static void Within(Rect inner,Rect outer){Assert.That(inner.xMin,Is.GreaterThanOrEqualTo(outer.xMin-2));Assert.That(inner.yMin,Is.GreaterThanOrEqualTo(outer.yMin-2));Assert.That(inner.xMax,Is.LessThanOrEqualTo(outer.xMax+2));Assert.That(inner.yMax,Is.LessThanOrEqualTo(outer.yMax+2));}
    static void Capture(Component match,string label,bool split)
    {
        var menus=(Component)Get(match,"menus");var cameras=(Camera[])Get(match,"cameras");
        var save=Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
        {
            Action<Camera> inspect=ui=>
            {
                var hud=(GameObject)Get(menus,"hud");var status=(Component)Get(menus,"status");var statusRect=status.GetComponent<RectTransform>();var backdrop=hud.transform.Find("Broadcast status backplate");
                Assert.That(statusRect.rect.width,Is.EqualTo(600).Within(.01));
                Assert.That(backdrop,Is.Not.Null);
                Assert.That(backdrop.gameObject.activeInHierarchy,Is.EqualTo(status.gameObject.activeInHierarchy),"Never leave an empty instruction backplate.");
                var countdownType=Type.GetType("LWS.TruckTerrierRacing.TerrierCountdown, Assembly-CSharp",true);
                foreach(var countdown in UnityEngine.Object.FindObjectsByType(countdownType,FindObjectsInactive.Exclude).Cast<Component>().Where(item=>item.gameObject.scene==match.gameObject.scene))
                {
                    int beat=(int)Get(countdown,"Beat");var countdownPanels=(RectTransform[])Get(countdown,"panels");var labels=(Array)Get(countdown,"labels");
                    for(int view=0;view<countdownPanels.Length;view++)if(countdownPanels[view].gameObject.activeInHierarchy)Assert.That((string)Get(labels.GetValue(view),"text"),Is.EqualTo(beat==0?"GO GO GO!":beat.ToString()),"Newly enabled views must show the current authoritative beat, including GO.");
                }
                if(status.gameObject.activeInHierarchy)Within(Pixels(statusRect,ui),new Rect(0,0,size.x,size.y));
                if(!split)return;
                var panels=(RectTransform[])Get(menus,"viewPanels");var oldTexts=(Array)Get(menus,"viewTexts");
                for(int view=0;view<cameras.Length&&view<4;view++)
                {
                    if(!cameras[view].enabled)continue;
                    var card=panels[view].Find("Compact owned viewport score");Assert.That(card,Is.Not.Null);
                    Assert.That(card.gameObject.activeInHierarchy,Is.True);
                    Assert.That(((Component)oldTexts.GetValue(view)).gameObject.activeInHierarchy,Is.False);
                    var old=panels[view].Find("View data backing");if(old!=null)Assert.That(old.gameObject.activeInHierarchy,Is.False);
                    foreach(string legacy in new[]{"Assigned truck portrait","Assigned participant color"}){var item=panels[view].Find(legacy);if(item!=null)Assert.That(item.gameObject.activeInHierarchy,Is.False,"Do not leave detached identity art from the retired viewport panel.");}
                    var viewport=cameras[view].rect;var bounds=new Rect(viewport.x*size.x,viewport.y*size.y,viewport.width*size.x,viewport.height*size.y);
                    var cardBounds=Pixels((RectTransform)card,ui);Within(cardBounds,bounds);
                    Assert.That(card.Find("Truck portrait").GetComponent<Image>().sprite,Is.Not.Null);
                    Rect? previous=null;
                    foreach(string name in new[]{"Participant name","Owned score","Driving telemetry"})
                    {
                        var text=card.Find(name).GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true));
                        Assert.That((string)Get(text,"text"),Is.Not.Empty,name);
                        Call(text,"ForceMeshUpdate",false,false);Assert.That(Get(text,"isTextOverflowing"),Is.False,label+" "+name+" "+Get(text,"text"));
                        var rect=Pixels(text.GetComponent<RectTransform>(),ui);Within(rect,cardBounds);
                        if(previous.HasValue)Assert.That(rect.Overlaps(previous.Value),Is.False);
                        previous=rect;
                    }
                }
            };
            save.Invoke(null,new object[]{menus.GetComponent<Canvas>(),cameras,size.x,size.y,"Logs/TruckSports/PlaytestRepair/CarnivalContextHud/"+label+"-"+size.y+".png",inspect});
        }
    }
    [UnityTest,Timeout(90000)]public IEnumerator WhackHasActualScoresWithoutPermanentPointLegend()=>Inspect("TruckWhackAMole","WhackMatch",true);
    [UnityTest,Timeout(90000)]public IEnumerator HillHasTargetMovementClockAndConditionalContestWarning()=>Inspect("TruckKingOfTheHill","HillMatch",true);
    [UnityTest,Timeout(90000)]public IEnumerator SeesawKeepsTiltAndTimerWithoutPermanentRules()=>Inspect("TruckSeesaw","SeesawMatch",true);
    [UnityTest,Timeout(90000)]public IEnumerator JoustKeepsPassClockAndLanceStateWithoutPointLegend()=>Inspect("TruckJousting","JoustMatch",true);
    [UnityTest,Timeout(90000)]public IEnumerator MagnetKeepsActualPolarityScrapAndClock()=>Inspect("TruckMagnetMayhem","MagnetMatch",true);
    [UnityTest,Timeout(90000)]public IEnumerator FreezeKeepsRoleIceStateAndRoundClock()=>Inspect("TruckFreezeTag","FreezeMatch",true);
    [UnityTest,Timeout(90000)]public IEnumerator HerdKeepsActualSheepCountsAndClock()=>Inspect("TruckSheepHerding","HerdMatch",true);
    [UnityTest,Timeout(90000)]public IEnumerator HideKeepsEngineLightCluesAndRoundTimeWithoutControlStrip()=>Inspect("TruckHideAndSeek","HideMatch",false);
    [UnityTest,Timeout(90000)]public IEnumerator FrogKeepsCrossingsAndTimeWithoutPermanentRetireInstructions()=>Inspect("TruckFrogger","FrogMatch",false);
    static IEnumerator Inspect(string sceneName,string type,bool compactViews)
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ContextHud"+Guid.NewGuid().ToString("N")});
        Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        Runtime("SportsPrefs").GetMethod("SetString").Invoke(null,new object[]{"TruckSports.Weather.Preset.v1","9aad5ff5dfa09ce468205f676714f891"});
        var isolation=new GameObject("Neutral software sources for context HUD inspection");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        var pads=new Gamepad[4];Component match=null;
        try
        {
            var scene=EditorSceneManager.LoadSceneInPlayMode("Assets/"+sceneName+"/Scenes/"+sceneName+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=Find(scene,type);Call(match,"AllBots");Assert.That(Call(match,"StartMatch"),Is.True);
            float deadline=Time.realtimeSinceStartup+8;while(Time.realtimeSinceStartup<deadline&&(float)Get(match,"RaceCountdownSeconds")>=0)yield return null;
            Time.timeScale=0;((Behaviour)match).enabled=false;yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Get(match,"CompactBroadcastStatus"),Is.True);Assert.That(Get(match,"Help"),Is.Not.Empty,"Rules and controls remain available in Help.");
            string id=(string)Get(match,"MatchId");int receipts=(int)Get(Get(match,"ScoreJournal"),"Count");
            var status=(string)Get(match,"Status");foreach(var instruction in new[]{"BUMPER CONTACT:","OPPOSITES ATTRACT","HOLD ACTION TO RETIRE","MUST PHYSICALLY BREAK","HIDERS DRIVE","ARMOR 1"})StringAssert.DoesNotContain(instruction,status);
            Capture(match,sceneName+"-shared",false);
            if(compactViews)
            {
                for(int i=0;i<4;i++)pads[i]=InputSystem.AddDevice<Gamepad>();Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});
                var setup=Get(match,"setup");int count=(int)Get(setup,"count");var players=(Array)Get(setup,"players");
                foreach(int humans in Enumerable.Range(1,Math.Min(4,count)))
                {
                    for(int slot=0;slot<count;slot++){var player=players.GetValue(slot);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),slot<humans?"WholeController":"AI"));Set(player,"pad",slot<humans?pads[slot]:null);}
                    Call(Get(match,"drivingViews"),"Select",1);for(int frame=0;frame<8;frame++){Call(match,"Present",.02f);yield return null;}yield return new WaitForSecondsRealtime(.2f);
                    var views=Get(Get(match,"drivingViews"),"rally");var owners=(int[])Get(views,"owners");
                    for(int view=0;view<humans;view++){var args=new object[]{view,-1,null,null};Assert.That(Call(match,"TryReadViewportCard",args),Is.True);Assert.That(args[1],Is.EqualTo(owners[view]));Assert.That(args[2],Is.Not.Null);}
                    Capture(match,sceneName+"-"+humans+"-software-owned-views",true);
                }
            }
            Assert.That(Get(match,"MatchId"),Is.EqualTo(id));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts),"Presentation must not award points.");
            ((Behaviour)match).enabled=true;Time.timeScale=1;Call(match,"AllBots");Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);yield return null;yield return null;Call(match,"SetupScreen");
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;match=null;
            TestContext.WriteLine("CARNIVAL_CONTEXT_HUD "+sceneName+"; actual saved-scene start/rematch, controlled frozen720/1080 shared/software-owned layouts; exact score journal and input ownership checks. Not a complete match or physical-device/human acceptance.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }
}
#endif
