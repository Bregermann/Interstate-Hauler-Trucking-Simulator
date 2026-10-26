#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class CarnivalArenaHudLifecycleTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object At(object owner,string name,int slot)=>((Array)Get(owner,name)).GetValue(slot);
    static string Text(object owner,string field)=>(string)Get(Get(owner,field),"text");
    static Component Find(Scene scene,string type)=>scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Component>(true)).First(component=>component!=null&&component.GetType().Name==type);
    static Rect Pixels(RectTransform rect,Camera ui){var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(corner=>RectTransformUtility.WorldToScreenPoint(ui,corner)).ToArray();return Rect.MinMaxRect(points.Min(point=>point.x),points.Min(point=>point.y),points.Max(point=>point.x),points.Max(point=>point.y));}
    static void Within(Rect child,Rect parent){Assert.That(child.xMin,Is.GreaterThanOrEqualTo(parent.xMin-2));Assert.That(child.xMax,Is.LessThanOrEqualTo(parent.xMax+2));Assert.That(child.yMin,Is.GreaterThanOrEqualTo(parent.yMin-2));Assert.That(child.yMax,Is.LessThanOrEqualTo(parent.yMax+2));}
    static void Capture(Component match,Component hud,string label,bool balloon)
    {
        var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
        {
            Action<Camera> inspect=ui=>
            {
                if(!(bool)Get(hud,"Showing"))return;Canvas.ForceUpdateCanvases();var row=(RectTransform)Get(hud,"row");var bounds=Pixels(row,ui);Within(bounds,new Rect(0,0,size.x,size.y));
                var cards=((Array)Get(hud,"cards")).Cast<object>().Where(card=>((RectTransform)Get(card,"root")).gameObject.activeInHierarchy).ToArray();
                var cardBounds=cards.Select(card=>Pixels((RectTransform)Get(card,"root"),ui)).ToArray();
                for(int i=0;i<cards.Length;i++)
                {
                    Within(cardBounds[i],bounds);for(int other=0;other<i;other++)Assert.That(cardBounds[i].Overlaps(cardBounds[other]),Is.False);
                    foreach(string field in new[]{"name","value","detail"}){var text=(Component)Get(cards[i],field);Call(text,"ForceMeshUpdate",false,false);Within(Pixels(text.GetComponent<RectTransform>(),ui),cardBounds[i]);Assert.That(Get(text,"isTextOverflowing"),Is.False,label+" "+field+" "+Get(text,"text"));}
                    var textBounds=new[]{"name","value","detail"}.Select(field=>Pixels(((Component)Get(cards[i],field)).GetComponent<RectTransform>(),ui)).ToArray();
                    for(int a=0;a<3;a++)for(int b=a+1;b<3;b++)Assert.That(textBounds[a].Overlaps(textBounds[b]),Is.False);
                    Assert.That(((Image)Get(cards[i],"portrait")).sprite,Is.Not.Null);
                    if(balloon)foreach(var pip in ((Array)Get(cards[i],"pips")).Cast<Image>()){var pipBounds=Pixels(pip.rectTransform,ui);Within(pipBounds,cardBounds[i]);foreach(var textBound in textBounds)Assert.That(pipBounds.Overlaps(textBound),Is.False);}
                }
                if(balloon){Assert.That(cards.Length,Is.EqualTo(8));Assert.That(cardBounds.Select(rect=>Mathf.RoundToInt(rect.yMin)).Distinct().Count(),Is.EqualTo(1));Assert.That(bounds.yMin,Is.LessThan(size.y*.025f),"All eight must be in ONE row at the very bottom.");}
            };
            save.Invoke(null,new object[]{((Component)Get(match,"menus")).GetComponent<Canvas>(),(Camera[])Get(match,"cameras"),size.x,size.y,"Logs/TruckSports/PlaytestRepair/CarnivalArenaHud/"+label+"-"+size.y+".png",inspect});
        }
    }
    static void Data(Component match,Component hud,string mode,int count)
    {
        Assert.That(Get(hud,"VisibleCards"),Is.EqualTo(count));var seen=new System.Collections.Generic.HashSet<int>();
        foreach(var card in ((Array)Get(hud,"cards")).Cast<object>().Where(card=>((RectTransform)Get(card,"root")).gameObject.activeInHierarchy))
        {
            int slot=(int)Get(card,"slot");Assert.That(seen.Add(slot),Is.True);var player=At(Get(match,"setup"),"players",slot);Assert.That(Text(card,"name"),Is.EqualTo(Get(player,"name")));Assert.That(((Image)Get(card,"stripe")).color,Is.EqualTo(Get(player,"color")));
            if(mode=="Keepaway"){Assert.That(Text(card,"value"),Is.EqualTo(((float[])Get(match,"possession"))[slot].ToString("F1")+"s"));if((int)Get(match,"Carrier")==slot)StringAssert.Contains("BALL",Text(card,"detail"));}
            else{var rules=Get(match,"rules");var points=(int[])Get(rules,mode=="Limbo"?"points":"scores");Assert.That(Text(card,"value"),Is.EqualTo(points[slot]+"P"));}
            if(mode=="Balloon")
            {
                Assert.That(Text(card,"detail"),Is.EqualTo(Call(hud,"SpikeState",slot)));var balloons=(bool[,])Get(Get(match,"rules"),"balloons");var pips=(Image[])Get(card,"pips");for(int p=0;p<3;p++)Assert.That(pips[p].color,Is.EqualTo(balloons[slot,p]?(Color)Get(player,"color"):new Color(.25f,.28f,.32f,.6f)));
            }
            if(mode=="Limbo")Assert.That(Text(card,"detail"),Is.EqualTo(Call(hud,"ClearanceState",slot)));
        }
        Assert.That(seen.Count,Is.EqualTo(count));
        foreach(var graphic in (Graphic[])Get(hud,"legacyGraphics"))Assert.That(graphic.enabled,Is.False,"Retire the old panels and their explanation text together.");
    }
    static void ProtectedArena(Component match,Component hud)
    {
        Call(Get(match,"drivingViews"),"Select",0);Call(hud,"LateUpdate");Assert.That(Get(hud,"Showing"),Is.False);
        var own=(RectTransform)Get(hud,"root");var original=((Component)Get(match,"menus")).GetComponentsInChildren<Component>(true).Where(component=>(component is Graphic||component is RectTransform)&&!component.transform.IsChildOf(own)).ToArray();
        var before=original.Select(component=>EditorJsonUtility.ToJson(component)).ToArray();Call(hud,"LateUpdate");
        for(int i=0;i<original.Length;i++)Assert.That(EditorJsonUtility.ToJson(original[i]),Is.EqualTo(before[i]),"Hot Potato shared Arena HUD changed: "+original[i].name);
    }
    static bool Outcome(Component match,string mode)
    {
        if(mode=="Keepaway")return (int)Get(match,"Pickups")>0&&((float[])Get(match,"possession")).Any(seconds=>seconds>.25f);
        if(mode=="HotPotato")return (int)Get(Get(match,"rules"),"Transfers")>0;
        if(mode=="Balloon")return (int)Get(match,"PhysicalPops")>0;
        return (int)Get(match,"PhysicalClears")+(int)Get(match,"PhysicalHits")>0;
    }
    static void TypedStateFixtures(Component match,Component hud,string mode)
    {
        int receipts=(int)Get(Get(match,"ScoreJournal"),"Count");
        if(mode=="Keepaway")
        {
            var shield=(float[])Get(match,"shield");float prior=shield[0];shield[0]=Time.time+1;Call(hud,"LateUpdate");StringAssert.Contains((int)Get(match,"Carrier")==0?"SAFE":"PROTECTED",Text(At(hud,"cards",0),"detail"));shield[0]=prior;
        }
        if(mode=="Balloon")
        {
            var original=Get(match,"rules");var rules=Activator.CreateInstance(original.GetType());Call(rules,"Reset",8);Set(match,"rules",rules);
            try{Assert.That(Call(rules,"Attack",0),Is.True);Assert.That(Call(hud,"SpikeState",0),Is.EqualTo("SPIKE WARNING"));Call(rules,"Step",.26f);Assert.That(Call(hud,"SpikeState",0),Is.EqualTo("SPIKE OUT"));Call(rules,"Step",.6f);StringAssert.StartsWith("READY ",(string)Call(hud,"SpikeState",0));Call(rules,"Step",3f);Assert.That(Call(hud,"SpikeState",0),Is.EqualTo("SPIKE READY"));Assert.That((int[])Get(rules,"scores"),Is.All.Zero);}
            finally{Set(match,"rules",original);}
        }
        if(mode=="Limbo")
        {
            var racer=At(match,"racers",0);object priorPhase=Get(racer,"Phase");float priorTime=(float)Get(racer,"AttemptTime");
            try{Set(racer,"<Phase>k__BackingField",Enum.Parse(priorPhase.GetType(),"Approach"));Set(racer,"<AttemptTime>k__BackingField",26f);Assert.That(Call(hud,"ClearanceState",0),Is.EqualTo("9s LEFT"));}
            finally{Set(racer,"<Phase>k__BackingField",priorPhase);Set(racer,"<AttemptTime>k__BackingField",priorTime);}
        }
        Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));Call(hud,"LateUpdate");
    }
    [UnityTest,Timeout(180000)]public IEnumerator KeepawayArenaKeepsExistingBottomBoardAndReplacesOnlyWorldNames()=>Inspect("TruckKeepaway","Keepaway",false);
    [UnityTest,Timeout(180000)]public IEnumerator KeepawayCabHasOneSharedBoardAcrossOneToFourViewsAndRetainsCompass()=>Inspect("TruckKeepawayFirstPerson","Keepaway",true);
    [UnityTest,Timeout(180000)]public IEnumerator HotPotatoModernizesOnlySplitHudAfterNativeTransfer()=>Inspect("TruckHotPotato","HotPotato",true);
    [UnityTest,Timeout(180000)]public IEnumerator BalloonNativePopKeepsEightAtVeryBottomInOneRow()=>Inspect("TruckBalloonBattle","Balloon",true);
    [UnityTest,Timeout(180000)]public IEnumerator LimboNativeBarOutcomeUsesActualClearancePointsAndHeight()=>Inspect("TruckLimbo","Limbo",true);
    static IEnumerator Inspect(string sceneName,string mode,bool views)
    {
        var prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ArenaHud"+Guid.NewGuid().ToString("N")});
        Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);Runtime("LWS.TruckSports.SportsPrefs").GetMethod("SetString").Invoke(null,new object[]{"TruckSports.Weather.Preset.v1","9aad5ff5dfa09ce468205f676714f891"});
        var isolation=new GameObject("Explicit Carnival arena HUD neutral sources");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        var pads=new Gamepad[4];Component match=null;
        try
        {
            string path=mode=="Keepaway"?"Assets/TruckKeepaway/Scenes/"+sceneName+".unity":"Assets/"+sceneName+"/Scenes/"+sceneName+".unity";
            var scene=EditorSceneManager.LoadSceneInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=Find(scene,mode=="Keepaway"?"KeepawayMatch":mode=="HotPotato"?"HotPotatoMatch":mode=="Balloon"?"BalloonMatch":"LimboMatch");
            var author=Type.GetType("LWS.TruckSports.Editor.CarnivalArenaHudAuthoring, Assembly-CSharp-Editor",true);var install=author.GetMethods().Single(method=>method.Name=="Install"&&method.GetParameters().Length==2);var hud=(Component)install.Invoke(null,new object[]{match,false});Assert.That(install.Invoke(null,new object[]{match,false}),Is.SameAs(hud));
            int count=mode=="Keepaway"?4:8;Set(Get(match,"setup"),"count",count);Call(match,"AllBots");Assert.That(Call(match,"StartMatch"),Is.True);
            float deadline=Time.realtimeSinceStartup+95;while(!Outcome(match,mode)&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Outcome(match,mode),Is.True,"Observe a real native possession/transfer/pop/bar contact or clear before HUD-only state fixtures.");
            Time.timeScale=0;((Behaviour)match).enabled=false;yield return new WaitForSecondsRealtime(.4f);Call(hud,"LateUpdate");
            int receipts=(int)Get(Get(match,"ScoreJournal"),"Count");string id=(string)Get(match,"MatchId");
            if(mode=="HotPotato"){ProtectedArena(match,hud);Capture(match,hud,sceneName+"-protected-arena",false);}
            if(mode=="Keepaway")
            {
                var icons=(SpriteRenderer[])Get(hud,"icons");var alive=(bool[])Get(match,"alive");for(int i=0;i<count;i++){Assert.That(icons[i].gameObject.activeSelf,Is.EqualTo(alive[i]));Assert.That(((Renderer[])Get(hud,"originalNames"))[i].enabled,Is.False);Assert.That(icons[i].sprite,Is.Not.Null);}
                if(!views)
                {
                    Assert.That(Get(hud,"Showing"),Is.False);var board=((GameObject)Get(Get(match,"menus"),"hud")).transform.Find("Broadcast participant cards");Assert.That(board,Is.Not.Null);Assert.That(board.gameObject.activeInHierarchy,Is.True);
                    var tmp=Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true);
                    for(int slot=0;slot<count;slot++){var card=board.Find("Participant card "+slot);Assert.That(card.gameObject.activeInHierarchy,Is.True);Assert.That(Get(card.Find("Name").GetComponent(tmp),"text"),Is.EqualTo(Get(At(Get(match,"setup"),"players",slot),"name")));Assert.That(Get(card.Find("Score badge").GetComponent(tmp),"text"),Is.EqualTo(((float[])Get(match,"possession"))[slot].ToString("F1")));}
                    Assert.That(((RectTransform)board).anchoredPosition.y,Is.EqualTo(18f));Capture(match,hud,sceneName+"-original-bottom-row",false);
                }
            }
            if(views)
            {
                for(int p=0;p<4;p++)pads[p]=InputSystem.AddDevice<Gamepad>();
                Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});
                for(int viewCount=1;viewCount<=4;viewCount++)
                {
                    for(int slot=0;slot<count;slot++){var player=At(Get(match,"setup"),"players",slot);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),slot<viewCount?"WholeController":"AI"));Set(player,"pad",slot<viewCount?pads[slot]:null);}
                    if(mode=="Keepaway")Call(match,"LayoutViews");else if(mode=="Limbo")Call(Get(match,"views"),"Layout",(object)null);else Call(Get(match,"drivingViews"),"Select",1);
                    for(int frame=0;frame<8;frame++){Call(match,"Present",.02f);yield return null;}Call(hud,"LateUpdate");yield return new WaitForSecondsRealtime(.35f);Data(match,hud,mode,count);Capture(match,hud,sceneName+"-owned-"+viewCount,mode=="Balloon");
                    if(mode=="Keepaway")
                    {
                        var guidance=match.GetComponent(Runtime("LWS.TruckKeepaway.KeepawayBallGuidance"));var poi=Get(guidance,"ballPOI");Call(guidance,"LateUpdate");Assert.That(Get(guidance,"ballPOI"),Is.SameAs(poi));for(int v=0;v<viewCount;v++)Assert.That(((Component)At(guidance,"views",v)).gameObject.activeSelf,Is.True,"Existing CNP offscreen ball indicator must remain live.");
                    }
                }
                TypedStateFixtures(match,hud,mode);Data(match,hud,mode,count);
            }
            if(mode=="HotPotato")ProtectedArena(match,hud);
            Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts),"The presentation and explicit state fixtures cannot award points.");
            Call(match,"Pause",0,false);Call(hud,"LateUpdate");Assert.That(Get(hud,"Showing"),Is.False);Call(match,"Resume");Time.timeScale=0;
            Call(match,"SetupScreen");Call(match,"AllBots");Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=0;Call(hud,"LateUpdate");Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(id));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);
            Call(match,"SetupScreen");Call(hud,"LateUpdate");Assert.That(Get(hud,"Showing"),Is.False);var hudType=hud.GetType();EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;match=null;Assert.That(UnityEngine.Object.FindObjectsByType(hudType,FindObjectsInactive.Include).Length,Is.Zero);
            TestContext.WriteLine("CARNIVAL_ARENA_HUD_NATIVE "+sceneName+"; real native outcome before frozen720/1080 and software-owned1–4-view checks; typed names/colors/portraits/points/state, no score mutation, pause/rematch/scene exit; Clear weather isolates presentation; short explicit typed-state fixtures do not claim physical outcomes or human/device/visual approval.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }
}
#endif
