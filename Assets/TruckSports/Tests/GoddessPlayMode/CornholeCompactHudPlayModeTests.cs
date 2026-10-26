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
public sealed class CornholeCompactHudPlayModeTests
{
 const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static Type Mode(string name)=>Type.GetType("LWS.TruckCornhole."+name+", Assembly-CSharp",true);
 static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value){if(owner.GetType().GetField(name,Any) is FieldInfo field)field.SetValue(owner,value);else owner.GetType().GetProperty(name,Any).GetSetMethod(true).Invoke(owner,new[]{value});}
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length&&method.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(owner,args);
 static object At(object owner,string name,int index)=>((Array)Get(owner,name)).GetValue(index);
 static object[] Courses(object match)=>((Array)Get(match,"courses")).Cast<object>().ToArray();
 static string Text(object label)=>(string)Get(label,"text");
 static Rect Bounds(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);return Rect.MinMaxRect(corners.Min(p=>p.x),corners.Min(p=>p.y),corners.Max(p=>p.x),corners.Max(p=>p.y));}
 static void Within(RectTransform inner,RectTransform outer){var a=Bounds(inner);var b=Bounds(outer);Assert.That(a.xMin,Is.GreaterThanOrEqualTo(b.xMin-.1f));Assert.That(a.xMax,Is.LessThanOrEqualTo(b.xMax+.1f));Assert.That(a.yMin,Is.GreaterThanOrEqualTo(b.yMin-.1f));Assert.That(a.yMax,Is.LessThanOrEqualTo(b.yMax+.1f));}
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly GameObject isolation;readonly UnityEngine.Random.State random=UnityEngine.Random.state;public Behaviour match,hud;public Component menus;
  public Fixture(string suffix){prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"CornholeCompact"+suffix});Sports("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);isolation=new GameObject("Explicit Cornhole compact HUD native validation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));}
  public IEnumerator Load(){EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCornhole/Scenes/TruckCornhole.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Behaviour)UnityEngine.Object.FindAnyObjectByType(Mode("CornholeMatch"));menus=(Component)Get(match,"menus");hud=(Behaviour)menus.GetComponent(Mode("CornholeCompactHud"));Assert.That(hud,Is.Not.Null,"Explicit CornholeHudAuthoring.InstallOpenScene must run before native validation.");}
  public void Start(int count,bool allViews=true,bool hard=false){Call(match,"ChangeSetup");Set(Get(match,"setup"),"count",count);Call(match,"AllAI");if(hard)foreach(var player in ((Array)Get(Get(match,"setup"),"players")).Cast<object>())Set(player,"difficulty",Enum.Parse(Get(player,"difficulty").GetType(),"Hard"));Sports("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{allViews});var presets=Get(match,"weatherPresets");var clear=((Array)Get(presets,"entries")).Cast<object>().FirstOrDefault(entry=>(string)Get(entry,"displayName")=="Clear");if(clear!=null)Call(presets,"Select",Get(clear,"id"));Assert.That(Call(match,"StartMatch"),Is.True);}
  public void Dispose(){Time.timeScale=1;if(match!=null){match.enabled=true;Call(match,"ChangeSetup");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();UnityEngine.Random.state=random;}
 }
 static void Capture(Fixture f,string label){var save=Sports("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);var cameras=Courses(f.match).Select(course=>(Camera)Get(Get(course,"chase"),"view")).Where(camera=>camera.enabled).ToArray();foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{f.menus.GetComponent<Canvas>(),cameras,size.x,size.y,"Logs/TruckSports/PlaytestRepair/CornholeCompactHud/"+label+"-"+size.y+".png",null});}
 static void ActualCard(Fixture f,int slot)
 {
  var card=At(f.hud,"cards",slot);var player=At(Get(f.match,"setup"),"players",slot);Assert.That(Text(Get(card,"score")),Is.EqualTo(Call(f.hud,"Score",slot)));Assert.That(Text(Get(card,"delta")),Is.EqualTo(Call(f.hud,"Delta",slot)));Assert.That(Get(card,"ownerId"),Is.EqualTo(Get(player,"participantId")));StringAssert.Contains((string)Get(player,"name"),Text(Get(card,"name")));Assert.That(Get(Get(card,"name"),"richText"),Is.False);Assert.That(Get(Get(card,"portrait"),"sprite"),Is.Not.Null);Assert.That(Get(Get(card,"stripe"),"color"),Is.EqualTo(Get(player,"color")));
 }
 [UnityTest,Timeout(240000)]
 public IEnumerator OriginalNaturalApproachReplayAndWholeRoundBankDriveCompactScoresWithoutNewReceipts()
 {
  using(var f=new Fixture("NaturalValues"))
  {
   yield return f.Load();UnityEngine.Random.InitState(174207);f.Start(4,true,true);TestContext.WriteLine("CORNHOLE_HUD_NATURAL_FIXTURE originalAi=Hard seed=174207 weather=Clear; same actual no-supplied-score setup as shared-board natural acceptance.");var course=Courses(f.match)[0];float deadline=Time.realtimeSinceStartup+30;while(Get(course,"Phase").ToString()!="Flight"&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(Get(course,"Phase").ToString(),Is.EqualTo("Flight"));Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);ActualCard(f,0);var context=Text(Get(At(f.hud,"cards",0),"context"));StringAssert.Contains("AIR",context);StringAssert.Contains(((float)Get(course,"PhaseElapsed")).ToString("F1")+"s",context);Assert.That(Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);Capture(f,"actual-flight-four");Time.timeScale=1;
   var replay=Get(course,"replay");deadline=Time.realtimeSinceStartup+45;while(!(bool)Get(replay,"Available")&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(Get(replay,"Available"),Is.True);Call(f.menus,"Action","replay0");Assert.That(Get(replay,"Playing"),Is.True);Time.timeScale=0;var body=(Rigidbody)Get(Get(course,"Current"),"body");var position=body.position;int attempts=(int)Get(course,"Attempts"),provisional=(int)Get(course,"Provisional");
   for(int angle=0;angle<4;angle++){yield return new WaitForSecondsRealtime(.14f);StringAssert.Contains("REPLAY",Text(Get(At(f.hud,"cards",0),"context")));StringAssert.Contains((string)Get(replay,"AngleName"),Text(Get(At(f.hud,"cards",0),"context")));ActualCard(f,0);Capture(f,"actual-replay-angle-"+Get(replay,"Angle"));Call(f.menus,"Action","angle0");}Assert.That(body.position,Is.EqualTo(position));Assert.That(Get(course,"Attempts"),Is.EqualTo(attempts));Assert.That(Get(course,"Provisional"),Is.EqualTo(provisional));Call(f.menus,"Action","live0");Assert.That(Get(replay,"Playing"),Is.False);Time.timeScale=1;
   deadline=Time.realtimeSinceStartup+155;while((int)Get(f.match,"RoundFinalizations")==0&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(Get(f.match,"RoundFinalizations"),Is.EqualTo(1));Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);var totals=(int[])Get(f.match,"totals");Assert.That(totals.Sum(),Is.GreaterThan(0),"Use genuine original AI/PhysX banked scores rather than supplied scores.");var snapshot=(int[])totals.Clone();int receipts=(int)Get(Get(f.match,"ScoreJournal"),"Count");for(int i=0;i<4;i++){ActualCard(f,i);StringAssert.Contains("FINAL",Text(Get(At(f.hud,"cards",i),"delta")));}Capture(f,"actual-first-round-bank");yield return new WaitForSecondsRealtime(.2f);Assert.That(totals,Is.EqualTo(snapshot));Assert.That(Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));int id=f.hud.GetInstanceID();f.Start(4);yield return new WaitForSecondsRealtime(.15f);Assert.That(f.hud.GetInstanceID(),Is.EqualTo(id));for(int i=0;i<4;i++){ActualCard(f,i);Assert.That(Text(Get(At(f.hud,"cards",i),"score")),Is.EqualTo("0 PTS"));}Assert.That(Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);
   TestContext.WriteLine("Original natural AI launch/replay/all-four angle actions and actual whole-round bank populated compact owner cards. No supplied pose/score/finish/receipt; UI reads did not alter totals/journal. Captures720/1080 and retained original replay input authority.");
  }
 }
 [UnityTest,Timeout(120000)]
 public IEnumerator OneToFourAndSingleCameraRosterKeepOwnedCardsCompactWindBottomRightActionsAndFallback()
 {
  using(var f=new Fixture("LayoutWind"))
  {
   yield return f.Load();var wind=f.menus.GetComponent(Mode("CornholeWindDisplay"));Assert.That(wind,Is.Not.Null);var originalActions=f.menus.GetComponentsInChildren(Sports("SportsMenuAction"),true).Length;
   for(int count=1;count<=4;count++)
   {
    f.Start(count);Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);Canvas.ForceUpdateCanvases();for(int i=0;i<count;i++){ActualCard(f,i);var card=(RectTransform)Get(At(f.hud,"cards",i),"root");var owner=(RectTransform)At(f.menus,"views",i);Within(card,owner);Assert.That(card.rect.width,Is.LessThanOrEqualTo(318));Assert.That(card.rect.height,Is.LessThanOrEqualTo(94));var panel=((Component)At(wind,"speeds",i)).transform.parent.GetComponent<RectTransform>();Within(panel,owner);Assert.That(panel.anchorMin,Is.EqualTo(new Vector2(1,0)));Assert.That(Bounds(card).Overlaps(Bounds(panel)),Is.False);var name=(Component)Get(At(f.hud,"cards",i),"name");Within((RectTransform)name.transform,card);}
    Assert.That(((Array)Get(f.hud,"legacyGraphics")).Cast<UnityEngine.UI.Graphic>().All(graphic=>!graphic.enabled),Is.True);Assert.That(((RectTransform)Get(f.hud,"roster")).gameObject.activeSelf,Is.False);Capture(f,"roster-"+count);
   }
   f.Start(4,false);Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);Assert.That(Courses(f.match).Count(course=>((Camera)Get(Get(course,"chase"),"view")).enabled),Is.EqualTo(1));var roster=(RectTransform)Get(f.hud,"roster");Assert.That(roster.gameObject.activeSelf,Is.True);for(int i=0;i<4;i++){var row=At(f.hud,"rosterRows",i);Assert.That(((RectTransform)Get(row,"root")).gameObject.activeSelf,Is.True);Assert.That(Get(row,"ownerId"),Is.EqualTo(Get(At(Get(f.match,"setup"),"players",i),"participantId")));Assert.That(Text(Get(row,"score")),Is.EqualTo(Call(f.hud,"Score",i)));Within((RectTransform)Get(row,"root"),roster);}Capture(f,"single-camera-all-four-scores");
   // Controlled render-only wind sample: freeze simulation and original match
   // tick, retain the actual wind display and capped EffectiveWind calculation.
   f.match.enabled=false;var atmosphere=Get(f.match,"atmosphere");var flow=(Vector3)Get(atmosphere,"Flow");var course0=Courses(f.match)[0];var current=Get(course0,"Current");var heading=(Vector3)Call(f.match,"WindHeading",current);Set(atmosphere,"Flow",-heading*30);float elapsed=(float)Get(course0,"PhaseElapsed");var totals=(int[])((int[])Get(f.match,"totals")).Clone();yield return new WaitForSecondsRealtime(.2f);StringAssert.Contains("10.0 MPH",Text(At(wind,"speeds",0)));Assert.That((Vector3)Get(atmosphere,"Flow"),Is.EqualTo(-heading*30));Assert.That(Get(course0,"PhaseElapsed"),Is.EqualTo(elapsed));Assert.That((int[])Get(f.match,"totals"),Is.EqualTo(totals));Set(atmosphere,"Flow",flow);f.match.enabled=true;
   Assert.That(f.menus.GetComponentsInChildren(Sports("SportsMenuAction"),true).Length,Is.EqualTo(originalActions));for(int i=0;i<20;i++){var button=(GameObject)At(f.menus,"replayButtons",i);Assert.That(button.GetComponent(Sports("SportsMenuAction")),Is.Not.Null);Assert.That(button.GetComponent<RectTransform>().sizeDelta,Is.EqualTo(new Vector2(128,38)));}
   f.hud.enabled=false;var graphics=((Array)Get(f.hud,"legacyGraphics")).Cast<UnityEngine.UI.Graphic>().ToArray();var baseline=(bool[])Get(f.hud,"originalGraphicEnabled");for(int i=0;i<graphics.Length;i++)Assert.That(graphics[i].enabled,Is.EqualTo(baseline[i]));Assert.That(roster.gameObject.activeSelf,Is.False);f.hud.enabled=true;yield return null;Call(f.match,"Pause",0,false);yield return null;Assert.That(((Array)Get(f.hud,"cards")).Cast<object>().All(card=>!((RectTransform)Get(card,"root")).gameObject.activeInHierarchy),Is.True);Call(f.match,"Resume");yield return new WaitForSecondsRealtime(.15f);Assert.That(((RectTransform)Get(At(f.hud,"cards",0),"root")).gameObject.activeInHierarchy,Is.True);
   TestContext.WriteLine("Saved original1–4views and single-camera four-owner roster, portraits/identity, compact bounds, existing bottom-right authoritative10MPH headwind render, original20replay actions, no score/clock/wind mutation, pause/rematch and exact original Graphic.enabled fallback checked at720/1080. Controlled wind input is not a weather/physics acceptance claim.");
  }
 }
}
#endif
