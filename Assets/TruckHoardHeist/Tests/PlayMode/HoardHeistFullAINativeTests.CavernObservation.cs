#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class HoardHeistFullAINativeTests
{
    [Serializable]public sealed class LiveCavernBody{public int id;public string kind;public Bounds bounds;public Vector3 position;}
    [Serializable]public sealed class LiveCavernFrame
    {public int run,frame,physicsActions;public float clock,time;public Vector3 guardian,motorBodyPosition,motorLinearVelocity,motorAngularVelocity;public Quaternion rotation,motorBodyRotation;public int originalMotorBodyId;public bool flying,transition;public string action,actualClip;public float stateTime;public Bounds actualGuardianSkin;public LiveCavernBody[] bodies;}
    [Serializable]public sealed class LiveCavernEvidence
    {
        public string scope="READ_ONLY_LIVE_ORIGINAL_SAVED_HEIST_MATCH_SAMPLES_NOT_CONTINUOUS_MOTION_OR_NEW_VAULT_CLEARANCE",status="OBSERVING",poseScope="guardian/rotation retain outer wrapper pose; motorBodyPosition/motorBodyRotation are authoritative vendor Rigidbody pose",actionScope="action is mapped mode cache; flying and actualClip/stateTime/transition describe actual locomotion";public int poseSchema=2;
        public int sampledFrames,actualFlightFrames;public bool telemetryTruncated,sourceBytesExact;public LiveCavernSource[] sources;public float simulationSampleInterval=.1f;public Bounds combinedGuardianBounds;public List<LiveCavernFrame> frames=new List<LiveCavernFrame>();
        public string[] limitations={"Finite .1simulated-second samples do not prove continuous animation or every possible driving route.","Source bounds use the established true-bake/full-world-matrix convention; absolute new-guardian rig acceptance still requires its independent CPU skin oracle.","This records the original saved match only; no new cave geometry or collision clearance is accepted by observation."};
    }
    [Serializable]public sealed class LiveCavernSource{public string path,beforeSha256,afterSha256;}
    [Serializable]public sealed class LiveCavernSummary{public string status;public int sampledFrames,flightFrames,storedFrames;public bool telemetryTruncated;public Bounds combinedGuardianBounds;}
    LiveCavernEvidence liveCavern;System.Reflection.MethodInfo liveMeasure;StreamWriter liveStream;float nextLiveSample;
    LiveCavernSummary LiveCavernProgressSummary()=>liveCavern==null?null:new LiveCavernSummary{status=liveCavern.status,sampledFrames=liveCavern.sampledFrames,flightFrames=liveCavern.actualFlightFrames,storedFrames=liveCavern.frames.Count,telemetryTruncated=liveCavern.telemetryTruncated,combinedGuardianBounds=liveCavern.combinedGuardianBounds};
    static string LiveSourceSha(string path){using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    void ConfigureLiveCavernObservation()
    {
        if(!Environment.GetCommandLineArgs().Contains("-tsDragonRecordLiveCavern"))return;
        liveCavern=new LiveCavernEvidence();nextLiveSample=-1;
        liveMeasure=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring, Assembly-CSharp-Editor",true).GetMethod("MeasureOwnedModelBounds");Assert.That(liveMeasure,Is.Not.Null,"Reuse the existing full source geometry authority.");
        var animator=(Animator)Read(motor,"animator");var paths=new[]{ScenePath,UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(((Component)Read(match,"world")).gameObject),UnityEditor.AssetDatabase.GetAssetPath((UnityEngine.Object)Read(match,"species")),UnityEditor.AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),UnityEditor.AssetDatabase.GetAssetPath(animator.avatar)}.Concat(guardian.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.sharedMesh!=null).Select(s=>UnityEditor.AssetDatabase.GetAssetPath(s.sharedMesh))).Where(p=>!string.IsNullOrEmpty(p)).Distinct().SelectMany(p=>new[]{p,p+".meta"}).Where(File.Exists).Distinct().ToArray();
        liveCavern.sources=paths.Select(p=>new LiveCavernSource{path=p,beforeSha256=LiveSourceSha(p)}).ToArray();
        liveStream=new StreamWriter(Path.Combine(output,"live-original-cavern-samples.jsonl"),false);
    }
    void ObserveLiveCavernFrame()
    {
        if(liveCavern==null||Time.time<nextLiveSample)return;nextLiveSample=Time.time+.1f;
        var actualMotorBody=(Rigidbody)Read(motor,"body");Assert.That(actualMotorBody,Is.Not.Null,"Actual original vendor motor body required.");
        var animator=(Animator)Read(motor,"animator");var state=animator.GetCurrentAnimatorStateInfo(0);var clips=animator.GetCurrentAnimatorClipInfo(0);
        var frame=new LiveCavernFrame{run=run,frame=Time.frameCount,clock=(float)Read(match,"Clock"),time=Time.time,guardian=guardian.transform.position,rotation=guardian.transform.rotation,motorBodyPosition=actualMotorBody.position,motorBodyRotation=actualMotorBody.rotation,motorLinearVelocity=actualMotorBody.linearVelocity,motorAngularVelocity=actualMotorBody.angularVelocity,originalMotorBodyId=actualMotorBody.GetInstanceID(),flying=(bool)Read(motor,"IsFlying"),transition=animator.IsInTransition(0),stateTime=state.normalizedTime,action=Read(motor,"CurrentAction").ToString(),actualClip=string.Join(";",clips.Select(c=>c.clip.name+"@"+c.weight.ToString("R",System.Globalization.CultureInfo.InvariantCulture))),actualGuardianSkin=(Bounds)liveMeasure.Invoke(null,new object[]{guardian.transform})};
        var bodies=new List<LiveCavernBody>();foreach(var truck in trucks){var component=(Component)truck;bodies.Add(new LiveCavernBody{id=component.GetInstanceID(),kind="ORIGINAL_NWH_COMPLETE_MODEL",position=component.transform.position,bounds=(Bounds)liveMeasure.Invoke(null,new object[]{component.transform})});}
        foreach(var item in treasure){var component=(Component)item;var shape=(Collider)Read(item,"shape");if(shape.enabled&&shape.gameObject.activeInHierarchy)bodies.Add(new LiveCavernBody{id=component.GetInstanceID(),kind="ORIGINAL_PHYSICAL_TREASURE",position=component.transform.position,bounds=shape.bounds});}
        frame.bodies=bodies.ToArray();frame.physicsActions=actions.Count;if(liveCavern.frames.Count<12000){liveCavern.frames.Add(frame);liveStream.WriteLine(JsonUtility.ToJson(frame));}else liveCavern.telemetryTruncated=true;if(liveCavern.sampledFrames==0)liveCavern.combinedGuardianBounds=frame.actualGuardianSkin;else liveCavern.combinedGuardianBounds.Encapsulate(frame.actualGuardianSkin);liveCavern.sampledFrames++;if(frame.flying)liveCavern.actualFlightFrames++;
        // Write on the existing bounded progress cadence rather than per frame.
    }
    void CompleteLiveCavernObservation()
    {
        if(liveCavern==null)return;liveCavern.status="LIVE_MATCH_SAMPLES_RECORDED_REQUIRES_CONTINUOUS_ENVELOPE_AND_NEW_GEOMETRY_ACCEPTANCE";
        File.WriteAllText(Path.Combine(output,"live-original-cavern-occupancy.json"),JsonUtility.ToJson(liveCavern,true));Assert.That(liveCavern.telemetryTruncated,Is.False,"Bounded telemetry exhausted; retain honest partial evidence.");Assert.That(liveCavern.sampledFrames,Is.GreaterThan(0));Assert.That(liveCavern.frames.Exists(f=>f.run==1),Is.True);Assert.That(liveCavern.frames.Exists(f=>f.run==2),Is.True);
        Assert.That(liveCavern.actualFlightFrames,Is.GreaterThan(0),"The requested live flight envelope remains unverified if ordinary AI never caused actual vendor flight; never synthesize it.");
    }
    void ReleaseLiveCavernObservation()
    {
        liveStream?.Dispose();liveStream=null;
        if(liveCavern!=null&&output!=null){if(liveCavern.status=="OBSERVING")liveCavern.status="PARTIAL_OR_INTERRUPTED_MATCH_OBSERVATION_NOT_FULL_MATCH_ACCEPTANCE";foreach(var source in liveCavern.sources??Array.Empty<LiveCavernSource>())source.afterSha256=File.Exists(source.path)?LiveSourceSha(source.path):null;liveCavern.sourceBytesExact=liveCavern.sources!=null&&liveCavern.sources.All(p=>p.beforeSha256==p.afterSha256);File.WriteAllText(Path.Combine(output,"live-original-cavern-occupancy.json"),JsonUtility.ToJson(liveCavern,true));Assert.That(liveCavern.sourceBytesExact,Is.True,"Original live scene/catalog/controller/avatar/skin bytes changed.");}liveMeasure=null;
    }
}
#endif
