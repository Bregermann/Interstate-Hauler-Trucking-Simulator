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

public sealed class CarnivalFiveModeHudTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object At(object owner,string name,int slot)=>((Array)Get(owner,name)).GetValue(slot);
    static Component Find(Scene scene,string name)=>scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Component>(true)).Single(component=>component!=null&&component.GetType().Name==name);
    static Rect Pixels(RectTransform rect,Camera camera){var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(corner=>RectTransformUtility.WorldToScreenPoint(camera,corner)).ToArray();return Rect.MinMaxRect(points.Min(point=>point.x),points.Min(point=>point.y),points.Max(point=>point.x),points.Max(point=>point.y));}
    static void Within(Rect inner,Rect outer){Assert.That(inner.xMin,Is.GreaterThanOrEqualTo(outer.xMin-2));Assert.That(inner.xMax,Is.LessThanOrEqualTo(outer.xMax+2));Assert.That(inner.yMin,Is.GreaterThanOrEqualTo(outer.yMin-2));Assert.That(inner.yMax,Is.LessThanOrEqualTo(outer.yMax+2));}
    static void Readable(Component text,string label){Call(text,"ForceMeshUpdate",false,false);Assert.That(Get(text,"isTextOverflowing"),Is.False,label+" "+Get(text,"text"));}
    static void Capture(Component match,string label,bool split)
    {
        var menus=(Component)Get(match,"menus");var cameras=(Camera[])Get(match,"cameras");
        var save=Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
        {
            Action<Camera> inspect=ui=>
            {
                Canvas.ForceUpdateCanvases();var status=(Component)Get(menus,"status");var backing=((GameObject)Get(menus,"hud")).transform.Find("Broadcast status backplate");Assert.That(backing,Is.Not.Null);Assert.That(backing.gameObject.activeInHierarchy,Is.EqualTo(status.gameObject.activeInHierarchy),"No empty explanation backplate.");Assert.That(status.GetComponent<RectTransform>().rect.width,Is.EqualTo(600).Within(.01));if(status.gameObject.activeInHierarchy)Readable(status,label);
                if(!split)return;
                var panels=(RectTransform[])Get(menus,"viewPanels");var texts=(Array)Get(menus,"viewTexts");
                for(int view=0;view<cameras.Length;view++)
                {
                    var card=panels[view].Find("Compact owned viewport score");Assert.That(card,Is.Not.Null);Assert.That(card.gameObject.activeInHierarchy,Is.True);Assert.That(((Component)texts.GetValue(view)).gameObject.activeInHierarchy,Is.False);var old=panels[view].Find("View data backing");if(old!=null)Assert.That(old.gameObject.activeInHierarchy,Is.False);
                    var viewport=cameras[view].rect;var bounds=new Rect(viewport.x*size.x,viewport.y*size.y,viewport.width*size.x,viewport.height*size.y);var cardBounds=Pixels((RectTransform)card,ui);Within(cardBounds,bounds);
                    foreach(string field in new[]{"Participant name","Owned score","Race status","Driving telemetry"}){var text=(Component)card.Find(field).GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true));Readable(text,label+" "+field);Within(Pixels(text.GetComponent<RectTransform>(),ui),cardBounds);}
                    var scoreBounds=Pixels((RectTransform)card.Find("Owned score"),ui);var detailBounds=Pixels((RectTransform)card.Find("Race status"),ui);Assert.That(scoreBounds.Overlaps(detailBounds),Is.False,"Separate round/attempt data must not overlap the score.");
                    var args=new object[]{view,-1,null,null};Assert.That(Call(match,"TryReadViewportCard",args),Is.True);int owner=(int)args[1];var player=At(Get(match,"setup"),"players",owner);Assert.That(Get(card.Find("Participant name").GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true)),"text"),Is.EqualTo(Get(player,"name")));Assert.That(Get(card.Find("Owned score").GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true)),"text"),Is.EqualTo(args[2]));Assert.That(Get(card.Find("Race status").GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true)),"text"),Is.EqualTo(args[3]));Assert.That(Get(card.Find("Driving telemetry").GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true)),"text"),Is.EqualTo(Call(match,"ViewportTelemetry",view)));
                }
            };
            save.Invoke(null,new object[]{menus.GetComponent<Canvas>(),cameras,size.x,size.y,"Logs/TruckSports/PlaytestRepair/CarnivalFiveModeHud/"+label+"-"+size.y+".png",inspect});
        }
    }
    [UnityTest,Timeout(120000)]public IEnumerator TrailerKeepsPhysicalHitCountDropClockBankedTotalsAndReplay()=>Inspect("TruckTrailerPlug","TrailerPlugMatch",false);
    [UnityTest,Timeout(120000)]public IEnumerator BungeeKeepsDistinctRoundAndBankedScoresAcrossOneToFourViews()=>Inspect("TruckBungeeDemolition","BungeeMatch",true);
    [UnityTest,Timeout(120000)]public IEnumerator SkeeKeepsLaneOwnerDeliveryScoreAndPixelCompositorAcrossOneToFourViews()=>Inspect("TruckSkeeBall","SkeeMatch",true);
    [UnityTest,Timeout(120000)]public IEnumerator FieldGoalKeepsBoardWindActualStateAndOriginalReplayTransport()=>Inspect("TruckFieldGoal","FieldGoalMatch",false);
    [UnityTest,Timeout(120000)]public IEnumerator CurlingKeepsRosterTeamScoreHammerAndHouseInset()=>Inspect("TruckCurling","CurlingMatch",false);
    static IEnumerator Inspect(string sceneName,string type,bool split)
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"FiveHud"+Guid.NewGuid().ToString("N")});Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);Runtime("SportsPrefs").GetMethod("SetString").Invoke(null,new object[]{"TruckSports.Weather.Preset.v1","9aad5ff5dfa09ce468205f676714f891"});
        var isolation=new GameObject("Neutral sources for typed HUD inspection");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));var pads=new Gamepad[4];Component match=null;
        try
        {
            var scene=EditorSceneManager.LoadSceneInPlayMode("Assets/"+sceneName+"/Scenes/"+sceneName+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=Find(scene,type);var setup=Get(match,"setup");Set(setup,"count",4);if(type=="CurlingMatch"){Set(match,"teamA",2);Set(match,"teamB",2);}if(type=="SkeeMatch")Set(match,"option",0);Call(match,"AllBots");Assert.That(Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(.2f);Time.timeScale=0;((Behaviour)match).enabled=false;yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Get(match,"CompactBroadcastStatus"),Is.True);Assert.That(Get(match,"Help"),Is.Not.Empty);string id=(string)Get(match,"MatchId");int receipts=(int)Get(Get(match,"ScoreJournal"),"Count");
            foreach(string instruction in new[]{"ACTION RELEASES","DIRECTION PUMPS","ACTION TUG","NO AUTO AIM","CONTACT IS THE KICK","RELEASE AT THE STRIPE","LEFT / RIGHT · EARLY CURL"})StringAssert.DoesNotContain(instruction,(string)Get(match,"Status"));
            if(split)
            {
                for(int i=0;i<4;i++)pads[i]=InputSystem.AddDevice<Gamepad>();Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});
                for(int humans=1;humans<=4;humans++)
                {
                    Call(match,"SetupScreen");for(int slot=0;slot<4;slot++){var player=At(setup,"players",slot);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),slot<humans?"WholeController":"AI"));Set(player,"pad",slot<humans?pads[slot]:null);}Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=0;id=(string)Get(match,"MatchId");receipts=(int)Get(Get(match,"ScoreJournal"),"Count");yield return new WaitForSecondsRealtime(.2f);
                    if(type=="BungeeMatch")for(int slot=0;slot<4;slot++){((int[])Get(match,"total"))[slot]=100+slot;Set(At(match,"bays",slot),"<Score>k__BackingField",7+slot);}
                    else{var rules=Get(match,"rules");for(int slot=0;slot<4;slot++){((int[])Get(rules,"totals"))[slot]=200+slot;((int[])Get(rules,"attempts"))[slot]=2+slot;}}
                    yield return new WaitForSecondsRealtime(.2f);Capture(match,sceneName+"-"+humans+"-controlled-score-fixture",true);
                    Assert.That(Get(match,"MatchId"),Is.EqualTo(id));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));
                }
            }
            else
            {
                if(type=="CurlingMatch")
                {
                    var house=(Camera)Get(match,"houseCamera");var before=house.rect;var target=house.targetTexture;var scores=(int[])Get(match,"scores");scores[0]=3;scores[1]=6;for(int slot=0;slot<4;slot++){var args=new object[]{slot,null,null};Assert.That(Call(match,"TryReadScoreCard",args),Is.True);Assert.That(args[1],Is.EqualTo((slot<2?3:6).ToString()));}Capture(match,sceneName+"-team-score-fixture",false);Assert.That(house.rect,Is.EqualTo(before));Assert.That(house.targetTexture,Is.SameAs(target));
                }
                else if(type=="FieldGoalMatch")
                {
                    var board=(Component)Get(match,"stadiumBoard");Assert.That(board,Is.Not.Null,"Keep the existing authored world board.");Call(board,"Refresh");Assert.That(((Component)Get(board,"controls")).gameObject.activeSelf,Is.False);var buttons=((GameObject[])Get(Get(match,"menus"),"replayButtons"));Assert.That(buttons.All(button=>button!=null),Is.True);Assert.That((Vector3)Get(match,"CurrentWind"),Is.EqualTo((Vector3)Get(Get(match,"atmosphere"),"Flow")));Capture(match,sceneName+"-preserved-board",false);
                    var phase=Get(match,"Phase");Set(match,"<Phase>k__BackingField",Enum.Parse(phase.GetType(),"Replay"));yield return new WaitForSecondsRealtime(.2f);Assert.That(Get(match,"ReplayControls"),Is.True);Assert.That(buttons.All(button=>button.activeInHierarchy),Is.True,"The same original replay transport remains usable when explanatory controls text is hidden.");Assert.That(((Component)Get(board,"controls")).gameObject.activeSelf,Is.False);Set(match,"<Phase>k__BackingField",phase);
                }
                else
                {
                    StringAssert.Contains("READY",(string)Get(match,"Status"));var rules=Get(match,"rules");Set(match,"<Phase>k__BackingField",Enum.Parse(Get(match,"Phase").GetType(),"Flight"));Set(rules,"<DistinctHits>k__BackingField",3);((int[])Get(rules,"score"))[0]=900;Set(match,"<Age>k__BackingField",4f);StringAssert.Contains("3 IMPACTS",(string)Get(match,"Status"));StringAssert.Contains("4s",(string)Get(match,"Status"));var args=new object[]{0,null,null};Assert.That(Call(match,"TryReadScoreCard",args),Is.True);Assert.That(args[1],Is.EqualTo("900"));Capture(match,sceneName+"-controlled-hit-and-bank-fixture",false);
                }
                Assert.That(Get(match,"MatchId"),Is.EqualTo(id));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));
            }
            Call(match,"Pause",0,false);Assert.That(Get(match,"Paused"),Is.True);yield return new WaitForSecondsRealtime(.15f);Assert.That(Get(match,"MatchId"),Is.EqualTo(id));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));Call(match,"Resume");Time.timeScale=0;
            Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=0;Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(id));Call(match,"SetupScreen");EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;match=null;
            TestContext.WriteLine("FIVE_MODE_TYPED_HUD "+sceneName+"; saved native start/rematch; controlled frozen score-field fixtures verify read-only translation/ownership and720/1080 layout. Not native scoring or complete-match/human/device acceptance.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }
}
#endif
