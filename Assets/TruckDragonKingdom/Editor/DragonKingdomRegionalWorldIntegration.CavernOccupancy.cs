#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LWS.TruckHoardHeist;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class CavernActionOccupancy
        {
            public string action,catalogClip,catalogClipSha256,clip,clipSha256,timesSha256;
            public int frames,sourceVertices;public float sourceFrameRate,duration;
            public Bounds actualWorldSkinBounds;
        }
        [Serializable]public sealed class CavernBodyOccupancy
        {
            public string kind,id;public int instanceId;
            public Vector3 position;public Bounds actualWorldBounds;
        }
        [Serializable]public sealed class CavernRoadOccupancy
        {
            public string edge;public Vector3 a,b;public float width;
        }
        [Serializable]public sealed class CavernOccupancySource{public string path,sha256;}
        [Serializable]public sealed class CavernOccupancyEvidence
        {
            public string scope="ORIGINAL_SAVED_HEIST_GUARDIAN_ALL_ORIGINAL_CONFIGURED_EFFECTIVE_ACTION_SAMPLES_AND_AUTHORED_PHYSICAL_OCCUPANCY_NOT_LIVE_MECANIM_TRANSITIONS_OR_VAULT_ACCEPTANCE";
            public string status="IN_PROGRESS",scene,species,error;
            public int actionMappings,actionSamples,sourceSkinVertices,originalTrucks,originalTreasure,roadSegments;
            public float authoredFloorY;
            public Vector3 guardianHome;public Quaternion originalGuardianRotation;
            public Bounds rawGuardianActionBounds;
            public bool originalGuardianMeshMembershipExact,sourceBytesPreserved,incomingSceneContextExact,previewDestroyed;
            public CavernOccupancySource[] protectedSources;public string[] changedSourcePaths;public CavernActionOccupancy[] actions;
            public CavernBodyOccupancy[] bodies;public CavernRoadOccupancy[] roads;
            public string[] limitations={"Original configured effective action clips (resolved by the existing motor authority) are sampled at their declared framerate and all authored scalar key times; this is not a proof of live Animator blends or every continuous instant.","Actual guardian travel, flight, collision response and mode transitions require additional runtime occupancy before a final cavern is accepted.","The prior187rectangular probe grid is not substituted for actual actors, trucks, cargo and road requirements."};
        }
        public static CavernOccupancyEvidence LastCavernOccupancy{get;private set;}

        public static void CaptureOriginalCavernOccupancyFromCommandLine()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled native Editor required for original-cavern occupancy inspection.");
            const string scenePath="Assets/TruckHoardHeist/Scenes/TruckHoardHeist.unity";
            if(!File.Exists(Io(scenePath)))throw new FileNotFoundException("The actual original saved Heist scene is required.",scenePath);
            var proof=new CavernOccupancyEvidence{scene=scenePath};LastCavernOccupancy=proof;
            var context=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Select(s=>new{scene=s,active=s==SceneManager.GetActiveScene(),dirty=s.isDirty,roots=s.GetRootGameObjects().Select(g=>g.GetInstanceID()).ToArray()}).ToArray();
            var sourceHashes=new Dictionary<string,string>();
            void Protect(string path)
            {
                if(string.IsNullOrEmpty(path)||!File.Exists(Io(path)))return;
                sourceHashes[path]=AssetSha(path);if(File.Exists(Io(path+".meta")))sourceHashes[path+".meta"]=AssetSha(path+".meta");
            }
            Protect(scenePath);Scene scene=default;GameObject copy=null,previewRoot=null;PreviewRenderUtility preview=null;
            var actions=new List<CavernActionOccupancy>();var bodies=new List<CavernBodyOccupancy>();
            try
            {
                scene=EditorSceneManager.OpenPreviewScene(scenePath);
                var match=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HoardHeistMatch>(true)).Single();
                if(match.world==null||match.species==null||match.dragons.Length==0||match.dragonSpeciesIds.Length==0||match.guardianHome==null)
                    throw new InvalidOperationException("The original configured guardian, world, catalog and home must all exist.");
                string id=match.dragonSpeciesIds[0];
                if(!match.species.TryGet(id,out var species)||!species.Supports(DragonSpeciesRole.HoardGuardian))
                    throw new InvalidOperationException("Actual source species must retain its HoardGuardian role.");
                var original=match.dragons[0];
                if(original.motor==null||original.motor.animator==null||original.motor.animator.runtimeAnimatorController==null)
                    throw new InvalidOperationException("Original configured Malbers/Animator binding required.");
                proof.species=id;proof.guardianHome=match.guardianHome.position;proof.originalGuardianRotation=original.transform.rotation;
                proof.authoredFloorY=match.world.Region("caverns").root.position.y;
                Protect(AssetDatabase.GetAssetPath(match.species));
                var actorSources=new[]{PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(original.gameObject),AssetDatabase.GetAssetPath(species.visualPrefab),AssetDatabase.GetAssetPath(original.motor.animator.runtimeAnimatorController),AssetDatabase.GetAssetPath(original.motor.animator.avatar)}
                    .Concat(species.animations.Where(m=>m!=null&&m.clip!=null).Select(m=>AssetDatabase.GetAssetPath(m.clip))).Where(p=>!string.IsNullOrEmpty(p)).Distinct().ToArray();
                foreach(string path in actorSources)foreach(string dependency in AssetDatabase.GetDependencies(path,true))Protect(dependency);
                foreach(string path in actorSources)Protect(path);

                preview=new PreviewRenderUtility();previewRoot=new GameObject("Inactive disposable original-cavern occupancy");previewRoot.hideFlags=HideFlags.HideAndDontSave;previewRoot.SetActive(false);preview.AddSingleGO(previewRoot);
                // An inactive preview parent prevents clone Awake/OnEnable from
                // registering gameplay systems before they can be disabled.
                copy=UnityEngine.Object.Instantiate(original.gameObject,previewRoot.transform,true);
                foreach(var behaviour in copy.GetComponentsInChildren<Behaviour>(true))behaviour.enabled=false;
                foreach(var body in copy.GetComponentsInChildren<Rigidbody>(true))body.isKinematic=true;
                var actor=copy.GetComponent<DragonCharacter>();
                if(actor==null||actor.motor==null||actor.motor.animator==null)throw new InvalidOperationException("The disposable actor must retain the original complete configured hierarchy.");
                var sourceSkins=original.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.sharedMesh!=null).ToArray();
                var copySkins=copy.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.sharedMesh!=null).ToArray();
                proof.originalGuardianMeshMembershipExact=sourceSkins.Length>0&&sourceSkins.Select(s=>s.sharedMesh).SequenceEqual(copySkins.Select(s=>s.sharedMesh));
                if(!proof.originalGuardianMeshMembershipExact)throw new InvalidOperationException("No omitted or substituted guardian skin is allowed.");
                proof.sourceSkinVertices=sourceSkins.Sum(s=>s.sharedMesh.vertexCount);
                var poses=copy.GetComponentsInChildren<Transform>(true).Select(t=>Tuple.Create(t,t.localPosition,t.localRotation,t.localScale)).ToArray();
                bool first=true;
                foreach(var mapping in species.animations)
                {
                    if(mapping==null||mapping.clip==null)throw new InvalidOperationException("Every original catalog action must have its actual source clip.");
                    var resolver=typeof(DragonAnimalMotor).GetMethod("ResolveMappedClip",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                    if(resolver==null)throw new InvalidOperationException("Existing authoritative motor clip resolver required.");
                    var clip=(AnimationClip)resolver.Invoke(actor.motor,new object[]{mapping.clip});
                    if(clip==null)throw new InvalidOperationException("Original configured action resolves to no effective clip: "+mapping.action);
                    string path=AssetDatabase.GetAssetPath(clip);Protect(path);
                    if(string.IsNullOrEmpty(path)||!File.Exists(Io(path))||clip.length<=0||clip.frameRate<=0)
                        throw new InvalidOperationException("Actual finite source clip and declared framerate required: "+mapping.action);
                    var times=new SortedSet<float>{0,clip.length};int frames=Mathf.CeilToInt(clip.length*clip.frameRate);
                    if(frames>12000)throw new InvalidOperationException("Unexpected source animation duration; preserve and inspect instead of silently truncating.");
                    for(int i=0;i<=frames;i++)times.Add(Mathf.Min(clip.length,i/clip.frameRate));
                    foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                    {var curve=AnimationUtility.GetEditorCurve(clip,binding);if(curve==null)throw new InvalidOperationException("Authored scalar curve missing for effective original action: "+mapping.action);foreach(var key in curve.keys)if(key.time>=0&&key.time<=clip.length)times.Add(key.time);}
                    var row=new CavernActionOccupancy{action=mapping.action.ToString(),catalogClip=AssetDatabase.GetAssetPath(mapping.clip),catalogClipSha256=AssetSha(AssetDatabase.GetAssetPath(mapping.clip)),clip=path,clipSha256=AssetSha(path),sourceFrameRate=clip.frameRate,duration=clip.length,sourceVertices=proof.sourceSkinVertices};
                    row.timesSha256=Hash(Encoding.UTF8.GetBytes(string.Join(",",times.Select(t=>t.ToString("R",CultureInfo.InvariantCulture))))).Replace("-","").ToLowerInvariant();
                    bool firstFrame=true;
                    foreach(float time in times)
                    {
                        foreach(var pose in poses){pose.Item1.SetLocalPositionAndRotation(pose.Item2,pose.Item3);pose.Item1.localScale=pose.Item4;}
                        clip.SampleAnimation(actor.motor.animator.gameObject,time);
                        var bounds=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(copy.transform);
                        if(firstFrame){row.actualWorldSkinBounds=bounds;firstFrame=false;}else row.actualWorldSkinBounds.Encapsulate(bounds);
                        if(first){proof.rawGuardianActionBounds=bounds;first=false;}else proof.rawGuardianActionBounds.Encapsulate(bounds);
                        row.frames++;proof.actionSamples++;
                    }
                    actions.Add(row);
                }
                proof.actionMappings=actions.Count;
                foreach(var truck in match.trucks)
                {
                    if(truck==null||truck.shape==null||truck.tractorMotor?.vehicle==null)
                        throw new InvalidOperationException("Every original full NWH competitor and its actual box must be present.");
                    bodies.Add(CavernBox("ORIGINAL_NWH_PRIMARY_CHASSIS",truck.name,truck.shape));
                    bodies.Add(new CavernBodyOccupancy{kind="ORIGINAL_NWH_COMPLETE_MODEL",id=truck.name,instanceId=truck.GetInstanceID(),position=truck.transform.position,actualWorldBounds=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(truck.transform)});proof.originalTrucks++;
                }
                foreach(var rack in match.cargoRacks){if(rack==null)throw new InvalidOperationException("Four original physical cargo racks required.");bodies.Add(CavernBox("ORIGINAL_CARGO_RACK",rack.name,rack));}
                foreach(var item in match.treasure)
                {
                    if(item==null||item.shape==null||item.body==null)throw new InvalidOperationException("All original finite treasure bodies and shapes must be present.");
                    bodies.Add(CavernBox("ORIGINAL_TREASURE",item.stableId,item.shape));proof.originalTreasure++;
                }
                foreach(var anchor in match.world.Region("caverns").truckStarts.Concat(match.quietCoverGoals).Concat(new[]{match.guardianHome}))
                {
                    if(anchor==null)throw new InvalidOperationException("Actual starts, quiet-cover positions and guardian home are required.");
                    bodies.Add(new CavernBodyOccupancy{kind="ORIGINAL_SEMANTIC_ANCHOR",id=anchor.name,instanceId=anchor.GetInstanceID(),position=anchor.position,actualWorldBounds=new Bounds(anchor.position,Vector3.zero)});
                }
                var roads=new List<CavernRoadOccupancy>();var volume=DragonKingdomWorldAuthoring.CavernUndergroundBounds();
                foreach(var edge in match.world.definition.graph.edges)for(int i=1;i<edge.samples.Count;i++)
                    if(DragonKingdomDiggerCavernAuthoring.Clip(volume,edge.samples[i-1].position,edge.samples[i].position,out var a,out var b)&&Vector3.Distance(a,b)>.001f)
                        roads.Add(new CavernRoadOccupancy{edge=edge.edgeId,a=a,b=b,width=edge.samples[i].roadWidthMeters});
                proof.roads=roads.ToArray();proof.roadSegments=roads.Count;
                if(proof.actionMappings==0||proof.actionSamples==0||proof.actionMappings!=species.animations.Length||proof.originalTrucks!=4||proof.originalTreasure!=match.treasure.Length||proof.roadSegments==0)
                    throw new InvalidOperationException("Complete actual original actor/body/road coverage required.");
                proof.status="ORIGINAL_CONFIGURED_ACTION_AND_AUTHORED_OCCUPANCY_MEASURED_LIVE_TRANSITIONS_AND_VAULT_GEOMETRY_PENDING";
            }
            catch(Exception error){proof.status="REJECTED_ORIGINAL_CAVERN_OCCUPANCY";proof.error=error.ToString();throw;}
            finally
            {
                if(preview!=null)preview.Cleanup();else if(previewRoot!=null)UnityEngine.Object.DestroyImmediate(previewRoot);else if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);
                if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
                proof.previewDestroyed=copy==null&&previewRoot==null&&(!scene.IsValid()||!scene.isLoaded);
                proof.incomingSceneContextExact=SceneManager.sceneCount==context.Length&&context.All(c=>c.scene.IsValid()&&c.scene.isLoaded&&c.scene.isDirty==c.dirty&&(c.scene==SceneManager.GetActiveScene())==c.active&&c.scene.GetRootGameObjects().Select(g=>g.GetInstanceID()).SequenceEqual(c.roots));
                proof.changedSourcePaths=sourceHashes.Where(p=>!File.Exists(Io(p.Key))||AssetSha(p.Key)!=p.Value).Select(p=>p.Key).ToArray();proof.sourceBytesPreserved=proof.changedSourcePaths.Length==0;
                proof.actions=actions.ToArray();proof.bodies=bodies.ToArray();proof.protectedSources=sourceHashes.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new CavernOccupancySource{path=p.Key,sha256=p.Value}).ToArray();
                Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"original-cavern-occupancy.json"),JsonUtility.ToJson(proof,true));
                if(!proof.sourceBytesPreserved||!proof.incomingSceneContextExact||!proof.previewDestroyed)
                    throw new InvalidOperationException("Read-only original-cavern occupancy failed exact source/context/preview preservation.");
            }
        }
        static CavernBodyOccupancy CavernBox(string kind,string id,BoxCollider box)
        {
            var half=box.size*.5f;var bounds=new Bounds(box.transform.TransformPoint(box.center),Vector3.zero);
            foreach(float x in new[]{-half.x,half.x})foreach(float y in new[]{-half.y,half.y})foreach(float z in new[]{-half.z,half.z})bounds.Encapsulate(box.transform.TransformPoint(box.center+new Vector3(x,y,z)));
            return new CavernBodyOccupancy{kind=kind,id=id,instanceId=box.GetInstanceID(),position=box.transform.position,actualWorldBounds=bounds};
        }
    }
}
#endif
