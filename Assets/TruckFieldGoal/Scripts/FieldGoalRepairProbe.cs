#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckSports;
namespace LWS.TruckFieldGoal {
// Explicit development acceptance uses private preferences and real saved-scene reloads.
public sealed class FieldGoalRepairProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){
  var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsFieldGoalRepair");bool run=at>=0;string mode=at>=0&&at+1<args.Length?args[at+1]:"migration";
#if UNITY_EDITOR
  if(UnityEditor.SessionState.GetBool("FieldGoal.RepairProbe",false)){run=true;mode=UnityEditor.SessionState.GetString("FieldGoal.RepairMode","migration");}UnityEditor.SessionState.SetBool("FieldGoal.RepairProbe",false);
#endif
  if(!run)return;var root=new GameObject("Field Goal explicit repair acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<FieldGoalRepairProbe>().mode=mode;
 }
 IDisposable scope;string folder,mode="migration";int checks,errors;readonly StringBuilder log=new StringBuilder();
 void Awake(){scope=SportsPrefs.IsolateValidation("FieldGoalRepair");SportsParticipantRegistry.ForgetCache();Application.logMessageReceived+=Error;}
 void Error(string value,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+value+"\n"+stack);Write();}}
 void Check(bool ok,string value){checks++;if(!ok)errors++;log.AppendLine((ok?"PASS ":"FAIL ")+value);Write();}
 void Write(){if(folder!=null)File.WriteAllText(folder+"/progress.txt",log.ToString());}
 IEnumerator Start(){
  folder=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"..":"../../..","Logs/TruckSports/FieldGoalRepair",Application.isEditor?"Native":"Windows",mode));Directory.CreateDirectory(folder);File.WriteAllText(folder+"/result.txt","RUNNING");log.AppendLine("FIELD_GOAL_REPAIR mode="+mode+" PID="+System.Diagnostics.Process.GetCurrentProcess().Id+" editor="+Application.isEditor);
  var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object next=null;try{var r=stack.Peek();if(!r.MoveNext()){stack.Pop();continue;}next=r.Current;if(next is IEnumerator child){stack.Push(child);continue;}}catch(Exception e){Check(false,e.ToString());break;}yield return next;}
  log.AppendLine("CHECKS="+checks);log.AppendLine("ERRORS="+errors);log.AppendLine(errors==0?"PASS":"FAIL");File.WriteAllText(folder+"/result.txt",log.ToString());
