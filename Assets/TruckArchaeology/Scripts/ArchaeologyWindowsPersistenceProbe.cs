#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckSports;

namespace LWS.TruckArchaeology
{
    // Opt-in actual player proof. Editor/vendor no-op persistence cannot pass this probe.
    public sealed class ArchaeologyWindowsPersistenceProbe:MonoBehaviour
    {
        const string Flag="-tsArchaeologyPersistence",Output="-tsArchaeologyPersistenceOutput";
        [RuntimeInitializeOnLoadMethod]static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)<0)return;
            if(Application.isEditor||Application.platform!=RuntimePlatform.WindowsPlayer){Debug.LogError("Archaeology Digger disk acceptance requires an actual Windows player.");return;}
            var g=new GameObject("Explicit actual Archaeology Windows disk acceptance");DontDestroyOnLoad(g);if(!SportsProbeInputIsolation.Active)g.AddComponent<SportsProbeInputIsolation>();g.AddComponent<ArchaeologyWindowsPersistenceProbe>();
        }
        [Serializable]sealed class StoredEnvelope{public int version;public string mode,payload;}
        string folder;readonly StringBuilder report=new StringBuilder();IDisposable preferences;int checks,errors;bool finished;
        ArchaeologyMatch match;
        void Check(bool ok,string detail){checks++;if(!ok)errors++;report.AppendLine((ok?"PASS ":"FAIL ")+detail);Flush();}
        void Flush(){if(folder!=null)File.WriteAllText(Path.Combine(folder,"progress.txt"),report.ToString());}
        void Error(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("RUNTIME_ERROR "+text);Flush();}}
        void Initialize()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,Output);if(i<0||i+1>=args.Length)throw new ArgumentException("Provide "+Output+" inside Logs/TruckSports/PlaytestRepair.");
            string root=Path.GetFullPath("Logs/TruckSports/PlaytestRepair").TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),guard=root+Path.DirectorySeparatorChar,requested=Path.GetFullPath(args[i+1]);
            if(!requested.StartsWith(guard,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Explicit canonical evidence folder required.");
            for(var p=new DirectoryInfo(requested);p!=null&&(p.FullName==root||p.FullName.StartsWith(guard,StringComparison.OrdinalIgnoreCase));p=p.Parent)if(p.Exists&&(p.Attributes&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Evidence cannot redirect through a junction/symlink.");
            Directory.CreateDirectory(requested);using(var stream=new FileStream(Path.Combine(requested,"result.txt"),FileMode.CreateNew))using(var writer=new StreamWriter(stream))writer.WriteLine("RUNNING "+DateTime.UtcNow.ToString("O"));folder=requested;
            preferences=SportsPrefs.IsolateValidation("ArchaeologyDiggerActualWindows"+System.Diagnostics.Process.GetCurrentProcess().Id);SportsParticipantRegistry.ForgetCache();Application.logMessageReceived+=Error;
        }
        IEnumerator Start()
        {
            Exception failure=null;try{Initialize();}catch(Exception error){failure=error;}
            if(failure!=null){Debug.LogError(failure);Application.Quit(2);yield break;}
            var stack=new Stack<IEnumerator>();stack.Push(Run());float deadline=Time.realtimeSinceStartup+250;
            while(stack.Count>0)
            {
                if(Time.realtimeSinceStartup>deadline){Check(false,"250s actual Windows persistence watchdog");break;}
                bool next=false;object current=null;try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null){Check(false,"PROBE_EXCEPTION "+failure);break;}if(!next){stack.Pop();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
            }
            Finish();
        }
        IEnumerator Until(Func<bool> condition,float seconds,string message){float end=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Check(condition(),message);}
        IEnumerator Capture(string name)
        {
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,false);bool correct=false;string path=Path.Combine(folder,name+"-"+size.x+"x"+size.y+".png");
                for(int attempt=0;attempt<10&&!correct;attempt++)
                {
                    yield return new WaitForSecondsRealtime(.2f);if(Screen.width!=size.x||Screen.height!=size.y)continue;
                    string captured=Path.Combine(folder,name+"-"+size.x+"x"+size.y+"-actual-attempt"+attempt+".png");
                    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(captured);yield return new WaitForSecondsRealtime(.3f);
                    if(File.Exists(captured)){byte[] bytes=File.ReadAllBytes(captured);correct=bytes.Length>24&&((bytes[16]<<24)|(bytes[17]<<16)|(bytes[18]<<8)|bytes[19])==size.x&&((bytes[20]<<24)|(bytes[21]<<16)|(bytes[22]<<8)|bytes[23])==size.y;if(correct)File.Move(captured,path);}
                }
                Check(correct,"Actual Windows backbuffer "+name+" "+size.x+"x"+size.y+" (no resize/substitute image)");
            }
        }
        IEnumerator Run()
        {
            yield return SportsProbeScene.Ensure<ArchaeologyMatch>("TruckArchaeology");match=FindAnyObjectByType<ArchaeologyMatch>();match.SetupScreen();match.AllBots();match.setup.count=4;Check(match.StartMatch(),"Original four-AI archaeology workshop crew starts");Time.timeScale=1;
            yield return Until(()=>match.earth.ActualEdits>=4&&match.equipment[0].hoist.Stage==LWS.TruckJenga.JengaCrane.MotorStage.Idle,80,"Original AI scanner/tool creates actual Digger edits before saving");
            Vector3 sample=match.earth.FirstEditPosition;float initial=match.earth.FirstEditOriginalY;
            Check(initial>0,"Actual first issued edit records its original Physics terrain ray before vendor deformation");
            match.Pause();yield return null;yield return null;
            var persistence=match.GetComponent<SportsWorkshopPersistence>();Check(persistence!=null&&persistence.stateProvider==match&&persistence.storage!=null&&persistence.modeId=="TruckArchaeology","Existing actual mode-bound Pixel Crushers workshop coordinator/storer required; no replacement backend");if(persistence==null||persistence.storage==null)throw new InvalidOperationException("Explicit original workshop binding required.");
            bool stored=persistence.SaveWorkshop();Check(stored,"Ordinary paused workshop coordinator writes and verifies actual Pixel Crushers disk record: "+persistence.Status);if(!stored)throw new InvalidOperationException(persistence.Status);
            var diskRecord=persistence.storage.RetrieveSavedGameData(0);var envelope=JsonUtility.FromJson<StoredEnvelope>(diskRecord.GetData("LWS.TruckSports.Workshop.v1."+persistence.modeId));Check(envelope!=null&&envelope.version==1&&envelope.mode==persistence.modeId&&!string.IsNullOrEmpty(envelope.payload),"Actual vendor disk record contains the existing versioned mode envelope");if(envelope==null||string.IsNullOrEmpty(envelope.payload))throw new InvalidOperationException("Actual stored workshop payload unavailable.");
            string saved=envelope.payload;File.WriteAllText(Path.Combine(folder,"actual-workshop-reference.json"),saved);var snapshot=JsonUtility.FromJson<ArchaeologyMatch.WorkshopState>(saved);Check(match.Rules.Delivered==0&&snapshot.scores.All(value=>value==0),"Explicit early-excavation checkpoint: ZERO delivery scores only; nonzero score restoration not claimed");report.AppendLine("OWNED_VENDOR_SNAPSHOT_RETAINED "+snapshot.terrainPrefix+" files="+snapshot.terrainNames.Length);report.AppendLine("OWNED_PIXEL_CRUSHERS_WORKSHOP_PATH "+persistence.storage.customPath);Flush();
            Check(snapshot.terrainNames.Any(n=>n.EndsWith(".vox3",StringComparison.Ordinal))&&snapshot.terrainNames.Contains("terrain.holes"),"Actual vendor voxel+hole files persisted, not Editor no-op");
            Check(match.earth.Surface(sample,out RaycastHit dug)&&dug.point.y<initial-.2f,"Original AI changed actual collision surface before save");float dugY=dug.point.y;Vector3[] positions=match.artifacts.Select(a=>a.body.position).ToArray();string source=match.MatchId;
            string bad=saved.Replace(match.artifacts[0].stableId,"archaeology.UNKNOWN");Check(!match.TryRestoreWorkshopJson(bad,out string reason)&&!string.IsNullOrEmpty(reason),"Unknown actual artifact ID rejected before mutation");Check(positions.SequenceEqual(match.artifacts.Select(a=>a.body.position)),"Rejected snapshot leaves all real artifact poses exact");yield return Capture("actual-vendor-dig-and-disk-save");
            // Exercise the actual ordinary pause-menu coordinator, without a
            // fresh Start that could conceal stale future scheduling/AI caches.
            match.Resume();yield return Until(()=>match.Clock>=snapshot.clock+20&&match.earth.Quiescent&&match.equipment[0].hoist.Stage==LWS.TruckJenga.JengaCrane.MotorStage.Idle,35,"Original live1x AI advances20simseconds beyond the real saved checkpoint");match.Pause();yield return null;yield return null;float advancedClock=match.Clock;int advancedEdits=match.earth.ActualEdits;Check(advancedClock>=snapshot.clock+20&&advancedEdits>4,"Actual later workshop state differs before backward restore; no pose/score injection");report.AppendLine("BACKWARD_RESTORE_BEFORE clock="+advancedClock.ToString("R")+" edits="+advancedEdits+" "+match.AiWellTelemetry(0));Flush();
            bool backwardRestored=persistence.LoadWorkshop();Check(backwardRestored,"Ordinary paused Pixel Crushers coordinator loads its actual earlier disk record: "+persistence.Status);Check(match.Paused&&Time.timeScale==0,"Coordinator restore retains the ordinary paused boundary");CheckRestoredState(snapshot,"same-paused-session earlier checkpoint");yield return null;yield return null;Check(match.earth.Surface(sample,out RaycastHit backwardSurface)&&Mathf.Abs(backwardSurface.point.y-dugY)<.03f,"Same paused-session actual earlier voxel collider restored from disk");int resumedFrom=match.earth.ActualEdits;float restoredClock=match.Clock;report.AppendLine("BACKWARD_RESTORE_AFTER clock="+restoredClock.ToString("R")+" edits="+resumedFrom+" "+match.AiWellTelemetry(0));Flush();
            match.Resume();yield return Until(()=>match.earth.ActualEdits>resumedFrom,6,"Backward-restored actual AI promptly resumes accepted Digger edits within6real seconds, without waiting for old future deadlines");Check(match.Clock<advancedClock,"Prompt post-restore work occurs before the superseded future clock");match.Pause();yield return null;yield return null;
            match.SetupScreen();match.AllBots();Check(match.StartMatch(),"Original fresh-match terrain reset starts");yield return null;yield return null;match.Pause();
            Check(match.earth.Surface(sample,out RaycastHit reset)&&Mathf.Abs(reset.point.y-initial)<.02f,"Supported vendor ClearScene restores original physics height before reload");
            Check(match.TryRestoreWorkshopJson(saved,out reason),"Actual Digger voxel reload/rebuild + known physical workshop restore: "+reason);yield return null;yield return null;
            Check(match.earth.Surface(sample,out RaycastHit restored)&&Mathf.Abs(restored.point.y-dugY)<.03f,"Actual vendor-rebuilt collision surface equals saved excavation");Check(match.RestoredSourceMatchId==source,"Original saved source-match context retained");Check(positions.Zip(match.artifacts,(p,a)=>Vector3.Distance(p,a.body.position)<.00001f).All(v=>v),"Actual saved artifact poses restored");Check(match.ScoreJournal.Count==0,"Restore never issues delivery receipts");CheckRestoredState(snapshot,"reset-then-paused restore");yield return Capture("actual-vendor-voxel-reload");
            // A fresh scene load exercises vendor file discovery, not just retained in-memory chunks.
            match.SetupScreen();SceneManager.LoadScene("TruckArchaeology");yield return null;yield return null;match=FindAnyObjectByType<ArchaeologyMatch>();match.AllBots();match.setup.count=snapshot.players;
            Check(match.TryRestoreWorkshopJson(saved,out reason),"Stopped scene accepts only validated real saved IDs/data: "+reason);Check(match.StartMatch(),"Original Start boundary consumes pending actual vendor snapshot");match.Pause();ObserveRestoredBodies(snapshot,"fresh-pending-immediate");yield return null;ObserveRestoredBodies(snapshot,"fresh-pending-frame1");yield return null;ObserveRestoredBodies(snapshot,"fresh-pending-frame2");
            Check(match.earth.Surface(sample,out RaycastHit reloaded)&&Mathf.Abs(reloaded.point.y-dugY)<.03f,"Fresh scene / real disk reconstruction reproduces actual saved excavation collider");Check(match.RestoredSourceMatchId==source,"Fresh scene restore retains source context");CheckRestoredState(snapshot,"fresh-scene pending Start restore");yield return Capture("fresh-scene-real-disk-reload");
            match.SetupScreen();Check(match.StartMatch(),"Final original rematch starts clean");yield return null;yield return null;match.Pause();Check(match.earth.Surface(sample,out RaycastHit final)&&Mathf.Abs(final.point.y-initial)<.02f,"Repeated vendor reset gives fair original terrain again");match.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<SportsFrontEnd>()!=null,"Actual return to menu cleans live mode");
        }
        void CheckRestoredState(ArchaeologyMatch.WorkshopState saved,string context)
        {
            Check(match.Clock==saved.clock,context+": actual saved clock restored exactly");Check(match.Rules.Scores.SequenceEqual(saved.scores),context+": exact actual saved score ledger restored (this checkpoint is zero-delivery)");Check(match.Tools.Select(tool=>(int)tool).SequenceEqual(saved.tools)&&match.Targets.SequenceEqual(saved.targets),context+": saved tool/target ownership restored");Check(match.Rules.Stages.Select(stage=>(int)stage).SequenceEqual(saved.artifacts.Select(item=>item.stage))&&match.Rules.Owners.SequenceEqual(saved.artifacts.Select(item=>item.owner))&&match.Rules.Preservation.SequenceEqual(saved.artifacts.Select(item=>item.preservation)),context+": actual known artifact stage/owner/preservation restored");
            var bodies=match.trucks.Take(saved.players).SelectMany(truck=>truck.GetComponentsInChildren<Rigidbody>(true)).ToArray();ObserveRestoredBodies(saved,context);
            Check(bodies.Length==saved.rigBodies.Length&&bodies.Select((body,i)=>SameBody(body,saved.rigBodies[i])).All(value=>value),context+": complete original NWH assembly poses/kinematic state and dynamic velocities restored");Check(match.equipment.Take(saved.players).Select((equipment,i)=>SameBody(equipment.head,saved.toolHeads[i])).All(value=>value),context+": saved actual kinematic tool head poses restored");Check(match.artifacts.Select((artifact,i)=>SameBody(artifact.body,saved.artifacts[i].body)).All(value=>value),context+": all known artifact physical poses/support state restored");Check(match.ScoreJournal.Count==0,context+": restoring state creates no fabricated delivery receipts");
        }
        void ObserveRestoredBodies(ArchaeologyMatch.WorkshopState saved,string context)
        {
            var bodies=match.trucks.Take(saved.players).SelectMany(truck=>truck.GetComponentsInChildren<Rigidbody>(true)).ToArray();
            for(int i=0;i<Math.Min(bodies.Length,saved.rigBodies.Length);i++)
            {
                var body=bodies[i];var expected=saved.rigBodies[i];report.AppendLine("RESTORE_BODY_DIAGNOSTIC context="+context+" frame="+Time.frameCount+" id="+expected.id+" rb="+body.position.ToString("F9")+" transform="+body.transform.position.ToString("F9")+" expected="+expected.position.ToString("F9")+" positionError="+Vector3.Distance(body.position,expected.position).ToString("R")+" rotationError="+Quaternion.Angle(body.rotation,expected.rotation).ToString("R")+" velocityError="+Vector3.Distance(body.linearVelocity,expected.velocity).ToString("R")+" angularError="+Vector3.Distance(body.angularVelocity,expected.angular).ToString("R")+" kinematic="+body.isKinematic+" expectedKinematic="+expected.kinematic+" interpolation="+body.interpolation);
            }
            Flush();
        }
        // R85 actual Windows pose round-trip at54m differed by0.019mm.
        // A0.1mm bound tolerates float/PhysX publication precision while
        // still checking both visible and physical poses; angles, velocity
        // and kinematic checks retain their original strict bounds.
        static bool SameBody(Rigidbody body,ArchaeologyMatch.SavedBody saved)
        {
            return Vector3.Distance(body.position,saved.position)<.0001f&&Quaternion.Angle(body.rotation,saved.rotation)<.01f&&Vector3.Distance(body.transform.position,saved.position)<.0001f&&Quaternion.Angle(body.transform.rotation,saved.rotation)<.01f&&body.isKinematic==saved.kinematic&&(saved.kinematic||Vector3.Distance(body.linearVelocity,saved.velocity)<.0001f&&Vector3.Distance(body.angularVelocity,saved.angular)<.0001f);
        }
        void Finish()
        {
            finished=true;Application.logMessageReceived-=Error;Time.timeScale=1;if(match!=null)match.SetupScreen();if(preferences!=null){preferences.Dispose();preferences=null;}SportsParticipantRegistry.ForgetCache();report.AppendLine("CHECKS="+checks+" ERRORS="+errors);report.AppendLine("Actual Windows-only Digger PRO save/reset/reload/collision tests, ordinary Pixel Crushers workshop disk coordinator, backward paused checkpoint restore and real ScreenCapture. ZERO-delivery score snapshot only; no nonzero-score restore acceptance. Original AI creates excavation; no pose/score/outcome injection. BuildIdentity/RunPlaytestWindowsValidation must supply exact candidate/process provenance externally. No human/controller/listening/performance acceptance.");if(folder!=null)File.WriteAllText(Path.Combine(folder,"result.txt"),report.ToString());Application.Quit(errors==0?0:2);
        }
        void OnDestroy(){Application.logMessageReceived-=Error;if(preferences!=null)preferences.Dispose();Time.timeScale=1;if(!finished&&folder!=null){report.AppendLine("INTERRUPTED");File.WriteAllText(Path.Combine(folder,"result.txt"),report.ToString());}}
    }
}
#endif
