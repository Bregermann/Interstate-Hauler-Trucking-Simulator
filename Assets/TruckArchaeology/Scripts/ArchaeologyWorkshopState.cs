using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
namespace LWS.TruckArchaeology
{
    public sealed partial class ArchaeologyMatch
    {
        string pendingRestore;bool applyingStart;
        public string RestoredSourceMatchId {get;private set;}="";
        [Serializable]public sealed class SavedBody {public string id;public Vector3 position,velocity,angular;public Quaternion rotation;public bool kinematic;}
        [Serializable]public sealed class SavedArtifact {public string id;public int stage,owner;public double preservation;public SavedBody body;}
        [Serializable]public sealed class WorkshopState
        {
            public int version=1;public string modeId="TruckArchaeology",sourceMatchId,terrainPrefix;public string[] profiles,terrainNames,terrainHashes;
            public int players;public bool cooperative;public float clock;public SavedArtifact[] artifacts;public SavedBody[] rigBodies,toolHeads;public int[] scores,tools,targets;
        }
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static string BodyId(int owner,Rigidbody body,Transform root)
        {
            string path="";for(Transform t=body.transform;t!=root;t=t.parent)path="/"+t.name+"#"+t.GetSiblingIndex()+path;return "crew."+owner+path;
        }
        SavedBody CaptureBody(string id,Rigidbody body)=>new SavedBody{id=id,position=body.position,rotation=body.rotation,velocity=body.linearVelocity,angular=body.angularVelocity,kinematic=body.isKinematic};
        public bool WorkshopSaveReady=>!Live&&Rules!=null&&!terrainResetPending&&earth.Quiescent&&equipment.Take(setup.count).All(e=>e.hoist.Stage==LWS.TruckJenga.JengaCrane.MotorStage.Idle)&&!unloading.Any(v=>v);
        public string CaptureWorkshopJson()
        {
            if(!WorkshopSaveReady)throw new InvalidOperationException("Pause/stop after physical crane motion finishes before saving.");
            string prefix=earth.PersistSnapshot(),folder=Path.Combine(Application.persistentDataPath,"DiggerData",prefix,earth.system.Guid);
            var files=Directory.GetFiles(folder).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            var data=new WorkshopState{sourceMatchId=RestoredSourceMatchId.Length>0?RestoredSourceMatchId:MatchId,terrainPrefix=prefix,terrainNames=files.Select(Path.GetFileName).ToArray(),terrainHashes=files.Select(Hash).ToArray(),profiles=setup.players.Take(setup.count).Select(p=>p.participantId).ToArray(),players=setup.count,cooperative=Rules.Cooperative,clock=Clock,scores=(int[])Rules.Scores.Clone(),tools=Tools.Select(t=>(int)t).ToArray(),targets=(int[])Targets.Clone(),
                artifacts=artifacts.Select(a=>new SavedArtifact{id=a.stableId,stage=(int)Rules.Stages[a.index],owner=Rules.Owners[a.index],preservation=Rules.Preservation[a.index],body=CaptureBody(a.stableId,a.body)}).ToArray(),
                toolHeads=equipment.Take(setup.count).Select((e,i)=>CaptureBody("tool.head."+i,e.head)).ToArray(),
                rigBodies=trucks.Take(setup.count).SelectMany((t,i)=>t.GetComponentsInChildren<Rigidbody>(true).Select(b=>CaptureBody(BodyId(i,b,t.transform),b))).ToArray()};
            return JsonUtility.ToJson(data);
        }
        static bool ValidBody(SavedBody body)=>body!=null&&float.IsFinite(body.position.x)&&float.IsFinite(body.position.y)&&float.IsFinite(body.position.z)&&body.position.magnitude<500&&body.position.y> -30&&body.position.y<100&&float.IsFinite(body.velocity.sqrMagnitude)&&body.velocity.magnitude<100&&float.IsFinite(body.angular.sqrMagnitude)&&body.angular.magnitude<30&&float.IsFinite(Quaternion.Dot(body.rotation,body.rotation))&&Mathf.Abs(Quaternion.Dot(body.rotation,body.rotation)-1)<.002f;
        public bool TryRestoreWorkshopJson(string json,out string reason)
        {
            reason="";WorkshopState data;
            if((Live&&!applyingStart)||earth==null||!earth.Quiescent||terrainResetPending||equipment==null||equipment.Take(setup.count).Any(e=>e.hoist.Stage!=LWS.TruckJenga.JengaCrane.MotorStage.Idle)){reason="Restore requires a stopped/paused quiescent physical workshop.";return false;}
            try{data=JsonUtility.FromJson<WorkshopState>(json);}catch(Exception){reason="Invalid archaeology JSON.";return false;}
            if(data==null||data.version!=1||data.modeId!="TruckArchaeology"||string.IsNullOrEmpty(data.sourceMatchId)||data.players!=setup.count||data.cooperative!=(option==0)||data.profiles==null||!data.profiles.SequenceEqual(setup.players.Take(setup.count).Select(p=>p.participantId))||!float.IsFinite(data.clock)||data.clock<0){reason="Unknown version, mode, crew, format or source context.";return false;}
            if(data.artifacts==null||data.artifacts.Length!=12||data.scores==null||data.scores.Length!=4||data.tools==null||data.tools.Length!=4||data.targets==null||data.targets.Length!=4||data.rigBodies==null||data.toolHeads==null||data.toolHeads.Length!=setup.count){reason="Incomplete workshop arrays.";return false;}
            int[] expectedScores=new int[4],cargoCounts=new int[4];
            for(int a=0;a<12;a++)
            {
                var item=data.artifacts[a];if(item==null||item.id!=artifacts[a].stableId||item.stage<0||item.stage>5||item.stage==(int)ArtifactStage.Lifting||item.owner< -1||item.owner>=setup.count||!double.IsFinite(item.preservation)||item.preservation<0||item.preservation>1||!ValidBody(item.body)||item.body.id!=item.id){reason="Unknown artifact identity or invalid physical/preservation state.";return false;}
                if(a>=setup.count*3&&(item.stage!=0||item.owner!=-1)){reason="Inactive plot cannot contain recovered finds.";return false;}
                if((item.stage<=(int)ArtifactStage.Scanned)!=item.body.kinematic&&item.stage!=(int)ArtifactStage.Delivered){reason="Physical support state contradicts the known excavation stage.";return false;}
                if(item.stage<(int)ArtifactStage.Lifting&&item.owner!=-1){reason="Unclaimed find cannot have a cargo owner.";return false;}
                if(item.stage==(int)ArtifactStage.Delivered&&(item.owner<0||!item.body.kinematic||Vector3.Distance(item.body.position,BenchSlot(item.owner,a))>.65f)){reason="Delivered find must actually rest on its known exhibit bench.";return false;}
                if(item.stage>=(int)ArtifactStage.Cargo)
                {
                    if(item.owner<0||(!data.cooperative&&a/3!=item.owner)){reason="Unknown cargo owner.";return false;}
                    if(item.stage==(int)ArtifactStage.Delivered)expectedScores[item.owner]+=(int)Math.Round(artifacts[a].value*item.preservation,MidpointRounding.AwayFromZero);else cargoCounts[item.owner]++;
                }
            }
            if(!data.scores.SequenceEqual(expectedScores)||cargoCounts.Any(c=>c>1)){reason="Score must equal actual delivered preserved values; one physical cargo per truck.";return false;}
            var rigBodies=trucks.Take(setup.count).SelectMany((t,i)=>t.GetComponentsInChildren<Rigidbody>(true).Select(b=>(id:BodyId(i,b,t.transform),body:b))).ToArray();
            if(rigBodies.Length!=data.rigBodies.Length){reason="Original NWH body identity count differs.";return false;}
            for(int i=0;i<rigBodies.Length;i++)if(!ValidBody(data.rigBodies[i])||data.rigBodies[i].id!=rigBodies[i].id){reason="Unknown original NWH body or invalid pose.";return false;}
            for(int a=0;a<12;a++)if(data.artifacts[a].stage==(int)ArtifactStage.Cargo)
            {
                var item=data.artifacts[a];int owner=item.owner;var root=data.rigBodies.Single(b=>b.id=="crew."+owner);var rack=equipment[owner].rack;
                Vector3 local=trucks[owner].transform.InverseTransformPoint(rack.transform.TransformPoint(rack.center));
                Vector3 expected=root.position+root.rotation*Vector3.Scale(local,trucks[owner].transform.lossyScale);
                Vector3 offset=Quaternion.Inverse(root.rotation)*(item.body.position-expected);
                if(Mathf.Abs(offset.x)>rack.bounds.extents.x+artifacts[a].shape.bounds.extents.x||Mathf.Abs(offset.z)>rack.bounds.extents.z+artifacts[a].shape.bounds.extents.z||offset.y<0||offset.y>artifacts[a].shape.size.y+1){reason="Cargo must occupy its actual original owner's physical deck.";return false;}
            }
            for(int i=0;i<setup.count;i++)if(!ValidBody(data.toolHeads[i])||data.toolHeads[i].id!="tool.head."+i||!data.toolHeads[i].kinematic){reason="Unknown physical tool head.";return false;}
            for(int i=0;i<4;i++)if(data.tools[i]<0||data.tools[i]>5||data.targets[i]< -1||data.targets[i]>=setup.count*3){reason="Unknown tool/target.";return false;}
            if(data.terrainNames==null||data.terrainHashes==null||data.terrainNames.Length!=data.terrainHashes.Length||data.terrainNames.Length<1||data.terrainNames.Distinct().Count()!=data.terrainNames.Length){reason="Incomplete vendor terrain snapshot.";return false;}
            try
            {
                if(!earth.ValidateSnapshot(data.terrainPrefix,out reason))return false;
                string folder=Path.Combine(Application.persistentDataPath,"DiggerData",data.terrainPrefix,earth.system.Guid);
                var actualNames=Directory.GetFiles(folder).Select(Path.GetFileName).OrderBy(n=>n,StringComparer.Ordinal);
                if(!actualNames.SequenceEqual(data.terrainNames.OrderBy(n=>n,StringComparer.Ordinal))){reason="The complete actual vendor directory must match the saved file/hash manifest.";return false;}
                for(int i=0;i<data.terrainNames.Length;i++)
                {
                    string name=data.terrainNames[i];if(string.IsNullOrEmpty(name)||name!=Path.GetFileName(name)||name.Contains("..")||!File.Exists(Path.Combine(folder,name))||Hash(Path.Combine(folder,name))!=data.terrainHashes[i]){reason="Vendor terrain data missing or changed; no scene state was written.";return false;}
                }
            }
            catch(Exception error)when(error is IOException||error is UnauthorizedAccessException||error is System.Security.SecurityException){reason="Vendor snapshot files are inaccessible or changed; no scene state was written.";return false;}
            // Every identity/value/file passed before the first scene write. Root Pixel Crushers owns storage.
            if(!Running&&!applyingStart){pendingRestore=json;return true;}
            earth.RestoreSnapshot(data.terrainPrefix);Rules=new ArchaeologyRules(data.players,data.cooperative);Clock=data.clock;RestoredSourceMatchId=data.sourceMatchId;
            for(int i=0;i<4;i++){Tools[i]=(ArchaeologyTool)data.tools[i];Targets[i]=data.targets[i];rest[i]=0;unloading[i]=false;}
            ResetRestoredWorkScheduling();
            foreach(var artifact in artifacts)artifact.ReleaseCargo();
            for(int i=0;i<rigBodies.Length;i++)ApplyBody(rigBodies[i].body,data.rigBodies[i]);
            for(int i=0;i<setup.count;i++)ApplyBody(equipment[i].head,data.toolHeads[i]);
            for(int a=0;a<12;a++){var item=data.artifacts[a];Rules.Stages[a]=(ArtifactStage)item.stage;Rules.Owners[a]=item.owner;Rules.Preservation[a]=item.preservation;ApplyBody(artifacts[a].body,item.body);if(item.stage==(int)ArtifactStage.Cargo)artifacts[a].SecureCargo(item.owner);}
            Array.Copy(data.scores,Rules.Scores,4);Gallery=Rules.Complete;if(Gallery)foreach(var truck in trucks.Take(setup.count))truck.Power(false);return true;
        }
        static void ApplyBody(Rigidbody body,SavedBody state)
        {
            body.isKinematic=false;body.position=state.position;body.rotation=state.rotation;body.linearVelocity=state.velocity;body.angularVelocity=state.angular;body.isKinematic=state.kinematic;
            // This is a validated stopped/paused snapshot, not a live movement
            // command. Publish its identical visible pose now: interpolated
            // Rigidbody setters otherwise leave this hierarchy at the old spawn
            // pose while time is paused (including attached Cab bodies).
            body.transform.SetPositionAndRotation(state.position,state.rotation);
        }
        // These are unsaved observations/deadlines of the superseded collider
        // timeline, not workshop progress. Reacquire from the restored real
        // terrain at the original .25/.2s cadence instead of waiting for a
        // future clock or digging at its cached future well.
        void ResetRestoredWorkScheduling()
        {
            ResetAiWells();nextStroke=nextExposure=Clock;finishAge=0;cursor=0;
            Array.Clear(heldAge,0,heldAge.Length);Array.Clear(previousHeld,0,previousHeld.Length);
            Array.Clear(commands,0,commands.Length);Array.Clear(actions,0,actions.Length);
        }
    }
}
