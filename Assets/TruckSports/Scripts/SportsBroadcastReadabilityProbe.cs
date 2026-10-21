#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckBowling;

namespace LWS.TruckSports
{
    // Explicit development acceptance only. Placement/deactivation fixtures below
    // test camera choices, never award points and are never saved into scenes.
    public sealed class SportsBroadcastReadabilityProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsBroadcastReadability")<0)return;
            var go=new GameObject("Explicit broadcast visual acceptance");DontDestroyOnLoad(go);
            if(!SportsProbeInputIsolation.Active)go.AddComponent<SportsProbeInputIsolation>();
            go.AddComponent<SportsBroadcastReadabilityProbe>();
        }
        [Serializable]sealed class Measurement
        {
            public string scene,stage,reason;
            public int primary,included,humans,width,height;
            public bool perspective,objectiveVisible;
            public float primaryViewportHeight,orthoSize,effectiveDistance;
            public Vector3 cameraPosition;
        }
        readonly List<Measurement> measurements=new List<Measurement>();
        readonly StringBuilder log=new StringBuilder();
        readonly Gamepad[] pads=new Gamepad[4];
        string folder;int checks,errors;bool finished;IDisposable preferences;float deadline;
        static readonly string[] Core={SportsNavigation.Kickball,SportsNavigation.Magnet,SportsNavigation.Keepaway,SportsNavigation.HotPotato,SportsNavigation.Volley,SportsNavigation.Hill};
        static readonly string[] Others={SportsNavigation.LogRolling,SportsNavigation.Dodgeball,SportsNavigation.Bungee,SportsNavigation.Terrier,SportsNavigation.Balloon,SportsNavigation.RedLight,SportsNavigation.Whack,SportsNavigation.Seesaw,SportsNavigation.Lava,SportsNavigation.Tether,SportsNavigation.Joust,SportsNavigation.Freeze,SportsNavigation.Hauler,SportsNavigation.Herd,SportsNavigation.Frog,SportsNavigation.Avalanche,SportsNavigation.Ninja,SportsNavigation.FareFight,SportsNavigation.Snake,SportsNavigation.Parking,SportsNavigation.Sumo,SportsNavigation.Derby,SportsNavigation.Buzkashi,SportsNavigation.PedestrianPlowing,SportsNavigation.PedestrianPlowingCab,SportsNavigation.KeepawayCab};
        void Check(bool value,string message){checks++;log.AppendLine((value?"PASS ":"FAIL ")+message);if(!value)errors++;File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Exception(string message,string stack,LogType type){if(type!=LogType.Exception&&type!=LogType.Error)return;errors++;log.AppendLine("ERROR "+message+"\n"+stack);File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Configure(SportsEventSession match,int humans)
        {
            for(int i=0;i<match.setup.players.Length;i++){var p=match.setup.players[i];p.source=i<humans?BowlingSource.WholeController:BowlingSource.AI;p.pad=i<humans?pads[i]:null;}
            SportsCameraOptions.Set(false);
        }
        IEnumerator Settle(float seconds=1.5f){yield return new WaitForSecondsRealtime(seconds);yield return new WaitForEndOfFrame();}
        void Measure(string scene,string stage,SportsBroadcastFraming framing,int humans)
        {
            var camera=framing.cameraRig.view;var bounds=framing.TruckBounds(framing.Primary);var rect=framing.ProjectedTruckRect(camera,framing.Primary);
            var sample=new Measurement{scene=scene,stage=stage,reason=framing.ShotReason,primary=framing.Primary,included=framing.IncludedTrucks,humans=humans,width=Screen.width,height=Screen.height,perspective=!camera.orthographic,objectiveVisible=framing.ObjectiveVisible,primaryViewportHeight=rect.height,orthoSize=camera.orthographicSize,effectiveDistance=Vector3.Distance(camera.transform.position,bounds.center),cameraPosition=camera.transform.position};measurements.Add(sample);
            File.WriteAllText(Path.Combine(folder,"measurements.json"),JsonUtility.ToJson(new MeasurementSet{frames=measurements.ToArray()},true));
            log.AppendLine("MEASURE "+scene+" "+stage+" height="+rect.height.ToString("F4")+" primary="+framing.Primary+" included="+framing.IncludedTrucks+" objective="+framing.ObjectiveVisible);
        }
        [Serializable]sealed class MeasurementSet{public Measurement[] frames;}
        IEnumerator Capture(string scene,string stage,SportsBroadcastFraming framing)
        {
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();
                Check(Screen.width==size.x&&Screen.height==size.y,scene+" actual render resolution "+size);
                Measure(scene,stage,framing,Enumerable.Range(0,framing.ParticipantCount).Count(framing.IsHuman));
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,scene+"-"+stage+"-"+size.x+".png"));yield return new WaitForSecondsRealtime(.2f);
            }
        }
        IEnumerator Start()
        {
            deadline=Time.realtimeSinceStartup+900;preferences=SportsPrefs.IsolateValidation("BroadcastReadability");
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsBroadcastOutput");
            folder=output>=0?Path.GetFullPath(args[output+1]):SportsEvidencePaths.Folder("PlaytestRepair/Broadcast");Directory.CreateDirectory(folder);
            log.AppendLine("platform="+Application.platform+"; explicit camera fixtures; software devices only; full-match acceptance is separate");
            Application.logMessageReceived+=Exception;
            for(int i=0;i<4;i++)pads[i]=InputSystem.AddDevice<Gamepad>("Broadcast camera test pad "+i);
            bool quick=Array.IndexOf(args,"quick")>=0;
            var scenes=quick?Core.Take(2).ToArray():Core.Concat(Others).ToArray();
            int subset=Array.IndexOf(args,"-tsBroadcastScenes");if(subset>=0)scenes=args[subset+1].Split(',');
            foreach(string scene in scenes)
            {
                Time.timeScale=1;SportsNavigation.LoadSport(scene);yield return new WaitForSecondsRealtime(.6f);
                var match=FindAnyObjectByType<SportsEventSession>();var framing=FindAnyObjectByType<SportsBroadcastFraming>();
                Check(match!=null&&framing!=null,scene+" saved scene has explicit existing authority and framing bindings");if(match==null||framing==null)continue;
                match.setup.count=match.MaximumPlayers;Configure(match,Core.Contains(scene)||scene==SportsNavigation.PedestrianPlowingCab?1:0);
                var driving=FindAnyObjectByType<SportsDrivingViews>();if(driving!=null&&!driving.localElevated)driving.Select(0);
                if(match is LWS.TruckKickball.KickballMatch kick){while(kick.ViewMode!=0)kick.ExtraAction("view");kick.option=7;}
                // Both Plowing scenes expose the same real shared-arena option.
                // Cab-only ownership is checked separately; a disabled arena view
                // must not be expected to run its broadcast director.
                if(match is LWS.TruckPedestrianPlowing.PlowMatch plow)plow.option=0;
                bool started=match.StartMatch();Check(started,scene+" real setup starts with human plus AI");if(!started)continue;
                yield return new WaitForSecondsRealtime(4);Time.timeScale=0;yield return Settle();
                if(match is LWS.TruckPedestrianPlowing.PlowCabMatch)
                {
                    Check(match.option==2&&!framing.OwnsShot,scene+" dedicated cab variant preserves cockpit authority");
                    Check(match.cameras.Length==1&&match.cameras[0].enabled&&match.cameras[0].rect==new Rect(0,0,1,1),scene+" human receives a full-screen working cab");
                    yield return ViewCapture(scene,"cab-preserved");
                    Time.timeScale=1;match.SetupScreen();Configure(match,0);Check(match.StartMatch(),scene+" AI-only cab rematch starts");yield return Settle(.7f);
                    Check(!framing.OwnsShot&&match.cameras.Any(c=>c!=null&&c.enabled),scene+" AI-only cab spectator remains rendered");
                    continue;
                }
                Check(framing.OwnsShot&&framing.Primary>=0&&framing.Valid(framing.Primary),scene+" shared camera has valid live target");
                if(!framing.OwnsShot||framing.Primary<0){Time.timeScale=1;continue;}
                if(Core.Contains(scene))
                {
                    framing.enabled=false;yield return Settle(.7f);yield return Capture(scene,"before",framing);
                    framing.enabled=true;yield return Settle(2);yield return Capture(scene,"after",framing);
                }
                else yield return Capture(scene,"after",framing);
                // Check between Update and LateUpdate as well as the rendered
                // frame: a legacy pose writer must not temporarily steal the rig.
                var between=framing.ProjectedTruckRect(framing.cameraRig.view,framing.Primary);
                Check(between.height>=framing.Configuration.minimumTruckHeight*.9f,scene+" legacy Update cannot overwrite active director readability");
                yield return new WaitForEndOfFrame();
                float height=framing.ProjectedTruckRect(framing.cameraRig.view,framing.Primary).height;
                Check(height>=framing.Configuration.minimumTruckHeight*.9f&&height<.5f,scene+" actual primary renderer height meets configured minimum within10% tolerance "+height);
                var center=framing.cameraRig.view.WorldToViewportPoint(framing.TruckBounds(framing.Primary).center);
                Check(center.z>0&&center.x>.02f&&center.x<.98f&&center.y>.04f&&center.y<.96f,scene+" primary truck is actually inside viewport");
                Check(float.IsFinite(framing.cameraRig.transform.position.y)&&framing.cameraRig.view.fieldOfView<=75,scene+" finite bounded lens and position");
                if(scene==SportsNavigation.Hill){var zone=SportsCameraFit.Projected(framing.cameraRig.view,framing.action.objective);Check(zone.xMin>=.03f&&zone.xMax<=.97f&&zone.yMin>=.16f&&zone.yMax<=.94f,scene+" complete active ring stays clear of the bottom HUD "+zone);}
                if(new[]{SportsNavigation.Kickball,SportsNavigation.Magnet,SportsNavigation.Keepaway,SportsNavigation.Volley}.Contains(scene))Check(framing.ObjectiveVisible,scene+" normal-play objective remains inside the action shot");
                int remote=-1;for(int i=0;i<framing.SubjectCount;i++)if(i!=framing.Primary&&framing.Valid(i)){remote=i;break;}
                if(remote>=0)
                {
                    var body=framing.Body(remote);var previous=body.position;float distance=Vector3.Distance(framing.cameraRig.transform.position,framing.PrimaryBounds.center);
                    body.position=previous+new Vector3(1500,0,1500);body.transform.position=body.position;Physics.SyncTransforms();yield return Settle();
                    float next=Vector3.Distance(framing.cameraRig.transform.position,framing.PrimaryBounds.center);
                    Check(next<distance*1.35f+5,scene+" far irrelevant AI cannot force unlimited zoom");
                    body.position=previous;body.transform.position=previous;Physics.SyncTransforms();
                }
                int primary=framing.Primary;var primaryObject=framing.Body(primary).gameObject;
                if(Enumerable.Range(0,framing.SubjectCount).Count(framing.Valid)>1){primaryObject.SetActive(false);yield return Settle(.25f);Check(framing.Primary!=primary&&framing.Valid(framing.Primary),scene+" invalid target releases immediately");primaryObject.SetActive(true);}
                // Rematch exercises the existing reset rather than recreating the camera.
                Time.timeScale=1;match.SetupScreen();Configure(match,0);Check(match.StartMatch(),scene+" AI-only rematch starts");yield return Settle(.7f);Check(match.cameras.Any(c=>c!=null&&c.enabled),scene+" AI-only OFF has a rendered spectator view");
                if(driving!=null&&!driving.localElevated)
                {
                    foreach(int humans in new[]{1,2,3,4})
                    {
                        if(humans>match.MaximumPlayers)continue;match.SetupScreen();Configure(match,humans);driving.Select(1);Check(match.StartMatch(),scene+" split rematch "+humans);yield return Settle(.3f);
                        int expected=Math.Min(humans,match.setup.count);Check(match.cameras.Length==expected&&driving.rally.owners.Take(expected).Distinct().Count()==expected,scene+" human-only viewport ownership "+humans);
                        Check(!framing.OwnsShot,scene+" shared framing does not override chase/cab cameras");
                        SportsCameraOptions.Set(true);driving.Layout();yield return Settle(.15f);
                        Check(match.cameras.Length==Math.Min(4,match.setup.count)&&driving.rally.owners.Take(match.cameras.Length).Distinct().Count()==match.cameras.Length,scene+" optional AI views use unique bounded viewports "+humans);
                        SportsCameraOptions.Set(false);driving.Layout();
                    }
                }
                if(scene==SportsNavigation.Magnet&&driving!=null)yield return SharedHumanGuidance(match,framing,driving);
                Time.timeScale=1;
            }
            if(!quick){yield return Hockey();yield return RetiredAICameras();}
            Finish();
        }
        IEnumerator SharedHumanGuidance(SportsEventSession match,SportsBroadcastFraming framing,SportsDrivingViews driving)
        {
            Time.timeScale=1;match.SetupScreen();Configure(match,4);driving.Select(0);
            Check(match.StartMatch(),"four-human shared camera guidance fixture starts");yield return Settle(.3f);Time.timeScale=0;
            var positions=new Vector3[4];for(int slot=0;slot<4;slot++){positions[slot]=framing.Body(slot).position;framing.Body(slot).position=new Vector3((slot&1)==0?-60:60,positions[slot].y,(slot&2)==0?-60:60);}
            Physics.SyncTransforms();yield return Settle(1.8f);
            var guidance=framing.GetComponent<SportsBroadcastGuidance>();
            Check(guidance!=null&&guidance.targets.Length>=5,"shared cue capacity covers objective plus every local human");
            for(int slot=0;slot<4;slot++)if(slot!=framing.Primary)
            {
                var position=framing.Body(slot).position;
                Check(guidance.targets.Any(p=>p.gameObject.activeInHierarchy&&Vector3.Distance(p.transform.position,position)<.1f),"offscreen human has their own actual CNP cue slot="+slot);
            }
            Check(guidance.ActiveCues>=3&&guidance.compass.gameObject.activeInHierarchy,"all three distant human competitors remain represented without zooming out");
            yield return ViewCapture(SportsNavigation.Magnet,"four-human-offscreen-cues");
            for(int slot=0;slot<4;slot++)framing.Body(slot).position=positions[slot];Physics.SyncTransforms();Time.timeScale=1;match.SetupScreen();
        }
        IEnumerator Hockey()
        {
            const string scene=SportsNavigation.Hockey;Time.timeScale=1;SportsNavigation.LoadSport(scene);yield return Settle(.7f);
            var match=FindAnyObjectByType<LWS.TruckHockey.HockeyMatch>();var framing=FindAnyObjectByType<SportsBroadcastFraming>();
            Check(match!=null&&framing!=null&&framing.source!=null,"Hockey saved legacy camera has read-only framing source");if(match==null||framing==null)yield break;
            var camera=framing.cameraRig.view;Vector3 originalPosition=camera.transform.position;Quaternion originalRotation=camera.transform.rotation;float originalOrtho=camera.orthographicSize;
            match.mode=LWS.TruckHockey.HockeyMode.AIVsAI;match.Configure();match.mode=LWS.TruckHockey.HockeyMode.Custom;match.players[0].source=LWS.TruckHockey.HockeySource.WholeController;match.players[0].pad=pads[0];match.Configure();
            Check(match.StartMatch(),"Hockey real human/AI setup starts");yield return Settle(4);Time.timeScale=0;yield return Settle();
            framing.enabled=false;camera.transform.SetPositionAndRotation(originalPosition,originalRotation);camera.orthographicSize=originalOrtho;yield return Settle(.3f);yield return Capture(scene,"before",framing);
            framing.enabled=true;yield return Settle(2);yield return Capture(scene,"after",framing);
            Check(framing.ProjectedTruckRect(camera,framing.Primary).height>=framing.Configuration.minimumTruckHeight*.9f,"Hockey real renderer projection meets readability minimum");Check(framing.ObjectiveVisible,"Hockey physical puck remains visible");
            Time.timeScale=1;match.mode=LWS.TruckHockey.HockeyMode.AIVsAI;match.Configure();Check(match.StartMatch(true),"Hockey actual AI-only rematch starts");yield return Settle(.7f);Check(framing.OwnsShot&&framing.Valid(framing.Primary),"Hockey AI-only director follows valid action");
        }
        IEnumerator ViewCapture(string scene,string stage)
        {
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,scene+"-"+stage+"-"+size.x+".png"));yield return new WaitForSecondsRealtime(.15f);}
        }
        IEnumerator RetiredAICameras()
        {
            SportsNavigation.LoadSport(SportsNavigation.Snake);yield return Settle(.7f);var match=FindAnyObjectByType<LWS.TruckSnake.SnakeMatch>();var framing=FindAnyObjectByType<SportsBroadcastFraming>();
            var driving=match.drivingViews;var spectator=match.GetComponent<SportsExpansionSpectators>();
            Check(spectator!=null&&driving.rally.retiredViews==spectator,"Snake uses the existing spectator authority, not a second camera pose writer");if(spectator==null)yield break;
            foreach(int humans in new[]{0,1})
            {
                match.SetupScreen();match.setup.count=4;Configure(match,humans);SportsCameraOptions.Set(true);driving.Select(2);Check(match.StartMatch(),"Snake real cab match starts for retirement fixture humans="+humans);yield return Settle(3.5f);
                int view=humans==0?0:1;int owner=driving.rally.owners[view];var ownership=(int[])driving.rally.owners.Clone();
                Check(match.setup.players[owner].source==BowlingSource.AI,"Retirement fixture selects a real AI-owned viewport");
                match.Fail(owner,"EXPLICIT CAMERA RETIREMENT FIXTURE");match.trucks[owner].body.position+=new Vector3(2500,300,2500);match.trucks[owner].body.angularVelocity=new Vector3(20,40,30);
                yield return Settle(.7f);int watched=spectator.WatchedSlot(view);
                Check(driving.rally.owners.SequenceEqual(ownership),"Eliminating AI never changes viewport/input ownership");
                Check(watched>=0&&watched!=owner&&match.BroadcastParticipantActive(watched),"Retired AI camera follows a living truck");
                Check(!driving.cabs[view].gameObject.activeSelf,"Retired AI interior is disabled in spectator view");
                Check(Vector3.Distance(driving.rally.views[view].transform.position,match.trucks[owner].body.position)>1000,"Retired AI camera does not chase the flying wreck");
                Check(framing.ProjectedTruckRect(driving.rally.views[view].view,watched).height>=.04f,"Spectated living truck stays readable in the actual quarter viewport");
                yield return ViewCapture(SportsNavigation.Snake,"retired-ai-humans-"+humans);
            }
            SportsCameraOptions.Set(false);
        }
        void Update(){if(!finished&&folder!=null&&Time.realtimeSinceStartup>deadline){Check(false,"Explicit broadcast probe watchdog; unfinished coroutine is a failure");Finish();}}
        void Finish()
        {
            if(finished)return;finished=true;Time.timeScale=1;Application.logMessageReceived-=Exception;
            foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            preferences?.Dispose();log.AppendLine("CHECKS="+checks+" ERRORS="+errors);File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());
            Application.Quit(errors==0?0:1);
        }
        void OnDestroy(){if(!finished){Time.timeScale=1;preferences?.Dispose();}}
    }
}
#endif
