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

public sealed class CarnivalSixArenaHudTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object At(object owner,string name,int index)=>((Array)Get(owner,name)).GetValue(index);
    static Component Find(Scene scene,string name)=>scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Component>(true)).Single(component=>component!=null&&component.GetType().Name==name);
    static string Text(object item)=>(string)Get(item,"text");
    static Component Label(Transform root,string name)=>(Component)root.Find(name).GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true));
    static string Official(object rules){string value=JsonUtility.ToJson(rules);foreach(string name in new[]{"Clock","Round","Heat","Winner","RoundWinner","TargetTurns","NetDegrees","Complete","Started"})if(rules.GetType().GetProperty(name,Any)!=null)value+="|"+name+"="+Get(rules,name);foreach(string name in new[]{"scores","score","points","ringouts","knockoffs","wins","survived","alive","attached","combo"})if(rules.GetType().GetField(name,Any)!=null)value+="|"+name+"="+string.Join(",",((Array)Get(rules,name)).Cast<object>());return value;}
    static Rect Pixels(RectTransform rect,Camera camera){var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(point=>RectTransformUtility.WorldToScreenPoint(camera,point)).ToArray();return Rect.MinMaxRect(points.Min(point=>point.x),points.Min(point=>point.y),points.Max(point=>point.x),points.Max(point=>point.y));}
    static void Within(Rect inner,Rect outer){Assert.That(inner.xMin,Is.GreaterThanOrEqualTo(outer.xMin-2));Assert.That(inner.yMin,Is.GreaterThanOrEqualTo(outer.yMin-2));Assert.That(inner.xMax,Is.LessThanOrEqualTo(outer.xMax+2));Assert.That(inner.yMax,Is.LessThanOrEqualTo(outer.yMax+2));}
    static void Readable(Component text){Call(text,"ForceMeshUpdate",false,false);Assert.That(Get(text,"isTextOverflowing"),Is.False,text.name+" "+Text(text));}
    static void Capture(Component match,string label,bool split)
    {
        var menus=(Component)Get(match,"menus");var cameras=(Camera[])Get(match,"cameras");var save=Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
        {
            Action<Camera> inspect=ui=>
            {
                Canvas.ForceUpdateCanvases();var status=(Component)Get(menus,"status");var backing=((GameObject)Get(menus,"hud")).transform.Find("Broadcast status backplate");Assert.That(backing,Is.Not.Null);Assert.That(status.GetComponent<RectTransform>().rect.width,Is.EqualTo(600).Within(.01));Assert.That(backing.gameObject.activeInHierarchy,Is.EqualTo(status.gameObject.activeInHierarchy),"No empty retired instruction plate.");if(status.gameObject.activeInHierarchy)Readable(status);
                if(!split)return;var panels=(RectTransform[])Get(menus,"viewPanels");
                for(int view=0;view<cameras.Length;view++)
                {
                    if(!cameras[view].enabled)continue;var card=panels[view].Find("Compact owned viewport score");Assert.That(card,Is.Not.Null);Assert.That(card.gameObject.activeInHierarchy,Is.True);Assert.That(((Component)At(menus,"viewTexts",view)).gameObject.activeInHierarchy,Is.False);var old=panels[view].Find("View data backing");if(old!=null)Assert.That(old.gameObject.activeInHierarchy,Is.False);
                    var viewport=cameras[view].rect;var pane=new Rect(viewport.x*size.x,viewport.y*size.y,viewport.width*size.x,viewport.height*size.y);var bounds=Pixels((RectTransform)card,ui);Within(bounds,pane);var args=new object[]{view,-1,null,null};Assert.That(Call(match,"TryReadViewportCard",args),Is.True);int owner=(int)args[1];Assert.That(Text(Label(card,"Participant name")),Is.EqualTo(Get(At(Get(match,"setup"),"players",owner),"name")));Assert.That(Text(Label(card,"Owned score")),Is.EqualTo(args[2]));Assert.That(Text(Label(card,"Race status")),Is.EqualTo(args[3]));Assert.That(Text(Label(card,"Driving telemetry")),Is.EqualTo(Call(match,"ViewportTelemetry",view)));
                    foreach(string field in new[]{"Participant name","Owned score","Race status","Driving telemetry"}){var text=Label(card,field);Readable(text);Within(Pixels(text.GetComponent<RectTransform>(),ui),bounds);}Assert.That(Pixels(Label(card,"Owned score").GetComponent<RectTransform>(),ui).Overlaps(Pixels(Label(card,"Race status").GetComponent<RectTransform>(),ui)),Is.False);var prompt=card.Find("Contextual action");if(prompt!=null&&prompt.gameObject.activeInHierarchy)Within(Pixels((RectTransform)prompt,ui),pane);
                }
            };
            save.Invoke(null,new object[]{menus.GetComponent<Canvas>(),cameras,size.x,size.y,"Logs/TruckSports/PlaytestRepair/CarnivalSixArenaHud/"+label+"-"+size.y+".png",inspect});
        }
    }
    static void Fixture(Component match,string mode)
    {
        var rules=Get(match,"rules");int count=(int)Get(Get(match,"setup"),"count");
        if(mode=="Lava")
        {
            Set(match,"<Countdown>k__BackingField",0f);var score=(int[])Get(rules,"scores");for(int slot=0;slot<count;slot++){score[slot]=5+slot;((float[])Get(rules,"survived"))[slot]=24+slot;((int[])Get(rules,"knockoffs"))[slot]=slot%3;}var current=(int[])Get(match,"current");current[0]=12;var platforms=(Array)Get(rules,"platforms");platforms.SetValue(Enum.Parse(platforms.GetValue(12).GetType(),"Warning"),12);((float[])Get(rules,"deadline"))[12]=1.4f;((float[])Get(match,"jumpAt"))[0]=.7f;
        }
        else if(mode=="Tether")
        {
            Set(match,"<Countdown>k__BackingField",0f);Set(rules,"<NetDegrees>k__BackingField",720f);((int[])Get(rules,"wins"))[0]=1;((int[])Get(rules,"wins"))[1]=0;
        }
        else if(mode=="Snake")
        {
            Set(match,"<Intro>k__BackingField",0f);Set(match,"noticeUntil",0f);for(int slot=0;slot<count;slot++){((int[])Get(rules,"score"))[slot]=70+slot;((int[])Get(rules,"combo"))[slot]=2+slot;((float[])Get(rules,"distance"))[slot]=80+slot;((float[])Get(rules,"survived"))[slot]=15+slot;}((float[])Get(match,"nextJump"))[0]=Time.time+1.5f;
        }
        else
        {
            Set(match,"<Stage>k__BackingField",Enum.Parse(Get(match,"Stage").GetType(),mode=="Sumo"?"Fight":mode=="Log"?"Balance":"Playing"));Set(match,"<Clock>k__BackingField",4f);var wins=(int[])Get(rules,"wins");for(int slot=0;slot<wins.Length;slot++)wins[slot]=slot%3;
            for(int slot=0;slot<count;slot++){var fighter=At(match,"fighters",slot);if(mode=="Sumo"){((int[])Get(rules,"points"))[slot]=10+slot;((int[])Get(rules,"ringouts"))[slot]=slot;Set(fighter,"cooldown",1.2f);}else Set(fighter,"<Cooldown>k__BackingField",1.2f);}
        }
    }
    static void AssertSource(Component match,string mode)
    {
        var rules=Get(match,"rules");int count=(int)Get(Get(match,"setup"),"count");
        for(int slot=0;slot<count;slot++)
        {
            var args=new object[]{slot,null,null};Assert.That(Call(match,"TryReadScoreCard",args),Is.True);string expected=mode=="Lava"?((int[])Get(rules,"scores"))[slot]+"P":mode=="Snake"?((int[])Get(rules,"score"))[slot].ToString():mode=="Sumo"?((int[])Get(rules,"points"))[slot].ToString():mode=="Dodge"?((int[])Get(rules,"wins"))[(int)Call(rules,"Team",slot)].ToString():mode=="Tether"?((int[])Get(rules,"wins"))[slot]+" / "+((int)Get(rules,"BestOf")/2+1):((int[])Get(rules,"wins"))[slot].ToString();Assert.That(args[1],Is.EqualTo(expected));
        }
        if(mode=="Lava")StringAssert.Contains("1.4s",(string)Get(match,"Status"));if(mode=="Tether"){Assert.That(Call(rules,"Progress",0),Is.EqualTo(.5f));Assert.That(Call(rules,"Progress",1),Is.EqualTo(0f));}if(mode=="Sumo"||mode=="Log")Assert.That(Get(match,"Status"),Is.EqualTo(""),"Calm active play needs no permanent status plate.");
    }
    [UnityTest,Timeout(150000)]public IEnumerator LavaPreservesNativeSurvivalFloorWarningAndJumpStateAcrossOwnedViews()=>Inspect("TruckFloorIsLava","Lava");
    [UnityTest,Timeout(150000)]public IEnumerator LogPreservesSharedBraceCooldownHeatWinsAndRoundClock()=>Inspect("TruckLogRolling","Log");
    [UnityTest,Timeout(150000)]public IEnumerator SumoPreservesSharedPointsRingoutsHeatWinsAndCostumeBodies()=>Inspect("TruckSumo","Sumo");
    [UnityTest,Timeout(150000)]public IEnumerator DodgePreservesTeamOwnerSurvivalStrikeClockAndBallOwnership()=>Inspect("TruckTrampolineDodgeball","Dodge");
    [UnityTest,Timeout(150000)]public IEnumerator SnakePreservesAttachedCargoMultiplierTimedClockAndJumpContext()=>Inspect("TruckSnake","Snake");
    [UnityTest,Timeout(150000)]public IEnumerator TetherPreservesActualOwnerDirectionAndFractionalWrapProgress()=>Inspect("TruckTetherball","Tether");
    static IEnumerator Inspect(string sceneName,string mode)
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"SixArenaHud"+Guid.NewGuid().ToString("N")});Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);Runtime("SportsPrefs").GetMethod("SetString").Invoke(null,new object[]{"TruckSports.Weather.Preset.v1","9aad5ff5dfa09ce468205f676714f891"});var isolation=new GameObject("Neutral six-arena HUD validation sources");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));var pads=new Gamepad[4];Component match=null;
        try
        {
            var scene=EditorSceneManager.LoadSceneInPlayMode("Assets/"+sceneName+"/Scenes/"+sceneName+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=Find(scene,mode+"Match");var setup=Get(match,"setup");Set(setup,"count",(int)Get(match,"MaximumPlayers"));if(mode=="Dodge"){Set(match,"teamA",4);Set(match,"teamB",4);Set(match,"option",0);}if(mode=="Snake")Set(match,"option",1);Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});Call(match,"AllBots");Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=0;((Behaviour)match).enabled=false;Fixture(match,mode);yield return new WaitForSecondsRealtime(.2f);AssertSource(match,mode);
            Assert.That(Get(match,"CompactBroadcastStatus"),Is.True);Assert.That(Get(match,"Help"),Is.Not.Empty);foreach(string rule in new[]{"ACTION: BRACE","ACTION: SHOVE","TOUCH STARTS","ACTION TO STRIKE","GREEN +1","LINE UP YOUR BUMPER"})StringAssert.DoesNotContain(rule,(string)Get(match,"Status"));int receipts=(int)Get(Get(match,"ScoreJournal"),"Count");string id=(string)Get(match,"MatchId");string original=Official(Get(match,"rules"));var bodies=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Rigidbody>(true)).Select(body=>(body,body.position,body.rotation,body.linearVelocity,body.angularVelocity)).ToArray();for(int read=0;read<30;read++)AssertSource(match,mode);Assert.That(Official(Get(match,"rules")),Is.EqualTo(original));foreach(var body in bodies){Assert.That(body.body.position,Is.EqualTo(body.position));Assert.That(body.body.rotation,Is.EqualTo(body.rotation));Assert.That(body.body.linearVelocity,Is.EqualTo(body.linearVelocity));Assert.That(body.body.angularVelocity,Is.EqualTo(body.angularVelocity));}Capture(match,sceneName+"-shared-fixture",false);
            if(mode!="Log"&&mode!="Sumo")
            {
                for(int slot=0;slot<4;slot++)pads[slot]=InputSystem.AddDevice<Gamepad>();Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});int count=(int)Get(setup,"count");int maximum=Math.Min(4,count);
                for(int humans=1;humans<=maximum;humans++)
                {
                    var humanSlots=mode=="Dodge"?new[]{0,4,1,5}:new[]{0,1,2,3};for(int slot=0;slot<count;slot++){int human=Array.IndexOf(humanSlots,slot,0,humans);var player=At(setup,"players",slot);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),human>=0?"WholeController":"AI"));Set(player,"pad",human>=0?pads[human]:null);}
                    object views;if(mode=="Dodge"){Set(match,"option",1);Call(match,"Layout");views=Get(match,"views");}else{Call(Get(match,"drivingViews"),"Select",1);views=Get(Get(match,"drivingViews"),"rally");}yield return new WaitForSecondsRealtime(.2f);var owners=(int[])Get(views,"owners");for(int view=0;view<humans;view++){var args=new object[]{view,-1,null,null};Assert.That(Call(match,"TryReadViewportCard",args),Is.True);Assert.That(args[1],Is.EqualTo(owners[view]));if(mode=="Tether")StringAssert.Contains(owners[view]==0?"CCW":"CW",(string)args[3]);if(mode=="Snake")StringAssert.Contains("180s LEFT",(string)Call(match,"ViewportTelemetry",view));if(mode=="Dodge")StringAssert.Contains("BALLS",(string)args[3]);}Capture(match,sceneName+"-"+humans+"-software-owned-fixture",true);
                }
            }
            Assert.That(Get(match,"MatchId"),Is.EqualTo(id));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));Assert.That(Official(Get(match,"rules")),Is.EqualTo(original));Call(match,"Pause",0,false);Assert.That(Get(match,"Paused"),Is.True);yield return new WaitForSecondsRealtime(.15f);Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));Call(match,"Resume");Time.timeScale=0;Call(match,"SetupScreen");Call(match,"AllBots");Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=0;Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(id));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Call(match,"SetupScreen");EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;match=null;TestContext.WriteLine("CARNIVAL_SIX_ARENA_HUD "+mode+"; saved native start/pause/rematch, frozen typed score/state fixtures,720/1080 shared and supported1–4software-owner views (Tether maximum2); original arena-only Log/Sumo cameras retained. No physical scoring, full-match, human/device or visual approval claim.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }
}
#endif
