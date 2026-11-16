#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LWS.TruckDragonRiders;
using LWS.TruckDragonRiders.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Applies measured course geometry to the original saved objects only,
    // after the exact reviewed terrain has actually been adopted. No race,
    // passage, lap, score, physics, saddle, or camera authority is replaced.
    public static class DragonKingdomRidersCourseMigration
    {
        [Serializable]public sealed class Receipt
        {
            public string scope="EXPLICIT_EXISTING_TWELVE_GATE_POSE_MIGRATION_NOT_ACTUAL_FLIGHT_OR_RACE_ACCEPTANCE",candidate,status="IN_PROGRESS",scene=DragonRidersAuthoring.ScenePath,error;
            public string sceneGuid,originalSha256,savedSha256,courseEvidenceSha256,adoptionEvidenceSha256,roadAppearanceEvidenceSha256,backup;
            public int gates,originalDocuments,changedDocuments;public bool originalLocalIdsPreserved,allOtherDocumentsExact,matchRulesExact,originalMetaExact;
        }
        public static Receipt LastReceipt{get;private set;}
        public static void ApplyMeasuredCourse()
        {
            string tag=Arg("-tsDragonWorldCandidate",""),coursePath=Arg("-tsDragonMountedCourseReceipt",""),adoptionPath=Arg("-tsDragonWorldAdoptionReceipt",""),appearancePath=Arg("-tsDragonRoadAppearanceReceipt","");
            string output=Arg("-tsDragonOutput","Logs/TruckSports/DragonKingdom/CourseAdoption");Directory.CreateDirectory(Io(output));string report=Path.Combine(output,"saved-riders-course-"+tag+".json"),path=DragonRidersAuthoring.ScenePath;
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||string.IsNullOrEmpty(tag)||string.IsNullOrEmpty(coursePath)||string.IsNullOrEmpty(adoptionPath))throw new InvalidOperationException("Stopped Editor and exact candidate/course/adoption evidence required. Never rebuild the Riders scene.");
            var course=JsonUtility.FromJson<DragonKingdomMountedCourseReview.Receipt>(File.ReadAllText(Io(coursePath)));
            var adoption=JsonUtility.FromJson<DragonKingdomWorldAdoptionPlan.WorldApplyReceipt>(File.ReadAllText(Io(adoptionPath)));
            if(course.candidate!=tag||course.status!="SAMPLED_FULL_MOUNT_ENVELOPE_CLEAR_REQUIRES_ACTUAL_FLIGHT"||!course.coordinatedCourse||!course.sourceBytesPreserved||course.contactQueries!=0||course.apertureFailures!=0||course.courseLayout?.Length!=12||course.rigs?.Length!=8||course.rigs.Any(r=>r.poses<170))throw new InvalidOperationException("The actual complete eight-rig geometry review has not passed.");
            if(adoption.candidate!=tag||adoption.status!="ADOPTED_ORIGINAL_IDENTITIES_AND_MODES_PRESERVED_REQUIRES_ACTUAL_PLAY_ACCEPTANCE"||!adoption.physicalGatesPassed||!adoption.allModeExternalStateAndSceneOwnedIdsPreserved||adoption.mountedCourseReceiptSha256!=HashFile(coursePath))throw new InvalidOperationException("Actual identity-preserving world adoption must match this exact measured course before changing saved gate poses.");
            if(File.Exists(Io(report)))
            {
                var prior=JsonUtility.FromJson<Receipt>(File.ReadAllText(Io(report)));
                if(prior.status=="SAVED_ORIGINAL_GATE_IDENTITIES_REQUIRES_ACTUAL_FLIGHT_AND_REMATCH"&&prior.savedSha256==HashFile(path)&&prior.courseEvidenceSha256==HashFile(coursePath)&&prior.adoptionEvidenceSha256==HashFile(adoptionPath)&&string.Equals(prior.roadAppearanceEvidenceSha256??"",string.IsNullOrEmpty(appearancePath)?"":HashFile(appearancePath),StringComparison.Ordinal)){LastReceipt=prior;return;}
                throw new InvalidOperationException("Preserve previous course evidence; later saved scene changes require explicit review.");
            }
            if(!string.IsNullOrEmpty(appearancePath))
            {
                DragonKingdomRoadSnowAuthoring.ValidateAdoptedSceneAfterAppearanceRefresh(adoptionPath,appearancePath,path);
            }
            else if(adoption.savedFiles.Single(f=>f.path==path).sha256!=HashFile(path))throw new InvalidOperationException("The actual saved Riders scene changed after guarded world adoption. Only the explicit successful -tsDragonRoadAppearanceReceipt may authorize the exact canonical clear-road binding before course migration.");
            for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(scene.isDirty||string.IsNullOrEmpty(scene.path))throw new InvalidOperationException("Save actual user scenes before the isolated explicit course migration.");}
            var setup=EditorSceneManager.GetSceneManagerSetup();byte[] original=File.ReadAllBytes(Io(path));string meta=HashFile(path+".meta");var before=Documents(Encoding.UTF8.GetString(original));bool saved=false,complete=false;
            var receipt=new Receipt{candidate=tag,sceneGuid=AssetDatabase.AssetPathToGUID(path),originalSha256=HashFile(path),courseEvidenceSha256=HashFile(coursePath),adoptionEvidenceSha256=HashFile(adoptionPath),roadAppearanceEvidenceSha256=string.IsNullOrEmpty(appearancePath)?"":HashFile(appearancePath),originalDocuments=before.Count};LastReceipt=receipt;
            receipt.backup=Path.Combine(output,"before-riders-course-"+tag+"-"+Guid.NewGuid().ToString("N")+".unity");File.WriteAllBytes(Io(receipt.backup),original);
            try
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var match=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonRidersMatch>(true)).Single();DragonRidersAuthoring.ValidateScene(match);
                if(DragonKingdomMountedCourseReview.GateHash(match.gates)!=course.originalCourseHash)throw new InvalidOperationException("The saved original ordered gates differ from the actually measured course input.");
                var root=scene.GetRootGameObjects().Single(r=>r.name=="Ordered kingdom aerial gate loop");var gates=root.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.enabled&&!c.isTrigger).OrderBy(c=>int.Parse(c.name.Substring("Aerial gate ".Length))).ToArray();
                if(gates.Length!=12||match.gates.Length!=12||match.trucks.Length!=4)throw new InvalidOperationException("Preserve twelve actual rings and four original configured trucks.");
                foreach(var p in new[]{new Vector3(-1600,-20,-800),new Vector3(0,-20,-800),new Vector3(-1600,-20,800),new Vector3(0,-20,800)})
                {var terrain=match.world.GetComponentsInChildren<Terrain>(true).Single(t=>Vector3.Distance(t.transform.position,p)<.001f);if(!AssetDatabase.GetAssetPath(terrain.terrainData).StartsWith("Assets/TruckDragonKingdom/VisualCandidates/"+tag+"/Terrain/",StringComparison.Ordinal))throw new InvalidOperationException("The original saved mode has not inherited the exact adopted candidate terrain.");}
                var allowed=new HashSet<string>(gates.Select(c=>LocalId(c.transform))){LocalId(match)};string rules=RulesWithoutGatePoses(match);
                for(int i=0;i<gates.Length;i++)
                {if(course.courseLayout[i].clearRadius!=match.gates[i].clearRadius)throw new InvalidOperationException("Measured course cannot change the original passage aperture.");gates[i].transform.SetPositionAndRotation(course.courseLayout[i].center,course.courseLayout[i].rotation);EditorUtility.SetDirty(gates[i].transform);}
                match.gates=course.courseLayout.Select(g=>new DragonRiderGateDefinition{center=g.center,rotation=g.rotation,clearRadius=g.clearRadius}).ToArray();EditorUtility.SetDirty(match);receipt.matchRulesExact=rules==RulesWithoutGatePoses(match);
                if(!receipt.matchRulesExact)throw new InvalidOperationException("Gate geometry migration altered original race rules/session/camera/truck bindings.");
                Physics.SyncTransforms();DragonRidersAuthoring.ValidateScene(match);EditorSceneManager.MarkSceneDirty(scene);saved=true;if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save original ordered course poses.");
                var after=Documents(File.ReadAllText(Io(path)));receipt.gates=gates.Length;receipt.originalLocalIdsPreserved=before.Keys.ToHashSet().SetEquals(after.Keys);receipt.changedDocuments=before.Count(p=>!after.TryGetValue(p.Key,out string value)||p.Value!=value);receipt.allOtherDocumentsExact=before.Where(p=>!allowed.Contains(p.Key)).All(p=>after.TryGetValue(p.Key,out string value)&&value==p.Value);receipt.originalMetaExact=HashFile(path+".meta")==meta&&AssetDatabase.AssetPathToGUID(path)==receipt.sceneGuid;
                if(!receipt.originalLocalIdsPreserved||!receipt.allOtherDocumentsExact||!receipt.originalMetaExact||receipt.changedDocuments>13)throw new InvalidOperationException("Saved course changed objects outside the original match gates field and twelve existing gate transforms.");
                receipt.savedSha256=HashFile(path);receipt.status="SAVED_ORIGINAL_GATE_IDENTITIES_REQUIRES_ACTUAL_FLIGHT_AND_REMATCH";complete=true;
            }
            catch(Exception error){receipt.error=error.ToString();throw;}
            finally
            {
                if(saved&&!complete){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);File.WriteAllBytes(Io(path),original);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);receipt.status="ROLLED_BACK_EXACT_ORIGINAL_RIDERS_SCENE";}
                EditorSceneManager.RestoreSceneManagerSetup(setup);File.WriteAllText(Io(report),JsonUtility.ToJson(receipt,true));
            }
        }
        static string RulesWithoutGatePoses(DragonRidersMatch match)
        {var gates=match.gates;try{match.gates=Array.Empty<DragonRiderGateDefinition>();return EditorJsonUtility.ToJson(match);}finally{match.gates=gates;}}
        static Dictionary<string,string> Documents(string text)
        {text=text.Replace("\r\n","\n");var headers=Regex.Matches(text,@"(?m)^--- !u!\d+ &(-?\d+)(?: stripped)?\n");var result=new Dictionary<string,string>();for(int i=0;i<headers.Count;i++){int end=i+1<headers.Count?headers[i+1].Index:text.Length;result.Add(headers[i].Groups[1].Value,text.Substring(headers[i].Index,end-headers[i].Index));}if(result.Count<100)throw new InvalidOperationException("Actual text-serialized authored Riders scene required for document preservation witness.");return result;}
        static string LocalId(UnityEngine.Object obj)=>GlobalObjectId.GetGlobalObjectIdSlow(obj).targetObjectId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        static string HashFile(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Io(path)))).Replace("-","").ToLowerInvariant();}
        static string Io(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
    }
}
#endif
