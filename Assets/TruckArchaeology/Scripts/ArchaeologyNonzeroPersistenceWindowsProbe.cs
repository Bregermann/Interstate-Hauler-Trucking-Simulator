#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using LWS.TruckSports;

namespace LWS.TruckArchaeology
{
    // Separate opt-in actual player proof; no change to the short V24 probe or
    // any original gameplay, vehicle, terrain, score or storage responsibility.
    public sealed class ArchaeologyNonzeroPersistenceWindowsProbe:MonoBehaviour
    {
        const string Flag="-tsArchaeologyEarnedPersistence",Output="-tsArchaeologyEarnedPersistenceOutput";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)<0)return;
            if(Application.isEditor||Application.platform!=RuntimePlatform.WindowsPlayer){Debug.LogError("Nonzero archaeology disk acceptance requires the actual Windows development player.");return;}
            var root=new GameObject("Explicit actual earned Archaeology checkpoint disk proof");DontDestroyOnLoad(root);if(!SportsProbeInputIsolation.Active)root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<ArchaeologyNonzeroPersistenceWindowsProbe>();
        }
        [Serializable]sealed class Envelope{public int version;public string mode,payload;}
        [Serializable]sealed class ReceiptFact{public string phase,eventId,matchId,participantId;public int slot;public double delta,total;public float clock;}
        [Serializable]sealed class StateFact
        {
            public string phase,sourceMatchId,matchId,screen;public int frame,edits,loads,unloads,delivered,journal;public float real,clock,timeScale;
            public int[] scores,stages,owners,targets,cuts;public string[] tools;public double[] preservation;public bool live,paused,quiescent;
        }
        [Serializable]sealed class FrameFact{public string file,label,sha256;public int width,height,requestedFrame,completedFrame;public StateFact requested,completed;}
        [Serializable]sealed class Summary
        {
            public string scope="ACTUAL_WINDOWS_GENUINELY_EARNED_NONZERO_CHECKPOINT_NOT_FULL_MATCH_OR_HUMAN_ACCEPTANCE",buildGuid,version,backend,storageMode,savedSourceMatchId;
            public int checks,errors,runtimeErrors,pid;public float seconds,naturalSeconds,physicsScale=1,naturalBudgetReal=500,outerBudgetReal=600;
            public bool interrupted,positiveSaved,resetLoadVerified,freshLoadVerified,duplicateCreditGuard,returnedToMenu,playerRegistryUnchanged,preferenceScopeRestored;
            public int[] independentlyExpectedScores,savedDeliveredIndices;public ReceiptFact[] receipts;public FrameFact[] frames;
        }
        readonly StringBuilder report=new StringBuilder();readonly List<ReceiptFact> receipts=new List<ReceiptFact>();readonly List<FrameFact> frames=new List<FrameFact>();readonly HashSet<int> rendered=new HashSet<int>();
        readonly UTF8Encoding utf8=new UTF8Encoding(false);string folder,runId,storageMode,priorScope,priorRegistry,phase="initializing";bool priorRegistryPresent,finished,evidenceOwned,positiveSaved,resetLoadVerified,freshLoadVerified,duplicateCreditGuard,returnedToMenu;
        float began,naturalSeconds;int checks,errors,runtimeErrors,oldWidth,oldHeight;FullScreenMode oldMode;IDisposable preferences;
        ArchaeologyMatch match;ArchaeologyMatch.WorkshopState checkpoint;int[] expectedScores,deliveredIndices;Vector3 terrainSample;float savedTerrainY;
        string[] artifactIds;int[] artifactValues;
        static string Hash(string path){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        void Note(string message){report.AppendLine(message);if(folder!=null)File.WriteAllText(Path.Combine(folder,"progress.txt"),report.ToString(),utf8);}
        void Check(bool valid,string message){checks++;if(!valid)errors++;Note((valid?"PASS ":"FAIL ")+phase+" "+message);}
        void Require(bool valid,string message){Check(valid,message);if(!valid)throw new InvalidOperationException(message);}
        void Error(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;runtimeErrors++;Note("RUNTIME_ERROR "+message+"\n"+stack);}
        void Initialize()
        {
            var args=Environment.GetCommandLineArgs();foreach(string arg in args)if(arg.StartsWith("-ts",StringComparison.Ordinal)&&arg!=Flag&&arg!=Output)throw new ArgumentException("Run earned-checkpoint proof separately from other diagnostic flags: "+arg);
            int i=Array.IndexOf(args,Output);if(i<0||i+1>=args.Length)throw new ArgumentException("Provide "+Output);
            string root=Path.GetFullPath("Logs/TruckSports/PlaytestRepair").TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),guard=root+Path.DirectorySeparatorChar,path=Path.GetFullPath(args[i+1]);
            if(!path.StartsWith(guard,StringComparison.OrdinalIgnoreCase)||path.Length>175)throw new ArgumentException("Fresh short canonical Logs evidence directory required.");
            for(var p=new DirectoryInfo(path);p!=null&&(p.FullName==root||p.FullName.StartsWith(guard,StringComparison.OrdinalIgnoreCase));p=p.Parent)if(p.Exists&&(p.Attributes&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Evidence cannot redirect through a reparse point.");
            Directory.CreateDirectory(path);if(Directory.GetFiles(path).Any(p=>Path.GetFileName(p)!="player.log"))throw new IOException("Retain prior evidence; use a fresh run directory.");
            using(var f=new FileStream(Path.Combine(path,"result.txt"),FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var w=new StreamWriter(f,utf8))w.WriteLine("RUNNING "+DateTime.UtcNow.ToString("O"));folder=path;evidenceOwned=true;
            began=Time.realtimeSinceStartup;runId=Guid.NewGuid().ToString("N");storageMode="ArchaeoEarned"+runId;oldWidth=Screen.width;oldHeight=Screen.height;oldMode=Screen.fullScreenMode;
            priorScope=SportsPrefs.StorageScope;priorRegistryPresent=PlayerPrefs.HasKey(SportsParticipantRegistry.Key);priorRegistry=PlayerPrefs.GetString(SportsParticipantRegistry.Key,"");preferences=SportsPrefs.IsolateValidation("ArchaeologyEarned"+runId);SportsParticipantRegistry.ForgetCache();
            Application.logMessageReceived+=Error;SceneManager.sceneLoaded+=BindFreshStorage;Time.timeScale=1;
            Note("ACTUAL_WINDOWS_NONZERO_CHECKPOINT PHYSICS_TIME_SCALE=1 NATURAL_REAL_BUDGET=500 OUTER_REAL_BUDGET=600; no fixedDelta/pose/force/contact/objective/score injection. Complete match is not required or claimed.");
            Note("BUILD guid="+Application.buildGUID+" version="+Application.version+" backend="+SystemInfo.graphicsDeviceType+" device="+SystemInfo.graphicsDeviceName+" pid="+System.Diagnostics.Process.GetCurrentProcess().Id);
            string fresh=StoragePath();Require(!Directory.Exists(fresh)&&!File.Exists(fresh),"Unique actual configured PixelCrushers workshop namespace is absent before scene load");
        }
        string StoragePath()=>Path.Combine(Application.persistentDataPath,"TruckSports","Workshops",storageMode);
        void BindFreshStorage(Scene loaded,LoadSceneMode mode)
        {
            if(loaded.name!="TruckArchaeology")return;
            var bindings=loaded.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SportsWorkshopPersistence>(true)).ToArray();
            if(bindings.Length!=1||bindings[0].storage==null)throw new InvalidOperationException("Exactly one original actual mode-bound PixelCrushers coordinator/storer required.");
            bindings[0].modeId=storageMode;bindings[0].storage.customPath=StoragePath();Note("ACTUAL_OWNED_STORAGE_BINDING mode="+storageMode+" path="+StoragePath()+" scene="+loaded.name);
        }
        void Attach(ArchaeologyMatch current)
        {if(match!=null)((ISportsScorePresentationSource)match).ScoreConfirmed-=Receipt;match=current;((ISportsScorePresentationSource)match).ScoreConfirmed+=Receipt;}
        void Receipt(SportsScoreEvent value)
        {receipts.Add(new ReceiptFact{phase=phase,eventId=value.EventId,matchId=value.MatchId,participantId=value.ParticipantId,slot=value.Slot,delta=value.Delta,total=value.Total,clock=match.Clock});Note("ACTUAL_RECEIPT "+JsonUtility.ToJson(receipts.Last()));}
        IEnumerator Start()
        {
            Exception failure=null;try{Initialize();}catch(Exception error){failure=error;}
            if(failure!=null){if(evidenceOwned){errors++;Note("INITIALIZATION_EXCEPTION "+failure);Finish(true);}else{Debug.LogException(failure);Application.Quit(2);}yield break;}
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0&&!finished)
            {
                if(Time.realtimeSinceStartup-began>600||runtimeErrors>12){Check(false,"Bounded outer watchdog or actual runtime exception flood");Finish(true);yield break;}
                bool next=false;object item=null;try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null){Check(false,"PROBE_EXCEPTION "+failure);Finish(true);yield break;}
                if(!next){stack.Pop();continue;}if(item is IEnumerator nested){stack.Push(nested);continue;}yield return item;
            }
            if(!finished)Finish(false);
        }
        StateFact State()=>match==null?null:new StateFact{phase=phase,frame=Time.frameCount,real=Time.realtimeSinceStartup-began,clock=match.Clock,timeScale=Time.timeScale,matchId=match.MatchId,sourceMatchId=match.RestoredSourceMatchId,screen=match.menus.Screen.ToString(),live=match.Live,paused=match.Paused,quiescent=match.earth.Quiescent,edits=match.earth.ActualEdits,loads=match.PhysicalLoads,unloads=match.PhysicalUnloads,delivered=match.Rules==null?0:match.Rules.Delivered,journal=match.ScoreJournal==null?0:match.ScoreJournal.Count,scores=match.Rules==null?null:(int[])match.Rules.Scores.Clone(),stages=match.Rules==null?null:match.Rules.Stages.Select(x=>(int)x).ToArray(),owners=match.Rules==null?null:(int[])match.Rules.Owners.Clone(),preservation=match.Rules==null?null:(double[])match.Rules.Preservation.Clone(),targets=(int[])match.Targets.Clone(),tools=match.Tools.Select(x=>x.ToString()).ToArray(),cuts=match.AcceptedCuts.ToArray()};
        void Trace(){var state=State();if(state!=null)File.AppendAllText(Path.Combine(folder,"actual-stages.jsonl"),JsonUtility.ToJson(state)+"\n",utf8);Note("ACTUAL_STATE "+JsonUtility.ToJson(state));}
        bool NaturalReady()=>match.Rules!=null&&match.Rules.Delivered>0&&match.Rules.Scores.Sum()>0&&match.PhysicalLoads>=match.Rules.Delivered&&match.PhysicalUnloads>=match.Rules.Delivered&&match.equipment.All(e=>e.hoist.Stage==LWS.TruckJenga.JengaCrane.MotorStage.Idle)&&match.earth.Quiescent;
        IEnumerator Run()
        {
            phase="natural-earned-checkpoint";yield return SportsProbeScene.Ensure<ArchaeologyMatch>("TruckArchaeology");Attach(FindAnyObjectByType<ArchaeologyMatch>());
            match.SetupScreen();match.AllBots();match.setup.count=4;match.option=1;Require(match.StartMatch(),"Ordinary four-AI Preservation Cup starts");Time.timeScale=1;
            artifactIds=match.artifacts.Select(a=>a.stableId).ToArray();artifactValues=match.artifacts.Select(a=>a.value).ToArray();float start=Time.realtimeSinceStartup,end=start+500,next=start;
            while(!NaturalReady()&&Time.realtimeSinceStartup<end&&!finished)
            {
                if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+5;Trace();Require(match.Live&&Time.timeScale==1,"Natural original AI remains live at exactly1x");}yield return null;
            }
            naturalSeconds=Time.realtimeSinceStartup-start;Trace();Require(NaturalReady(),"Genuine delivered nonzero value AND completed physical crane unloading within500real seconds");
            match.Pause();yield return null;yield return null;Require(match.Paused&&match.WorkshopSaveReady,"Original stopped/quiescent/idle save boundary, with no crane cancellation or gate bypass");
            var persistence=match.GetComponent<SportsWorkshopPersistence>();Require(persistence!=null&&persistence.stateProvider==match&&persistence.modeId==storageMode&&persistence.storage.customPath==StoragePath(),"Same configured original PixelCrushers storer uses only the fresh validation namespace");
            Require(persistence.SaveWorkshop(),"Actual ordinary coordinator saves and verifies earned disk record: "+persistence.Status);
            var disk=persistence.storage.RetrieveSavedGameData(0);var envelope=JsonUtility.FromJson<Envelope>(disk.GetData("LWS.TruckSports.Workshop.v1."+storageMode));Require(envelope!=null&&envelope.version==1&&envelope.mode==storageMode&&!string.IsNullOrEmpty(envelope.payload),"Actual slot0 contains original compatible workshop envelope");
            checkpoint=JsonUtility.FromJson<ArchaeologyMatch.WorkshopState>(envelope.payload);Require(TryReadEarnedValues(checkpoint,artifactIds,artifactValues,out expectedScores,out deliveredIndices,out string reason),"Saved nonzero per-owner values independently equal actual delivered preserved values: "+reason);
            File.WriteAllText(Path.Combine(folder,"actual-earned-workshop-reference.json"),envelope.payload,utf8);Require(checkpoint.players==4&&checkpoint.profiles.SequenceEqual(match.setup.players.Take(4).Select(p=>p.participantId)),"Actual four stable profile IDs retained independently of names");
            Require(deliveredIndices.All(a=>receipts.Any(r=>r.phase==phase&&r.eventId=="arch.delivery."+a&&r.slot==checkpoint.artifacts[a].owner&&r.delta==Math.Round(artifactValues[a]*checkpoint.artifacts[a].preservation,MidpointRounding.AwayFromZero))),"Every saved delivered artifact was witnessed earning its real original owner receipt");
            positiveSaved=true;terrainSample=match.earth.FirstEditPosition;Require(match.earth.Surface(terrainSample,out RaycastHit surface)&&surface.point.y<match.earth.FirstEditOriginalY-.2f,"Actual earned checkpoint includes changed physical excavation surface");savedTerrainY=surface.point.y;
            Require(checkpoint.terrainNames.Any(n=>n.EndsWith(".vox3",StringComparison.Ordinal))&&checkpoint.terrainNames.Contains("terrain.holes"),"Actual vendor persisted voxel and terrain-hole files exist in snapshot manifest");
            Note("ACTUAL_NONZERO_CHECKPOINT scores="+string.Join(",",expectedScores)+" deliveredIndices="+string.Join(",",deliveredIndices)+" sourceMatchId="+checkpoint.sourceMatchId+" clock="+checkpoint.clock.ToString("R"));yield return Capture("earned-positive-checkpoint");
            phase="reset-zero-control";match.SetupScreen();Require(match.StartMatch(),"Ordinary rematch resets prior earned state without score writes");yield return null;yield return null;match.Pause();yield return null;yield return null;
            Require(match.Rules.Scores.All(v=>v==0)&&match.Rules.Delivered==0&&match.ScoreJournal.Count==0,"Original rematch provides genuinely different all-zero score/delivery/journal control");yield return Capture("ordinary-reset-zero");
            phase="actual-disk-positive-reload";persistence=match.GetComponent<SportsWorkshopPersistence>();Require(persistence.LoadWorkshop(),"Ordinary coordinator loads actual positive disk checkpoint: "+persistence.Status);yield return null;yield return null;VerifyRestored("same-scene-reset-load");resetLoadVerified=true;yield return Capture("actual-nonzero-disk-restored");yield return ObserveNoDuplicateCredit();
            phase="fresh-scene-pending-positive-reload";match.SetupScreen();((ISportsScorePresentationSource)match).ScoreConfirmed-=Receipt;match=null;SceneManager.LoadScene("TruckArchaeology");yield return null;yield return null;Attach(FindAnyObjectByType<ArchaeologyMatch>());match.AllBots();match.setup.count=4;match.option=checkpoint.cooperative?0:1;
            persistence=match.GetComponent<SportsWorkshopPersistence>();Require(persistence.modeId==storageMode&&persistence.storage.customPath==StoragePath(),"Fresh scene reuses same actual validation disk namespace, no second SaveSystem");Require(persistence.LoadWorkshop(),"Actual coordinator validates positive disk payload into stopped pending restore: "+persistence.Status);Require(match.StartMatch(),"Original Start consumes real pending positive checkpoint");match.Pause();yield return null;yield return null;VerifyRestored("fresh-scene-pending-start");freshLoadVerified=true;yield return Capture("fresh-scene-nonzero-restored");yield return ObserveNoDuplicateCredit();
            phase="ordinary-menu-unload";match.SetupScreen();Scene old=match.gameObject.scene;SportsNavigation.FrontEnd();yield return null;yield return null;Require(FindAnyObjectByType<SportsFrontEnd>()!=null&&!old.isLoaded,"Ordinary menu return unloads the original restored sport");returnedToMenu=true;
        }
        // Independent observational arithmetic only: does not call rules.Deliver,
        // change scores, validate terrain in place or grant any objective credit.
        public static bool TryReadEarnedValues(ArchaeologyMatch.WorkshopState saved,string[] knownIds,int[] originalValues,out int[] expected,out int[] delivered,out string reason)
        {
            expected=new int[4];delivered=Array.Empty<int>();reason="";
            if(saved==null||saved.players<1||saved.players>4||saved.scores==null||saved.scores.Length!=4||saved.artifacts==null||saved.artifacts.Length!=12||knownIds==null||knownIds.Length!=12||knownIds.Distinct().Count()!=12||originalValues==null||originalValues.Length!=12){reason="Incomplete or ambiguous actual ledger.";return false;}
            var earned=new List<int>();
            try
            {
                for(int a=0;a<12;a++)
                {
                    var item=saved.artifacts[a];if(item==null||string.IsNullOrEmpty(knownIds[a])||item.id!=knownIds[a]||originalValues[a]<1||item.stage<0||item.stage>5||!double.IsFinite(item.preservation)||item.preservation<0||item.preservation>1){reason="Unknown actual artifact identity/value/stage/preservation.";return false;}
                    if(item.stage!=(int)ArtifactStage.Delivered)continue;
                    if(a>=saved.players*3||item.owner<0||item.owner>=saved.players||!saved.cooperative&&a/3!=item.owner||item.body==null||item.body.id!=item.id||!item.body.kinematic){reason="Delivered value lacks its original active owner and physical support state.";return false;}
                    checked{expected[item.owner]+=(int)Math.Round(originalValues[a]*item.preservation,MidpointRounding.AwayFromZero);}earned.Add(a);
                }
            }
            catch(OverflowException){reason="Earned value overflow.";return false;}
            delivered=earned.ToArray();if(delivered.Length==0||expected.Sum(v=>(long)v)<=0){reason="No genuinely delivered positive checkpoint.";return false;}
            if(!expected.SequenceEqual(saved.scores)){reason="Per-owner saved scores differ from independently earned preserved values.";return false;}return true;
        }
        // R85 actual Windows pose round-trip at54m differed by0.019mm.
        // A0.1mm bound tolerates float/PhysX publication precision while
        // still checking both visible and physical poses; angles, velocity
        // and kinematic checks retain their original strict bounds.
        static bool SameBody(Rigidbody body,ArchaeologyMatch.SavedBody saved)=>Vector3.Distance(body.position,saved.position)<.0001f&&Quaternion.Angle(body.rotation,saved.rotation)<.01f&&Vector3.Distance(body.transform.position,saved.position)<.0001f&&Quaternion.Angle(body.transform.rotation,saved.rotation)<.01f&&body.isKinematic==saved.kinematic&&(saved.kinematic||Vector3.Distance(body.linearVelocity,saved.velocity)<.0001f&&Vector3.Distance(body.angularVelocity,saved.angular)<.0001f);
        void VerifyRestored(string label)
        {
            Trace();Require(match.Paused&&Time.timeScale==0&&match.Clock==checkpoint.clock,"Stopped restored checkpoint retains actual saved clock");
            Require(TryReadEarnedValues(checkpoint,match.artifacts.Select(a=>a.stableId).ToArray(),match.artifacts.Select(a=>a.value).ToArray(),out var independentlyExpected,out var actualDelivered,out string reason),"Known live original values independently validate saved checkpoint: "+reason);
            Require(independentlyExpected.SequenceEqual(expectedScores)&&actualDelivered.SequenceEqual(deliveredIndices)&&match.Rules.Scores.SequenceEqual(expectedScores)&&match.Rules.Scores.Any(v=>v>0),"Actual restored positive per-owner score array equals earned checkpoint exactly");
            Require(match.Rules.Stages.Select(v=>(int)v).SequenceEqual(checkpoint.artifacts.Select(a=>a.stage))&&match.Rules.Owners.SequenceEqual(checkpoint.artifacts.Select(a=>a.owner))&&match.Rules.Preservation.SequenceEqual(checkpoint.artifacts.Select(a=>a.preservation)),"All actual artifact stages/owners/preservation restored exactly");
            Require(match.RestoredSourceMatchId==checkpoint.sourceMatchId&&match.setup.players.Take(4).Select(p=>p.participantId).SequenceEqual(checkpoint.profiles),"Source match and stable profile identities retained");
            Require(match.Tools.Select(v=>(int)v).SequenceEqual(checkpoint.tools)&&match.Targets.SequenceEqual(checkpoint.targets),"Saved owner tool/target selections restored");
            var bodies=match.trucks.Take(4).SelectMany(t=>t.GetComponentsInChildren<Rigidbody>(true)).ToArray();
            for(int i=0;i<Math.Min(bodies.Length,checkpoint.rigBodies.Length);i++){var b=bodies[i];var s=checkpoint.rigBodies[i];Note("ACTUAL_RESTORED_BODY "+label+" id="+s.id+" rbError="+Vector3.Distance(b.position,s.position).ToString("R")+" transformError="+Vector3.Distance(b.transform.position,s.position).ToString("R")+" angle="+Quaternion.Angle(b.rotation,s.rotation).ToString("R")+" velocityError="+Vector3.Distance(b.linearVelocity,s.velocity).ToString("R")+" angularError="+Vector3.Distance(b.angularVelocity,s.angular).ToString("R"));}
            Require(bodies.Length==8&&checkpoint.rigBodies.Length==8&&bodies.Select((body,i)=>SameBody(body,checkpoint.rigBodies[i])).All(v=>v),"All four/eight original NWH root/Cab physics AND visible poses/dynamic velocities match saved state");
            Require(match.equipment.Take(4).Select((e,i)=>SameBody(e.head,checkpoint.toolHeads[i])).All(v=>v)&&match.artifacts.Select((a,i)=>SameBody(a.body,checkpoint.artifacts[i].body)).All(v=>v),"Actual tool heads and all twelve artifact physical/visible poses match saved state");
            Require(deliveredIndices.All(a=>match.artifacts[a].body.isKinematic&&match.artifacts[a].CargoJoint==null&&Vector3.Distance(match.artifacts[a].body.position,match.BenchSlot(checkpoint.artifacts[a].owner,a))<=.65f),"Saved earned artifacts really occupy their original exhibit benches with no cargo attachment");
            Require(match.earth.Surface(terrainSample,out RaycastHit hit)&&Mathf.Abs(hit.point.y-savedTerrainY)<.03f,"Actual vendor-rebuilt voxel collision surface equals positive checkpoint");
            Require(match.ScoreJournal.Count==0,"Historical restored points create ZERO fabricated new score receipts");
        }
        IEnumerator ObserveNoDuplicateCredit()
        {
            string restorePhase=phase;int first=receipts.Count;match.Resume();float until=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<until&&!finished){Require(match.Live&&Time.timeScale==1,"Original restored AI resumes at1x");yield return new WaitForSecondsRealtime(.25f);}
            match.Pause();yield return null;yield return null;Trace();var newReceipts=receipts.Skip(first).ToArray();var forbidden=new HashSet<string>(deliveredIndices.Select(a=>"arch.delivery."+a),StringComparer.Ordinal);
            Require(newReceipts.All(r=>!forbidden.Contains(r.eventId)),"No saved delivered ID earns a duplicate receipt during ordinary resumed simulation");
            Require(deliveredIndices.All(a=>match.Rules.Stages[a]==ArtifactStage.Delivered&&match.Rules.Owners[a]==checkpoint.artifacts[a].owner&&match.artifacts[a].body.isKinematic),"Actual saved delivered artifacts retain historical delivered owner/support state");
            int[] recomputed=new int[4];for(int a=0;a<12;a++)if(match.Rules.Stages[a]==ArtifactStage.Delivered)recomputed[match.Rules.Owners[a]]+=(int)Math.Round(artifactValues[a]*match.Rules.Preservation[a],MidpointRounding.AwayFromZero);
            Require(recomputed.SequenceEqual(match.Rules.Scores),"Current per-owner scores still equal actual unique delivered values after resume (new genuine IDs permitted)");
            Require(match.Rules.Scores.Select((v,i)=>v>=expectedScores[i]).All(v=>v),"No loss of earned checkpoint values after actual resumed simulation");duplicateCreditGuard=true;phase=restorePhase;
        }
        void Rendered(ScriptableRenderContext context,Camera camera){if(camera!=null)rendered.Add(camera.GetInstanceID());}
        void LegacyRendered(Camera camera){if(camera!=null)rendered.Add(camera.GetInstanceID());}
        static bool Png(string file,int w,int h)
        {
            if(!File.Exists(file))return false;var b=File.ReadAllBytes(file);byte[] sig={137,80,78,71,13,10,26,10};if(b.Length<64||!b.Take(8).SequenceEqual(sig))return false;
            return ((b[16]<<24)|(b[17]<<16)|(b[18]<<8)|b[19])==w&&((b[20]<<24)|(b[21]<<16)|(b[22]<<8)|b[23])==h&&b[b.Length-8]==73&&b[b.Length-7]==69&&b[b.Length-6]==78&&b[b.Length-5]==68;
        }
        IEnumerator Capture(string label)
        {
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                string file=Path.Combine(folder,label+"-"+size.x+"x"+size.y+".png");if(File.Exists(file))throw new IOException("Keep prior original capture.");
                var cameras=match.cameras.Where(c=>c!=null&&c.isActiveAndEnabled&&c.targetTexture==null).ToArray();rendered.Clear();RenderPipelineManager.endCameraRendering+=Rendered;Camera.onPostRender+=LegacyRendered;var before=State();int requested=Time.frameCount;
                try
                {
                    yield return SportsWindowCapture.Save(size.x,size.y,file);Require(Screen.width==size.x&&Screen.height==size.y&&Png(file,size.x,size.y),"Actual unaltered Windows backbuffer "+label+" "+size.x+"x"+size.y);
                    Require(cameras.Length>0&&cameras.All(c=>rendered.Contains(c.GetInstanceID())),"Original active gameplay cameras actually rendered into this capture");
                    var image=new Texture2D(2,2,TextureFormat.RGB24,false);try{Require(image.LoadImage(File.ReadAllBytes(file))&&image.width==size.x&&image.height==size.y,"Original screenshot decodes at actual dimensions");var pixels=image.GetPixels32();int count=0,lit=0;float min=1,max=0;for(int i=0;i<pixels.Length;i+=97){float l=(pixels[i].r+pixels[i].g+pixels[i].b)/765f;min=Mathf.Min(min,l);max=Mathf.Max(max,l);if(l>.024f)lit++;count++;}Require(lit/(float)count>.1f&&max-min>.012f,"Actual composed image is nonblank; direct readability/visual review remains required");}finally{Destroy(image);}
                    frames.Add(new FrameFact{file=Path.GetFileName(file),label=label,sha256=Hash(file),width=size.x,height=size.y,requestedFrame=requested,completedFrame=Time.frameCount,requested=before,completed=State()});
                }
                finally{RenderPipelineManager.endCameraRendering-=Rendered;Camera.onPostRender-=LegacyRendered;}
            }
        }
        void Update(){if(evidenceOwned&&!finished&&Time.realtimeSinceStartup-began>600){Check(false,"600real-second independent optional-stage outer watchdog");Finish(true);}}
        void Finish(bool interrupted)
        {
            if(finished)return;finished=true;Application.logMessageReceived-=Error;SceneManager.sceneLoaded-=BindFreshStorage;RenderPipelineManager.endCameraRendering-=Rendered;Camera.onPostRender-=LegacyRendered;
            try{if(match!=null){((ISportsScorePresentationSource)match).ScoreConfirmed-=Receipt;match.SetupScreen();}}catch(Exception error){errors++;Note("CLEANUP_EXCEPTION "+error);}
            bool registrySame=PlayerPrefs.HasKey(SportsParticipantRegistry.Key)==priorRegistryPresent&&PlayerPrefs.GetString(SportsParticipantRegistry.Key,"")==priorRegistry;Check(registrySame,"Ordinary player registry raw bytes unchanged outside isolated preferences");preferences?.Dispose();preferences=null;SportsParticipantRegistry.ForgetCache();bool restored=SportsPrefs.StorageScope==priorScope;Check(restored,"Prior preference routing restored");Time.timeScale=1;
            if(!interrupted)Check(positiveSaved&&resetLoadVerified&&freshLoadVerified&&duplicateCreditGuard&&returnedToMenu,"Complete earned-positive/save/reset/reload/fresh-pending/resume/menu evidence, no full-match claim");
            var summary=new Summary{buildGuid=Application.buildGUID,version=Application.version,backend=SystemInfo.graphicsDeviceType.ToString(),storageMode=storageMode,savedSourceMatchId=checkpoint==null?null:checkpoint.sourceMatchId,checks=checks,errors=errors,runtimeErrors=runtimeErrors,pid=System.Diagnostics.Process.GetCurrentProcess().Id,seconds=Time.realtimeSinceStartup-began,naturalSeconds=naturalSeconds,interrupted=interrupted,positiveSaved=positiveSaved,resetLoadVerified=resetLoadVerified,freshLoadVerified=freshLoadVerified,duplicateCreditGuard=duplicateCreditGuard,returnedToMenu=returnedToMenu,playerRegistryUnchanged=registrySame,preferenceScopeRestored=restored,independentlyExpectedScores=expectedScores,savedDeliveredIndices=deliveredIndices,receipts=receipts.ToArray(),frames=frames.ToArray()};
            File.WriteAllText(Path.Combine(folder,"summary.json"),JsonUtility.ToJson(summary,true),utf8);Note("CHECKS="+checks+" ERRORS="+errors+" SCENES=1 SCREENSHOTS="+frames.Count+" RUNTIME_ERRORS="+runtimeErrors+" PHYSICS_TIME_SCALE=1 INTERRUPTED="+interrupted);
            Note("Actual Windows-only original1x AI/Digger physically earned nonzero preserved deliveries and PixelCrushers disk checkpoint, reset/fresh-pending restore and bounded no-duplicate-credit observation. No full-match, human/controller/wheel/listening/performance certification; exact build/process provenance supplied by RunPlaytestWindowsValidation/BuildIdentity.");
            File.WriteAllText(Path.Combine(folder,"result.txt"),report.ToString(),utf8);if(oldWidth>0&&oldHeight>0)Screen.SetResolution(oldWidth,oldHeight,oldMode);Application.Quit(errors==0&&!interrupted?0:2);
        }
        void OnDestroy()
        {
            Application.logMessageReceived-=Error;SceneManager.sceneLoaded-=BindFreshStorage;RenderPipelineManager.endCameraRendering-=Rendered;Camera.onPostRender-=LegacyRendered;if(match!=null)((ISportsScorePresentationSource)match).ScoreConfirmed-=Receipt;
            preferences?.Dispose();preferences=null;SportsParticipantRegistry.ForgetCache();Time.timeScale=1;if(!finished&&evidenceOwned)File.WriteAllText(Path.Combine(folder,"result.txt"),report+"\nINTERRUPTED; no completed positive-checkpoint acceptance.\n",utf8);if(!finished&&oldWidth>0&&oldHeight>0)Screen.SetResolution(oldWidth,oldHeight,oldMode);
        }
    }
}
#endif
