#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    // Explicit scene-instance repair. Existing runtime, shared world asset,
    // source mounts and NWH assemblies remain the authorities they already are.
    public static class DragonRiderStartFormationAuthoring
    {
        const string OwnedGroup="Owned source-qualified Rider start formation";
        [Serializable]public sealed class Plan
        {
            public string scope="ACTUAL_SOURCE_QUALIFIED_RIG_ENVELOPES_AND_NATIVE_WORLD_START_APPROACH_QUERIES_NOT_RUNTIME_OR_VISUAL_ACCEPTANCE";
            public string ridersScene,worldPrefab,sourceQualification;public float enclosingRadius,minimumSpacing,firstGateDistance;
            public Pose[] starts;public int overlapQueries,sweepQueries;public bool allSourceChoicesIncluded,allStartsAndApproachesClear,equalFirstGateDistance,sourceFilesExact,originalTruckStateExact,sharedWorldAssetExact,savedReviewScene;
        }
        // Every crew gets the same distance to the first target. The circle's
        // chord length determines separation; no one gets a forward head start.
        public static Pose[] ProposeArc(Vector3 firstGate,Vector3 forward,float radius,float altitude)
        {
            if(!Finite(firstGate)||!Finite(forward)||!float.IsFinite(radius)||!float.IsFinite(altitude)||radius<=0||altitude<0)throw new ArgumentException("Finite positive source-measured formation required.");
            var horizontal=Vector3.ProjectOnPlane(forward,Vector3.up);if(horizontal.sqrMagnitude<.0001f)throw new ArgumentException("Actual first-gate horizontal approach required.");horizontal.Normalize();
            return Enumerable.Range(0,4).Select(i=>
            {
                float angle=Mathf.Lerp(-40,40,i/3f);var behind=Quaternion.AngleAxis(angle,Vector3.up)*(-horizontal);
                var position=firstGate+behind*radius+Vector3.up*altitude;return new Pose(position,Quaternion.LookRotation(firstGate-position,Vector3.up));
            }).ToArray();
        }
        static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
        public static bool Separate(Pose[] poses,float radius,float margin)
        {
            if(poses==null||poses.Length!=4||!float.IsFinite(radius)||!float.IsFinite(margin)||radius<=0||margin<0||poses.Any(p=>!Finite(p.position)))return false;
            for(int i=0;i<4;i++)for(int j=0;j<i;j++)if(Vector3.Distance(poses[i].position,poses[j].position)<radius*2+margin)return false;return true;
        }
        public static Plan Measure(DragonRidersMatch match,DragonKingdomMountedCourseReview.Receipt qualification)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||match==null||match.world==null||match.gates==null||match.gates.Length<8||qualification?.rigs==null||!qualification.sourceBytesPreserved||qualification.qualifiedSource==null)throw new InvalidOperationException("Stopped actual saved source-qualified eight-choice Riders scene required; metadata-only radii are insufficient.");
            var scene=match.gameObject.scene;
            if(!scene.IsValid()||!scene.isLoaded||scene.path!=qualification.ridersScene||qualification.rigs.Length!=8||qualification.rigs.Select(r=>r.crew+"/"+r.species).Distinct().Count()!=8||!Enumerable.Range(0,4).All(i=>qualification.rigs.Count(r=>r.crew==i)==2)||qualification.rigs.Any(r=>r.maximumGateRadialExtent<=0||!float.IsFinite(r.maximumGateRadialExtent)||r.recordedSourcePoses<=0))throw new InvalidOperationException("All eight genuine current-source mount envelopes and the exact saved scene are required.");
            var source=qualification.qualifiedSource;
            if(source.status!="CURRENT_SOURCE_SEMANTIC_QUALIFICATION_COMPLETE_NATIVE_TEST_RESULT_REMAINS_SEPARATE"||!source.currentSourceBytesExact||!source.currentConfigurationExact||!source.currentProfilesExact||!source.currentNwhExact||!source.unitAncestors||!source.noTestDependencies||source.maximumNwhDimensionDrift!=0||!string.IsNullOrEmpty(source.failure)||!string.IsNullOrEmpty(source.cleanupFailure)||source.currentSourceSnapshot==null||source.currentSourceSnapshot.Length==0||!CurrentSourcesExact(source.currentSourceSnapshot,null))throw new InvalidOperationException("Actual current profile/NWH/source qualification and every referenced source byte must still be exact.");
            string prefab=AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(match.world.gameObject));
            if(string.IsNullOrEmpty(prefab)||!File.Exists(prefab)||Hash(prefab)!=qualification.worldPrefabSha256||Hash(scene.path)!=qualification.ridersSceneSha256)throw new InvalidOperationException("World or Riders scene differs from the actual source-qualified review; qualify it before formation repair.");
            // The review's radii are centered on the truck. Include the actual
            // measured offset to enclose every possible choice about its motor.
            float radius=qualification.rigs.Max(r=>r.maximumGateRadialExtent+r.neutralSeatOffset.magnitude)+.5f;
            if(!float.IsFinite(radius)||radius<=0)throw new InvalidOperationException("Finite whole-assembly radius required.");
            var obstacles=new HashSet<Collider>(match.world.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger));
            foreach(var root in scene.GetRootGameObjects().Where(r=>r.name=="Ordered kingdom aerial gate loop"))foreach(var c in root.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger))obstacles.Add(c);
            if(obstacles.Count==0)throw new InvalidOperationException("Actual world colliders must be loaded; an empty scene cannot qualify starts.");
            Physics.SyncTransforms();var first=match.gates[0];var forward=first.rotation*Vector3.forward;var approach=first.center-forward*Mathf.Max(first.clearRadius*1.5f,radius+10);
            float minimum=(radius*2+4)/(2*Mathf.Sin(40f/3*Mathf.Deg2Rad));var plan=new Plan{ridersScene=scene.path,worldPrefab=prefab,sourceQualification=qualification.qualifiedProductionEvidence,enclosingRadius=radius,minimumSpacing=radius*2+4,allSourceChoicesIncluded=true,sourceFilesExact=true};
            // Bounded pre-match authoring search, never a mid-race teleport.
            foreach(float altitude in new[]{0f,40f,80f,120f,180f,240f})foreach(float extension in new[]{0f,40f,80f,140f})
            {
                var poses=ProposeArc(first.center,forward,minimum+extension,altitude);if(!Separate(poses,radius,4))continue;bool clear=true;
                foreach(var pose in poses)
                {
                    plan.overlapQueries++;if(Physics.OverlapSphere(pose.position,radius,~0,QueryTriggerInteraction.Ignore).Any(obstacles.Contains)){clear=false;break;}
                    var delta=approach-pose.position;plan.sweepQueries++;if(delta.sqrMagnitude<.01f||Physics.SphereCastAll(pose.position,radius,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Any(hit=>obstacles.Contains(hit.collider))){clear=false;break;}
                }
                if(!clear)continue;plan.starts=poses;plan.firstGateDistance=Vector3.Distance(poses[0].position,first.center);plan.equalFirstGateDistance=poses.All(p=>Mathf.Abs(Vector3.Distance(p.position,first.center)-plan.firstGateDistance)<.001f);plan.allStartsAndApproachesClear=true;return plan;
            }
            throw new InvalidOperationException("No source-measured four-rider formation clears the actual world and first approach within the bounded authoring search; preserve the scene and repair the course.");
        }
        [MenuItem("Truck Sports/Dragon Riders/Install Source-Qualified Starts in OPEN Review Scene")]
        public static void InstallInOpenReviewScene()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var matches=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).ToArray();
            if(matches.Length!=1||DragonKingdomMountedCourseReview.LastReceipt==null)throw new InvalidOperationException("Open the exact clean Riders review scene and run its current source-qualified mounted-course review first.");
            var proof=InstallOnExplicitReviewScene(matches[0],DragonKingdomMountedCourseReview.LastReceipt);Debug.Log(JsonUtility.ToJson(proof,true));
        }
        public static Plan InstallOnExplicitReviewScene(DragonRidersMatch match,DragonKingdomMountedCourseReview.Receipt qualification)
        {
            string path=match==null?null:match.gameObject.scene.path;
            if(string.IsNullOrEmpty(path)||!path.StartsWith("Assets/TruckDragonRiders/ReviewCandidates/",StringComparison.Ordinal)||!path.EndsWith(".unity",StringComparison.Ordinal)||path.Split('/').Contains("..")||match.gameObject.scene.isDirty)throw new InvalidOperationException("Explicit clean sport-owned review scene required; no canonical or hand-edited scene rebuilding.");
            if(match.transform.Find(OwnedGroup)!=null||match.world.riderStarts==null||match.world.riderStarts.Length!=4||match.world.riderStarts.Any(t=>t==null||!t.IsChildOf(match.world.transform)))throw new InvalidOperationException("Existing custom start references must be preserved; no automatic regeneration or overwrite.");
            var plan=Measure(match,qualification);var previous=match.world.riderStarts;GameObject group=null;
            var truckState=match.trucks.SelectMany(t=>t.GetComponentsInChildren<Component>(true)).Where(c=>c is Transform||c is Rigidbody||c is Collider||(c!=null&&c.GetType().Namespace!=null&&c.GetType().Namespace.StartsWith("NWH",StringComparison.Ordinal))).Distinct().ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));

            try
            {
                group=new GameObject(OwnedGroup);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(group,match.gameObject.scene);group.transform.SetParent(match.transform,false);
                var starts=plan.starts.Select((pose,i)=>{var go=new GameObject("Qualified Rider start "+(i+1));go.transform.SetParent(group.transform,false);go.transform.SetPositionAndRotation(pose.position,pose.rotation);return go.transform;}).ToArray();
                match.world.riderStarts=starts;PrefabUtility.RecordPrefabInstancePropertyModifications(match.world);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
                if(!EditorSceneManager.SaveScene(match.gameObject.scene))throw new InvalidOperationException("Could not save the explicit review scene.");plan.savedReviewScene=true;
                plan.sharedWorldAssetExact=Hash(plan.worldPrefab)==qualification.worldPrefabSha256;plan.originalTruckStateExact=truckState.All(row=>row.Key!=null&&EditorJsonUtility.ToJson(row.Key)==row.Value);plan.sourceFilesExact=CurrentSourcesExact(qualification.qualifiedSource.currentSourceSnapshot,path);
                if(!plan.sharedWorldAssetExact||!plan.originalTruckStateExact||!plan.sourceFilesExact)throw new InvalidOperationException("Review-scene start repair changed original source or NWH state; retain the failed candidate and investigate.");return plan;
            }
            catch
            {
                match.world.riderStarts=previous;PrefabUtility.RecordPrefabInstancePropertyModifications(match.world);if(group!=null)UnityEngine.Object.DestroyImmediate(group);throw;
            }
        }
        static bool CurrentSourcesExact(DragonRiderSaddleAuthoring.QualifiedCourseSourceFile[] rows,string changedScene)
        {
            string allowed=changedScene==null?null:Path.GetFullPath(changedScene);
            foreach(var row in rows)
            {
                if(string.IsNullOrEmpty(row.path)||string.IsNullOrEmpty(row.sha256))return false;
                if(allowed!=null&&string.Equals(Path.GetFullPath(row.path),allowed,StringComparison.OrdinalIgnoreCase))continue;
                if(!string.Equals(Hash(row.path),row.sha256.Replace("-",""),StringComparison.OrdinalIgnoreCase))return false;
            }
            return true;
        }
        static string Hash(string path)
        {using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(@"\\?\"+Path.GetFullPath(path)))).Replace("-","").ToLowerInvariant();}
    }
}
#endif
