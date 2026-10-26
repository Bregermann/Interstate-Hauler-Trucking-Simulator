#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using LWS.TruckBowling;
using LWS.TruckHockey;
using LWS.TruckAirHockey;
using LWS.TruckCornhole;
using LWS.TruckCurling;
using LWS.TruckSumo;
using LWS.TruckSkeeBall;
using LWS.TruckWifeCarrying;
using LWS.TruckTargetLaunch;
using LWS.TruckTerrierRacing;
using LWS.TruckDockDiving;
namespace LWS.TruckSports
{
    // Explicit Windows-only profile/setup/render acceptance. Original AI and
    // original mode lifecycle own every movement, contact, turn and score.
    public sealed class SportsCarnivalAppearanceWindowsProbe:MonoBehaviour
    {
        const string Flag="-tsCarnivalAppearance",Output="-tsCarnivalAppearanceOutput";
        [RuntimeInitializeOnLoadMethod]static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)<0)return;
            if(Application.isEditor||Application.platform!=RuntimePlatform.WindowsPlayer){Debug.LogError("Carnival appearance acceptance requires the actual Windows player.");return;}
            var root=new GameObject("Explicit Carnival actual Windows appearance acceptance");DontDestroyOnLoad(root);if(!SportsProbeInputIsolation.Active)root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<SportsCarnivalAppearanceWindowsProbe>();
        }
        sealed class Venue{public string name;public int count;public float limit;public Venue(string name,int count,float limit){this.name=name;this.count=count;this.limit=limit;}}
        static readonly Venue[] Venues={new Venue("TruckSumo",4,8),new Venue("TruckAirHockey",2,8),new Venue("TruckCornhole",4,40),new Venue("TruckCurling",4,55),new Venue("TruckSkeeBall",4,14),new Venue("TruckWifeCarrying",8,14),new Venue("TruckTargetLaunch",4,90),new Venue("TruckTerrierRacing",12,25),new Venue("TruckDockDiving",8,45)};
        sealed class Actor{public string label;public Transform root;public Rigidbody body;public SportsTruckAppearanceHost host;public Func<int> owner;public Func<bool> required;public Collider[] core=Array.Empty<Collider>();public NWH.WheelController3D.WheelController[] wheels=Array.Empty<NWH.WheelController3D.WheelController>();}
        readonly StringBuilder log=new StringBuilder(),actorsLog=new StringBuilder("scene\tphase\tactor\trigidbodyId\tcolliderIds\tactualOwner\texpectedProfile\thostProfile\trequestedBody\teffectiveBody\trootScale\tposition\tvelocity\tstate\n"),frames=new StringBuilder("scene\tlabel\tsize\trequestFrame\tinspectionFrame\trequestState\tinspectionState\tview\towner\tvisibleActorMeshes\tobjective\tnonblack\trange\n");
        readonly HashSet<string> requestedBodies=new HashSet<string>(),effectiveBodies=new HashSet<string>();readonly HashSet<int> rendered=new HashSet<int>();readonly Dictionary<string,SportsTruckAppearanceData> saved=new Dictionary<string,SportsTruckAppearanceData>();
        SportsTruckAppearanceCatalog catalog;SportsEventSession sport;CornholeMatch corn;AirHockeyMatch air;Actor[] actors=Array.Empty<Actor>();Camera[] capturing;string folder,sceneName;IDisposable preferences;
        int checks,errors,visited,screenshots,oldWidth,oldHeight,targetNaturalSamples,targetLavaObserved,targetLavaNotObserved;bool targetLavaObservedThisVisit;FullScreenMode oldMode;float began;bool finished,evidenceOwned;string priorScope,playerRegistry;bool playerRegistryPresent;
        const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static object Read(object owner,string name){if(owner==null)return null;var type=owner.GetType();var field=type.GetField(name,Any);if(field!=null)return field.GetValue(owner);var property=type.GetProperty(name,Any);return property!=null&&property.GetIndexParameters().Length==0?property.GetValue(owner):null;}
        void Check(bool value,string message){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+message);Flush();}
        void Note(string message){log.AppendLine(message);Flush();}
        void Flush(){if(folder!=null)File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Error(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;Note("ERROR "+message+"\n"+stack);}
        IEnumerator Start()
        {
            IEnumerator run;try{Initialize();run=Run();}catch(Exception error){if(evidenceOwned){errors++;Note("INITIALIZATION_EXCEPTION "+error);Finish();}else{Debug.LogException(error);Application.Quit(2);}yield break;}
            var work=new Stack<IEnumerator>();work.Push(run);
            while(work.Count>0&&!finished)
            {
                if(Time.realtimeSinceStartup-began>570){Check(false,"570second overall appearance watchdog; remaining modes are unverified");break;}
                bool next=false;object value=null;Exception failure=null;try{next=work.Peek().MoveNext();if(next)value=work.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null){errors++;Note("PROBE_EXCEPTION "+failure);break;}if(!next){work.Pop();continue;}if(value is IEnumerator nested){work.Push(nested);continue;}yield return value;
            }
            Finish();
        }
        void Initialize()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,Output);if(index<0||index+1>=args.Length)throw new ArgumentException("Provide "+Output+" inside Logs/TruckSports/PlaytestRepair.");
            string root=Path.GetFullPath("Logs/TruckSports/PlaytestRepair").TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),guard=root+Path.DirectorySeparatorChar;string requested=Path.GetFullPath(args[index+1]);if(!requested.StartsWith(guard,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Canonical appearance evidence output required.");
            for(var directory=new DirectoryInfo(requested);directory!=null&&(directory.FullName==root||directory.FullName.StartsWith(guard,StringComparison.OrdinalIgnoreCase));directory=directory.Parent)if(directory.Exists&&(directory.Attributes&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Evidence path cannot redirect through a junction/symlink.");
            Directory.CreateDirectory(requested);using(var stream=new FileStream(Path.Combine(requested,"result.txt"),FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(stream))writer.Write("RUNNING "+DateTime.UtcNow.ToString("O"));folder=requested;evidenceOwned=true;
            oldWidth=Screen.width;oldHeight=Screen.height;oldMode=Screen.fullScreenMode;began=Time.realtimeSinceStartup;priorScope=SportsPrefs.StorageScope;playerRegistryPresent=PlayerPrefs.HasKey(SportsParticipantRegistry.Key);playerRegistry=PlayerPrefs.GetString(SportsParticipantRegistry.Key,"");preferences=SportsPrefs.IsolateValidation("CarnivalAppearance"+System.Diagnostics.Process.GetCurrentProcess().Id);SportsParticipantRegistry.ForgetCache();Application.logMessageReceived+=Error;SportsTimeOfDay.Select(SportsTimeChoice.Day);SportsCameraOptions.Set(false);
            catalog=Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");if(catalog==null||!catalog.Validate(out _)||catalog.bodies.Length<7)throw new InvalidOperationException("Actual built seven-body catalog required.");
            Note("ACTUAL_WINDOWS_APPEARANCE explicit nine venues; original AI/setup/profile APIs only; no pose/force/score/result injection. GPU/performance/audio/controller/human acceptance not claimed. Backend="+SystemInfo.graphicsDeviceType+" device="+SystemInfo.graphicsDeviceName);
        }
        IEnumerator Run()
        {
            foreach(var venue in Venues){yield return Visit(venue);if(Time.realtimeSinceStartup-began>550)break;}
            Check(visited==Venues.Length,"all nine named ownership styles visited "+visited+"/"+Venues.Length);Check(catalog.bodies.All(body=>requestedBodies.Contains(body.id)),"all seven real catalog bodies were requested through persistent profiles");Check(catalog.bodies.All(body=>effectiveBodies.Contains(body.id)),"all seven real catalog bodies appeared on actual prepared actors across the nine ownership styles");SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<SportsFrontEnd>()!=null,"final original collection menu available");yield return Capture("collection-return",true);
        }
        ISportsParticipantSlot[] Participants=>corn!=null?corn.setup.players:air!=null?air.players:sport.setup.players;
        int Count=>corn!=null?corn.setup.count:air!=null?2:sport.setup.count;
        string MatchId=>corn!=null?corn.MatchId:air!=null?(string)Read(air,"participantMatchId"):sport.MatchId;
        void Stop(){Time.timeScale=1;if(corn!=null)corn.ChangeSetup();else if(air!=null)air.ChangeSetup();else if(sport!=null)sport.SetupScreen();}
        bool Begin()=>corn!=null?corn.StartMatch():air!=null?air.StartMatch(false):sport.StartMatch();
        void Refresh(){if(corn!=null)corn.RefreshParticipants(false);else if(air!=null)air.RefreshParticipants(false);else sport.RefreshParticipants(false);}
        void Configure(Venue venue)
        {
            Stop();if(corn!=null){corn.setup.count=4;corn.AllAI();}else if(air!=null)air.Configure(AirHockeyMode.AIVsAI);else{sport.setup.count=venue.count;sport.option=0;if(sport.TeamSport){sport.teamA=2;sport.teamB=2;}sport.NormalizeSetup();sport.AllBots();}
            SportsCameraOptions.Set(corn!=null||sport is SkeeMatch);foreach(var weather in FindObjectsByType<SportsChaosWeather>(FindObjectsSortMode.None))if(weather.gameObject.scene==SceneManager.GetActiveScene()&&weather.presets!=null){var clear=weather.presets.entries.FirstOrDefault(entry=>entry.displayName=="Clear");if(clear!=null)weather.presets.Select(clear.id);}
            foreach(var driver in FindObjectsByType<SportsDrivingViews>(FindObjectsSortMode.None))if(driver.gameObject.scene==SceneManager.GetActiveScene())driver.Select(0);
        }
        void Profiles(int venue)
        {
            string[] choices={"truck.monster","novelty.icecream","truck.fire","semi.lowrider","novelty.hotdog","truck.lifted","semi.standard"};saved.Clear();
            for(int slot=0;slot<Count;slot++)
            {
                var person=SportsParticipantRegistry.Create("Same Named Driver");Check(SportsParticipantRegistry.UseInSeat(slot,person.id),sceneName+" actual registry assigns distinct profile to seat "+slot);var body=catalog.FindBody(choices[(venue+slot)%choices.Length]);var draft=SportsParticipantRegistry.GetAppearance(person.id);draft.bodyId=body.id;draft.selections.Clear();
                foreach(string category in new[]{SportsTruckAppearanceSlots.Hat,SportsTruckAppearanceSlots.Eyes,SportsTruckAppearanceSlots.Mirrors,SportsTruckAppearanceSlots.Wheels,SportsTruckAppearanceSlots.RearOrnament,SportsTruckAppearanceSlots.Dashboard})
                {var candidates=catalog.cosmetics.Where(item=>item.slotId==category&&item.CompatibleWith(body.id)).ToArray();if(candidates.Length>0){var selected=candidates[(venue+slot)%candidates.Length];draft.selections.Add(new SportsTruckCosmeticSelection{slotId=category,itemId=selected.id});}}
                Check(SportsParticipantRegistry.TrySetAppearance(person.id,draft),sceneName+" actual atomic appearance preference commit "+person.id);requestedBodies.Add(body.id);var persisted=SportsParticipantRegistry.GetAppearance(person.id);Check(persisted!=null&&persisted.bodyId==body.id&&persisted.selections.Count>0,sceneName+" actual saved nondefault/varied body and accessories "+person.id+" "+body.id);saved.Add(person.id,persisted.Copy());
            }
            string bytes=SportsPrefs.GetString(SportsParticipantRegistry.Key);SportsParticipantRegistry.ForgetCache();foreach(var pair in saved)Check(Equivalent(SportsParticipantRegistry.GetAppearance(pair.Key),pair.Value),sceneName+" exact semantic profile survives registry save/cache reload "+pair.Key);Check(bytes==SportsPrefs.GetString(SportsParticipantRegistry.Key),sceneName+" profile reload leaves stored registry bytes unchanged");Refresh();Check(Participants.Take(Count).Select(p=>p.ParticipantId).Distinct().Count()==Count,sceneName+" duplicate display names retain distinct actual IDs");Check(Participants.Take(Count).All(p=>p.IsBot),sceneName+" all actual assigned sources remain original AI after profile reload");
        }
        Actor EventActor(string label,SportsEventTruck truck,Func<int> owner,Func<bool> required=null)=>new Actor{label=label,root=truck.transform,body=truck.body,host=truck.appearanceHost,owner=owner,required=required??(()=>true)};
        Actor[] Actors()
        {
            if(corn!=null)return corn.courses.SelectMany(course=>course.trucks.Select((truck,index)=>new Actor{label="course"+course.index+" retained"+index,root=truck.transform,body=truck.body,host=truck.appearanceHost,owner=()=>course.index,required=()=>true})).ToArray();
            if(air!=null)return air.trucks.Select((truck,index)=>new Actor{label="staticAir"+index,root=truck.transform,body=truck.body,host=truck.GetComponent<SportsTruckAppearanceHost>(),owner=()=>truck.slot,required=()=>true}).ToArray();
            if(sport is SumoMatch sumo)return sumo.fighters.Select((fighter,index)=>EventActor("sumo"+index,fighter.truck,()=>index)).ToArray();
            if(sport is CurlingMatch curling)return curling.stones.Select((stone,index)=>EventActor("retainedStone"+index,stone.truck,()=>stone.InPlay||stone.truck.appearanceHost!=null&&stone.truck.appearanceHost.Applied?stone.truck.slot:-1,()=>stone.InPlay)).ToArray();
            if(sport is SkeeMatch skee)return skee.courses.Select((course,index)=>EventActor("skeeLane"+index,course.truck,()=>course.owner,()=>index<skee.ActiveCourses)).ToArray();
            if(sport is WifeMatch wife)return wife.pairs.SelectMany((pair,index)=>new[]{EventActor("pair"+index+" driver",pair.driver,()=>wife.DriverRole(index)),EventActor("pair"+index+" passenger.55",pair.passenger,()=>wife.PassengerRole(index))}).ToArray();
            if(sport is TargetMatch target)return target.racers.Select((racer,index)=>EventActor("target"+index,racer.truck,()=>index)).ToArray();
            if(sport is TerrierMatch terrier)return terrier.racers.Select((racer,index)=>EventActor("terrierLocal"+index,racer.truck,()=>index)).ToArray();
            if(sport is DockMatch dock)return new[]{EventActor("actualDockTurn",dock.truck,()=>dock.Active)}.Concat(dock.staged.Select((truck,index)=>EventActor("stagedDock"+index,truck,()=>index))).ToArray();
            throw new InvalidOperationException("Named appearance venue has no explicit actor/ownership map.");
        }
        string State()
        {
            object match=corn!=null?(object)corn:air!=null?air:sport;var parts=new List<string>();foreach(string name in new[]{"Phase","Stage","Clock","HeatTime","Heat","Locks","Active","Round","Delivery","Launches","CompletedAttempts"}){var value=Read(match,name);if(value!=null)parts.Add(name+"="+value);}return string.Join(" ",parts);
        }
        string Rules()
        {
            object match=corn!=null?(object)corn:air!=null?air:sport;var result=new StringBuilder(State());
            foreach(var owner in new[]{match,Read(match,"rules"),Read(match,"series")})
            {
                if(owner==null)continue;foreach(var field in owner.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public).OrderBy(f=>f.Name))
                {var value=field.GetValue(owner);if(value is int||value is bool||value is float||value is Enum)result.Append('|').Append(field.Name).Append('=').Append(value);else if(value is int[] integers)result.Append('|').Append(field.Name).Append('=').Append(string.Join(",",integers));else if(value is bool[] flags)result.Append('|').Append(field.Name).Append('=').Append(string.Join(",",flags));}
            }
            return result.ToString();
        }
        static bool OriginalBodyOwns(Actor actor,Collider shape)=>shape.GetComponentInParent<Rigidbody>(true)==actor.body;
        static string ColliderIds(Actor actor)=>string.Join(",",actor.root.GetComponentsInChildren<Collider>(true).Where(c=>OriginalBodyOwns(actor,c)).Select(c=>c.GetInstanceID()).OrderBy(id=>id));
        static HashSet<Collider> GeneratedWheels(Actor actor)=>new HashSet<Collider>(actor.wheels.Where(wheel=>wheel!=null).Select(wheel=>(Collider)wheel.wheel.meshCollider).Where(shape=>shape!=null));
        void BindPhysicsAuthority()
        {
            foreach(var actor in actors){actor.wheels=actor.root.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);var generated=GeneratedWheels(actor);actor.core=actor.root.GetComponentsInChildren<Collider>(true).Where(shape=>OriginalBodyOwns(actor,shape)&&!generated.Contains(shape)).ToArray();}
        }
        void CheckPhysicsAuthority(string phase)
        {
            foreach(var actor in actors)
            {
                Check(actor.root!=null&&actor.body!=null,sceneName+" original actor/body remains alive "+phase+" "+actor.label);if(actor.root==null||actor.body==null)continue;var current=actor.root.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);Check(current.Length==actor.wheels.Length&&current.All(actor.wheels.Contains),sceneName+" original NWH controller references retained "+phase+" "+actor.label);var generated=GeneratedWheels(actor);var core=actor.root.GetComponentsInChildren<Collider>(true).Where(shape=>OriginalBodyOwns(actor,shape)&&!generated.Contains(shape)).ToArray();bool same=core.Length==actor.core.Length&&core.All(actor.core.Contains);Check(same,sceneName+" original active/inactive chassis/equipment collider identities retained; only exact existing NWH wheel.meshCollider initialization excluded "+phase+" "+actor.label);if(!same)Note("COLLIDER_MISMATCH "+phase+" "+actor.label+" baseline="+string.Join(",",actor.core.Select(c=>c!=null?c.GetInstanceID():0))+" current="+string.Join(",",core.Select(c=>c.GetInstanceID()))+" details="+string.Join(";",actor.root.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()+":"+c.GetType().Name+":"+c.transform.parent.name+"/"+c.name+":active="+c.gameObject.activeInHierarchy+":nativeOwner="+(c.attachedRigidbody!=null?c.attachedRigidbody.GetInstanceID():0)+":hierarchyOwner="+(c.GetComponentInParent<Rigidbody>(true)!=null?c.GetComponentInParent<Rigidbody>(true).GetInstanceID():0)+":exactNwh="+generated.Contains(c))));
            }
        }
        string Identity()=>string.Join("/",actors.Select(actor=>actor.label+":"+actor.root.GetInstanceID()+":"+actor.body.GetInstanceID()+":"+string.Join(",",actor.core.Select(shape=>shape!=null?shape.GetInstanceID():0).OrderBy(id=>id))+":"+string.Join(",",actor.wheels.Select(wheel=>wheel!=null?wheel.GetInstanceID():0).OrderBy(id=>id))+":"+actor.root.localScale.ToString("R")));
        string PhysicalState()=>string.Join("/",actors.Select(actor=>actor.body.GetInstanceID()+":"+actor.body.position.ToString("R")+":"+actor.body.rotation.ToString("R")+":"+actor.body.linearVelocity.ToString("R")+":"+actor.body.angularVelocity.ToString("R")));
        string HostState()=>string.Join("/",actors.Select(actor=>actor.host==null?"MISSING":actor.host.GetInstanceID()+":"+actor.host.AppearanceRevision+":"+actor.host.ProfileId+":"+(actor.host.AppearanceRoot!=null?actor.host.AppearanceRoot.GetInstanceID():0)));
        void Audit(string phase,bool validate)
        {
            foreach(var actor in actors)
            {
                int owner=actor.owner();string id=owner>=0&&owner<Count?Participants[owner].ParticipantId:"UNPREPARED";var host=actor.host;var draft=owner>=0&&owner<Count?SportsParticipantRegistry.GetAppearance(id):null;
                actorsLog.AppendLine(sceneName+"\t"+phase+"\t"+actor.label+"\t"+actor.body.GetInstanceID()+"\t"+ColliderIds(actor)+"\t"+owner+"\t"+id+"\t"+(host!=null?host.ProfileId:"MISSING")+"\t"+draft?.bodyId+"\t"+(host!=null?host.EffectiveBodyId:"")+"\t"+actor.root.lossyScale+"\t"+actor.body.position+"\t"+actor.body.linearVelocity+"\t"+State());
                if(!validate)continue;Check(host!=null,sceneName+" explicit adapter bound to original actor "+actor.label);if(host==null||!actor.required())continue;
                Check(owner>=0&&owner<Count&&host.Applied&&host.ProfileId==id,sceneName+" actual prepared actor agrees with original owner slot/ID "+actor.label+" slot="+owner);
                if(!host.Applied)continue;Check(draft!=null&&host.EffectiveBodyId==draft.bodyId,sceneName+" known requested body is actually used, not silently standardized "+actor.label);Check(host.originalBodyRenderers.All(renderer=>renderer!=null&&!renderer.enabled),sceneName+" original replaceable body renderers stay hidden under selected art "+actor.label);var report=host.EvaluateDraft(draft);Check(report.canApply&&host.EffectiveBodyId==report.effectiveBodyId,sceneName+" actual composed body agrees with existing fit report "+actor.label+" body="+host.EffectiveBodyId);
                effectiveBodies.Add(host.EffectiveBodyId);var fit=Array.Find(host.compatibleBodies,item=>item!=null&&item.bodyId==host.EffectiveBodyId);Check(fit!=null&&fit.paintSlots.Length>0,sceneName+" actual selected body has explicit participant paint/livery slots "+actor.label);if(fit!=null&&owner>=0&&owner<Count){var expected=Participants[owner].ParticipantColor;foreach(var slot in fit.paintSlots){var renderer=host.ComposedBodyRenderers[slot.rendererIndex];var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,slot.materialIndex);var actual=block.GetColor("_BaseColor");Check(Mathf.Abs(actual.r-expected.r)<2e-6f&&Mathf.Abs(actual.g-expected.g)<2e-6f&&Mathf.Abs(actual.b-expected.b)<2e-6f&&Mathf.Abs(actual.a-expected.a)<2e-6f,sceneName+" actual participant paint/livery matches current owner's palette "+actor.label+" renderer="+slot.rendererIndex+" material="+slot.materialIndex+" expected="+expected.ToString("F5")+" actual="+actual.ToString("F5"));}}var model=host.AppearanceRoot;Check(model!=null&&model.GetComponentsInChildren<Collider>(true).Length==0&&model.GetComponentsInChildren<Rigidbody>(true).Length==0,sceneName+" selected appearance remains renderer-only "+actor.label);
                Check(model!=null&&model.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).SelectMany(r=>r.sharedMaterials).All(m=>m!=null&&m.shader!=null&&m.shader.isSupported&&m.shader.name!="Hidden/InternalErrorShader"),sceneName+" actual selected materials have supported shaders "+actor.label);
            }
            if(sport is WifeMatch wife)foreach(var pair in wife.pairs)Check(Mathf.Abs(pair.passenger.transform.lossyScale.x/pair.driver.transform.lossyScale.x-.55f)<.0001f,"Wife original passenger physical scale remains.55; body/equipment composition uses existing fit");
            File.WriteAllText(Path.Combine(folder,"actors.tsv"),actorsLog.ToString());Note("RULE_STATE "+sceneName+" "+phase+" "+Rules());
        }
        Component Adapter(string type)
        {var found=Type.GetType(type+", Assembly-CSharp",false);return found!=null?FindObjectsByType(found,FindObjectsInactive.Include,FindObjectsSortMode.None).Cast<Component>().FirstOrDefault(component=>component.gameObject.scene==SceneManager.GetActiveScene()):null;}
        IEnumerator EnsureVenue(string name)
        {
            switch(name){case "TruckSumo":yield return SportsProbeScene.Ensure<SumoMatch>(name);break;case "TruckAirHockey":yield return SportsProbeScene.Ensure<AirHockeyMatch>(name);break;case "TruckCornhole":yield return SportsProbeScene.Ensure<CornholeMatch>(name);break;case "TruckCurling":yield return SportsProbeScene.Ensure<CurlingMatch>(name);break;case "TruckSkeeBall":yield return SportsProbeScene.Ensure<SkeeMatch>(name);break;case "TruckWifeCarrying":yield return SportsProbeScene.Ensure<WifeMatch>(name);break;case "TruckTargetLaunch":yield return SportsProbeScene.Ensure<TargetMatch>(name);break;case "TruckTerrierRacing":yield return SportsProbeScene.Ensure<TerrierMatch>(name);break;case "TruckDockDiving":yield return SportsProbeScene.Ensure<DockMatch>(name);break;default:throw new InvalidOperationException("Unreviewed probe venue.");}
        }
        void CheckCabState()
        {
            if(sport is SkeeMatch skee)foreach(var course in skee.courses.Take(skee.ActiveCourses)){Check(course.appearanceCab!=null&&course.appearanceCab.accessoryMount!=null&&course.appearanceCab.accessoryMount.gameObject.layer==25+course.lane,"Skee actual per-lane cab accessory binding/layer retained "+course.lane);Check(course.truck.appearanceHost.AppearanceRoot.GetComponentsInChildren<Renderer>(true).All(renderer=>renderer.gameObject.layer==10+course.lane),"Skee selected actual exterior uses its original lane layer "+course.lane);}
            if(sport is CurlingMatch curling)Check(curling.appearanceCab!=null&&curling.appearanceCab.accessoryMount!=null&&curling.Current.truck.appearanceHost!=null,"Curling actual active retained delivery has original cab accessory adapter");
            if(sport is DockMatch){var split=Adapter("LWS.TruckDockDiving.DockSelectedSplitPresentation");Check(split!=null&&Read(split,"Applied") is bool applied&&applied&&Read(split,"UpperRoot") is Transform upper&&upper!=null,"Dock active turn has actual selected upper/lower presentation, not a present-but-failed fallback adapter");}
            foreach(var actor in actors.Where(actor=>actor.required()&&actor.host!=null&&actor.host.Applied)){var draft=SportsParticipantRegistry.GetAppearance(actor.host.ProfileId);if(draft==null)continue;var report=actor.host.EvaluateDraft(draft);bool dashboard=draft.selections.Any(item=>item.slotId==SportsTruckAppearanceSlots.Dashboard)&&!report.unavailableSlots.Contains(SportsTruckAppearanceSlots.Dashboard);if(!dashboard)continue;var cabs=FindObjectsByType<SportsTruckAppearanceCabBinding>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(binding=>binding.gameObject.scene==SceneManager.GetActiveScene()&&Read(binding,"owner")==actor.host).ToArray();foreach(var cab in cabs)if(cab.gameObject.activeInHierarchy){var graphic=Read(cab,"graphic") as GameObject;Check(graphic!=null&&graphic.GetComponentsInChildren<Collider>(true).Length==0&&graphic.GetComponentsInChildren<Rigidbody>(true).Length==0,"Actual active cab selected dashboard is render-only "+actor.label);}}
        }
        bool IsFinished=>corn!=null?corn.Finished:air!=null?air.rules.Phase==AirHockeyPhase.Finished:sport.Finished;
        void PauseMode(){if(corn!=null)corn.Pause();else if(air!=null)air.Pause();else sport.Pause();}
        void ResumeMode(){if(corn!=null)corn.Resume();else if(air!=null)air.Resume();else sport.Resume();}
        IEnumerator PauseResume()
        {
            if(IsFinished){Check(false,sceneName+" natural workload ended before required real pause/resume sample");yield break;}PauseMode();yield return null;yield return null;Check(Time.timeScale==0,sceneName+" original pause API freezes simulation");string rules=Rules(),physical=PhysicalState(),hosts=HostState();yield return new WaitForSecondsRealtime(.2f);yield return Capture("actual-pause",false);Check(Rules()==rules&&PhysicalState()==physical&&HostState()==hosts,sceneName+" actual pause/capture preserves rule clock, body poses/velocities, profile owners and appearance revisions exactly");ResumeMode();yield return null;yield return null;Check(Time.timeScale>0,sceneName+" original resume API returns simulation ownership");CheckPhysicsAuthority("after-resume");Audit("after-resume",true);
        }
        IEnumerator Visit(Venue venue)
        {
            sceneName=venue.name;yield return EnsureVenue(sceneName);yield return new WaitForSecondsRealtime(.15f);sport=FindAnyObjectByType<SportsEventSession>();corn=FindAnyObjectByType<CornholeMatch>();air=FindAnyObjectByType<AirHockeyMatch>();Check(SceneManager.GetActiveScene().name==sceneName&&(sport!=null||corn!=null||air!=null),sceneName+" actual original saved scene loaded");if(sport==null&&corn==null&&air==null)yield break;visited++;Configure(venue);Profiles(Array.IndexOf(Venues,venue));actors=Actors();BindPhysicsAuthority();string identity=Identity();Audit("setup-before-report",false);
            if(sport is DockMatch)Check(Adapter("LWS.TruckDockDiving.DockSelectedSplitPresentation")!=null,"Dock staged8+actual turn selected split adapter MUST be authored; absence fails, never skipped");
            if(sport is TerrierMatch)Check(Adapter("LWS.TruckTerrierRacing.TerrierAppearanceBridge")!=null,"Terrier local12 selected body/debris/replay adapter MUST be authored");
            if(sport is TargetMatch)Check(Adapter("LWS.TruckTargetLaunch.TargetAppearanceWreckBridge")!=null,"Target original48-piece selected rendering bridge MUST be authored");
            yield return SetupReport();Check(Begin(),sceneName+" original all-AI setup starts through source validation");yield return null;yield return null;Audit("original-start",true);CheckPhysicsAuthority("original-start");Check(Identity()==identity,sceneName+" original body/collider roots and physical root scales retained across setup/start");
            float deadline=Time.realtimeSinceStartup+venue.limit;while(!NaturalReady()&&Time.realtimeSinceStartup<deadline)yield return null;Check(NaturalReady(),sceneName+" bounded natural ownership/workload reached "+State());Audit("natural-live",true);CheckPhysicsAuthority("natural-live");CheckCabState();if(sport is TargetMatch naturalTarget){CheckTargetNatural(naturalTarget);ObserveTargetLava(naturalTarget);}yield return Capture("actual-AI-natural",false);
            if(corn!=null)Check(corn.courses.Any(course=>course.Attempts>=2),"Corn original AI created a subsequent delivery while retaining original sixteen actor roots; no score/pileup fabricated");
            if(sport is CurlingMatch curling)Check(curling.Delivery>=1&&curling.Current.InPlay,"Curling actual next delivery prepared after original resolution; prior stones retain original owners");
            if(sport is TargetMatch observedTarget)ObserveTargetLava(observedTarget);
            if(sport is TerrierMatch){var bridge=Adapter("LWS.TruckTerrierRacing.TerrierAppearanceBridge");Check(bridge!=null&&Convert.ToInt32(Read(bridge,"ActualEmissions"))>0&&string.IsNullOrEmpty(Read(bridge,"LastFallback") as string),"Terrier observed original failed-AI selected wreck emission without fallback; brief startup does not claim full match");}
            if(sport is SkeeMatch skee)CheckSkee(skee);yield return PauseResume();
            if(sport is TargetMatch finalTarget){ObserveTargetLava(finalTarget);if(!targetLavaObservedThisVisit){targetLavaNotObserved++;Note("NOT_OBSERVED_TARGET_LAVA sample="+targetNaturalSamples+" resolvedAttempt="+TargetResolved(finalTarget)+"; no natural lava observed. Windows selected-body fracture remains UNVERIFIED; controlled native lava evidence is separate.");}}
            string previous=MatchId;Stop();Check(Begin()&&MatchId!=previous,sceneName+" original rematch creates fresh match identity");yield return null;yield return null;Check(Identity()==identity,sceneName+" rematch reuses original physical actor/collider identities");Audit("actual-rematch",true);CheckPhysicsAuthority("actual-rematch");CheckCabState();yield return Capture("actual-AI-rematch",false);Stop();Audit("return-setup",false);CheckSavedProfiles();
            var oldActors=actors.Select(actor=>actor.root).ToArray();SportsNavigation.FrontEnd();yield return null;yield return null;yield return new WaitForSecondsRealtime(.1f);Check(FindAnyObjectByType<SportsFrontEnd>()!=null&&oldActors.All(root=>root==null),sceneName+" original menu exit unloads actors/hosts");CheckSavedProfiles();sport=null;corn=null;air=null;actors=Array.Empty<Actor>();
        }
        static bool Equivalent(SportsTruckAppearanceData a,SportsTruckAppearanceData b)=>a!=null&&b!=null&&JsonUtility.ToJson(a)==JsonUtility.ToJson(b);
        void CheckSavedProfiles(){SportsParticipantRegistry.ForgetCache();foreach(var entry in saved)Check(Equivalent(SportsParticipantRegistry.GetAppearance(entry.Key),entry.Value),sceneName+" requested profile retained exactly through match/rematch/menu reload "+entry.Key);}
        bool NaturalReady()
        {
            if(corn!=null)return corn.courses.Any(course=>course.Attempts>=2);
            if(air!=null)return air.MatchSeconds>=3;
            if(sport is SumoMatch sumo)return sumo.Stage==SumoStage.Fight&&sumo.Clock>=2;
            if(sport is CurlingMatch curling)return curling.Delivery>=1&&curling.Current.InPlay&&(curling.Phase==CurlingPhase.Approach||curling.Phase==CurlingPhase.Slide);
            if(sport is SkeeMatch skee)return skee.Clock>=5&&skee.courses.Any(course=>course.Phase==SkeePhase.Flight||course.Phase==SkeePhase.Replay);
            if(sport is WifeMatch wife)return wife.Phase==WifePhase.Race&&wife.Clock>=3;
            if(sport is TargetMatch target)return TargetResolved(target)||TargetEmissions()>0;
            if(sport is TerrierMatch terrier){var bridge=Adapter("LWS.TruckTerrierRacing.TerrierAppearanceBridge");return bridge!=null&&Convert.ToInt32(Read(bridge,"ActualEmissions"))>0;}
            if(sport is DockMatch dock)return dock.Active>=1&&dock.Phase==DockPhase.Approach;
            return false;
        }
        static bool TargetResolved(TargetMatch target)=>Enumerable.Range(0,target.setup.count).Any(slot=>target.rules.attempts[slot]>0&&target.rules.resolved[slot]);
        int TargetEmissions(){var bridge=Adapter("LWS.TruckTargetLaunch.TargetAppearanceWreckBridge");return bridge!=null?Convert.ToInt32(Read(bridge,"ActualEmissions")):0;}
        void CheckTargetNatural(TargetMatch target)
        {
            targetNaturalSamples++;targetLavaObservedThisVisit=false;int emissions=TargetEmissions();bool resolved=TargetResolved(target),banked=Enumerable.Range(0,target.setup.count).Any(slot=>target.rules.attempts[slot]>0&&target.rules.resolved[slot]&&target.rules.last[slot]>0&&target.rules.totals[slot]>=target.rules.last[slot]);
            Check(resolved||emissions>0,"Target original rules confirm a genuine resolved attempt or actual lava emission, without requiring a safe AI landing to become a wreck");
            foreach(int slot in Enumerable.Range(0,target.setup.count).Where(slot=>target.rules.attempts[slot]>0&&target.rules.resolved[slot]))Check(new[]{0,10,25,50,100}.Contains(target.rules.last[slot])&&target.rules.totals[slot]>=target.rules.last[slot],"Target actual rules retain resolved attempt/banked score slot="+slot+" attempts="+target.rules.attempts[slot]+" last="+target.rules.last[slot]+" total="+target.rules.totals[slot]);
            Note("TARGET_NATURAL_OBSERVATION sample="+targetNaturalSamples+" round="+target.rules.Round+" attempts="+string.Join(",",target.rules.attempts)+" resolved="+string.Join(",",target.rules.resolved)+" last="+string.Join(",",target.rules.last)+" totals="+string.Join(",",target.rules.totals)+" positiveBank="+banked+" actualEmissions="+emissions);
        }
        void ObserveTargetLava(TargetMatch target)
        {
            var bridge=Adapter("LWS.TruckTargetLaunch.TargetAppearanceWreckBridge");int emissions=TargetEmissions(),impacts=target.volcano!=null?target.volcano.Impacts:0;if(emissions<=0&&impacts<=0)return;
            // Genuine lava without selected graphics must fail; an unobserved hazard never becomes an adapter pass.
            int slot=target.volcano!=null?target.volcano.LastImpactSlot:-1;string emitted=Read(bridge,"LastEmittedBodyId") as string;var host=slot>=0&&slot<target.setup.count?target.racers[slot].truck.appearanceHost:null;string id=slot>=0&&slot<Count?Participants[slot].ParticipantId:null;
            Check(bridge!=null&&emissions>0&&string.IsNullOrEmpty(Read(bridge,"LastFallback") as string)&&host!=null&&host.Applied&&host.ProfileId==id&&saved.TryGetValue(id,out var requested)&&host.EffectiveBodyId==requested.bodyId&&emitted==requested.bodyId,"Target observed actual AI-triggered fracture matches exact impacted owner/profile/requested body on original pieces, without fallback or injected explosion slot="+slot+" emitted="+emitted);CheckPhysicsAuthority("actual-target-lava");
            if(!targetLavaObservedThisVisit){targetLavaObservedThisVisit=true;targetLavaObserved++;Note("OBSERVED_TARGET_LAVA actualImpacts="+impacts+" selectedEmissions="+emissions+" body="+Read(bridge,"LastEmittedBodyId"));}
        }
        IEnumerator SetupReport()
        {
            var presenters=FindObjectsByType<SportsTruckCompatibilityPresenter>(FindObjectsSortMode.None).Where(p=>p.gameObject.scene==SceneManager.GetActiveScene()).ToArray();if(presenters.Length==0){Check(false,"SETUP_REPORT_UNAVAILABLE "+sceneName+" actual named sport fit modal is missing; capture diagnostic and fail");yield return Capture("actual-setup-no-fit-modal",true);yield break;}
            Check(presenters.Length==1,sceneName+" single integrated setup report authority");var presenter=presenters[0];var source=presenter.source;string rules=Rules(),physics=PhysicalState(),hosts=HostState(),registry=SportsPrefs.GetString(SportsParticipantRegistry.Key);Check(source!=null&&source.IsSetup&&source.Count==Count,sceneName+" real compatibility source uses actual setup roster");if(source==null||!source.IsSetup)yield break;
            for(int slot=0;slot<Count;slot++){var view=source.Read(slot);Check(view.profileId==Participants[slot].ParticipantId,sceneName+" setup report identity "+slot);Check(view.hasHost&&view.canApply,sceneName+" setup fit reports actual installed selected-body support "+slot);Note("SETUP_FIT "+sceneName+" slot="+slot+" id="+view.profileId+" requested="+view.requested+" effective="+view.effective+" unavailable="+view.unavailable+" notice="+view.notice);}
            Check(presenter.reportButton!=null,sceneName+" original TRUCK FIT entry is integrated");if(presenter.reportButton==null)yield break;presenter.reportButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);Check(presenter.IsOpen&&presenter.Modal!=null&&presenter.Modal.IsOpen,sceneName+" actual Heat fit modal opened via original entry");if(presenter.Modal==null)yield break;
            int pages=Mathf.CeilToInt(Count/4f);for(int page=0;page<pages;page++)
            {
                var modal=presenter.Modal;for(int row=0;row<modal.rows.Length;row++){int slot=page*4+row;if(slot>=Count)break;string id=Participants[slot].ParticipantId;Check(modal.rows[row].root.activeInHierarchy&&modal.rows[row].name.text.Contains(id.Substring(0,Mathf.Min(8,id.Length))),sceneName+" actual visible modal row matches profile "+slot);foreach(var text in new[]{modal.rows[row].name,modal.rows[row].fit,modal.rows[row].status}){text.ForceMeshUpdate();Check(!text.isTextOverflowing,sceneName+" actual fit row text fits its saved TMP rectangle slot="+slot+" field="+text.name);}}
                if(page==0||page==pages-1)yield return Capture("actual-fit-modal-page"+(page+1),true);if(page<pages-1){modal.next.onClick.Invoke();yield return null;}
            }
            for(int page=1;page<pages;page++){presenter.Modal.previous.onClick.Invoke();yield return null;}presenter.Modal.rows[0].details.onClick.Invoke();yield return null;var detail=presenter.Modal.detailText;detail.ForceMeshUpdate();string full=detail.text.Replace("\n","").Replace("\r","");Check(presenter.Modal.details.activeInHierarchy&&full.Contains(Participants[0].ParticipantId)&&!detail.isTextOverflowing,sceneName+" actual first profile detail page shows its full stable ID without TMP overflow");yield return Capture("actual-fit-first-detail",true);presenter.Modal.back.onClick.Invoke();yield return null;
            presenter.Modal.close.onClick.Invoke();float deadline=Time.realtimeSinceStartup+3;while(presenter.IsOpen&&Time.realtimeSinceStartup<deadline)yield return null;Check(!presenter.IsOpen,sceneName+" original modal close restores setup lease/focus");Check(Rules()==rules&&PhysicalState()==physics&&HostState()==hosts&&SportsPrefs.GetString(SportsParticipantRegistry.Key)==registry,sceneName+" report opening/paging/capture/close leaves rules, physical states, selected host revisions and profile bytes exact");
        }
        Camera[] Views()
        {
            if(corn!=null)return corn.courses.Select(course=>course.chase.view).Where(camera=>camera!=null&&camera.enabled).ToArray();
            if(sport!=null&&sport.cameras!=null)return sport.cameras.Where(camera=>camera!=null&&camera.enabled&&camera.gameObject.activeInHierarchy).Distinct().ToArray();
            return FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(camera=>camera.enabled&&camera.gameObject.activeInHierarchy&&camera.gameObject.scene==SceneManager.GetActiveScene()&&camera.cameraType==CameraType.Game).ToArray();
        }
        void Rendered(ScriptableRenderContext context,Camera camera){if(capturing!=null&&capturing.Contains(camera))rendered.Add(camera.GetInstanceID());}
        void LegacyRendered(Camera camera){if(capturing!=null&&capturing.Contains(camera))rendered.Add(camera.GetInstanceID());}
        Renderer[] Subjects()
        {
            var result=new List<Renderer>();foreach(var actor in actors)if(actor.root!=null)result.AddRange(actor.root.GetComponentsInChildren<Renderer>(true));
            void Ghosts(object replay){if(replay==null)return;if(Read(replay,"ghosts") is Transform[] copies)foreach(var copy in copies)if(copy!=null)result.AddRange(copy.GetComponentsInChildren<Renderer>(true));if(Read(replay,"ghost") is Transform single)result.AddRange(single.GetComponentsInChildren<Renderer>(true));}
            if(sport!=null)Ghosts(Read(sport,"replay"));if(corn!=null)foreach(var course in corn.courses)Ghosts(course.replay);if(sport is SkeeMatch skee)foreach(var course in skee.courses)Ghosts(course.replay);
            object pool=sport!=null?Read(sport,"wrecks"):null;if(pool==null&&sport!=null)pool=Read(Read(sport,"volcano"),"wrecks");if(Read(pool,"pieces") is Array pieces)foreach(var piece in pieces){if(Read(piece,"body") is Rigidbody body)result.AddRange(body.GetComponentsInChildren<Renderer>(true));if(Read(piece,"ghost") is Transform ghost)result.AddRange(ghost.GetComponentsInChildren<Renderer>(true));}
            return result.Where(renderer=>renderer!=null&&renderer.enabled&&!renderer.forceRenderingOff&&renderer.gameObject.activeInHierarchy&&renderer.GetComponent<TMP_Text>()==null&&(renderer is MeshRenderer||renderer is SkinnedMeshRenderer)&&renderer.bounds.size.sqrMagnitude>.000001f).Distinct().ToArray();
        }
        bool Objective(Camera camera,Plane[] planes)
        {
            var roots=new List<Transform>();if(corn!=null)roots.AddRange(corn.courses.Select(course=>course.board));if(air!=null)roots.Add(air.puck.transform);if(sport is SumoMatch sumo)roots.Add(sumo.ring);if(sport is SkeeMatch skee)roots.AddRange(skee.courses.Select(course=>course.target));if(sport is TargetMatch target)roots.Add(target.target);if(sport is TerrierMatch terrier)roots.Add(terrier.course.finish);if(sport is DockMatch dock)roots.Add(dock.waterVolume.transform);
            if(sport is CurlingMatch)return FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Any(renderer=>renderer.gameObject.scene==SceneManager.GetActiveScene()&&renderer.name.IndexOf("house",StringComparison.OrdinalIgnoreCase)>=0&&renderer.enabled&&(camera.cullingMask&(1<<renderer.gameObject.layer))!=0&&GeometryUtility.TestPlanesAABB(planes,renderer.bounds));
            return roots.Where(root=>root!=null).SelectMany(root=>root.GetComponentsInChildren<Renderer>(true)).Any(renderer=>renderer.enabled&&renderer.gameObject.activeInHierarchy&&(camera.cullingMask&(1<<renderer.gameObject.layer))!=0&&GeometryUtility.TestPlanesAABB(planes,renderer.bounds));
        }
        int Owner(int view,Camera camera)
        {
            if(corn!=null)return Array.FindIndex(corn.courses,course=>course.chase.view==camera);if(sport!=null&&sport.TryReadViewportCard(view,out int participant,out _,out _))return participant;if(sport is CurlingMatch curling)return curling.Current.truck.slot;if(sport is DockMatch dock)return dock.Active;if(air!=null)return -1;return -1;
        }
        void CheckSkee(SkeeMatch match)
        {
            var pixels=match.GetComponent<SkeeArcadePixels>();Check(pixels!=null&&pixels.Active&&pixels.FeedCount==4,"Skee original simultaneous4 mode creates four actual compositor feeds");if(pixels==null||!pixels.Active)return;
            Check(pixels.OutputCamera!=null&&pixels.OutputCamera.cullingMask==0&&pixels.OutputCamera.GetComponent<AudioListener>()==null&&pixels.OutputCamera.cameraType==CameraType.Game,"Skee output remains zero-world-culling Game camera with no listener");
            float area=0;var textures=new List<RenderTexture>();for(int view=0;view<pixels.FeedCount;view++){var texture=pixels.TextureFor(view);var rect=pixels.ViewportFor(view);area+=rect.width*rect.height;textures.Add(texture);Check(texture!=null&&texture.IsCreated()&&texture.filterMode==FilterMode.Point,"Skee actual low-resolution point feed "+view);for(int other=0;other<view;other++)Check(!rect.Overlaps(pixels.ViewportFor(other)),"Skee actual feed rectangles remain distinct "+view+"/"+other);}Check(Mathf.Abs(area-1)<.001f&&textures.Distinct().Count()==4,"Skee actual four unique feeds fill the backbuffer through original viewport metadata");
        }
        IEnumerator Capture(string label,bool setup)
        {
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.45f);float until=Time.realtimeSinceStartup+4;while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<until)yield return null;Check(Screen.width==size.x&&Screen.height==size.y,sceneName+" actual Windows backbuffer "+size);
                var views=Views();var pixels=sport is SkeeMatch skee?skee.GetComponent<SkeeArcadePixels>():null;capturing=views;if(pixels!=null&&pixels.Active&&pixels.OutputCamera!=null)capturing=capturing.Concat(new[]{pixels.OutputCamera}).Distinct().ToArray();rendered.Clear();RenderPipelineManager.endCameraRendering+=Rendered;Camera.onPostRender+=LegacyRendered;
                string path=Path.Combine(folder,sceneName+"-"+label+"-"+size.x+"x"+size.y+".png");if(File.Exists(path))throw new InvalidOperationException("Prior capture evidence is retained: "+path);
                try
                {
                    yield return new WaitForEndOfFrame();int request=Time.frameCount;string state=State();ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.25f);for(int retry=0;retry<30&&!PngReady(path);retry++)yield return new WaitForSecondsRealtime(.1f);Check(PngReady(path),sceneName+" actual game-window ScreenCapture completed "+label);if(PngReady(path))Inspect(path,label,size,views,pixels,setup,request,state);Check(capturing.Length>0&&capturing.All(camera=>rendered.Contains(camera.GetInstanceID())),sceneName+" every original captured camera rendered during the backbuffer interval");
                }
                finally{RenderPipelineManager.endCameraRendering-=Rendered;Camera.onPostRender-=LegacyRendered;capturing=null;}
            }
        }
        static bool PngReady(string path)
        {
            if(!File.Exists(path))return false;try{var bytes=File.ReadAllBytes(path);return bytes.Length>64&&bytes[0]==137&&bytes[1]==80&&bytes[2]==78&&bytes[3]==71&&bytes[bytes.Length-8]==73&&bytes[bytes.Length-7]==69&&bytes[bytes.Length-6]==78&&bytes[bytes.Length-5]==68;}catch(IOException){return false;}
        }
        void Inspect(string path,string label,Vector2Int size,Camera[] views,SkeeArcadePixels pixels,bool setup,int request,string requestedState)
        {
            var image=new Texture2D(2,2,TextureFormat.RGB24,false);
            try
            {
                bool loaded=image.LoadImage(File.ReadAllBytes(path));Check(loaded&&image.width==size.x&&image.height==size.y,sceneName+" PNG is exact original resolution, no resized/substitute frame");if(!loaded)return;screenshots++;var colors=image.GetPixels32();var subjects=setup?Array.Empty<Renderer>():Subjects();Check(views.Length>0,sceneName+" actual camera ownership exists during capture");
                for(int view=0;view<views.Length;view++)
                {
                    var camera=views[view];if(camera==null)continue;Rect rect=pixels!=null&&pixels.Active&&view<pixels.FeedCount?pixels.ViewportFor(view):camera.rect;var planes=GeometryUtility.CalculateFrustumPlanes(camera);int visible=subjects.Count(renderer=>(camera.cullingMask&(1<<renderer.gameObject.layer))!=0&&GeometryUtility.TestPlanesAABB(planes,renderer.bounds));bool objective=!setup&&Objective(camera,planes);int owner=Owner(view,camera);
                    if(!setup){Check(visible>0||objective,sceneName+" actual selected actor/recorded debris or original gameplay objective intersects view "+view);if(owner>=0)Check(owner<Count&&Participants[owner].IsBot,sceneName+" original view maps to actual AI owner "+owner+", never a claimed human");}
                    float low=1,high=0;int nonblack=0,total=0;// Dense coverage avoids sparse-grid aliasing on flat Air tables and sparse arcade pixels; thresholds remain unchanged.
                    for(int y=0;y<54;y++)for(int x=0;x<96;x++){int px=Mathf.Clamp(Mathf.RoundToInt((rect.x+rect.width*(.05f+.9f*(x+.5f)/96))*image.width),0,image.width-1),py=Mathf.Clamp(Mathf.RoundToInt((rect.y+rect.height*(.05f+.9f*(y+.5f)/54))*image.height),0,image.height-1);var color=colors[py*image.width+px];float luma=(color.r+color.g+color.b)/765f;low=Mathf.Min(low,luma);high=Mathf.Max(high,luma);if(color.r+color.g+color.b>18)nonblack++;total++;}float fraction=nonblack/(float)total;Check(fraction>.15f&&high-low>.012f,sceneName+" actual "+(setup?"setup/modal":"world")+" pixels nonblank/variable view="+view+" range="+(high-low).ToString("F3")+" nonblack="+fraction.ToString("F3"));
                    frames.AppendLine(sceneName+"\t"+label+"\t"+size.x+"x"+size.y+"\t"+request+"\t"+Time.frameCount+"\t"+requestedState+"\t"+State()+"\t"+view+"\t"+owner+"\t"+visible+"\t"+objective+"\t"+fraction.ToString("F4")+"\t"+(high-low).ToString("F4"));
                }
                File.WriteAllText(Path.Combine(folder,"frames.tsv"),frames.ToString());
            }
            finally{Destroy(image);}
        }
        void Finish()
        {
            if(finished)return;finished=true;RenderPipelineManager.endCameraRendering-=Rendered;Camera.onPostRender-=LegacyRendered;Application.logMessageReceived-=Error;try{Stop();}catch(Exception cleanup){errors++;log.AppendLine("CLEANUP_EXCEPTION "+cleanup);}Check(PlayerPrefs.HasKey(SportsParticipantRegistry.Key)==playerRegistryPresent&&PlayerPrefs.GetString(SportsParticipantRegistry.Key,"")==playerRegistry,"ordinary player participant registry untouched by isolated probe");preferences?.Dispose();preferences=null;SportsParticipantRegistry.ForgetCache();Check(SportsPrefs.StorageScope==priorScope,"original preference routing scope restored");Time.timeScale=1;
            log.AppendLine("TARGET_NATURAL_SAMPLES="+targetNaturalSamples+" TARGET_LAVA_OBSERVED="+targetLavaObserved+" NOT_OBSERVED_TARGET_LAVA="+targetLavaNotObserved+" WINDOWS_TARGET_FRACTURE="+(targetLavaObserved>0?"OBSERVED_TARGETED_SAMPLE_ONLY":"UNVERIFIED_NOT_OBSERVED"));
            log.AppendLine("CHECKS="+checks+" ERRORS="+errors+" SCENES="+visited+" SCREENSHOTS="+screenshots+" PID="+System.Diagnostics.Process.GetCurrentProcess().Id+" SECONDS="+(Time.realtimeSinceStartup-began).ToString("F2"));log.AppendLine("Actual Windows ScreenCapture, real persistent profiles/setup reports, original AI/start/rematch/menu lifecycle. No injected poses/forces/scores/results; no full-match, physical human/controller/audio/listening/performance/GPU acceptance. RunPlaytestWindowsValidation/BuildIdentity supply exact build-content/process identity externally.");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());File.WriteAllText(Path.Combine(folder,"actors.tsv"),actorsLog.ToString());File.WriteAllText(Path.Combine(folder,"frames.tsv"),frames.ToString());Debug.Log("TS_CARNIVAL_APPEARANCE_FINISHED code="+(errors==0?0:2));if(oldWidth>0&&oldHeight>0)Screen.SetResolution(oldWidth,oldHeight,oldMode);Application.Quit(errors==0?0:2);
        }
        void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering-=Rendered;Camera.onPostRender-=LegacyRendered;Application.logMessageReceived-=Error;Time.timeScale=1;preferences?.Dispose();preferences=null;SportsParticipantRegistry.ForgetCache();if(!finished&&evidenceOwned&&folder!=null)File.WriteAllText(Path.Combine(folder,"result.txt"),log+"\nINTERRUPTED; no acceptance footer.");if(!finished&&oldWidth>0&&oldHeight>0)Screen.SetResolution(oldWidth,oldHeight,oldMode);
        }
    }
}
#endif
