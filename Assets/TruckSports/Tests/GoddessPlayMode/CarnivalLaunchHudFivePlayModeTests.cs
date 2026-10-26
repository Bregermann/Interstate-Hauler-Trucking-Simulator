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
public sealed class CarnivalLaunchHudFivePlayModeTests
{
 const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static readonly string[] Folders={"TruckDockDiving","TruckGiantSwing","TruckTargetLaunch","TruckBeerPong","TruckSkyTow"};
 static readonly string[] Modes={"Dock","Swing","Target","Pong","SkyTow"};
 static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value){if(owner.GetType().GetField(name,Any) is FieldInfo field)field.SetValue(owner,value);else owner.GetType().GetProperty(name,Any).GetSetMethod(true).Invoke(owner,new[]{value});}
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length&&method.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(owner,args);
 static object At(object owner,string name,int index)=>((Array)Get(owner,name)).GetValue(index);
 static string Label(Component text)=>(string)Get(text,"text");
 static Rect Bounds(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);return Rect.MinMaxRect(corners.Min(p=>p.x),corners.Min(p=>p.y),corners.Max(p=>p.x),corners.Max(p=>p.y));}
 static void Within(RectTransform inner,RectTransform outer){var a=Bounds(inner);var b=Bounds(outer);Assert.That(a.xMin,Is.GreaterThanOrEqualTo(b.xMin-.2f));Assert.That(a.xMax,Is.LessThanOrEqualTo(b.xMax+.2f));Assert.That(a.yMin,Is.GreaterThanOrEqualTo(b.yMin-.2f));Assert.That(a.yMax,Is.LessThanOrEqualTo(b.yMax+.2f));}
 static object[] ReadCard(object match,int slot){var args=new object[]{slot,null,null};Assert.That(match.GetType().GetMethod("TryReadScoreCard").Invoke(match,args),Is.True);return args;}
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly GameObject isolation;public Behaviour match;public Component menus;public int mode;
  public Fixture(string suffix){prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"LaunchHudFive"+suffix});Sports("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);isolation=new GameObject("Explicit five mode-owned launch HUD validation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));}
  public IEnumerator Load(int index){Time.timeScale=1;mode=index;EditorSceneManager.LoadSceneInPlayMode("Assets/"+Folders[index]+"/Scenes/"+Folders[index]+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Behaviour)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS."+Folders[index]+"."+Modes[index]+"Match, Assembly-CSharp",true));menus=(Component)Get(match,"menus");var author=Type.GetType("LWS."+Folders[index]+".Editor."+Modes[index]+"CompactHudAuthoring, Assembly-CSharp-Editor",true);author.GetMethod("Install").Invoke(null,new object[]{match});}
  public void Start(int count){Time.timeScale=1;Call(match,"SetupScreen");if(mode==3){Set(match,"option",count==1?1:0);Set(match,"teamA",count==4?2:1);Set(match,"teamB",Mathf.Max(1,count-(count==4?2:1)));}Set(Get(match,"setup"),"count",count);Call(match,"AllBots");Sports("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{true});Assert.That(Call(match,"StartMatch"),Is.True);}
  public void Dispose(){Time.timeScale=1;if(match!=null){match.enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
 }
 static void Capture(Fixture f,string label){var save=Sports("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);var cameras=(Camera[])Get(f.match,"cameras");foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{f.menus.GetComponent<Canvas>(),cameras,size.x,size.y,"Logs/TruckSports/PlaytestRepair/CarnivalLaunchHudFive/"+Modes[f.mode]+"-"+label+"-"+size.y+".png",null});}
 static void CompactState(Fixture f)
 {
  Assert.That(Get(f.match,"CompactBroadcastStatus"),Is.True);var status=(Component)Get(f.menus,"status");var rect=(RectTransform)status.transform;Assert.That(rect.sizeDelta,Is.EqualTo(new Vector2(600,48)));bool multi=(bool)Get(f.match,"MultiViewHUD");Assert.That(status.gameObject.activeInHierarchy,Is.EqualTo(!multi&&(bool)Get(f.match,"BroadcastStatusVisible")));var backing=status.transform.parent.Find("Broadcast status backplate");if(backing!=null)Assert.That(backing.gameObject.activeSelf,Is.EqualTo(status.gameObject.activeInHierarchy),"No permanent empty status rectangle.");
  string value=(string)Get(f.match,"Status");foreach(var instruction in new[]{"HOLD ACTION","DRIVE TO","LEFT/RIGHT BANK","TIME FORE / AFT","ACTION near","10 / 25 / 50 / 100"})StringAssert.DoesNotContain(instruction,value);Assert.That(((string)Get(f.match,"Help")).Length,Is.GreaterThan(250));
  int count=(int)Get(f.match,"ScoreCardCount");for(int i=0;i<count;i++){var card=ReadCard(f.match,i);Assert.That((string)card[1],Is.Not.Null.And.Not.Empty);Assert.That((string)card[2],Is.Not.Null);int owner=(int)Call(f.match,"ScoreCardSource",i);Assert.That(owner,Is.InRange(0,(int)Get(Get(f.match,"setup"),"count")-1));}
  if(f.mode==0){int active=(int)Get(f.match,"Active");var best=(int[])Get(f.match,"best");for(int slot=0;slot<count;slot++)if(best[slot]==0)Assert.That(ReadCard(f.match,slot)[2],Is.EqualTo(slot==active&&!(bool)Get(f.match,"Finished")?"UP NOW":"WAITING"),"Unscored/current status fits the compact Dock card.");}
  if(!multi)return;Assert.That(Get(f.match,"CompactViewportHud"),Is.True);var cameras=(Camera[])Get(f.match,"cameras");var panels=(RectTransform[])Get(f.menus,"viewPanels");for(int view=0;view<cameras.Length&&view<panels.Length;view++)
  {
   if(!cameras[view].enabled)continue;var args=new object[]{view,-1,null,null};Assert.That(f.match.GetType().GetMethod("TryReadViewportCard").Invoke(f.match,args),Is.True);var root=panels[view].Find("Compact owned viewport score") as RectTransform;Assert.That(root,Is.Not.Null);Assert.That(root.gameObject.activeSelf,Is.True);Within(root,panels[view]);Assert.That(root.rect.width,Is.LessThanOrEqualTo(340));Assert.That(root.rect.height,Is.LessThanOrEqualTo(116));Assert.That(Label(root.Find("Owned score").GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true))),Is.EqualTo(args[2]));Assert.That(Label(root.Find("Race status").GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true))),Is.EqualTo(args[3]));Assert.That(Label(root.Find("Driving telemetry").GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true))),Is.EqualTo(Call(f.match,"ViewportTelemetry",view)));Assert.That(((Component)At(f.menus,"viewTexts",view)).gameObject.activeSelf,Is.False);var old=panels[view].Find("View data backing");if(old!=null)Assert.That(old.gameObject.activeSelf,Is.False);
  }
 }
 [UnityTest,Timeout(240000)]
 public IEnumerator OriginalFiveModesKeepOneToFourRostersActualOwnersCompactViewsAndReplayActionsAt720And1080()
 {
  using(var f=new Fixture("Layouts"))for(int mode=0;mode<5;mode++)
  {
   yield return f.Load(mode);var replayActions=((GameObject[])Get(f.menus,"replayButtons")).Select(button=>button.GetComponent(Sports("SportsMenuAction")).GetInstanceID()).ToArray();
   for(int count=1;count<=4;count++){f.Start(count);var configuredCameras=((Camera[])Get(f.match,"cameras")).Select(camera=>camera.GetInstanceID()).ToArray();Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);Canvas.ForceUpdateCanvases();CompactState(f);Assert.That(Get(Get(f.match,"setup"),"count"),Is.EqualTo(count));Assert.That(Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);Capture(f,"roster-"+count);Assert.That(((GameObject[])Get(f.menus,"replayButtons")).Select(button=>button.GetComponent(Sports("SportsMenuAction")).GetInstanceID()).ToArray(),Is.EqualTo(replayActions));Assert.That(((Camera[])Get(f.match,"cameras")).Select(camera=>camera.GetInstanceID()).ToArray(),Is.EqualTo(configuredCameras));}
   f.Start(4);Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);CompactState(f);Assert.That(Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);
  }
 }
 static bool Air(Fixture f)
 {
  string phase=Get(f.match,f.mode==2?"Stage":"Phase").ToString();return f.mode==4?phase=="Run"&&(float)Get(f.match,"Age")>6:f.mode==2?((Array)Get(f.match,"racers")).Cast<object>().Any(racer=>Get(racer,"Phase").ToString()=="Airborne"):phase=="Flight";
 }
 [UnityTest,Timeout(300000)]
 public IEnumerator NaturalOriginalAiFlightValuesRemainReadOnlyAcrossTheFiveModes()
 {
  using(var f=new Fixture("Natural"))for(int mode=0;mode<5;mode++)
  {
   yield return f.Load(mode);f.Start(mode==3?2:4);float deadline=Time.realtimeSinceStartup+(mode==1?75:55);while(!Air(f)&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(Air(f),Is.True,"Original "+Modes[mode]+" AI must physically reach flight without supplied body poses, velocities, launch flags or results.");Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);CompactState(f);var bodies=UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);var positions=bodies.Select(body=>body.position).ToArray();var velocities=bodies.Select(body=>body.linearVelocity).ToArray();int receipts=(int)Get(Get(f.match,"ScoreJournal"),"Count");int cards=(int)Get(f.match,"ScoreCardCount");var values=Enumerable.Range(0,cards).Select(i=>(string)ReadCard(f.match,i)[1]).ToArray();for(int repeat=0;repeat<5;repeat++){for(int i=0;i<cards;i++)Assert.That(ReadCard(f.match,i)[1],Is.EqualTo(values[i]));string status=(string)Get(f.match,"Status");Assert.That(status,Is.Not.Null.And.Not.Empty);}for(int i=0;i<bodies.Length;i++){Assert.That(bodies[i].position,Is.EqualTo(positions[i]));Assert.That(bodies[i].linearVelocity,Is.EqualTo(velocities[i]));}Assert.That(Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));Capture(f,"natural-original-flight");TestContext.WriteLine("NATURAL_LAUNCH_HUD mode="+Modes[mode]+" status="+Get(f.match,"Status")+" scores="+string.Join(",",values)+" originalAiFlight=true unchangedReceipts="+receipts);Time.timeScale=1;
  }
 }
 [UnityTest,Timeout(180000)]
 public IEnumerator SuppliedNonzeroRenderValuesBindTypedUnitsStateAndWindWithoutPublishingScores()
 {
  using(var f=new Fixture("SuppliedRender"))for(int mode=0;mode<5;mode++)
  {
   yield return f.Load(mode);f.Start(4);Time.timeScale=0;f.match.enabled=false;int receipts=(int)Get(Get(f.match,"ScoreJournal"),"Count");
   if(mode==0){((int[])Get(f.match,"best"))[0]=1234;Set(f.match,"Phase",Enum.Parse(Get(f.match,"Phase").GetType(),"Flight"));Set(f.match,"Timing","PERFECT");Set(f.match,"flight",4.2f);Set(f.match,"used",.4f);}
   else if(mode==1){((int[])Get(f.match,"points"))[0]=347;Set(f.match,"Phase",Enum.Parse(Get(f.match,"Phase").GetType(),"Pumping"));Set(f.match,"Energy",60f);Set(f.match,"Age",5f);}
   else if(mode==2){var rules=Get(f.match,"rules");((int[])Get(rules,"totals"))[0]=347;Set(f.match,"CurrentWind",new Vector3(3,0,4));var racer=At(f.match,"racers",0);Set(racer,"Phase",Enum.Parse(Get(racer,"Phase").GetType(),"Passive"));Set(racer,"StableTime",.9f);}
   else if(mode==3){var rules=Get(f.match,"rules");((int[])Get(rules,"remaining"))[0]=7;((int[])Get(rules,"hits"))[0]=3;Set(f.match,"Phase",Enum.Parse(Get(f.match,"Phase").GetType(),"Charging"));Set(f.match,"Charge",.68f);Set(f.match,"AimYaw",-12f);}
   else{var rules=Get(f.match,"rules");((int[])Get(rules,"score"))[0]=347;((int[])Get(rules,"hits"))[0]=7;((int[])Get(rules,"streak"))[0]=4;Set(f.match,"Phase",Enum.Parse(Get(f.match,"Phase").GetType(),"Run"));Set(f.match,"Age",37f);}
   yield return new WaitForSecondsRealtime(.15f);CompactState(f);if(mode==0){Assert.That(ReadCard(f.match,0)[1],Is.EqualTo((1234/12).ToString()));StringAssert.Contains("PERFECT",(string)Get(f.match,"Status"));}else if(mode==3){Assert.That(ReadCard(f.match,0)[1],Is.EqualTo("7"));StringAssert.Contains("CUPS LEFT",(string)ReadCard(f.match,0)[2]);StringAssert.Contains("68%",(string)Get(f.match,"Status"));StringAssert.Contains("-12°",(string)Get(f.match,"Status"));}else Assert.That(ReadCard(f.match,0)[1],Is.EqualTo("347"));if(mode==2)StringAssert.Contains("11.2 MPH",(string)Call(f.match,"ViewportTelemetry",0));if(mode==4)StringAssert.Contains("83s LEFT",(string)Call(f.match,"ViewportTelemetry",0));Assert.That(Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));Capture(f,"supplied-render-values");TestContext.WriteLine("SUPPLIED_RENDER_ONLY mode="+Modes[mode]+" explicit nonzero UI values; no physical score/flight claim, journal unchanged="+receipts);f.match.enabled=true;Time.timeScale=1;
  }
 }
}
#endif
