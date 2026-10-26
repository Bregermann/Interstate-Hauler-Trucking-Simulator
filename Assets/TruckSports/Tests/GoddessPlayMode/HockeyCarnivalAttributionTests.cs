#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class HockeyCarnivalAttributionTests
{
 const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly GameObject isolation;Delegate handler;
  public Behaviour match;public readonly List<object> receipts=new List<object>();
  public Fixture(string suffix){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"HockeyCarnival"+suffix});isolation=new GameObject("Hockey attribution device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
  public object rules=>Get(match,"rules");public object contact=>Get(match,"puckContact");public Rigidbody puck=>(Rigidbody)Get(match,"puck");
  public object Player(int slot)=>((Array)Get(match,"players")).GetValue(slot);
  public Component Truck(int slot)=>(Component)((Array)Get(match,"trucks")).GetValue(slot);
  public IEnumerator Load()
  {
   EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHockey/Scenes/TruckHockey.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
   match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckHockey.HockeyMatch")).Cast<Behaviour>().Single();Set(match,"mode",Enum.Parse(match.GetType().GetField("mode").FieldType,"AIVsAI"));Call(match,"Configure");
   for(int i=0;i<6;i++)Set(Player(i),"name","SAME DISPLAY NAME");
   var scoreEvent=match.GetType().GetEvent("ScoreConfirmed");Action<object> receive=receipts.Add;handler=Delegate.CreateDelegate(scoreEvent.EventHandlerType,receive.Target,receive.Method);scoreEvent.AddEventHandler(match,handler);
   Assert.That((bool)Call(match,"StartMatch",false),Is.True);foreach(Behaviour agent in (Array)Get(match,"agents"))agent.enabled=false;
   yield return Live();Quiet();Assert.That(Enumerable.Range(0,6).Select(i=>(string)Get(Player(i),"participantId")).Distinct().Count(),Is.EqualTo(6));
  }
  public IEnumerator Live(){float deadline=Time.realtimeSinceStartup+9;while(!(bool)Get(rules,"Live")&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((bool)Get(rules,"Live"),Is.True);}
  public void Quiet()
  {
   for(int i=0;i<6;i++){var truck=Truck(i);Call(truck,"ClearInput");((Behaviour)truck).enabled=false;var body=(Rigidbody)Get(truck,"body");body.isKinematic=true;body.position=new Vector3(-9+i*3,.36f,-6);body.rotation=Quaternion.identity;var stick=(Behaviour)Get(truck,"stick");if(stick!=null){stick.enabled=false;((Collider)Get(stick,"blade")).enabled=false;}}
   Physics.SyncTransforms();
  }
  public void Dispose(){Time.timeScale=1;if(match!=null){if(handler!=null)match.GetType().GetEvent("ScoreConfirmed").RemoveEventHandler(match,handler);Call(match,"ChangeSetup");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
 }
 static IEnumerator Touch(Fixture f,int slot,bool blade)
 {
  f.Quiet();var truck=f.Truck(slot);
  if(blade){var stick=Get(truck,"stick");Assert.That(stick,Is.Not.Null);var body=(Rigidbody)Get(stick,"bladeBody");body.position=new Vector3(0,.22f,0);body.rotation=Quaternion.identity;((Collider)Get(stick,"blade")).enabled=true;}
  else{var body=(Rigidbody)Get(truck,"body");body.position=new Vector3(0,.36f,0);body.rotation=Quaternion.identity;}
  f.puck.position=new Vector3(-2,.22f,0);f.puck.linearVelocity=Vector3.right*4;Physics.SyncTransforms();int contacts=(int)Get(f.contact,"TruckContacts");float deadline=Time.realtimeSinceStartup+2;
  while((int)Get(f.contact,"TruckContacts")==contacts&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
  Assert.That((int)Get(f.contact,"TruckContacts"),Is.GreaterThan(contacts),"Actual PhysX "+(blade?"articulated blade":"truck body")+" contact required.");Assert.That((int)Get(f.contact,"LastTouch"),Is.EqualTo(slot));Assert.That((string)Get(f.contact,"LastTouchParticipantId"),Is.EqualTo((string)Get(f.Player(slot),"participantId")));
  f.puck.linearVelocity=Vector3.zero;f.Quiet();yield return new WaitForFixedUpdate();
 }
 static IEnumerator Cross(Fixture f,int netTeam,int expectedGoals)
 {
  f.Quiet();int sign=(int)Call(f.rules,"DefendingSign",netTeam);float line=(float)Get(f.match,"goalLine");f.puck.position=new Vector3(sign*(line-1.5f),.22f,0);f.puck.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
  f.puck.linearVelocity=Vector3.right*(sign*8);float deadline=Time.realtimeSinceStartup+3;while((int)Get(f.rules,"Goals")<expectedGoals&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That((int)Get(f.rules,"Goals"),Is.EqualTo(expectedGoals),"Original completed-physics-segment goal detection must award exactly once.");
 }
 static void Receipt(object receipt,string id,bool own,bool teamOnly,int total)
 {
  Assert.That((bool)Get(receipt,"IsTeam"),Is.EqualTo(teamOnly));Assert.That((string)Get(receipt,"ParticipantId"),Is.EqualTo(id));Assert.That((string)Get(receipt,"TeamId"),Is.EqualTo("blue"));Assert.That((double)Get(receipt,"Delta"),Is.EqualTo(1));Assert.That((double)Get(receipt,"Total"),Is.EqualTo(total));Assert.That((string)Get(receipt,"Reason"),Is.EqualTo(own?"OWN GOAL":"GOAL"));
 }

 [UnityTest,Timeout(60000)]
 public IEnumerator StableIdentityHistoryRejectsNamesRebindingAndContactsOutsideTheRelevantPlay()
 {
  using(var f=new Fixture("Identity"))
  {
   yield return f.Load();f.match.enabled=false;Call(f.contact,"RecordTouch",0);Call(f.contact,"RecordTouch",1);Call(f.contact,"RecordTouch",3);string blue=(string)Get(f.Player(1),"participantId"),orange=(string)Get(f.Player(3),"participantId");
   Assert.That((int)Call(f.contact,"ScorerSlot",0),Is.EqualTo(1));Assert.That((int)Call(f.contact,"ScorerSlot",1),Is.EqualTo(3));Assert.That((int)Get(f.contact,"LastTouchTeam"),Is.EqualTo(1));Assert.That((string)Get(f.contact,"LastTouchParticipantId"),Is.EqualTo(orange));
   // Controlled roster rebinding proves identity lookup, not physical traversal.
   string first=(string)Get(f.Player(0),"participantId");Set(f.Player(0),"participantId",blue);Set(f.Player(1),"participantId",first);Set(f.Player(0),"name","RENAMED AFTER TOUCH");Assert.That((int)Call(f.contact,"ScorerSlot",0),Is.Zero);
   Set(f.Player(0),"participantId",Guid.NewGuid().ToString("N"));Assert.That((int)Call(f.contact,"ScorerSlot",0),Is.EqualTo(-1),"A replacement sharing the slot/name must not inherit scorer credit.");Call(f.contact,"ForgetTruckContact",3);Assert.That((int)Get(f.contact,"LastTouch"),Is.EqualTo(3),"A single truck recovery is not a new play.");
   Call(f.rules,"Pause");Call(f.contact,"RecordTouch",2);Assert.That((string)Get(f.contact,"LastTouchParticipantId"),Is.EqualTo(orange));Call(f.rules,"Resume");Call(f.contact,"RecordTouch",-1);Call(f.contact,"RecordTouch",6);Call(f.match,"ResetPositions");
   Assert.That((int)Get(f.contact,"LastTouch"),Is.EqualTo(-1));Assert.That((string)Get(f.contact,"LastTouchParticipantId"),Is.Null);Assert.That((int)Get(f.contact,"LastTouchTeam"),Is.EqualTo(-1));for(int team=0;team<2;team++)Assert.That((int)Call(f.contact,"ScorerSlot",team),Is.EqualTo(-1));
   Call(f.rules,"Faceoff");Call(f.contact,"RecordTouch",2);Assert.That((int)Get(f.contact,"LastTouch"),Is.EqualTo(-1));Assert.That(f.receipts.Count,Is.Zero);Assert.That(((int[])Get(f.rules,"Score")).Sum(),Is.Zero);
   TestContext.WriteLine("Controlled contact-history boundary test with six duplicate names: stable IDs, rebinding/removal eligibility, pause/faceoff exclusion, transient contact forget and original full play reset. No goal or physical-touch claim in this test.");
  }
 }

 [UnityTest,Timeout(120000)]
 public IEnumerator BodyAndBladeTouchesProduceOneOwnGoalReceiptWithEligibleScorerOrTeamFallback()
 {
  using(var f=new Fixture("Physical"))
  {
   yield return f.Load();yield return Touch(f,1,false);string blue=(string)Get(f.Player(1),"participantId");yield return Touch(f,3,true);Assert.That((int)Call(f.contact,"ScorerSlot",0),Is.EqualTo(1));yield return Cross(f,1,1);
   Assert.That(f.receipts.Count,Is.EqualTo(1));Receipt(f.receipts[0],blue,true,false,1);Assert.That((int)Get(f.match,"LastGoalScorerSlot"),Is.EqualTo(1));Assert.That((string)Get(f.match,"LastGoalScorerId"),Is.EqualTo(blue));Assert.That((bool)Get(f.match,"LastGoalOwnGoal"),Is.True);Assert.That((string)Get(f.match,"Notice"),Does.Contain("OWN GOAL"));Assert.That(((int[])Get(f.rules,"Score")),Is.EqualTo(new[]{1,0}));
   var menus=Get(f.match,"menus");var presentation=Get(menus,"presentation");Call(presentation,"Refresh");Assert.That((string)Get(Get(menus,"messageText"),"text"),Does.Contain("OWN GOAL"));
   Call(f.match,"ConfirmGoal");Call(f.match,"ConfirmGoal");yield return new WaitForFixedUpdate();Assert.That(f.receipts.Count,Is.EqualTo(1));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(1));
   float deadline=Time.realtimeSinceStartup+4;while(Get(f.rules,"Phase").ToString()!="Faceoff"&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(Get(f.rules,"Phase").ToString(),Is.EqualTo("Faceoff"));Assert.That((int)Get(f.contact,"LastTouch"),Is.EqualTo(-1));for(int team=0;team<2;team++)Assert.That((int)Call(f.contact,"ScorerSlot",team),Is.EqualTo(-1));
   yield return f.Live();yield return Touch(f,3,false);yield return Cross(f,1,2);Assert.That(f.receipts.Count,Is.EqualTo(2));Receipt(f.receipts[1],null,true,true,2);Assert.That((int)Get(f.match,"LastGoalScorerSlot"),Is.EqualTo(-1));Assert.That((string)Get(f.match,"Notice"),Does.Contain("TEAM GOAL").And.Contain("OWN GOAL"));Assert.That(((int[])Get(f.rules,"Score")),Is.EqualTo(new[]{2,0}));
   yield return f.Live();yield return Touch(f,2,true);string direct=(string)Get(f.Player(2),"participantId");yield return Cross(f,1,3);Assert.That(f.receipts.Count,Is.EqualTo(3));Receipt(f.receipts[2],direct,false,false,3);Assert.That((string)Get(f.match,"Notice"),Does.Not.Contain("OWN GOAL"));
   string prior=(string)Get(f.match,"MatchId");Call(f.match,"Restart");Assert.That((string)Get(f.match,"MatchId"),Is.Not.EqualTo(prior));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);Assert.That((string)Get(f.match,"LastGoalScorerId"),Is.Null);Assert.That((int)Call(f.contact,"ScorerSlot",0),Is.EqualTo(-1));Assert.That(((int[])Get(f.rules,"Score")).Sum(),Is.Zero);
   prior=(string)Get(f.match,"MatchId");Call(f.match,"ChangeSetup");Assert.That((bool)Call(f.match,"StartMatch",true),Is.True);Assert.That((string)Get(f.match,"MatchId"),Is.Not.EqualTo(prior));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);Assert.That((int)Call(f.contact,"ScorerSlot",1),Is.EqualTo(-1));Assert.That(f.receipts.Count,Is.EqualTo(3));
   TestContext.WriteLine("Real saved-rink body/blade contacts and original full-puck goal crossing: eligible scoring-team identity credited across defender own goal; next-play own goal has team-only recipient; direct goal receives correct identity. Team delta unchanged, one event per goal, current presentation notice, faceoff histories, restart and swapped-end rematch checked. Controlled initial puck/truck poses; no full AI match claim.");
  }
 }
}
#endif
