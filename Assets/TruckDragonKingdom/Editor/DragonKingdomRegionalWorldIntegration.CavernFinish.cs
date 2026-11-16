#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Digger.Modules.Core.Sources;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class CavernFinishSource
        {public string source,guid,sha256,metaSha256;public int primaryMeshes,primaryVertices,embeddedLights;public string[] meshes,materials;}
        [Serializable]public sealed class CavernFinishAudit
        {public string scope="ACTUAL_OWNED_CAVE_SOURCE_PREFABS_MESHES_MATERIALS_AND_SINGLE_LANTERN_LIGHT_NOT_WORLD_RENDER_ACCEPTANCE";public bool sourceBytesPreserved,sourcesUsable;public CavernFinishSource[] sources;}
        [Serializable]public sealed class CavernFinishPart
        {public string name,source,sourceGuid,sourceSha256,sourceMetaSha256;public Vector3 position,scale;public Quaternion rotation;public Bounds bounds;public Vector3[] actualDiggerFloorContacts;public bool guardianClear,routeStartAnchorClear;public int lights;public RidgeGeometryBinding[] geometry;}
        [Serializable]public sealed class CavernFinishEvidence
        {
            public string scope="EXPLICIT_ISOLATED_CAVERN_SOURCE_ART_AND_ONE_VENDOR_LIGHT_PER_LANTERN_REQUIRES_RENDER_AND_ACTUAL_GUARDIAN_PLAY_ACCEPTANCE",status="IN_PROGRESS",error,sourceOccupancy,sourceOccupancySha256,liveOccupancy,liveOccupancySha256;
            public int crystals,lanterns,rocks,sourceActionBounds,recordedLiveBounds;public float guardianMargin=3;public Bounds conservativeRecordedGuardianEnvelope;
            public bool recordedLiveEvidenceIsPartial=true,sourceBytesPreserved,originalAnchorsGraphAndExistingObjectsPreserved,singleSourceLightPerLantern;
            public bool analyticRockFiveInteriorAnchorUsed;public Vector3 originalRockFiveHint,analyticRockFiveInteriorHint;
            public bool analyticRockSixInteriorAnchorUsed;public Vector3 originalRockSixHint,analyticRockSixInteriorHint;
            public bool analyticRockSevenInteriorAnchorUsed;public Vector3 originalRockSevenHint,analyticRockSevenInteriorHint;
            public ArtistDependency[] sourceDependencies;public CavernFinishPart[] parts;public Placement[] placements;public FinishPlacementFailure[] failures;public string[] changedSources;
        }
        [Serializable]sealed class CavernRecordedLiveBounds{public int sampledFrames,actualFlightFrames;public bool telemetryTruncated,sourceBytesExact;public CavernRecordedLiveFrame[] frames;}
        [Serializable]sealed class CavernRecordedLiveFrame{public Bounds actualGuardianSkin;}

        static string[] CavernFinishSources()=>new[]{"A","C","F","H"}.Select(letter=>DragonKingdomWorldAuthoring.Art+"Tarbo Studio/TARBO-TowerDefensePack/Prefabs/Crystals/TB_Str_Crystal_"+letter+".prefab")
            .Concat(Enumerable.Range(1,4).Select(i=>DragonKingdomWorldAuthoring.Cliff+"RockFormation_"+i.ToString("00")+".prefab"))
            .Concat(new[]{DragonKingdomWorldAuthoring.Village+"props/P_PROP_lantern_04.prefab",DragonKingdomWorldAuthoring.Village+"light/Light_lantern.prefab"}).ToArray();
        public static CavernFinishAudit CaptureCavernFinishSourceAudit()
        {
            var before=CavernFinishSources().SelectMany(p=>AssetDatabase.GetDependencies(p,true)).Distinct().Where(p=>File.Exists(Io(p))).SelectMany(p=>new[]{p,p+".meta"}).Distinct().ToDictionary(p=>p,AssetSha);var rows=new List<CavernFinishSource>();
            foreach(string path in CavernFinishSources())
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Actual owned cavern source is unavailable: "+path);
                var filters=RidgePrimaryMeshFilters(prefab.transform).ToArray();var materials=prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray();int lights=prefab.GetComponentsInChildren<Light>(true).Length;
                bool lightOnly=path.EndsWith("/light/Light_lantern.prefab",StringComparison.Ordinal);if((!lightOnly&&(filters.Length==0||filters.Any(f=>f.sharedMesh==null||f.sharedMesh.vertexCount==0)||materials.Length==0||materials.Any(m=>m==null)))||(path.EndsWith("/props/P_PROP_lantern_04.prefab",StringComparison.Ordinal)||lightOnly)&&lights!=1)throw new InvalidOperationException("Actual source model/material or single source-backed lantern light is incomplete: "+path);
                rows.Add(new CavernFinishSource{source=path,guid=AssetDatabase.AssetPathToGUID(path),sha256=AssetSha(path),metaSha256=AssetSha(path+".meta"),primaryMeshes=filters.Length,primaryVertices=filters.Sum(f=>f.sharedMesh.vertexCount),embeddedLights=lights,meshes=filters.Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).Distinct().ToArray(),materials=materials.Where(m=>m!=null).Select(AssetDatabase.GetAssetPath).Distinct().ToArray()});
            }
            var proof=new CavernFinishAudit{sources=rows.ToArray(),sourcesUsable=true,sourceBytesPreserved=before.All(p=>AssetSha(p.Key)==p.Value)};if(!proof.sourceBytesPreserved)throw new InvalidOperationException("Cavern source audit changed source bytes.");return proof;
        }

        static void InstallOwnedCavernCrystals(DragonKingdomWorld world,string folder,LandmarkFinish result,List<Placement> records,Func<Bounds,bool> additionalClearance=null)
        {
            // The original full-world twelve-crystal recipe, unchanged. The
            // isolated probe supplies its additional measured guardian guard.
            var cavern=world.Region("caverns");string[] crystal={"A","C","F","H"};
            for(int i=0;i<12;i++)
            {float angle=(i+.5f)*Mathf.PI*2/12;var p=new Vector3(520+Mathf.Cos(angle)*72,105,780+Mathf.Sin(angle)*48);string source=DragonKingdomWorldAuthoring.Art+"Tarbo Studio/TARBO-TowerDefensePack/Prefabs/Crystals/TB_Str_Crystal_"+crystal[i%4]+".prefab";FinishPlace(world,folder,cavern,source,p,6+i%4*1.8f,i*90,true,2,result,records,"Owned underground crystal outcrop "+(i+1),false,true,additionalClearance);result.cavernCrystals++;}
        }
        static Bounds RequireRecordedCavernGuardianEnvelope(string sourcePath,string livePath,CavernFinishEvidence proof)
        {
            if(AssetSha(sourcePath)!="e416c05571a5639224e62d5b37b52f84d256e26037b112a3c772fc8598bc7ed3"||AssetSha(livePath)!="3eaa23ac338e7dbd0f28c179ccace11eaad51c8b0adbc3ce4def2b418b5b5cff")throw new InvalidOperationException("Actual inspected original action and recorded live guardian cohort required for additional cavern art clearance.");
            var source=JsonUtility.FromJson<CavernOccupancyEvidence>(File.ReadAllText(Io(sourcePath)));var live=JsonUtility.FromJson<CavernRecordedLiveBounds>(File.ReadAllText(Io(livePath)));
            if(!source.sourceBytesPreserved||!source.originalGuardianMeshMembershipExact||source.actions==null||source.actions.Length!=31||source.protectedSources==null||live.frames==null||live.frames.Length!=2913||live.sampledFrames!=2913||live.actualFlightFrames!=2622||live.telemetryTruncated||!live.sourceBytesExact)throw new InvalidOperationException("Complete untruncated source-action and visited-world skin records required; no empty guardian waiver.");
            foreach(var item in source.protectedSources)if(item.path.StartsWith("Assets/",StringComparison.Ordinal)&&AssetSha(item.path)!=item.sha256)throw new InvalidOperationException("Recorded original guardian source asset changed: "+item.path);
            var bounds=source.actions.Select(a=>a.actualWorldSkinBounds).Concat(live.frames.Select(f=>f.actualGuardianSkin)).ToArray();if(bounds.Any(b=>b.size.x<=0||b.size.y<=0||b.size.z<=0||!ArchitectureFinite(b.center)||!ArchitectureFinite(b.size)))throw new InvalidOperationException("Actual finite nonempty guardian mesh envelopes required.");var union=bounds[0];foreach(var b in bounds.Skip(1))union.Encapsulate(b);
            proof.sourceOccupancy=sourcePath;proof.sourceOccupancySha256=AssetSha(sourcePath);proof.liveOccupancy=livePath;proof.liveOccupancySha256=AssetSha(livePath);proof.sourceActionBounds=31;proof.recordedLiveBounds=2913;proof.conservativeRecordedGuardianEnvelope=union;return union;
        }
        static bool CavernNativeFloorSupport(DragonKingdomWorld world,Bounds bounds,out Vector3[] contacts)
        {
            var points=new List<Vector3>();foreach(float x in new[]{bounds.min.x,bounds.center.x,bounds.max.x})foreach(float z in new[]{bounds.min.z,bounds.center.z,bounds.max.z})
            {
                var hits=Physics.RaycastAll(new Vector3(x,115,z),Vector3.down,16,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.transform.IsChildOf(world.transform)&&h.collider.GetComponentInParent<DiggerSystem>()!=null&&h.normal.y>.4f&&Mathf.Abs(h.point.y-105)<.3f).OrderByDescending(h=>h.point.y).ToArray();if(hits.Length==0||Mathf.Abs(hits[0].point.y-bounds.min.y)>.35f){contacts=points.ToArray();return false;}points.Add(hits[0].point);
            }
            contacts=points.ToArray();return points.Count==9;
        }
        public static CavernFinishEvidence InstallSharedCavernFinish(DragonKingdomWorld world,string folder,string sourceOccupancyPath,string liveOccupancyPath)
        {
            if(world==null||EditorUtility.IsPersistent(world)||!world.gameObject.scene.IsValid()||EditorApplication.isPlayingOrWillChangePlaymode||!folder.StartsWith(Root+"VisualCandidates/",StringComparison.Ordinal)||folder.Substring((Root+"VisualCandidates/").Length).Contains("/"))throw new InvalidOperationException("Actual stopped isolated native Digger candidate and explicit owned candidate folder required.");
            var region=world.Region("caverns");if(region.root.Cast<Transform>().Any(t=>t.name.StartsWith("Owned underground crystal outcrop ",StringComparison.Ordinal)||t.name.StartsWith("Owned working lantern caverns ",StringComparison.Ordinal)||t.name.StartsWith("Owned supported cavern rock ",StringComparison.Ordinal)))throw new InvalidOperationException("Fresh cavern finishing only; never stack duplicate art or lights.");
            CaptureCavernFinishSourceAudit();var proof=new CavernFinishEvidence();var guardian=RequireRecordedCavernGuardianEnvelope(sourceOccupancyPath,liveOccupancyPath,proof);guardian.Expand(proof.guardianMargin*2);
            var sourcePaths=CavernFinishSources().SelectMany(p=>AssetDatabase.GetDependencies(p,true)).Distinct().Where(p=>File.Exists(Io(p))).OrderBy(p=>p,StringComparer.Ordinal).ToArray();proof.sourceDependencies=sourcePaths.Select(p=>new ArtistDependency{path=p,guid=AssetDatabase.AssetPathToGUID(p),sha256=AssetSha(p),metaSha256=AssetSha(p+".meta")}).ToArray();var before=sourcePaths.SelectMany(p=>new[]{p,p+".meta"}).Distinct().ToDictionary(p=>p,AssetSha);
            var existing=world.GetComponentsInChildren<Transform>(true).ToDictionary(t=>t,t=>Pose(t)+"/"+t.gameObject.activeSelf);string graph=JsonUtility.ToJson(world.definition.graph);var anchors=CriticalTransforms(world).Distinct().ToDictionary(t=>t,Pose);var finish=new LandmarkFinish();var records=new List<Placement>();
            bool Clear(Bounds b)=>!guardian.Intersects(b)&&CavernNativeFloorSupport(world,b,out _);
            try
            {
                Directory.CreateDirectory(Io(folder+"/Materials/Finish"));AssetDatabase.Refresh();
                InstallOwnedCavernCrystals(world,folder,finish,records,Clear);
                InstallOwnedLanterns(world,folder,finish,records,new[]{"caverns"},Clear);
                for(int i=0;i<8;i++)
                {
                    float angle=(i+.25f)*Mathf.PI*2/8;var hint=new Vector3(520+Mathf.Cos(angle)*82,105,790+Mathf.Sin(angle)*53);string source=DragonKingdomWorldAuthoring.Cliff+"RockFormation_"+(i%4+1).ToString("00")+".prefab";
                    if(i>=4&&i<=6&&Environment.GetCommandLineArgs().Contains("-tsDragonCavernAnalyticLobes"))
                    {
                        // V372's lower analytic lobes change the floor at the
                        // old west edge and the southern lobe crossover. The
                        // measured rock5(6.272x7.031m), rock6(7.560x7.580m)
                        // and rock7(8.364x8.132m) footprints fit these interior
                        // west/south banks, clear
                        // of recorded art, guardian and original road/anchors.
                        // Actual nine-contact support and every existing
                        // clearance/overlap gate still decide placement.
                        if(i==4){proof.originalRockFiveHint=hint;hint=new Vector3(452,105,805);proof.analyticRockFiveInteriorHint=hint;proof.analyticRockFiveInteriorAnchorUsed=true;}
                        else if(i==5){proof.originalRockSixHint=hint;hint=new Vector3(458.5f,105,751);proof.analyticRockSixInteriorHint=hint;proof.analyticRockSixInteriorAnchorUsed=true;}
                        else{proof.originalRockSevenHint=hint;hint=new Vector3(550,105,738);proof.analyticRockSevenInteriorHint=hint;proof.analyticRockSevenInteriorAnchorUsed=true;}
                    }
                    FinishPlace(world,folder,region,source,hint,4.5f+(i%3)*1.3f,i*47,true,2,finish,records,"Owned supported cavern rock "+(i+1),false,true,Clear);proof.rocks++;
                }
                var parts=new List<CavernFinishPart>();foreach(var record in records)
                {
                    var t=region.root.Cast<Transform>().Single(v=>v.name==record.name);var bounds=BoundsOf(t);if(!Clear(bounds)||!CavernNativeFloorSupport(world,bounds,out var contacts)||!FinishBoundsClearOfRoads(world,bounds))throw new InvalidOperationException("Final actual cavern art lost native floor/guardian/route clearance: "+t.name);
                    parts.Add(new CavernFinishPart{name=t.name,source=record.source,sourceGuid=AssetDatabase.AssetPathToGUID(record.source),sourceSha256=AssetSha(record.source),sourceMetaSha256=AssetSha(record.source+".meta"),position=t.position,rotation=t.rotation,scale=t.localScale,bounds=bounds,actualDiggerFloorContacts=contacts,guardianClear=true,routeStartAnchorClear=true,lights=t.GetComponentsInChildren<Light>(true).Length,geometry=RidgeGeometryBindings(t,new Dictionary<string,string>())});
                }
                proof.parts=parts.ToArray();proof.crystals=finish.cavernCrystals;proof.lanterns=finish.lanterns;proof.singleSourceLightPerLantern=finish.singleSourceLightPerLantern&&parts.Where(p=>p.name.StartsWith("Owned working lantern caverns ",StringComparison.Ordinal)).All(p=>p.lights==1)&&parts.Sum(p=>p.lights)==12;
                proof.originalAnchorsGraphAndExistingObjectsPreserved=existing.All(p=>p.Key!=null&&Pose(p.Key)+"/"+p.Key.gameObject.activeSelf==p.Value)&&anchors.All(p=>p.Key!=null&&Pose(p.Key)==p.Value)&&graph==JsonUtility.ToJson(world.definition.graph);if(proof.crystals!=12||proof.lanterns!=12||proof.rocks!=8||parts.Count!=32||!proof.singleSourceLightPerLantern||!proof.originalAnchorsGraphAndExistingObjectsPreserved)throw new InvalidOperationException("Complete source-backed cavern art/light and original anchor preservation required.");proof.status="SOURCE_ART_NATIVE_SUPPORT_CLEARANCE_AND_SINGLE_LIGHT_OWNERSHIP_PASSED_REQUIRES_ACTUAL_RENDER_AND_GUARDIAN_PLAY_REVIEW";return proof;
            }
            catch(Exception e){proof.status="REJECTED_SHARED_CAVERN_FINISH";proof.error=e.ToString();foreach(Transform t in region.root.Cast<Transform>().Where(t=>!existing.ContainsKey(t)).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);throw;}
            finally
            {
                proof.failures=finish.failures.ToArray();proof.placements=records.ToArray();proof.changedSources=before.Where(p=>AssetSha(p.Key)!=p.Value).Select(p=>p.Key).ToArray();proof.sourceBytesPreserved=proof.changedSources.Length==0;Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"shared-cavern-finish.json"),JsonUtility.ToJson(proof,true));if(!proof.sourceBytesPreserved)throw new InvalidOperationException("Shared cavern finishing changed original source bytes.");
            }
        }
        [Serializable]sealed class CavernSavedFinishProbe
        {public string scope="DISPOSABLE_FINISHING_OF_ACTUAL_SAVED_NATIVE_CAVE_WITHOUT_REEXCAVATION_NOT_PERSISTENT_FINISHED_WORLD_OR_RENDER_ACCEPTANCE",sourcePreview,status="IN_PROGRESS",error;public bool originalSourceAndVoxelBytesExact,temporaryVariantsRemoved;public CavernFinishEvidence finish;}
        public static string CaptureSharedCavernFinishSavedProbe()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Terrain.activeTerrains.Length!=0)throw new InvalidOperationException("Stopped isolated Editor required for actual saved native cavern finishing trial.");
            string tag=Argument("-tsDragonCavernFinishSavedProbe",""),source=Argument("-tsDragonCavernOccupancyEvidence",""),live=Argument("-tsDragonCavernLiveEvidence","");if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Explicit existing saved native cave probe tag required.");
            string preview=Root+"VisualCandidates/"+tag+"/Cavern Input Probe.unity",data="Assets/DiggerData/Scenes/DK-"+tag;if(!File.Exists(Io(preview))||!Directory.Exists(Io(data)))throw new InvalidOperationException("Actual completed saved native cave and voxel data required; this trial never excavates.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);if(s.isDirty||string.IsNullOrEmpty(s.path))throw new InvalidOperationException("Preserve saved incoming scene context before actual saved-cave finishing trial.");}
            var watched=AssetDatabase.GetDependencies(preview,true).Concat(new[]{preview}).Concat(Directory.GetFiles(Io(data),"*",SearchOption.AllDirectories)).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(Io(p))).Distinct().ToDictionary(p=>p,AssetSha);var membership=Directory.GetFiles(Io(data),"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            string leaf="CFn-"+Guid.NewGuid().ToString("N").Substring(0,8),folder=Root+"VisualCandidates/"+leaf;
            string candidateRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"TruckDragonKingdom","VisualCandidates")),createdAbsolute=Path.GetFullPath(Path.Combine(candidateRoot,leaf));
            bool ExactTemporaryScope()=>leaf.Length==12&&leaf.StartsWith("CFn-",StringComparison.Ordinal)&&leaf.Substring(4).All(Uri.IsHexDigit)&&string.Equals(Path.GetDirectoryName(createdAbsolute),candidateRoot,StringComparison.OrdinalIgnoreCase)&&string.Equals(Path.GetFileName(createdAbsolute),leaf,StringComparison.Ordinal)&&string.Equals(Path.GetFullPath(folder),createdAbsolute,StringComparison.OrdinalIgnoreCase);
            if(!ExactTemporaryScope())throw new InvalidOperationException("Temporary finishing target must resolve to its exact fresh named folder under this project's Assets/TruckDragonKingdom/VisualCandidates.");
            var report=new CavernSavedFinishProbe{sourcePreview=preview};var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();UnityEngine.SceneManagement.Scene scene=default;bool created=false;
            try
            {
                if(Directory.Exists(createdAbsolute)||File.Exists(createdAbsolute)||File.Exists(createdAbsolute+".meta")||AssetDatabase.IsValidFolder(folder))throw new InvalidOperationException("Preserve existing assets; temporary finishing requires a fresh exact folder.");Directory.CreateDirectory(createdAbsolute);created=true;AssetDatabase.Refresh();scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(preview,UnityEditor.SceneManagement.OpenSceneMode.Additive);UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(scene);var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();Physics.SyncTransforms();
                report.finish=InstallSharedCavernFinish(world,folder,source,live);report.status="ACTUAL_SAVED_NATIVE_CAVE_FINISHING_TRIAL_PASSED_REQUIRES_NEW_PALETTE_RENDER_AND_GUARDIAN_PLAY_REVIEW";
            }
            catch(Exception e){report.status="REJECTED_ACTUAL_SAVED_CAVERN_FINISHING_TRIAL";report.error=e.ToString();throw;}
            finally
            {
                if(scene.IsValid()&&scene.isLoaded)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);
                if(!ExactTemporaryScope())throw new InvalidOperationException("Unexpected resolved temporary finishing cleanup target; preserve it without deletion.");if(created&&Directory.Exists(createdAbsolute))AssetDatabase.DeleteAsset(folder);report.temporaryVariantsRemoved=!Directory.Exists(createdAbsolute);report.originalSourceAndVoxelBytesExact=watched.All(p=>File.Exists(Io(p.Key))&&AssetSha(p.Key)==p.Value)&&membership.SequenceEqual(Directory.GetFiles(Io(data),"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal));Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"shared-cavern-finish-saved-probe.json"),JsonUtility.ToJson(report,true));if(!report.temporaryVariantsRemoved||!report.originalSourceAndVoxelBytesExact)throw new InvalidOperationException("Actual saved-cave finishing trial changed source/voxel bytes or left temporary variants.");
            }
            return JsonUtility.ToJson(report,true);
        }
    }
}
#endif
