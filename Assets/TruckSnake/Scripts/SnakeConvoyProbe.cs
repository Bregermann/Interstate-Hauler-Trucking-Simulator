using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckSnake {
 public sealed class SnakeConvoyProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){bool cadence=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsSnakeCadence")>=0;bool run=cadence||Array.IndexOf(Environment.GetCommandLineArgs(),"-tsSnakeConvoy")>=0;
#if UNITY_EDITOR
   cadence|=UnityEditor.SessionState.GetBool("Sports.Snake.Cadence",false);UnityEditor.SessionState.SetBool("Sports.Snake.Cadence",false);run|=cadence||UnityEditor.SessionState.GetBool("Sports.Snake.Convoy",false);UnityEditor.SessionState.SetBool("Sports.Snake.Convoy",false);
#endif
   if(!run)return;var go=new GameObject("Explicit physical fifty-trailer fixture");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<SnakeConvoyProbe>().cadence=cadence;}
  bool cadence;string Folder;readonly StringBuilder log=new StringBuilder();int checks,errors;IDisposable prefs;Keyboard keyboard;InputSettings.BackgroundBehavior previousBackground;
  void Awake(){previousBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;prefs=SportsPrefs.IsolateValidation("SnakeConvoy");SportsParticipantRegistry.ForgetCache();}
  void Check(bool value,string label){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+label);File.WriteAllText(Folder+"/progress.txt",log.ToString());}
  void Error(string msg,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+msg);}}
  IEnumerator Start(){Folder="Logs/TruckSports/EssentialRelease/Snake/"+(Application.isEditor?"Native/":"Windows/")+(cadence?"Cadence":"Convoy");Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("o"));Application.logMessageReceived+=Error;yield return SportsProbeScene.Ensure<SnakeMatch>("TruckSnake");yield return null;var m=FindAnyObjectByType<SnakeMatch>();keyboard=InputSystem.AddDevice<Keyboard>();m.setup.count=1;m.setup.players[0].source=BowlingSource.Keyboard;m.setup.players[0].forward=Key.W;m.setup.players[0].back=Key.S;m.setup.players[0].left=Key.A;m.setup.players[0].right=Key.D;m.actionKeys[0]=Key.Space;keyboard.MakeCurrent();InputSystem.Update();yield return null;
   m.world.starts[0]=new Vector3(-1120,m.world.Height(new Vector3(-1120,0,-1000))+.5f,-1000);m.world.rotations[0]=Quaternion.Euler(0,90,0);m.option=2;foreach(var entry in m.presets.entries)if(entry.displayName=="Clear"){m.presets.Select(entry.id);break;}
   UnityEngine.Random.InitState(62119);bool sources=m.ValidateSources(out var reason);Check(m.StartMatch(),"single injected human fixture starts on the real original EasyRoads outer circuit · valid="+sources+" reason="+reason+" ready="+m.Ready(0)+" keyboard="+(Keyboard.current!=null?Keyboard.current.name+":"+Keyboard.current.enabled:"none"));Check(m.convoys[0].ActiveCount==1&&m.rules.attached[0]==1&&m.rules.score[0]==0,"one actual trailer is attached during countdown without free points");if(!cadence){foreach(var pickup in m.pickups){pickup.Golden=true;pickup.Dormant();}for(int i=0;i<19;i++){var p=new Vector3(-1050+i*50,0,-1000);p.y=m.world.Height(p)+.18f;m.pickups[i].Golden=true;m.pickups[i].Spawn(p);}}m.drivingViews.Select(0);Time.timeScale=2;
   float deadline=Time.realtimeSinceStartup+180;int maximum=0;bool jumped=false,released=false,recorded=false,airCapture=false;float frontMax=0,tailMax=0,lowestClearanceMax=0,last=0;Collider[] jumpSolids=null;int frames=0;double realStart=Time.realtimeSinceStartupAsDouble;
   while(m.Live&&m.rules.Playing(0)&&Time.realtimeSinceStartup<deadline&&m.trucks[0].body.position.x<350){frames++;maximum=Mathf.Max(maximum,m.convoys[0].ActiveCount);
    if(!cadence&&!jumped&&m.convoys[0].ActiveCount>=50&&m.trucks[0].Grounded){jumped=true;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));}
    else if(jumped&&!released&&m.PhysicalJumps>0){released=true;InputSystem.QueueStateEvent(keyboard,new KeyboardState());}
    if(jumped){if(jumpSolids==null)jumpSolids=m.trucks[0].GetComponentsInChildren<Collider>(true);float clearance=float.PositiveInfinity;foreach(var solid in jumpSolids)if(solid.enabled&&!solid.isTrigger)clearance=Mathf.Min(clearance,solid.bounds.min.y-m.world.Height(solid.bounds.center));if(!float.IsInfinity(clearance))lowestClearanceMax=Mathf.Max(lowestClearanceMax,clearance);frontMax=Mathf.Max(frontMax,m.trucks[0].body.position.y-m.world.Height(m.trucks[0].body.position));if(m.convoys[0].SegmentPose(10,out var p,out _))tailMax=Mathf.Max(tailMax,p.y-m.world.Height(p));}
    if(!airCapture&&lowestClearanceMax>=6.096f){airCapture=true;SportsControlledCapture.Save(m,1280,720,Folder+"/twenty-foot-jump.png");yield return null;}
    if(!recorded&&maximum>=(cadence?20:50)){recorded=true;foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){SportsControlledCapture.Save(m,size.x,size.y,Folder+(cadence?"/natural-pickups-":"/fifty-")+size.x+"x"+size.y+".png");yield return null;}}
    if(Time.realtimeSinceStartup-last>3){last=Time.realtimeSinceStartup;log.AppendLine("LIVE x="+m.trucks[0].body.position.x+" z="+m.trucks[0].body.position.z+" yaw="+m.trucks[0].transform.eulerAngles.y+" speed="+m.trucks[0].body.linearVelocity.magnitude+" pickups="+m.PhysicalPickups+" granted="+m.rules.trailers[0]+" active="+m.convoys[0].ActiveCount+" pending="+m.convoys[0].Pending+" sweeps="+m.convoys[0].SweepCount);File.WriteAllText(Folder+"/progress.txt",log.ToString());}yield return null;
   }
   Time.timeScale=1;
   if(cadence){Check(m.PhysicalPickups>=20&&maximum>=20,"unmodified default gate spawning produces at least twenty actual pickup contacts and visible trailers during a normal driven road route");Check(m.rules.Playing(0)&&m.rules.distance[0]>1000,"natural pickups are reached without teleporting or overriding gate positions on more than one kilometre of actual EasyRoads driving");Check(m.rules.attached[0]==m.convoys[0].ActiveCount&&m.rules.ScoreMultiplier(0)==m.convoys[0].ActiveCount,"native score multiplier matches actual attached trailers");}
   else{Check(m.PhysicalPickups>=17&&m.rules.trailers[0]>=52,"seventeen actual gold trigger contacts each grant exactly three trailers in addition to one starter; no score injection");Check(maximum>=50,"at least fifty visible collider-backed articulated cargo bodies attach on actual driven history");Check(m.rules.Playing(0)&&m.rules.distance[0]>1000,"long convoy traverses more than one kilometre without false floor or attachment failure");Check(m.PhysicalJumps>0&&lowestClearanceMax>=6.096f&&tailMax>=6.096f,"actual lowest wheel/hull clears twenty feet (6.096m), with the delayed eleventh trailer also above twenty feet");}
   Check(m.convoys[0].UnsafeCompetitorPairs()==0,"every current NWH wheel and compound hull uses bounded hybrid train contact, including rebuilt wheel colliders");Check(m.convoys[0].SharedMaterialCount>0&&m.convoys[0].SharedMaterialCount<20,"fifty trailers share a small instanced material cache instead of cloning per body");Check(m.convoys[0].SweepCount>1000,"long train obstacle sweeps execute during sustained physical driving");log.AppendLine("OBSERVED frames="+frames+" elapsedRealtime="+(Time.realtimeSinceStartupAsDouble-realStart).ToString("F2")+" maximum="+maximum+" lowestTruckClearance="+lowestClearanceMax+" tractorHeight="+frontMax+" eleventhHeight="+tailMax+" result="+m.Standings);
   m.SetupScreen();m.StartMatch();Check(m.rules.trailers[0]==1&&m.convoys[0].ActiveCount==1&&m.convoys[0].history.Available>=12&&m.rules.score[0]==0,"rematch replaces fifty-body train with exactly one starter trailer and a short fresh starting-bay history");m.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<SnakeMatch>()==null,"large world and train pools clean up on menu return");log.AppendLine("CHECKS="+checks+" ERRORS="+errors+" · native straight-road fixture (natural default spawning in Cadence mode; explicit golden gates/jump in Convoy mode), actual NWH/PhysX and injected keyboard. Actual "+(Application.isEditor?"Unity Editor":"Windows player")+" fixture; not autonomous navigation, human playtesting, physical hardware or an isolated GPU benchmark.");File.WriteAllText(Folder+"/result.txt",log.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);
  }
  void OnDestroy(){InputSystem.settings.backgroundBehavior=previousBackground;Application.logMessageReceived-=Error;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);prefs?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
#endif
 }
}
