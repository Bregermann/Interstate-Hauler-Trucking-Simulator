#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CarnivalLegacyHudCleanupTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object target,string name)=>target.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(target):target.GetType().GetProperty(name,Any).GetValue(target);
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,Any).SetValue(target,value);
    static object At(object target,string name,int index)=>((Array)Get(target,name)).GetValue(index);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length&&method.GetParameters().Select((parameter,index)=>args[index]==null||parameter.ParameterType.IsInstanceOfType(args[index])).All(match=>match)).Invoke(target,args);
    static Component Find(Scene scene,string name)=>scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Component>(true)).Single(component=>component!=null&&component.GetType().Name==name);
    static string Text(object value)=>(string)Get(value,"text");
    static Rect Pixels(RectTransform rect,Camera camera){var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(point=>RectTransformUtility.WorldToScreenPoint(camera,point)).ToArray();return Rect.MinMaxRect(points.Min(point=>point.x),points.Min(point=>point.y),points.Max(point=>point.x),points.Max(point=>point.y));}
    static string Fingerprint(object value)
    {
        string result=JsonUtility.ToJson(value);foreach(string name in new[]{"Phase","ShotId","Turn","Winner","Remaining","Goals","Timer","Points","Server","Eligible","Bounces","ResolvedTotal","FinalTotal","Frame","Ball","RemainingPins","Deliveries","Finished","Pocketed","Score"})
        {if(value.GetType().GetField(name,Any)==null&&value.GetType().GetProperty(name,Any)==null)continue;if(name=="FinalTotal"&&!(bool)Get(value,"Finished")){result+="|FinalTotal=PENDING";continue;}var data=Get(value,name);result+="|"+name+"="+(data is IEnumerable items&&!(data is string)?string.Join(",",items.Cast<object>()):data?.ToString());}return result;
    }
    static string Official(Component match,string mode)=>mode=="Bowling"?string.Join(";",((Array)Get(match,"lanes")).Cast<object>().Select(lane=>Fingerprint(Get(Get(lane,"Owner"),"score")))):Fingerprint(Get(match,"rules"));
    static void Hidden(object value,string label){var component=(Component)value;Assert.That(component.gameObject.activeSelf,Is.False,label);}
    static void Readable(object value,string label){var text=(Component)value;Assert.That(text.gameObject.activeInHierarchy,Is.True,label);Assert.That(Text(text),Is.Not.Empty,label);Call(text,"ForceMeshUpdate",false,false);Assert.That(Get(text,"isTextOverflowing"),Is.False,label+" "+Text(text));}
    static void Start(Component match,object owner,string mode,bool rematch=false)
    {
        if(mode=="Billiards"){if(rematch)Call(owner,"RestartSeries");else Assert.That(Call(owner,"StartMatch"),Is.True);}
        else if(mode=="Bowling")Assert.That(Call(match,"StartMatch"),Is.True);
        else Assert.That(Call(match,"StartMatch",rematch),Is.True);
    }
    static void Refresh(object ui,object owner,string mode)
    {
        if(mode=="Billiards")Call(Get(owner,"hud"),"Refresh");
        else if(mode=="Bowling")for(int lane=0;lane<4;lane++)Call(ui,"RefreshLane",lane);
        else if(mode=="Wallball"){var menus=Get(owner,"menus");Set(menus,"nextHud",0f);Call(menus,"Update");}
        else Call(ui,"Refresh");
    }
    static void AssertData(Component match,object owner,object menus,object ui,string mode)
    {
        var rules=mode=="Bowling"?null:Get(match,"rules");
        if(mode=="Billiards")
        {
            Hidden(Get(ui,"bindings"),"Retired bare bindings graphic");Assert.That(((Component)Get(ui,"bindings")).GetComponent<UnityEngine.UI.Image>(),Is.Null,"The bindings graphic has no dedicated empty backplate.");
            var wins=(int[])Get(Get(owner,"series"),"Wins");for(int side=0;side<2;side++){Assert.That(Text(At(ui,"names",side)),Is.EqualTo(Call(owner,"Name",side)));Assert.That(Text(At(ui,"scores",side)),Is.EqualTo(wins[side].ToString()));Readable(At(ui,"remaining",side),"Objective balls remain");}
            Readable(Get(ui,"turn"),"Actual turn");Readable(Get(ui,"powerText"),"Original Heat power");Readable(Get(ui,"speedText"),"Actual speed");int turn=(int)Get(rules,"Turn");var truck=Get(At(owner,"players",turn),"truck");Assert.That(Text(Get(ui,"speedText")),Is.EqualTo((Mathf.Abs((float)Get(truck,"Speed"))*2.236936f).ToString("0.0")+" MPH"));float strength=Mathf.Clamp01((float)Get(truck,"Speed")*(float)Get(truck,"powerScale")/(float)Get(truck,"maxShotSpeed"))*100;Assert.That(Get(Get(ui,"power"),"currentValue"),Is.EqualTo(strength));
        }
        else if(mode=="Hockey")
        {
            var obsolete=((RectTransform)Get(ui,"scoreBug")).Find("Hockey scoring rule");if(obsolete!=null)Assert.That(obsolete.gameObject.activeSelf,Is.False,"Obsolete rule subtitle must not remain beside live score data.");var controls=(Component)Get(menus,"controlsText");Hidden(controls,"Retired Hockey controls");Assert.That(controls.transform.parent.name,Is.EqualTo("Hockey compact footer"));Assert.That(controls.transform.parent.gameObject.activeSelf,Is.False,"No empty full-width footer.");var score=(int[])Get(rules,"Score");Assert.That(Text(Get(menus,"scoreText")),Is.EqualTo(score[0]+"   :   "+score[1]));int seconds=Mathf.CeilToInt((float)Get(rules,"Remaining"));Assert.That(Text(Get(menus,"clockText")),Is.EqualTo((seconds/60).ToString("00")+":"+(seconds%60).ToString("00")));Readable(Get(menus,"teamText"),"Blue defending direction");Readable(Get(menus,"orangeTeamText"),"Orange defending direction");
        }
        else if(mode=="AirHockey")
        {
            Hidden(Get(ui,"controls"),"Retired Air Hockey controls");foreach(GameObject dock in (Array)Get(ui,"legacyDocks"))if(dock!=null)Assert.That(dock.activeSelf,Is.False,"No retired dock backing.");var score=(int[])Get(rules,"Score");for(int side=0;side<2;side++){Assert.That(Text(At(ui,"names",side)),Is.EqualTo(Get(At(match,"players",side),"name")));Assert.That(Text(At(ui,"points",side)),Is.EqualTo(score[side].ToString()));}int seconds=Mathf.FloorToInt((float)Get(match,"MatchSeconds"));Assert.That(Text(Get(ui,"clock")),Is.EqualTo("PLAY "+(seconds/60).ToString("00")+":"+(seconds%60).ToString("00")));StringAssert.Contains((string)Get(At(match,"players",(int)Get(rules,"Server")),"name"),Text(Get(menus,"serveText")));
        }
        else if(mode=="Wallball")
        {
            var controls=(Component)Get(menus,"controlsText");Hidden(controls,"Retired Wallball bindings");Assert.That(controls.GetComponent<UnityEngine.UI.Image>(),Is.Null,"Bare bindings text has no backing to retain.");var score=(int[])Get(rules,"Score");StringAssert.Contains(Call(match,"TeamName",0)+"  "+score[0],Text(Get(menus,"scoreText")));StringAssert.Contains(score[1]+"  "+Call(match,"TeamName",1),Text(Get(menus,"scoreText")));StringAssert.Contains("SERVER: "+Call(match,"TeamName",Get(rules,"Server")),Text(Get(menus,"serveText")));Readable(Get(menus,"scoreText"),"Actual team score");
        }
        else
        {
            for(int lane=0;lane<4;lane++)
            {
                var source=At(match,"lanes",lane);var player=Get(source,"Owner");var score=Get(player,"score");Assert.That(Text(At(menus,"headers",lane)),Is.EqualTo(Get(player,"name")));Assert.That(Text(At(ui,"totals",lane)),Is.EqualTo(((int)Get(score,"ResolvedTotal")).ToString()));StringAssert.Contains("FRAME "+((int)Get(score,"Frame")+1),Text(At(ui,"frames",lane)));var label=(Component)At(menus,"guidance",lane);var footer=(RectTransform)label.transform.parent;Assert.That(footer.rect.width,Is.EqualTo(520).Within(.01));Assert.That(footer.rect.height,Is.EqualTo(44).Within(.01));Readable(label,"Compact real lane telemetry");if(Get(source,"Phase").ToString()=="Approach")Assert.That(Text(label),Is.EqualTo((Mathf.Abs((float)Get(Get(source,"truck"),"Speed"))*2.236936f).ToString("0")+" MPH · APPROACH"));StringAssert.DoesNotContain("STEER",Text(label));StringAssert.DoesNotContain("CONTROLS",Text(label));
            }
        }
    }
    static void Capture(Component match,object menus,object owner,object ui,string mode)
    {
        var canvas=((Component)menus).GetComponentInParent<Canvas>();if(canvas==null&&mode=="Billiards")canvas=((GameObject)Get(ui,"content")).GetComponentInParent<Canvas>();if(canvas==null&&ui is Component presentation)canvas=presentation.GetComponentInParent<Canvas>();Assert.That(canvas,Is.Not.Null,"Capture the actual owned HUD canvas; legacy menus may live in a separate scene branch.");var cameras=match.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Camera>(true)).Where(camera=>camera.enabled&&camera.gameObject.activeInHierarchy&&camera.targetTexture==null).ToArray();Assert.That(cameras,Is.Not.Empty);
        var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
        {
            Action<Camera> inspect=camera=>{Canvas.ForceUpdateCanvases();AssertData(match,owner,menus,ui,mode);if(mode=="Bowling")for(int lane=0;lane<4;lane++){var footer=(RectTransform)((Component)At(menus,"guidance",lane)).transform.parent;var bounds=Pixels(footer,camera);var view=((Camera)Get(Get(At(match,"lanes",lane),"cab"),"view")).rect;Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(view.x*size.x-2));Assert.That(bounds.xMax,Is.LessThanOrEqualTo(view.xMax*size.x+2));Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(view.y*size.y-2));Assert.That(bounds.yMax,Is.LessThanOrEqualTo(view.yMax*size.y+2));}};save.Invoke(null,new object[]{canvas,cameras,size.x,size.y,"Logs/TruckSports/PlaytestRepair/CarnivalLegacyHud/"+mode+"-controlled-"+size.y+".png",inspect});
        }
    }
    [UnityTest,Timeout(120000)]public IEnumerator BilliardsKeepsPowerSpeedTurnAndObjectiveBallsWithoutBindingsStrip()=>Inspect("TruckBilliards","Billiards");
    [UnityTest,Timeout(120000)]public IEnumerator HockeyKeepsTeamScoresClockAndContextWithoutEmptyFooter()=>Inspect("TruckHockey","Hockey");
    [UnityTest,Timeout(120000)]public IEnumerator AirHockeyKeepsActualNamesScoresServeAndPlayClockWithoutDocks()=>Inspect("TruckAirHockey","AirHockey");
    [UnityTest,Timeout(120000)]public IEnumerator WallballKeepsTeamScoresServingAndEligibilityWithoutBindings()=>Inspect("TruckWallball","Wallball");
    [UnityTest,Timeout(120000)]public IEnumerator BowlingKeepsFramesScoresPhysicalLaneStateAndOriginalPinsetter()=>Inspect("TruckBowling","Bowling");
    static IEnumerator Inspect(string sceneName,string mode)
    {
        var prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"LegacyHud"+Guid.NewGuid().ToString("N")});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);Runtime("LWS.TruckSports.SportsPrefs").GetMethod("SetString").Invoke(null,new object[]{"TruckSports.Weather.Preset.v1","9aad5ff5dfa09ce468205f676714f891"});var isolation=new GameObject("Neutral legacy HUD validation sources");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));Component match=null;object owner=null,menus=null,ui=null;
        try
        {
            var scene=EditorSceneManager.LoadSceneInPlayMode("Assets/"+sceneName+"/Scenes/"+sceneName+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=Find(scene,mode=="Billiards"?"BilliardsMatch":mode+"Match");owner=mode=="Billiards"?Get(match,"session"):(object)match;menus=Get(owner,"menus");
            if(mode=="Billiards")Call(owner,"SelectMode",Enum.Parse(Runtime("LWS.TruckBilliards.BilliardsMode"),"AIVsAI"));else if(mode=="Bowling"){Call(match,"PresetAI",4,Enum.Parse(Runtime("LWS.TruckBowling.BowlingMode"),"Simultaneous"));Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{true});}else if(mode=="Hockey"){Set(match,"mode",Enum.Parse(Get(match,"mode").GetType(),"AIVsAI"));Call(match,"Configure");}else Call(match,"Configure",Enum.Parse(Get(match,"mode").GetType(),"AIVsAI"));
            Start(match,owner,mode);Time.timeScale=0;((Behaviour)match).enabled=false;if(mode=="Billiards")foreach(Behaviour agent in (Array)Get(owner,"agents"))agent.enabled=false;
            if(mode=="Bowling")foreach(Behaviour lane in (Array)Get(match,"lanes"))lane.enabled=false;
            ui=mode=="Billiards"?Get(Get(owner,"hud"),"presentation"):mode=="Wallball"?null:Get(menus,"presentation");Assert.That(mode=="Wallball"||ui!=null,Is.True,"Existing authored presentation required.");
            // These are explicit frozen readout fixtures, not physical scoring evidence.
            if(mode=="Billiards"){var wins=(int[])Get(Get(owner,"series"),"Wins");wins[0]=1;wins[1]=2;int turn=(int)Get(Get(match,"rules"),"Turn");var truck=Get(At(owner,"players",turn),"truck");Set(truck,"<Speed>k__BackingField",2.4f);}
            else if(mode=="Bowling")for(int lane=0;lane<4;lane++){Call(Get(Get(At(match,"lanes",lane),"Owner"),"score"),"Record",7);Call(Get(Get(At(match,"lanes",lane),"Owner"),"score"),"Record",1);((Rigidbody)Get(Get(At(match,"lanes",lane),"truck"),"body")).linearVelocity=Vector3.forward*(4+lane);}
            else{var score=(int[])Get(Get(match,"rules"),"Score");score[0]=2;score[1]=5;}
            yield return new WaitForSecondsRealtime(.2f);Refresh(ui,owner,mode);yield return null;AssertData(match,owner,menus,ui,mode);
            var bodies=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Rigidbody>(true)).Select(body=>(body,body.position,body.rotation,body.linearVelocity,body.angularVelocity)).ToArray();string before=Official(match,mode);for(int repeat=0;repeat<30;repeat++)Refresh(ui,owner,mode);Assert.That(Official(match,mode),Is.EqualTo(before));foreach(var body in bodies){Assert.That(body.body.position,Is.EqualTo(body.position));Assert.That(body.body.rotation,Is.EqualTo(body.rotation));Assert.That(body.body.linearVelocity,Is.EqualTo(body.linearVelocity));Assert.That(body.body.angularVelocity,Is.EqualTo(body.angularVelocity));}Capture(match,menus,owner,ui,mode);
            if(mode=="Bowling")
            {
                var lane=At(match,"lanes",0);var phase=Get(lane,"Phase");foreach(string state in new[]{"Rolling","Replay","Sweeping","Finished"}){Set(lane,"<Phase>k__BackingField",Enum.Parse(phase.GetType(),state));Call(ui,"RefreshLane",0);string telemetry=Text(At(menus,"guidance",0));StringAssert.Contains(state=="Rolling"?"PIN ACTION":state=="Replay"?"REPLAY":state=="Finished"?"LANE COMPLETE":"PINSETTER",telemetry);}Set(lane,"<Phase>k__BackingField",phase);Assert.That(Official(match,mode),Is.EqualTo(before));
            }
            if(mode=="Billiards")Call(owner,"Pause");else if(mode=="Bowling")Call(match,"Pause",0,false);else Call(match,"Pause",false);Refresh(ui,owner,mode);if(mode=="Bowling")Assert.That(((Array)Get(menus,"viewRoots")).Cast<RectTransform>().All(root=>!root.gameObject.activeSelf),Is.True);else Assert.That((mode=="Billiards"?(GameObject)Get(ui,"content"):(GameObject)Get(menus,"hud")).activeSelf,Is.False);
            var screen=Get(mode=="Billiards"?owner:menus,"Screen");var help=Enum.Parse(screen.GetType(),"Help");Call(mode=="Billiards"?owner:menus,"Show",help);yield return null;var page=((GameObject[])Get(menus,"pages"))[Convert.ToInt32(help)];string helpText=string.Join("\n",page.GetComponentsInChildren<Component>(true).Where(component=>component!=null&&component.GetType().GetProperty("text")?.PropertyType==typeof(string)).Select(component=>Text(component)));Assert.That(helpText.Length,Is.GreaterThan(100),"Original Help remains populated.");if(mode=="Billiards")StringAssert.Contains((string)Call(owner,"Bindings",0,false),helpText);else StringAssert.Contains(mode=="Bowling"?"CONTROLS":"Keyboard",helpText);
            Call(owner,"Resume");Time.timeScale=0;yield return new WaitForSecondsRealtime(.2f);Refresh(ui,owner,mode);AssertData(match,owner,menus,ui,mode);Start(match,owner,mode,true);Time.timeScale=0;yield return new WaitForSecondsRealtime(.2f);Refresh(ui,owner,mode);
            if(mode=="Billiards")Assert.That(((int[])Get(Get(owner,"series"),"Wins")).Sum(),Is.Zero);else if(mode=="Bowling")foreach(object player in (Array)Get(Get(match,"setup"),"players"))Assert.That(Get(Get(player,"score"),"Deliveries"),Is.Zero);else Assert.That(((int[])Get(Get(match,"rules"),"Score")).Sum(),Is.Zero);
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;match=null;TestContext.WriteLine("CARNIVAL_LEGACY_HUD "+mode+"; saved native start/help/pause/resume/rematch; controlled frozen score/speed/phase readout fixtures and720/1080 captures;30read-only refreshes preserve original rules and bodies. Existing physical gameplay tests retained. No physical scoring, full-match, human/device or visual approval claim.");
        }
        finally{Time.timeScale=1;if(match!=null)((Behaviour)match).enabled=true;UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }
}
#endif
