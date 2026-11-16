#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class RidgePlacementDiagnosis
        {public string scope="ACTUAL_RETAINED23_SAME172_RIDGE_SOURCE_ROOTS_MEASURED_PLACEMENT_AND_REJECTION_WITNESSES_NO_WORLD_REGEN_OR_ASSET_SAVE_AFTER_APPROVED_IMPORT_PREFLIGHT",status="IN_PROGRESS",placementError,cleanupError;public int targetRoots,attemptedParts,physicsDescriptorRows;public bool placementPassed,original172PosesRestored,originalPhysicsRestored,sourceBytesPreserved,sourceBaselinePrepared;public QuayImportEvidence importPreflight;public QuayDependencyChange[] preflightSourceChanges,unexpectedGeometrySourceChanges;public RidgePhysicsDifference[] physicsDifferences;public RidgeComposition ridge;}
        [Serializable]public sealed class RidgePhysicsDifference
        {public string componentKey,path,type,before,after;}
        static Dictionary<string,RidgePhysicsDifference> CaptureRidgePhysicsDescriptors(GameObject world)
        {
            var result=new Dictionary<string,RidgePhysicsDifference>(StringComparer.Ordinal);
            foreach(var component in world.GetComponentsInChildren<Component>(true))
            {
                if(!(component is Transform)&&!(component is Collider)&&!(component is Rigidbody)&&!(component is MeshFilter))continue;
                string path=component.transform.name;for(var parent=component.transform.parent;parent!=null;parent=parent.parent)path=parent.name+"/"+path;
                string value=component is MeshFilter mesh?AssetDatabase.GetAssetPath(mesh.sharedMesh)+"|"+(mesh.sharedMesh==null?0:mesh.sharedMesh.vertexCount):EditorJsonUtility.ToJson(component);
                string key=component.GetInstanceID().ToString(System.Globalization.CultureInfo.InvariantCulture);
                result.Add(key,new RidgePhysicsDifference{componentKey=key,path=path,type=component.GetType().FullName,before=value});
            }
            return result;
        }
        static RidgePhysicsDifference[] RidgePhysicsChanges(Dictionary<string,RidgePhysicsDifference> before,GameObject world)
        {
            var after=CaptureRidgePhysicsDescriptors(world);var changes=new List<RidgePhysicsDifference>();
            foreach(string key in before.Keys.Union(after.Keys).OrderBy(k=>k,StringComparer.Ordinal))
            {
                before.TryGetValue(key,out var oldRow);after.TryGetValue(key,out var newRow);
                if(oldRow?.before==newRow?.before)continue;
                changes.Add(new RidgePhysicsDifference{componentKey=key,path=oldRow?.path??newRow.path,type=oldRow?.type??newRow.type,before=oldRow?.before,after=newRow?.before});
            }
            return changes.ToArray();
        }
        public static RidgePlacementDiagnosis LastRidgePlacementDiagnosis{get;private set;}
        public static void CaptureActualRetainedRidgePlacements()
        {
            const string folder=Root+"VisualCandidates/RoundedIntegrated23";string preview=folder+"/DragonKingdomPreview.unity";if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists(Io(preview)))throw new InvalidOperationException("Stopped actual retained23 required; no world generation in ridge diagnosis.");var paths=QuayProbeDependencyPaths(preview);var originalHashes=paths.ToDictionary(p=>p,AssetSha);var originalPayloads=QuayImportPayloads(paths);var prior=PrepareValidQuayProbeSceneSetup();var originalIdentities=CaptureQuayProtectedImportIdentities();Dictionary<string,string> frozen=null;Dictionary<string,RidgePhysicsDifference> physicsRows=null;var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(Io(folder+"/Integration Receipt.json")));var records=receipt.placements.ToList();var proof=new RidgePlacementDiagnosis();LastRidgePlacementDiagnosis=proof;Scene scene=default;DragonKingdomWorld world=null;string physics=null;var poses=new Dictionary<Transform,Tuple<Vector3,Quaternion,Vector3>>();
            try
            {
                scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();frozen=PrepareFrozenQuayGeometrySources(paths,originalHashes,originalPayloads,originalIdentities,prior,changes=>proof.preflightSourceChanges=changes,out var imports);proof.importPreflight=imports;proof.sourceBaselinePrepared=true;physics=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);
                physicsRows=CaptureRidgePhysicsDescriptors(world.gameObject);proof.physicsDescriptorRows=physicsRows.Count;
                foreach(var record in records.Where(r=>r.name.StartsWith("Owned alpine shelter geology ",StringComparison.Ordinal)||r.name.StartsWith("Owned living cliff roost tree ",StringComparison.Ordinal)||r.name.StartsWith("Owned forest underbrush ",StringComparison.Ordinal)))
                {var root=world.Region(record.region).root.Cast<Transform>().Single(t=>t.name==record.name);poses.Add(root,Tuple.Create(root.localPosition,root.localRotation,root.localScale));}proof.targetRoots=poses.Count;if(poses.Count!=172)throw new InvalidOperationException("All actual original24 alpine/8roost/140underbrush roots required.");
                try{proof.ridge=RecomposeExistingRidgeArt(world,Root+"VisualCandidates/ReadonlyRidgeDiagnostic26",records,diagnoseAll:true);proof.placementPassed=proof.ridge.passed;}
                catch(Exception error){proof.placementError=error.ToString();proof.ridge=LastRidgeComposition;}
                proof.attemptedParts=proof.ridge?.parts?.Length??0;proof.status=proof.placementPassed?"ACTUAL172_RIDGE_PLACEMENT_PASSED_REQUIRES_FRESH_WORLD_AND_RENDER_REVIEW":"ACTUAL_RIDGE_REJECTION_CAPTURED_REQUIRES_PLACEMENT_REPAIR";
            }
            finally
            {
                // Ridge authoring changes translation only. Reassigning an untouched
                // quaternion can normalize it and update serialized Euler hints.
                // Preserve every original rotation/scale byte instead of invoking
                // those setters during diagnostic cleanup.
                foreach(var pair in poses)if(!pair.Key.localPosition.Equals(pair.Value.Item1))pair.Key.localPosition=pair.Value.Item1;
                Physics.SyncTransforms();proof.original172PosesRestored=poses.Count==172&&poses.All(p=>p.Key.localPosition.Equals(p.Value.Item1)&&p.Key.localRotation.Equals(p.Value.Item2)&&p.Key.localScale.Equals(p.Value.Item3));proof.physicsDifferences=world!=null&&physicsRows!=null?RidgePhysicsChanges(physicsRows,world.gameObject):Array.Empty<RidgePhysicsDifference>();proof.originalPhysicsRestored=world!=null&&physics==DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject)&&proof.physicsDifferences.Length==0;try{if(scene.IsValid()&&scene.isLoaded&&!EditorSceneManager.CloseScene(scene,true))throw new InvalidOperationException("Owned ridge diagnostic scene did not close.");EditorSceneManager.RestoreSceneManagerSetup(prior);}catch(Exception cleanup){proof.cleanupError=cleanup.ToString();}proof.unexpectedGeometrySourceChanges=frozen==null?Array.Empty<QuayDependencyChange>():QuayDependencyChanges(frozen);proof.sourceBytesPreserved=proof.sourceBaselinePrepared&&proof.unexpectedGeometrySourceChanges.Length==0;Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"actual-retained23-ridge-placement-diagnosis.json"),JsonUtility.ToJson(proof,true));if(!proof.original172PosesRestored||!proof.originalPhysicsRestored||!proof.sourceBytesPreserved||!string.IsNullOrEmpty(proof.cleanupError))throw new InvalidOperationException("Actual ridge diagnosis must restore all original source,172poses,physics and scene context.");
            }
        }
    }
}
#endif
