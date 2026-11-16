#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class RejectedDiagnosticFile{public string path,sha256;}
        [Serializable]public sealed class RejectedWorldDiagnostic
        {
            public string scope="EXPLICIT_REJECTED_CURRENT_WORLD_DIAGNOSTIC_ONLY_NOT_FINISHED_WORLD_ADOPTION_OR_GAMEPLAY_ACCEPTANCE",status="NOT_CAPTURED",error,tag,folder,prefab,definition,graphJsonSha256,definitionJsonSha256,criticalSha256,criticalReloadedSha256,livePhysicsBefore,livePhysicsAfter;
            public bool authorizesAdoption,diagnosticReadable,savedReloaded,geometryExact,providersExact,rendererBindingsExact,worldConfigurationExact,liveStateRestored,diggerFilesExact,sourceBytesPreserved,mainSourceAuditRecorded;
            public DragonKingdomRoadSurfaceProbe.RejectedWorldSignature before,reloaded;public RejectedDiagnosticFile[] sourceFiles,snapshotFiles,diggerFiles;public ArtistMarket artist;
        }
        [Serializable]public sealed class RejectedArtistProbe
        {public string scope="READ_ONLY_REJECTED_CURRENT_WORLD_ORIGINAL61_ARTIST_NATIVE256_TRIAL_DIAGNOSIS_NOT_WORLD_ACCEPTANCE",status="IN_PROGRESS",error,cleanupError,diagnosticReceipt,diagnosticReceiptSha256;public ArtistMarket placement;public bool placementPassed,originalGroupPosesRestored,sourceFilesExact,sceneContextRestored;}
        static bool RejectedDiagnosticRequested(string[] args)=>args.Contains("-tsDragonRetainRejectedWorld");
        static string RejectedWorldObjectPath(Transform value,Transform root)
        {if(value==null)return "<missing>";var parts=new List<string>();for(var t=value;t!=null;t=t.parent){parts.Add(t==root?"@":t.name+"["+t.GetSiblingIndex()+"]");if(t==root)break;}parts.Reverse();return string.Join("/",parts);}
        static string RejectedDiagnosticFolder(string tag,string candidate)
        {
            if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_')||candidate!=Root+"VisualCandidates/"+tag)throw new InvalidOperationException("Exact owned current-candidate folder required; canonical/user paths cannot receive rejected diagnostics.");return candidate+"/RJ";
        }
        static string RejectedPhysical(string path)
        {
            if(path.StartsWith("Packages/",StringComparison.Ordinal))
            {
                var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);if(package==null||!path.StartsWith("Packages/"+package.name+"/",StringComparison.Ordinal)||path.Split('/').Contains(".."))throw new InvalidOperationException("Exact public package source registration required.");path=Path.Combine(package.resolvedPath,path.Substring(("Packages/"+package.name+"/").Length));
            }
            return Io(path);
        }
        static string RejectedHash(string path)=>Hash(File.ReadAllBytes(RejectedPhysical(path))).Replace("-","").ToLowerInvariant();
        static RejectedDiagnosticFile[] RejectedFiles(IEnumerable<string> paths)=>paths.Distinct(StringComparer.Ordinal).OrderBy(p=>p,StringComparer.Ordinal).Select(p=>new RejectedDiagnosticFile{path=p,sha256=RejectedHash(p)}).ToArray();
        static bool RejectedFilesExact(RejectedDiagnosticFile[] files)=>files!=null&&files.All(f=>File.Exists(RejectedPhysical(f.path))&&string.Equals(RejectedHash(f.path),f.sha256,StringComparison.OrdinalIgnoreCase));
        static string RejectedTextHash(string text)=>Hash(Encoding.UTF8.GetBytes(text)).Replace("-","").ToLowerInvariant();
        static string RejectedCriticalHash(DragonKingdomWorld world)=>RejectedTextHash(string.Join("\n",CriticalTransforms(world).Distinct().Select(t=>RejectedWorldObjectPath(t,world.transform)+"|"+Pose(t))));
        static string[] RejectedDiggerMembers(string folder)
        {
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));return Directory.GetFiles(Io(folder),"*",SearchOption.AllDirectories).Select(p=>{if(p.StartsWith(@"\\?\",StringComparison.Ordinal))p=p.Substring(4);string relative=Path.GetRelativePath(project,Path.GetFullPath(p)).Replace('\\','/');if(!relative.StartsWith(folder+"/",StringComparison.Ordinal))throw new InvalidOperationException("Digger diagnostic membership escaped its exact owned folder.");return relative;}).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        }
        static void WriteRejectedDiagnostic(RejectedWorldDiagnostic proof)
        {if(!string.IsNullOrEmpty(proof.folder)&&Directory.Exists(Io(proof.folder)))File.WriteAllText(Io(proof.folder+"/Rejected Diagnostic.json"),JsonUtility.ToJson(proof,true));}
        static RejectedWorldDiagnostic CaptureRejectedCurrentWorld(DragonKingdomWorld world,string candidate,Receipt receipt,Dictionary<string,string> protectedFiles,Dictionary<Transform,string> critical,string expectedGraph)
        {
            var proof=new RejectedWorldDiagnostic{tag=receipt.tag,artist=receipt.finish?.artistMarket};DragonKingdomDefinition original=null,copy=null;GameObject reloaded=null;string definitionJson=null;bool bound=false;
            try
            {
                proof.folder=RejectedDiagnosticFolder(receipt.tag,candidate);proof.prefab=proof.folder+"/Shared Dragon Kingdom.prefab";proof.definition=proof.folder+"/Dragon Kingdom.asset";
                if(receipt.status!="REJECTED_CANDIDATE"||world==null||EditorUtility.IsPersistent(world)||!world.gameObject.scene.IsValid()||world.definition==null||receipt.cavern?.status!="MEASURED_CAVERN_REQUIRES_FULL_WORLD_GATES_AND_RENDER_REVIEW"||receipt.cavern.dataFolder!="Assets/DiggerData/Scenes/DK-"+receipt.tag||receipt.currentFinalTerrainRecipe?.status!="CURRENT_WORLD_VENDOR_RECIPE_PASSED_REQUIRES_ORIGINAL_ROOF_DIGGER_AND_FULL_WORLD_ACCEPTANCE"||DragonKingdomDiggerCavernAuthoring.IsBusy)throw new InvalidOperationException("Only an incomplete current-world scene AFTER qualified recipe/completed Digger jobs can be retained; this is never acceptance.");
                if(Directory.Exists(Io(proof.folder))||File.Exists(Io(proof.folder+".meta")))throw new InvalidOperationException("Preserve prior rejected diagnostic; no overwrite.");
                if(critical==null||critical.Any(p=>p.Key==null||Pose(p.Key)!=p.Value)||JsonUtility.ToJson(world.definition.graph)!=expectedGraph)throw new InvalidOperationException("Cannot retain a diagnostic as source-exact when original critical/graph poses changed.");
                original=world.definition;definitionJson=JsonUtility.ToJson(original);proof.definitionJsonSha256=RejectedTextHash(definitionJson);proof.graphJsonSha256=RejectedTextHash(expectedGraph);proof.criticalSha256=RejectedCriticalHash(world);proof.livePhysicsBefore=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);
                proof.sourceFiles=protectedFiles.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new RejectedDiagnosticFile{path=p.Key,sha256=p.Value.Replace("-","").ToLowerInvariant()}).ToArray();
                Directory.CreateDirectory(Io(proof.folder));AssetDatabase.Refresh();
                // Persist ONLY current-owned dependencies. Never SaveAssets:
                // other dirty source/vendor/user assets are not ours to save.
                foreach(var dependency in EditorUtility.CollectDependencies(new UnityEngine.Object[]{world.gameObject}).Where(o=>o!=null&&AssetDatabase.Contains(o)))
                {string path=AssetDatabase.GetAssetPath(dependency);if(path.StartsWith(candidate+"/",StringComparison.Ordinal)||path.StartsWith(receipt.cavern.dataFolder+"/",StringComparison.Ordinal))AssetDatabase.SaveAssetIfDirty(dependency);}
                proof.diggerFiles=RejectedFiles(RejectedDiggerMembers(receipt.cavern.dataFolder));
                copy=UnityEngine.Object.Instantiate(original);copy.name=original.name;if(JsonUtility.ToJson(copy)!=definitionJson)throw new InvalidOperationException("Rejected definition copy differs from actual current rules/graph.");AssetDatabase.CreateAsset(copy,proof.definition);copy.name=original.name;EditorUtility.SetDirty(copy);AssetDatabase.SaveAssetIfDirty(copy);if(JsonUtility.ToJson(copy)!=definitionJson)throw new InvalidOperationException("Saved private definition differs from actual current definition.");world.definition=copy;bound=true;
                proof.before=DragonKingdomRoadSurfaceProbe.DescribeRejectedWorldWithoutMutation(world);
                if(PrefabUtility.SaveAsPrefabAsset(world.gameObject,proof.prefab)==null)throw new InvalidOperationException("Actual rejected world prefab did not save.");
                reloaded=PrefabUtility.LoadPrefabContents(proof.prefab);var saved=reloaded.GetComponent<DragonKingdomWorld>();if(saved==null||saved.definition!=copy||saved.GetComponentsInChildren<Component>(true).Any(c=>c==null))throw new InvalidOperationException("Rejected snapshot lost current definition/source scripts.");
                proof.reloaded=DragonKingdomRoadSurfaceProbe.DescribeRejectedWorldWithoutMutation(saved);proof.geometryExact=proof.before.geometry==proof.reloaded.geometry;proof.providersExact=proof.before.providers==proof.reloaded.providers;proof.rendererBindingsExact=proof.before.rendererBindings==proof.reloaded.rendererBindings;proof.worldConfigurationExact=proof.before.worldConfiguration==proof.reloaded.worldConfiguration;proof.criticalReloadedSha256=RejectedCriticalHash(saved);
                proof.savedReloaded=proof.geometryExact&&proof.providersExact&&proof.rendererBindingsExact&&proof.worldConfigurationExact&&proof.criticalReloadedSha256==proof.criticalSha256&&RejectedTextHash(JsonUtility.ToJson(saved.definition))==proof.definitionJsonSha256;
                if(!proof.savedReloaded)throw new InvalidOperationException("Rejected saved/reloaded world differs from actual live geometry/providers/critical/world state; not a usable diagnostic.");
                proof.snapshotFiles=RejectedFiles(AssetDatabase.GetDependencies(proof.prefab,true).Concat(new[]{proof.prefab,proof.definition}).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(RejectedPhysical(p))));
                proof.diggerFilesExact=proof.diggerFiles.Select(p=>p.path).SequenceEqual(RejectedDiggerMembers(receipt.cavern.dataFolder))&&RejectedFilesExact(proof.diggerFiles);if(!proof.diggerFilesExact)throw new InvalidOperationException("Rejected save changed actual completed Digger data/mesh membership.");proof.status="REJECTED_SNAPSHOT_SAVED_PENDING_EXISTING_FINAL_SOURCE_AUDIT";
            }
            catch(Exception error){proof.error=error.ToString();proof.status="REJECTED_DIAGNOSTIC_CAPTURE_FAILED_OR_UNQUALIFIED";}
            finally
            {
                try{if(reloaded!=null)PrefabUtility.UnloadPrefabContents(reloaded);if(bound&&world!=null)world.definition=original;if(copy!=null&&!AssetDatabase.Contains(copy))UnityEngine.Object.DestroyImmediate(copy);proof.livePhysicsAfter=world==null?null:DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);proof.liveStateRestored=original!=null&&world!=null&&world.definition==original&&JsonUtility.ToJson(original)==definitionJson&&JsonUtility.ToJson(world.definition.graph)==expectedGraph&&critical!=null&&critical.All(p=>p.Key!=null&&Pose(p.Key)==p.Value)&&proof.livePhysicsBefore==proof.livePhysicsAfter;}
                catch(Exception error){proof.error+="\nCleanup: "+error;proof.liveStateRestored=false;}
                // Never throw a new snapshot exception over the actual build
                // failure. The unchanged Main source audit still runs next.
                proof.authorizesAdoption=false;proof.diagnosticReadable=false;try{WriteRejectedDiagnostic(proof);}catch(Exception error){proof.error+="\nDiagnostic receipt: "+error;}
            }
            return proof;
        }
        static void CompleteRejectedDiagnosticSourceAudit(Receipt receipt)
        {
            var p=receipt.rejectedDiagnostic;if(p==null)return;p.sourceBytesPreserved=receipt.sourceBytesPreserved;p.mainSourceAuditRecorded=true;p.diagnosticReadable=p.savedReloaded&&p.geometryExact&&p.providersExact&&p.rendererBindingsExact&&p.worldConfigurationExact&&p.liveStateRestored&&p.diggerFilesExact&&p.sourceBytesPreserved&&string.IsNullOrEmpty(p.error);p.status=p.diagnosticReadable?"REJECTED_CURRENT_WORLD_SNAPSHOT_READABLE_ONLY_FOR_DIAGNOSIS_NOT_ADOPTION":"REJECTED_DIAGNOSTIC_UNQUALIFIED";p.authorizesAdoption=false;
            try{WriteRejectedDiagnostic(p);}catch(Exception error){p.error+="\nFinal diagnostic receipt: "+error;p.diagnosticReadable=false;p.status="REJECTED_DIAGNOSTIC_UNQUALIFIED";}
        }
        static void RequireRejectedDiagnosticHeader(RejectedWorldDiagnostic p)
        {
            if(p==null||p.authorizesAdoption||!p.diagnosticReadable||p.status!="REJECTED_CURRENT_WORLD_SNAPSHOT_READABLE_ONLY_FOR_DIAGNOSIS_NOT_ADOPTION"||!string.IsNullOrEmpty(p.error)||!p.savedReloaded||!p.geometryExact||!p.providersExact||!p.rendererBindingsExact||!p.worldConfigurationExact||!p.liveStateRestored||!p.diggerFilesExact||!p.sourceBytesPreserved||!p.mainSourceAuditRecorded||p.sourceFiles==null||p.sourceFiles.Length==0||p.snapshotFiles==null||p.snapshotFiles.Length==0||p.diggerFiles==null||p.diggerFiles.Length==0||string.IsNullOrEmpty(p.criticalSha256)||p.criticalSha256!=p.criticalReloadedSha256||p.before==null||p.reloaded==null||p.before.meshReferences<=0||p.before.terrains<4||p.before.colliders<=0||JsonUtility.ToJson(p.before)!=JsonUtility.ToJson(p.reloaded))throw new InvalidOperationException("An explicitly rejected, source-closed real snapshot is required for diagnosis; it never authorizes adoption.");
            string folder=RejectedDiagnosticFolder(p.tag,Root+"VisualCandidates/"+p.tag);if(p.folder!=folder||p.prefab!=folder+"/Shared Dragon Kingdom.prefab"||p.definition!=folder+"/Dragon Kingdom.asset")throw new InvalidOperationException("Rejected snapshot path/owner identity changed.");
        }
        public static RejectedArtistProbe CaptureRetainedRejectedArtistPlacement()
        {
            string path=Argument("-tsDragonRejectedWorldDiagnostic",null);if(string.IsNullOrEmpty(path)||!File.Exists(RejectedPhysical(path)))throw new InvalidOperationException("Explicit actual rejected diagnostic receipt required; no regeneration or old-world substitute.");
            var snapshot=JsonUtility.FromJson<RejectedWorldDiagnostic>(File.ReadAllText(RejectedPhysical(path)));RequireRejectedDiagnosticHeader(snapshot);if(Path.GetFullPath(path)!=Path.GetFullPath(snapshot.folder+"/Rejected Diagnostic.json"))throw new InvalidOperationException("Rejected receipt owner path differs.");
            string integration=Root+"VisualCandidates/"+snapshot.tag+"/Integration Receipt.json";var parent=JsonUtility.FromJson<Receipt>(File.ReadAllText(RejectedPhysical(integration)));
            if(parent.status!="REJECTED_CANDIDATE"||!parent.sourceBytesPreserved||parent.originalPhysicalGatesPassed||parent.tag!=snapshot.tag||parent.rejectedDiagnostic==null||JsonUtility.ToJson(parent.rejectedDiagnostic)!=JsonUtility.ToJson(snapshot))throw new InvalidOperationException("Exact genuinely rejected Main receipt/source-audit link required; no acceptance or standalone metadata substitute.");
            var watch=snapshot.sourceFiles.Concat(snapshot.snapshotFiles).Concat(snapshot.diggerFiles).Concat(RejectedFiles(new[]{path,integration})).GroupBy(f=>f.path,StringComparer.Ordinal).Select(g=>{if(g.Select(f=>f.sha256).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=1)throw new InvalidOperationException("Contradictory rejected source inventory.");return g.First();}).ToArray();
            if(!RejectedFilesExact(watch)||!snapshot.diggerFiles.Select(f=>f.path).SequenceEqual(RejectedDiggerMembers(parent.cavern.dataFolder)))throw new InvalidOperationException("Rejected native source/Digger/terrain bytes or membership are stale.");
            if(Terrain.activeTerrains.Length!=0)throw new InvalidOperationException("Close unrelated active terrain scenes before global PhysX diagnosis; never close user scenes automatically.");
            var prior=PrepareValidQuayProbeSceneSetup();var active=SceneManager.GetActiveScene();string Context()=>string.Join("|",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>{var scene=SceneManager.GetSceneAt(i);return scene.handle+":"+scene.path+":"+scene.isLoaded+":"+scene.isDirty;}));string context=Context();Scene sceneOwned=default;GameObject loaded=null;Transform market=null;Vector3 position=default,scale=default;Quaternion rotation=default;bool poseCaptured=false;DragonKingdomRoadSurfaceProbe.RejectedWorldSignature original=null;var proof=new RejectedArtistProbe{diagnosticReceipt=path,diagnosticReceiptSha256=RejectedHash(path)};
            try
            {
                sceneOwned=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);loaded=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(snapshot.prefab),sceneOwned);var world=loaded.GetComponent<DragonKingdomWorld>();Physics.SyncTransforms();original=DragonKingdomRoadSurfaceProbe.DescribeRejectedWorldWithoutMutation(world);
                if(JsonUtility.ToJson(original)!=JsonUtility.ToJson(snapshot.reloaded)||RejectedCriticalHash(world)!=snapshot.criticalSha256||RejectedTextHash(JsonUtility.ToJson(world.definition.graph))!=snapshot.graphJsonSha256)throw new InvalidOperationException("Actual registered rejected clone differs from the preserved current world before probing.");
                market=world.Region("village").root.Cast<Transform>().Single(t=>t.name==ArtistMarketName);if(market.childCount!=61||market.localScale!=Vector3.one*1.75f||Quaternion.Angle(market.rotation,Quaternion.Euler(0,180,0))>.001f||snapshot.artist?.instances!=61||snapshot.artist.prefabs!=28||snapshot.artist.children?.Length!=61)throw new InvalidOperationException("Exact retained whole61-instance/28-prefab artist hierarchy required.");
                var layout=ReadArtistLayout();for(int i=0;i<61;i++){var child=market.GetChild(i);var row=layout.instances[i];if(child.name!=snapshot.artist.children[i].name||child.localPosition!=row.position||child.localRotation!=row.rotation||child.localScale!=row.scale)throw new InvalidOperationException("Retained source artist child TRS/identity changed: "+i);}
                position=market.localPosition;rotation=market.localRotation;scale=market.localScale;poseCaptured=true;proof.placement=JsonUtility.FromJson<ArtistMarket>(JsonUtility.ToJson(snapshot.artist));
                try{PlaceActualArtistMarket(world,market,proof.placement);proof.placementPassed=true;proof.status="ORIGINAL256_WHOLE_GROUP_NATIVE_PLACEMENT_PASSED_ON_REJECTED_WORLD_DIAGNOSIS_ONLY";}
                catch(Exception error){proof.error=error.ToString();proof.status="ORIGINAL256_WHOLE_GROUP_NATIVE_REJECTION_REASONS_RECORDED";}
                finally{market.SetLocalPositionAndRotation(position,rotation);market.localScale=scale;Physics.SyncTransforms();proof.originalGroupPosesRestored=JsonUtility.ToJson(original)==JsonUtility.ToJson(DragonKingdomRoadSurfaceProbe.DescribeRejectedWorldWithoutMutation(world));}
            }
            catch(Exception error){proof.error+="\n"+error;proof.status="REJECTED_DIAGNOSTIC_PROBE_SOURCE_OR_CONTEXT_FAILURE";throw;}
            finally
            {
                try{if(loaded!=null)UnityEngine.Object.DestroyImmediate(loaded);if(sceneOwned.IsValid()&&sceneOwned.isLoaded&&!EditorSceneManager.CloseScene(sceneOwned,true))throw new InvalidOperationException("Owned registered diagnostic scene did not close.");if(active.IsValid()&&active.isLoaded)EditorSceneManager.SetActiveScene(active);proof.sceneContextRestored=Context()==context&&EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path).SequenceEqual(prior.Select(s=>s.path));}
                catch(Exception error){proof.cleanupError=error.ToString();}
                proof.sourceFilesExact=RejectedFilesExact(watch)&&snapshot.diggerFiles.Select(f=>f.path).SequenceEqual(RejectedDiggerMembers(parent.cavern.dataFolder));string output=Output();Directory.CreateDirectory(Io(output));File.WriteAllText(Io(Path.Combine(output,"actual-rejected-world-artist-placement.json")),JsonUtility.ToJson(proof,true));
                if(!proof.sourceFilesExact||!proof.sceneContextRestored||!string.IsNullOrEmpty(proof.cleanupError)||poseCaptured&&!proof.originalGroupPosesRestored)throw new InvalidOperationException("Read-only rejected placement probe did not restore all original group/scene/source/data state.");
            }
            return proof;
        }
        [Serializable]public sealed class RejectedRidgeProbe
        {public string scope="ACTUAL_RETAINED_REJECTED172_RIDGE_DIAGNOSIS_NO_REGEN_SAVE_OR_ADOPTION",diagnosticReceipt,error,cleanupError;public bool placementPassed,sourceFilesExact,sceneContextRestored,original172PosesRestored;public string[] changedRestoredLocalPoses;public string originalGeometryHash,restoredGeometryHash;public RidgeComposition placement;}
        public static RejectedRidgeProbe CaptureRetainedRejectedRidgePlacement()
        {
            string path=Argument("-tsDragonRejectedWorldDiagnostic",null);if(string.IsNullOrEmpty(path)||!File.Exists(RejectedPhysical(path)))throw new InvalidOperationException("Explicit actual rejected snapshot required.");
            var snapshot=JsonUtility.FromJson<RejectedWorldDiagnostic>(File.ReadAllText(RejectedPhysical(path)));RequireRejectedDiagnosticHeader(snapshot);if(Path.GetFullPath(path)!=Path.GetFullPath(snapshot.folder+"/Rejected Diagnostic.json"))throw new InvalidOperationException("Rejected owner path differs.");
            string integration=Root+"VisualCandidates/"+snapshot.tag+"/Integration Receipt.json";var parent=JsonUtility.FromJson<Receipt>(File.ReadAllText(RejectedPhysical(integration)));
            if(parent.status!="REJECTED_CANDIDATE"||!parent.sourceBytesPreserved||parent.originalPhysicalGatesPassed||parent.tag!=snapshot.tag||JsonUtility.ToJson(parent.rejectedDiagnostic)!=JsonUtility.ToJson(snapshot))throw new InvalidOperationException("Exact rejected Main/source link required.");
            var watch=snapshot.sourceFiles.Concat(snapshot.snapshotFiles).Concat(snapshot.diggerFiles).Concat(RejectedFiles(new[]{path,integration})).GroupBy(f=>f.path,StringComparer.Ordinal).Select(g=>{if(g.Select(f=>f.sha256).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=1)throw new InvalidOperationException("Conflicting rejected source hashes.");return g.First();}).ToArray();
            if(!RejectedFilesExact(watch)||!snapshot.diggerFiles.Select(f=>f.path).SequenceEqual(RejectedDiggerMembers(parent.cavern.dataFolder)))throw new InvalidOperationException("Retained source/terrain/Digger bytes or membership changed.");
            if(Terrain.activeTerrains.Length!=0)throw new InvalidOperationException("Preserve unrelated active terrain scenes; no global PhysX probe alongside them.");
            var prior=PrepareValidQuayProbeSceneSetup();var active=SceneManager.GetActiveScene();Scene scene=default;GameObject loaded=null;DragonKingdomRoadSurfaceProbe.RejectedWorldSignature original=null;var positions=new Dictionary<Transform,Vector3>();var rotations=new Dictionary<Transform,Quaternion>();var scales=new Dictionary<Transform,Vector3>();var proof=new RejectedRidgeProbe{diagnosticReceipt=path};
            try
            {
                scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);loaded=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(snapshot.prefab),scene);var world=loaded.GetComponent<DragonKingdomWorld>();Physics.SyncTransforms();original=DragonKingdomRoadSurfaceProbe.DescribeRejectedWorldWithoutMutation(world);
                if(JsonUtility.ToJson(original)!=JsonUtility.ToJson(snapshot.reloaded)||RejectedCriticalHash(world)!=snapshot.criticalSha256||RejectedTextHash(JsonUtility.ToJson(world.definition.graph))!=snapshot.graphJsonSha256)throw new InvalidOperationException("Retained clone geometry/critical/graph differs before diagnosis.");
                var records=parent.placements.Where(p=>p.name.StartsWith("Owned alpine shelter geology ",StringComparison.Ordinal)||p.name.StartsWith("Owned living cliff roost tree ",StringComparison.Ordinal)||p.name.StartsWith("Owned forest underbrush ",StringComparison.Ordinal)).GroupBy(p=>p.region+"/"+p.name,StringComparer.Ordinal).Select(g=>g.First()).ToList();
                foreach(var record in records){var t=world.Region(record.region).root.GetComponentsInChildren<Transform>(true).Single(child=>child.name==record.name);positions.Add(t,t.localPosition);rotations.Add(t,t.localRotation);scales.Add(t,t.localScale);}if(positions.Count!=172)throw new InvalidOperationException("Exactly the original172 ridge roots required.");
                try{proof.placement=RecomposeExistingRidgeArt(world,Root+"VisualCandidates/ReadonlyRejectedRidgeProbe",records,true);proof.placementPassed=proof.placement.passed;}
                catch(Exception error){proof.error=error.ToString();proof.placement=LastRidgeComposition;}
                finally{foreach(var pair in positions)if(!pair.Key.localPosition.Equals(pair.Value))pair.Key.localPosition=pair.Value;Physics.SyncTransforms();proof.changedRestoredLocalPoses=positions.Where(p=>!p.Key.localPosition.Equals(p.Value)||!p.Key.localRotation.Equals(rotations[p.Key])||!p.Key.localScale.Equals(scales[p.Key])).Select(p=>p.Key.name).ToArray();proof.originalGeometryHash=RejectedTextHash(JsonUtility.ToJson(original));proof.restoredGeometryHash=RejectedTextHash(JsonUtility.ToJson(DragonKingdomRoadSurfaceProbe.DescribeRejectedWorldWithoutMutation(world)));proof.original172PosesRestored=positions.Count==172&&proof.changedRestoredLocalPoses.Length==0&&proof.originalGeometryHash==proof.restoredGeometryHash;}
            }
            finally
            {
                try{if(loaded!=null)UnityEngine.Object.DestroyImmediate(loaded);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(active.IsValid()&&active.isLoaded)EditorSceneManager.SetActiveScene(active);proof.sceneContextRestored=EditorSceneManager.GetSceneManagerSetup().Select(x=>x.path).SequenceEqual(prior.Select(x=>x.path));}catch(Exception error){proof.cleanupError=error.ToString();}
                proof.sourceFilesExact=RejectedFilesExact(watch)&&snapshot.diggerFiles.Select(f=>f.path).SequenceEqual(RejectedDiggerMembers(parent.cavern.dataFolder));Directory.CreateDirectory(Io(Output()));File.WriteAllText(Io(Path.Combine(Output(),"actual-rejected-world-ridge-placement.json")),JsonUtility.ToJson(proof,true));
                if(!proof.original172PosesRestored||!proof.sourceFilesExact||!proof.sceneContextRestored||!string.IsNullOrEmpty(proof.cleanupError))throw new InvalidOperationException("Rejected ridge diagnosis failed exact pose/physics/source/context restoration.");
            }
            return proof;
        }
        [Serializable]public sealed class RejectedNaturalGroveProbe
        {
            public string scope="ACTUAL_RETAINED_WORLD_TWO_SOURCE_AUTHORED_NATURAL_GROVES_NO_REGEN_MATERIAL_WRITES_SAVE_OR_ADOPTION",diagnosticReceipt,cleanupError;
            public bool allGroupsPassed,originalWorldRestored,sourceFilesExact,sceneContextRestored;public DragonKingdomDiggerCavernAuthoring.NativeDoorwayClearance nominalDoorwayClearance;public WaterfrontBearingDiagnostic[] waterfrontBearings;public FinishedWaterfront waterfrontSupports;public CurrentCoastCopyProbe coastCopies;public NativeCoreContactProbe finalArchitectureContacts;public AuthoredGroup[] groups;public string[] failures;
        }
        public static RejectedNaturalGroveProbe CaptureRetainedRejectedNaturalGroves()=>CaptureRetainedRejectedNaturalGrovesCore(false,false,false);
        public static RejectedNaturalGroveProbe CaptureRetainedRejectedWaterfrontSupport()=>CaptureRetainedRejectedNaturalGrovesCore(true,false,false);
        public static RejectedNaturalGroveProbe CaptureRetainedRejectedCoastCopies()=>CaptureRetainedRejectedNaturalGrovesCore(false,true,false);
        public static RejectedNaturalGroveProbe CaptureRetainedRejectedFinalArchitectureContacts()=>CaptureRetainedRejectedNaturalGrovesCore(false,false,true);
        static RejectedNaturalGroveProbe CaptureRetainedRejectedNaturalGrovesCore(bool completeWaterfrontSupport,bool completeCoastCopies,bool finalArchitectureContacts)
        {
            if(completeWaterfrontSupport&&!Environment.GetCommandLineArgs().Contains("-tsDragonWaterfrontSupportDiagnostic"))throw new InvalidOperationException("Explicit temporary waterfront support measurement required.");
            if(completeCoastCopies&&!Environment.GetCommandLineArgs().Contains("-tsDragonCoastCoordDiagnostic"))throw new InvalidOperationException("Explicit owned-copy current coast measurement required.");
            if(finalArchitectureContacts&&!Environment.GetCommandLineArgs().Contains("-tsDragonFinalArchitectureDiagnostic"))throw new InvalidOperationException("Explicit actual saved architecture contact measurement required.");
            string path=Argument("-tsDragonRejectedWorldDiagnostic",null);if(string.IsNullOrEmpty(path)||!File.Exists(RejectedPhysical(path)))throw new InvalidOperationException("Explicit retained rejected world required.");
            var snapshot=JsonUtility.FromJson<RejectedWorldDiagnostic>(File.ReadAllText(RejectedPhysical(path)));RequireRejectedDiagnosticHeader(snapshot);
            if(Path.GetFullPath(path)!=Path.GetFullPath(snapshot.folder+"/Rejected Diagnostic.json"))throw new InvalidOperationException("Exact rejected snapshot owner required.");
            string integration=Root+"VisualCandidates/"+snapshot.tag+"/Integration Receipt.json";var parent=JsonUtility.FromJson<Receipt>(File.ReadAllText(RejectedPhysical(integration)));
            if(parent.status!="REJECTED_CANDIDATE"||!parent.sourceBytesPreserved||parent.originalPhysicalGatesPassed||JsonUtility.ToJson(parent.rejectedDiagnostic)!=JsonUtility.ToJson(snapshot))throw new InvalidOperationException("Actual rejected Main source link required.");
            var watch=snapshot.sourceFiles.Concat(snapshot.snapshotFiles).Concat(snapshot.diggerFiles).Concat(RejectedFiles(new[]{path,integration})).GroupBy(f=>f.path,StringComparer.Ordinal).Select(g=>{if(g.Select(f=>f.sha256).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=1)throw new InvalidOperationException("Conflicting retained source hashes.");return g.First();}).ToArray();
            if(!RejectedFilesExact(watch)||!snapshot.diggerFiles.Select(f=>f.path).SequenceEqual(RejectedDiggerMembers(parent.cavern.dataFolder))||Terrain.activeTerrains.Length!=0)throw new InvalidOperationException("Exact source/data closure and isolated native Terrain context required.");
            var prior=PrepareValidQuayProbeSceneSetup();var active=SceneManager.GetActiveScene();Scene scene=default;GameObject loaded=null;var previousLast=LastAuthoredGroups;var originalActive=new Dictionary<GameObject,bool>();var temporary=new List<GameObject>();var groups=new List<AuthoredGroup>();var failures=new List<string>();var proof=new RejectedNaturalGroveProbe{diagnosticReceipt=path};
            if(completeCoastCopies)proof.scope="ACTUAL_SOURCE_CLOSED_CURRENT_CORE_COAST_12_FRESH_OWNED_DATA_COPIES_AND_NATIVE_CONTACTS_ORIGINAL_WORLD_AND_ASSETS_RESTORED_NOT_ADOPTION";else if(completeWaterfrontSupport)proof.scope="ACTUAL_SOURCE_CLOSED_TEMPORARY_WATERFRONT_SUPPORT_GEOMETRY_NO_ASSET_SAVE_OR_ADOPTION";
            if(finalArchitectureContacts)proof.scope="ACTUAL_SOURCE_CLOSED_SIXTEEN_TERRAIN_WORLD_FOUR_CORE_SELECTION_AND_ALL525_NATIVE_ARCHITECTURE_CONTACTS_NO_WRITES_OR_ADOPTION";
            try
            {
                scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);loaded=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(snapshot.prefab),scene);var world=loaded.GetComponent<DragonKingdomWorld>();Physics.SyncTransforms();var original=DragonKingdomRoadSurfaceProbe.DescribeRejectedWorldWithoutMutation(world);
                if(JsonUtility.ToJson(original)!=JsonUtility.ToJson(snapshot.reloaded)||RejectedCriticalHash(world)!=snapshot.criticalSha256)throw new InvalidOperationException("Actual retained world differs before grove measurement.");
                proof.nominalDoorwayClearance=DragonKingdomDiggerCavernAuthoring.MeasureNativeDoorwaySpace(world);
                bool waterfrontSupport=completeWaterfrontSupport;bool waterfrontOnly=waterfrontSupport||Environment.GetCommandLineArgs().Contains("-tsDragonWaterfrontBearingDiagnostic");if(waterfrontOnly)proof.waterfrontBearings=MeasureFinishedWaterfrontBearings(world,parent.finishedArchitecture);
                try
                {
                    if(finalArchitectureContacts)proof.finalArchitectureContacts=MeasureActualRetainedFinalArchitectureContacts(world,Root+"VisualCandidates/"+snapshot.tag,parent);
                    if(completeCoastCopies)
                    {
                        var children=world.transform.Cast<Transform>().Select(t=>t.gameObject).ToArray();try{proof.coastCopies=CaptureActualOwnedCurrentCoast(world);}finally{temporary.AddRange(world.transform.Cast<Transform>().Select(t=>t.gameObject).Except(children));}
                    }
                    if(waterfrontSupport)
                    {
                        var harbor=world.Region("harbor").root;var children=harbor.Cast<Transform>().Select(t=>t.gameObject).ToArray();
                        try{proof.waterfrontSupports=CompleteFinishedWaterfrontPhysicalSupport(world,parent.finishedArchitecture,Root+"VisualCandidates/ReadonlyWaterfrontProbe",false);}
                        finally{temporary.AddRange(harbor.Cast<Transform>().Select(t=>t.gameObject).Except(children));}
                    }
                    foreach(var plan in ActualAuthoredGroupPlans().Where(p=>p.natural&&!waterfrontOnly&&!completeCoastCopies&&!finalArchitectureContacts))
                    {
                        var region=world.Region(plan.region).root;var existing=region.Cast<Transform>().SingleOrDefault(t=>t.name==plan.name);
                        if(existing!=null){originalActive.Add(existing.gameObject,existing.gameObject.activeSelf);existing.gameObject.SetActive(false);}
                        var children=region.Cast<Transform>().Select(t=>t.gameObject).ToArray();Physics.SyncTransforms();LastAuthoredGroups=Array.Empty<AuthoredGroup>();
                        try{var result=InstallActualAuthoredGroup(world,Root+"VisualCandidates/ReadonlyNaturalGroveProbe",plan,new LandmarkFinish(),new List<Placement>(),false);groups.Add(result);}
                        catch(Exception error){failures.Add(plan.name+": "+error);var result=(LastAuthoredGroups??Array.Empty<AuthoredGroup>()).SingleOrDefault(g=>g.name==plan.name);if(result!=null)groups.Add(result);}
                        finally{temporary.AddRange(region.Cast<Transform>().Select(t=>t.gameObject).Except(children));}
                    }
                    proof.groups=groups.ToArray();proof.failures=failures.ToArray();proof.allGroupsPassed=groups.Count==2&&groups.All(g=>g.passed&&g.sourceLocalPosesExact&&g.instances==34&&g.parts.Length==34)&&failures.Count==0;
                }
                finally
                {
                    foreach(var go in temporary)if(go!=null)UnityEngine.Object.DestroyImmediate(go);foreach(var pair in originalActive)if(pair.Key!=null&&pair.Key.activeSelf!=pair.Value)pair.Key.SetActive(pair.Value);Physics.SyncTransforms();proof.originalWorldRestored=JsonUtility.ToJson(original)==JsonUtility.ToJson(DragonKingdomRoadSurfaceProbe.DescribeRejectedWorldWithoutMutation(world));
                }
            }
            finally
            {
                LastAuthoredGroups=previousLast;
                try{if(loaded!=null)UnityEngine.Object.DestroyImmediate(loaded);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(active.IsValid()&&active.isLoaded)EditorSceneManager.SetActiveScene(active);proof.sceneContextRestored=EditorSceneManager.GetSceneManagerSetup().Select(x=>x.path).SequenceEqual(prior.Select(x=>x.path));}catch(Exception error){proof.cleanupError=error.ToString();}
                proof.sourceFilesExact=RejectedFilesExact(watch)&&snapshot.diggerFiles.Select(f=>f.path).SequenceEqual(RejectedDiggerMembers(parent.cavern.dataFolder));Directory.CreateDirectory(Io(Output()));File.WriteAllText(Io(Path.Combine(Output(),"actual-retained-natural-groves.json")),JsonUtility.ToJson(proof,true));
                if(!proof.originalWorldRestored||!proof.sourceFilesExact||!proof.sceneContextRestored||!string.IsNullOrEmpty(proof.cleanupError))throw new InvalidOperationException("Retained grove probe failed exact source/world/context restoration.");
            }
            return proof;
        }

    }
}
#endif