#if UNITY_EDITOR
  if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
  Application.Quit(errors==0?0:2);
 }
 IEnumerator Load(){SportsNavigation.LoadSport(SportsNavigation.FieldGoal);yield return null;yield return null;}
 IEnumerator Run(){if(mode=="weatherplay")yield return WeatherPlay();else if(mode=="performance")yield return Performance();else if(mode=="physics")yield return PhysicsChecks();else if(mode=="setup")yield return SetupChecks();else if(mode=="brand")yield return Brand();else if(mode=="rendering")yield return Rendering();else if(mode=="weather")yield return Weather();else if(mode=="migration")yield return Migration();else throw new ArgumentException("Unknown Field Goal repair probe "+mode);}
 IEnumerator WeatherPlay(){
  yield return Load();var m=FindAnyObjectByType<FieldGoalMatch>();
  foreach(var name in new[]{"Heavy Rain","Blizzard","Hurricane"}){
   m.SetupScreen();var preset=m.weatherPresets.entries.First(p=>p.displayName==name);m.weatherPresets.Select(preset.id);m.option=0;m.setup.count=2;m.NormalizeSetup();m.AllBots();for(int i=0;i<2;i++)m.setup.players[i].difficulty=LWS.TruckBowling.BowlingDifficulty.Normal;
   Check(m.StartMatch(),"actual two-AI weather game starts "+name);Time.timeScale=8;int seed=m.MatchSeed;var snow=m.GetComponent<SportsSnowSurface>();bool launched=false,snowContact=false,captured=false;float end=Time.realtimeSinceStartup+140;int maximumRound=0;
   while(!m.Finished&&Time.realtimeSinceStartup<end){
    maximumRound=Mathf.Max(maximumRound,m.rules.Round);launched|=m.football.LaunchObserved;snowContact|=snow!=null&&snow.Coverage>.6f&&snow.Contacts>0;
    if(m.MatchSeed!=seed||m.atmosphere.PresetId!=preset.id){Check(false,"weather reset during an actual attempt "+name);break;}
    if(!captured&&m.Phase==FieldGoalPhase.Flight&&m.FlightAge>.2f){captured=true;log.AppendLine("ACTUAL_WEATHER_KICK "+name+" wind="+m.CurrentWind+" grip="+m.truck.grip+" launch="+m.football.LaunchVelocity);Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);if(Application.isEditor)SportsControlledCapture.Save(m,1280,720,folder+"/"+name+"-controlled.png");else{Screen.SetResolution(1280,720,false);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(folder+"/"+name+"-window.png");yield return new WaitForSecondsRealtime(.2f);}Time.timeScale=8;}
    yield return null;
   }
   Check(m.Finished&&m.rules.attempts.Take(2).All(n=>n>=5),"complete physical five-attempt weather game "+name);
   Check(launched&&m.rules.attempts.Sum()>=10,"weather game requires actual bumper launches "+name);Check(m.truck.grip<1.35f,"wet/snow preset applies equivalent NWH grip "+name);
   if(preset.Snow)Check(snowContact,"substantial actual snow coverage and grounded NWH tire contact observed "+name);
   log.AppendLine("WEATHER_MATCH "+name+" goals="+string.Join(",",m.rules.goals)+" attempts="+string.Join(",",m.rules.attempts)+" maximumRound="+maximumRound+" snowContact="+snowContact);Write();if(!m.Finished)break;
  }
  m.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<SportsFrontEnd>()!=null&&FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"weather games clean up into collection");
 }
 IEnumerator Performance(){
  yield return Load();var m=FindAnyObjectByType<FieldGoalMatch>();m.weatherPresets.Select(m.weatherPresets.entries.First(p=>p.displayName=="Blizzard").id);m.option=0;m.setup.count=4;m.NormalizeSetup();m.AllBots();if(!Application.isEditor){Screen.SetResolution(1920,1080,false);yield return new WaitForSecondsRealtime(.5f);}Check(m.StartMatch(),"four-AI snowy match begins for rendered single-camera hot-seat sample");Time.timeScale=1;
  var samples=new System.Collections.Generic.List<float>();int focused=0,live=0,rematches=0;yield return new WaitForSecondsRealtime(2);float until=Time.realtimeSinceStartup+60;
  while(Time.realtimeSinceStartup<until){if(m.Finished){m.StartMatch();rematches++;}samples.Add(Time.unscaledDeltaTime*1000);if(Application.isFocused)focused++;if(m.Live)live++;yield return null;}
  samples.Sort();log.AppendLine("PERFORMANCE realSeconds=60 editor="+Application.isEditor+" resolution="+Screen.width+"x"+Screen.height+" frames="+samples.Count+" meanMs="+samples.Average()+" p95Ms="+samples[(int)(samples.Count*.95f)]+" p99Ms="+samples[(int)(samples.Count*.99f)]+" focusedFrames="+focused+" liveFrames="+live+" rematches="+rematches+" gpu="+SystemInfo.graphicsDeviceName);Check(samples.Count>=600&&live>=samples.Count*.98f,"sustained rendered snow match advances without exceptions");log.AppendLine("One broadcast camera is this sport's supported sequential presentation. Other applications were not closed or controlled. This is not four-view or hardware-input acceptance.");
 }
 IEnumerator PhysicsChecks(){
  yield return Load();var m=FindAnyObjectByType<FieldGoalMatch>();var model=m.flightModel;model.Kick(Vector3.forward*14,Vector3.zero,.46f,out var baseline,out var spin);model.Kick(Vector3.forward*8,Vector3.zero,.46f,out var soft,out _);model.Kick(Vector3.forward*20,Vector3.zero,.46f,out var hard,out _);
  Check(soft.z<baseline.z&&baseline.z<hard.z&&hard.z<=32,"measured bumper speed changes bounded kick power");model.Kick(new Vector3(4,0,14),Vector3.zero,.46f,out var angled,out _);Check(angled.x>4,"actual approach direction produces a shank without homing");
  model.Kick(Vector3.forward*14,Vector3.up*.4f,.46f,out var high,out _);model.Kick(Vector3.forward*14,Vector3.down*.4f,.46f,out var low,out _);Check(high.y<baseline.y&&low.y>baseline.y,"contact height changes elevation rather than forcing one arc");
  model.Kick(Vector3.forward*14,Vector3.right*.35f,.46f,out var right,out var sideSpin);model.Kick(Vector3.forward*14,Vector3.left*.35f,.46f,out var left,out _);Check(right.x<0&&left.x>0&&sideSpin!=spin,"off-center contact changes line and angular motion");
  Check(model.AirAcceleration(Vector3.zero,Vector3.zero,Vector3.zero)==Vector3.zero,"still ball in still air receives no invented motion");Check(Vector3.Dot(model.AirAcceleration(baseline,Vector3.zero,Vector3.zero),baseline)<0,"relative-air drag opposes flight");Check(model.AirAcceleration(Vector3.forward*100,Vector3.up*100,Vector3.right*100).magnitude<=model.maximumDragAcceleration+model.maximumSpinAcceleration+.001f,"aerodynamic acceleration remains bounded");
  var scene=SceneManager.CreateScene("Owned Field Goal isolated aerodynamic fixtures",new CreateSceneParameters(LocalPhysicsMode.Physics3D));var physics=scene.GetPhysicsScene();var root=new GameObject("Explicitly initialized real Rigidbody flight comparator");SceneManager.MoveGameObjectToScene(root,scene);var body=root.AddComponent<Rigidbody>();body.mass=.75f;body.linearDamping=.025f;body.angularDamping=.025f;
  var winds=new[]{Vector3.zero,Vector3.back*15,Vector3.forward*15,Vector3.right*15,Vector3.left*15};var landings=new Vector3[5];var crossings=new Vector3[5];
  try{for(int i=0;i<winds.Length;i++){
   body.position=new Vector3(0,1.23f,0);body.rotation=Quaternion.identity;body.linearVelocity=baseline;body.angularVelocity=spin;body.WakeUp();var previous=body.position;bool crossed=false;
   for(int step=0;step<600;step++){body.AddForce(model.AirAcceleration(body.linearVelocity,body.angularVelocity,winds[i]),ForceMode.Acceleration);physics.Simulate(.01f);var now=body.position;if(!crossed&&now.z>=30){crossings[i]=Vector3.Lerp(previous,now,Mathf.InverseLerp(previous.z,now.z,30));crossed=true;}if(now.y<=.3f){landings[i]=now;break;}previous=now;}
   Check(landings[i].z>10&&!float.IsNaN(landings[i].x),"real PhysX flight remains finite and lands comparison="+i);log.AppendLine("INITIALIZED_PHYSX wind="+winds[i]+" kick="+baseline+" spin="+spin+" goalCross="+crossings[i]+" landing="+landings[i]);
  }}finally{Destroy(root);SceneManager.UnloadSceneAsync(scene);}
  Check(landings[1].z<landings[0].z&&landings[0].z<landings[2].z,"same initialized kick travels shorter in headwind and farther in tailwind");Check(landings[3].x>2&&landings[4].x< -2,"continuous crosswinds produce opposite physical drift");Check(crossings[3].x>crossings[0].x+.7f&&crossings[4].x<crossings[0].x-.7f,"same scoring-plane approach drifts by more than1.5 ball radii in crosswind");
  log.AppendLine("These are deterministic contact-model checks and explicitly initialized real Rigidbody simulations. They do not replace driven bumper contacts, actual Weather Maker matches, or hardware testing.");
 }
 IEnumerator SetupChecks(){
  yield return Load();var m=FindAnyObjectByType<FieldGoalMatch>();var picker=m.menus.pages[0].GetComponentInChildren<SportsWeatherPicker>(true);Check(picker!=null&&picker.dropdown.items.Count==m.weatherPresets.entries.Length+1,"Heat selector exposes Random and all installed forecasts");
  var clear=m.weatherPresets.entries.First(p=>p.displayName=="Clear");picker.Choose(m.weatherPresets.Find(clear.id)+1);m.option=0;m.setup.count=4;m.NormalizeSetup();m.AllBots();Check(m.StartMatch(),"four-player bumper competition starts from shared forecast");var selected=m.atmosphere.PresetId;int seed=m.MatchSeed;yield return new WaitForSeconds(.2f);Check(m.Forecast==clear&&selected==clear.id,"explicit selected forecast overrides Random");
  Check(((CapsuleCollider)m.football.shape).direction==1&&m.football.shape.bounds.size.y>m.football.shape.bounds.size.z,"saved authoritative football is upright on its tee");Check(m.transform.Find("Stationary football holder truck")!=null&&m.transform.Find("Stationary football holder truck").GetComponentsInChildren<Rigidbody>(true).Length==0,"holder reuses the truck model without another physics or input owner");Check(m.endZone!=null&&Mathf.Abs(m.endZone.position.z+4.572f-m.GoalZ)<.001f,"goalposts align with the back of the ten-yard end zone");
  m.Resolve(false,"EXPLICIT PROGRESSION FIXTURE",m.football.Reference);m.ExtraAction("replayskip");typeof(FieldGoalMatch).GetProperty("Phase").SetValue(m,FieldGoalPhase.AttemptResult);typeof(FieldGoalMatch).GetProperty("PhaseAge").SetValue(m,1.3f);yield return new WaitForSeconds(.1f);Check(m.atmosphere.PresetId==selected&&m.MatchSeed==seed,"ordinary turn progression does not reroll or reset weather");
  m.Pause();var wind=m.CurrentWind;var ball=m.football.body.position;yield return new WaitForSecondsRealtime(.2f);Check(m.Paused&&m.football.body.position==ball&&m.CurrentWind==wind,"pause preserves ball and authoritative wind state");m.Resume();m.SetupScreen();yield return Load();m=FindAnyObjectByType<FieldGoalMatch>();Check(m.weatherPresets.Selection==clear.id,"stable shared weather preference survives saved-scene reload");
  Check(m.menus.pages[0].GetComponentInChildren<SportsWeatherPicker>(true).dropdown.selectedItemIndex==m.weatherPresets.Find(clear.id)+1,"reloaded Heat selector displays the saved preference");
  m.menus.Show(SportsEventScreen.Setup);yield return new WaitForSecondsRealtime(.2f);picker=m.menus.pages[0].GetComponentInChildren<SportsWeatherPicker>(true);UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(picker.gameObject);UnityEngine.EventSystems.ExecuteEvents.Execute(picker.gameObject,new UnityEngine.EventSystems.BaseEventData(UnityEngine.EventSystems.EventSystem.current),UnityEngine.EventSystems.ExecuteEvents.submitHandler);yield return new WaitForSecondsRealtime(.4f);Check(picker.dropdown.isOn,"actual UI Submit opens Heat dropdown with its installed animation");
  var last=picker.dropdown.items.Last().itemButton;UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(last.gameObject);yield return new WaitForSecondsRealtime(.2f);var scroll=picker.dropdown.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);var viewport=scroll.viewport!=null?scroll.viewport:scroll.GetComponent<RectTransform>();var corners=new Vector3[4];last.GetComponent<RectTransform>().GetWorldCorners(corners);var minimum=viewport.InverseTransformPoint(corners[0]);var maximum=minimum;foreach(var corner in corners){var local=viewport.InverseTransformPoint(corner);minimum=Vector3.Min(minimum,local);maximum=Vector3.Max(maximum,local);}var bounds=new Bounds((minimum+maximum)*.5f,maximum-minimum);log.AppendLine("DROPDOWN_NAV selected="+UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name+" bounds="+bounds+" viewport="+viewport.rect+" content="+scroll.content.rect+" normalized="+scroll.verticalNormalizedPosition);Check(bounds.min.y>=viewport.rect.yMin-2&&bounds.max.y<=viewport.rect.yMax+2,"last forecast remains visible under selected-item navigation");if(Application.isEditor)SportsControlledCapture.Save(m,1280,720,folder+"/weather-picker-last-1280-controlled.png");
  UnityEngine.EventSystems.ExecuteEvents.Execute(last.gameObject,new UnityEngine.EventSystems.BaseEventData(UnityEngine.EventSystems.EventSystem.current),UnityEngine.EventSystems.ExecuteEvents.submitHandler);yield return new WaitForSecondsRealtime(.3f);Check(!picker.dropdown.isOn&&m.weatherPresets.Selection==m.weatherPresets.entries.Last().id,"real vendor item callback closes list and saves exactly its forecast");picker.dropdown.Animate();yield return new WaitForSecondsRealtime(.3f);picker.dropdown.items[m.weatherPresets.Find(clear.id)+1].itemButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);Check(m.weatherPresets.Selection==clear.id,"another actual item click restores Clear");
  if(Application.isEditor)foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})SportsControlledCapture.Save(m,size.x,size.y,folder+"/setup-"+size.x+"-controlled.png");
  picker.dropdown.Animate();yield return new WaitForSecondsRealtime(.4f);if(Application.isEditor)SportsControlledCapture.Save(m,1280,720,folder+"/weather-picker-open-1280-controlled.png");var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();try{UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));yield return null;yield return null;Check(!picker.dropdown.isOn,"injected Escape cancels open forecast list through the existing UI action");}finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);}
  m.weatherPresets.Select(SportsWeatherPresets.RandomId);m.option=0;m.setup.count=1;m.NormalizeSetup();m.AllBots();Check(m.StartMatch(),"Random forecast starts normally");int firstSeed=m.MatchSeed;m.SetupScreen();yield return Load();m=FindAnyObjectByType<FieldGoalMatch>();m.option=0;m.setup.count=1;m.NormalizeSetup();m.AllBots();Check(m.StartMatch()&&m.MatchSeed!=firstSeed&&m.weatherPresets.Find(m.Forecast.id)>=0,"Random advances its persistent sequence after a real scene reload");m.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<SportsFrontEnd>()!=null&&FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"setup/reload returns to collection with one audio owner");
 }
 IEnumerator Brand(){
  SportsNavigation.OpenSelection=false;SportsNavigation.FrontEnd();yield return null;yield return null;var front=FindAnyObjectByType<SportsFrontEnd>();front.Show(0);yield return new WaitForSecondsRealtime(.3f);
  var root=front.pages[0].transform.Find("Truck Sports Party original wordmark");var image=root!=null?root.GetComponent<UnityEngine.UI.Image>():null;
  Check(front.Page==0&&front.pages.Count(p=>p.activeInHierarchy)==1,"title has exactly one visible page");Check(image!=null&&image.sprite!=null&&image.preserveAspect&&!image.raycastTarget,"original wordmark is wired without intercepting menu input");
  Check(!front.pages[0].transform.Find("Title").gameObject.activeSelf,"old text title is replaced by the graphical mark");
  foreach(var key in new[]{"play","help","settings","quit","participants"})Check(front.pages[0].GetComponentsInChildren<SportsMenuAction>(true).Count(a=>a.action==key)==1,"existing title action preserved "+key);
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){
   if(Application.isEditor)SportsControlledCapture.Save(front.GetComponent<Canvas>(),new[]{Camera.main},size.x,size.y,folder+"/title-"+size.x+"-controlled.png");
   else{Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.4f);ScreenCapture.CaptureScreenshot(folder+"/title-"+size.x+"-window.png");yield return new WaitForSecondsRealtime(.2f);Check(Screen.width==size.x&&Screen.height==size.y,"actual title window size "+size);}
  }
  front.Action("play");yield return null;Check(front.Page==1&&front.catalogue.cards.Length==32,"existing playable catalogue remains reachable");front.Action("back");yield return null;Check(front.Page==0,"back returns to the new title");front.Action("settings");yield return null;Check(front.Page==3,"settings remains reachable");front.Action("back");yield return null;Check(front.Page==0&&FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"title returns with one audio owner");
 }
 IEnumerator Rendering(){
  yield return Load();var m=FindAnyObjectByType<FieldGoalMatch>();m.menus.Show(SportsEventScreen.Play);var p=m.weatherPresets.entries.First(x=>x.displayName=="Hurricane");m.atmosphere.ConfigurePreset(p,123);Time.timeScale=1;
  var sky=m.atmosphere.vendor.GetComponentInChildren<DigitalRuby.WeatherMaker.WeatherMakerSkySphereScript>(true);var fog=m.atmosphere.vendor.GetComponentInChildren<DigitalRuby.WeatherMaker.WeatherMakerFullScreenFogScript>(true);
  for(int i=0;i<8;i++){sky.GetComponent<MeshRenderer>().enabled=(i&1)!=0;m.atmosphere.clouds.enabled=(i&2)!=0;fog.enabled=(i&4)!=0;m.menus.header.text="RENDER DIAGNOSTIC · ORDINARY SKY MESH "+((i&1)!=0)+" CLOUD "+((i&2)!=0)+" FOG "+((i&4)!=0);yield return new WaitForSeconds(1);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(folder+"/passes-"+i+".png");yield return new WaitForSeconds(.3f);}
  Check(true,"eight temporary rendering isolation frames captured; visual inspection still required");
 }
 IEnumerator Migration(){
  yield return Load();var m=FindAnyObjectByType<FieldGoalMatch>();Check(m.OptionCount==2&&!m.IsTruck&&!m.UsesAction,"only bumper competition and bumper practice remain playable");
  Check(!m.boot.gameObject.activeInHierarchy&&!m.bootMechanism.gameObject.activeInHierarchy,"saved detached boot and mechanism are disabled before play");
  var saved=typeof(SportsEventSession).GetNestedType("Saved",BindingFlags.NonPublic);if(saved==null)throw new InvalidOperationException("Existing match-save contract missing");
  int[] options={0,1,2,3,0,1},versions={0,0,0,0,1,1},expected={0,0,1,1,0,1};
  for(int i=0;i<options.Length;i++){
   m.SetupScreen();m.option=0;m.setup.count=4;m.NormalizeSetup();m.AllBots();for(int p=0;p<4;p++)m.setup.players[p].name="Saved driver "+p;m.Save();
   var ids=m.setup.players.Take(4).Select(p=>p.participantId).ToArray();string key=m.PreferenceKey;var fixture=JsonUtility.FromJson(SportsPrefs.GetString(key),saved);saved.GetField("option").SetValue(fixture,options[i]);saved.GetField("optionVersion").SetValue(fixture,versions[i]);SportsPrefs.SetString(key,JsonUtility.ToJson(fixture));SportsPrefs.Save();
   yield return Load();m=FindAnyObjectByType<FieldGoalMatch>();Check(m.option==expected[i]&&m.Practice==(expected[i]==1),"actual saved-scene load maps option="+options[i]+" version="+versions[i]);
   Check(m.setup.count==(expected[i]==1?1:4),"competition keeps four slots and practice remains solo case="+i);
   Check(m.setup.players.Take(4).Select(p=>p.participantId).SequenceEqual(ids)&&m.setup.players.Take(4).Select(p=>p.name).SequenceEqual(new[]{"Saved driver 0","Saved driver 1","Saved driver 2","Saved driver 3"}),"migration preserves all saved driver identities/names case="+i);
   m.Save();var roundtrip=JsonUtility.FromJson(SportsPrefs.GetString(key),saved);Check((int)saved.GetField("optionVersion").GetValue(roundtrip)==1,"normal save writes the new mode schema case="+i);
   yield return Load();m=FindAnyObjectByType<FieldGoalMatch>();Check(m.option==expected[i]&&m.Practice==(expected[i]==1),"second reload never remaps new practice to competition case="+i);
  }
  m.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<SportsFrontEnd>()!=null&&FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"repeated saved-scene reloads return to one collection/audio owner");
 }
 IEnumerator Weather(){
  yield return Load();var m=FindAnyObjectByType<FieldGoalMatch>();
#if UNITY_EDITOR
  var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<SportsWeatherPresets>("Assets/TruckSports/Settings/SportsWeatherPresets.asset");
#else
  var catalog=m.weatherPresets;
#endif
  catalog.ValidateCatalog();m.menus.Show(SportsEventScreen.Play);Time.timeScale=4;
  foreach(var preset in catalog.entries){
   m.atmosphere.ConfigurePreset(preset,12345);yield return new WaitForSeconds(1.2f);m.atmosphere.Tick(1.2f);
   Check(m.atmosphere.PresetDriven&&m.atmosphere.PresetId==preset.id,"actual Weather Maker profile transition "+preset.displayName);
   var expected=preset.profile.PrecipitationProfile!=null?preset.profile.PrecipitationProfile.PrecipitationType:DigitalRuby.WeatherMaker.WeatherMakerPrecipitationType.None;
   Check(m.atmosphere.precipitation.Precipitation==expected,"actual precipitation manager matches profile "+preset.displayName);
   var wind=m.atmosphere.wind;float scale=wind.WindProfile!=null?wind.WindProfile.WindMainMultiplier:0;var actual=scale>.00001f?Vector3.ProjectOnPlane(wind.CurrentWindVelocity,Vector3.up)*(preset.metresPerSecondPerWindUnit/scale):Vector3.zero;
   Check((actual-m.atmosphere.Flow).sqrMagnitude<.00001f&&!float.IsNaN(actual.x),"physics wind reads the vendor wind, without a second wind controller "+preset.displayName);
   log.AppendLine("WEATHER_SMOKE "+preset.displayName+" precipitation="+expected+" intensity="+m.atmosphere.precipitation.PrecipitationIntensity+" wind="+m.atmosphere.Flow+" grip="+m.atmosphere.Grip);
   if(Application.isEditor){m.menus.header.text="WEATHER PROFILE SMOKE · "+preset.displayName;SportsControlledCapture.Save(m,1280,720,folder+"/"+preset.id+"-controlled.png");}
   if(preset.displayName=="Hurricane"||preset.displayName=="Clear"||preset.displayName=="Blizzard"){
    yield return new WaitForSeconds(.5f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(folder+"/"+preset.id+"-gameview.png");yield return new WaitForSeconds(.5f);
   }
  }
  SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<SportsFrontEnd>()!=null&&FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"all-profile smoke exits cleanly with one listener");
 }
 void OnDestroy(){Application.logMessageReceived-=Error;scope?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
}
}
#endif
