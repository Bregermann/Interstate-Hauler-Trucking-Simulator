using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckSports;
using LWS.TruckDragonKingdom.Ambience;

namespace LWS.TruckDragonKingdom.Editor
{
    // Functional terrain adoption only. Keep ORIGINAL prefab/scene objects and
    // their local file IDs; never copy the candidate prefab over the original.
    public static class DragonKingdomShoulderAdoption
    {
        const string Root="Assets/TruckDragonKingdom/";
        const string Prefab=Root+"Prefabs/Shared Dragon Kingdom.prefab";
        const string Preview=Root+"Scenes/DragonKingdomPreview.unity";
        const string Definition=Root+"Configuration/Dragon Kingdom.asset";
        static readonly string[] Modes={"TruckHoardHeist","TruckDragonEggDelivery","TruckDragonRiders","TruckDragonEscape"};
        [Serializable]public sealed class TerrainReference {public string objectPath,oldData,newData;}
        [Serializable]public sealed class ModeEvidence
        {public string scene,sceneBytes,overrides,worldPoses,ambientRoutes,bodyAndCollision;public int trucks,worldObjects,rigidbodies,colliders,groundRouteChecks;}
        [Serializable]public sealed class Receipt
        {
            public string scope="FUNCTIONAL_SHOULDERS_ONLY_ART_REMAINS_PARTIAL_NOT_VISUAL_WINDOWS_OR_HUMAN_ACCEPTANCE",candidateTag;
            public string prefabGuid,previewGuid,definitionGuid;public int prefabTerrainChanges,previewTerrainChanges,originalPrefabObjects;
            public TerrainReference[] terrainReferences;public ModeEvidence[] modes;
            public string terrainScope="Only four hazard-site shelves:65m complete candidate shoulder support,125m roll-in; existing habitat/navmesh loops stay on original heights.";
            public bool allOriginalPrefabLocalIdsPreserved,allOriginalPreviewLocalIdsPreserved,oldTerrainModelDefinitionMaterialBytesPreserved,allModeSceneBytesOverridesAmbientAndTractorsPreserved,originalPhysicalGatesPassed;
        }
        public static Receipt LastReceipt{get;private set;}
        public static void AdoptValidatedShouldersOnly()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonWorldCandidate");
            if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit -tsDragonWorldCandidate tag required for functional shoulder adoption.");
            AdoptValidatedShouldersOnly(args[at+1]);
        }
        public static void AdoptValidatedShouldersOnly(string tag)
        {
            if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Valid candidate tag required.");
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for explicit shoulder adoption.");
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isDirty||string.IsNullOrEmpty(s.path)||s.path==Preview)throw new InvalidOperationException("Save and close original preview/user edits before adoption.");}
            string candidate=Root+"VisualCandidates/"+tag+"/Shared Dragon Kingdom.prefab";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(candidate);if(source==null)throw new InvalidOperationException("Existing physically validated shoulder candidate required; no generation.");
            var candidateRefs=source.GetComponentsInChildren<Terrain>(true).ToDictionary(t=>PathOf(t.transform,source.transform),t=>t.terrainData);
            if(candidateRefs.Count!=4||candidateRefs.Values.Any(d=>!AssetDatabase.GetAssetPath(d).StartsWith(Root+"VisualCandidates/"+tag+"/Terrain/",StringComparison.Ordinal)))throw new InvalidOperationException("Exactly four candidate-only TerrainData references required.");
            var prior=EditorSceneManager.GetSceneManagerSetup();
            var receipt=new Receipt{candidateTag=tag,prefabGuid=AssetDatabase.AssetPathToGUID(Prefab),previewGuid=AssetDatabase.AssetPathToGUID(Preview),definitionGuid=AssetDatabase.AssetPathToGUID(Definition)};
            var protectedFiles=Directory.GetFiles(Root+"Terrain","*",SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Root+"Models","*",SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(Root+"Materials","*",SearchOption.AllDirectories)).Concat(new[]{Definition,Definition+".meta",Prefab+".meta",Preview+".meta"})
                .ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(p)));
            var originals=new[]{Prefab,Preview}.ToDictionary(p=>p,p=>File.ReadAllBytes(p));
            string output=Output();Directory.CreateDirectory(output);string backup=Path.Combine(output,"before-shoulders-only");
            foreach(var p in originals){string target=Path.Combine(backup,p.Key.Replace('/',Path.DirectorySeparatorChar));if(!File.Exists(target)){Directory.CreateDirectory(Path.GetDirectoryName(target));File.WriteAllBytes(target,p.Value);}}
            var modePaths=Modes.Select(m=>"Assets/"+m+"/Scenes/"+m+".unity").Where(File.Exists).ToArray();
            var modeBytes=modePaths.ToDictionary(p=>p,p=>File.ReadAllBytes(p));
            var changes=new List<TerrainReference>();GameObject contents=null;Scene preview=default;bool completed=false,mutated=false;
            try
            {
                var beforeModes=modePaths.Select(p=>InspectMode(p,null)).ToArray();
                var terrainRefs=BuildFunctionalTerrain(source.GetComponent<DragonKingdomWorld>(),candidateRefs,tag);
                var originalIds=PrefabIds(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));receipt.originalPrefabObjects=originalIds.Count;
                contents=PrefabUtility.LoadPrefabContents(Prefab);string poses=WorldPoseHash(contents);
                receipt.prefabTerrainChanges=ApplyTerrain(contents,terrainRefs,changes);
                if(WorldPoseHash(contents)!=poses)throw new InvalidOperationException("Shoulder-only adoption changed original prefab transforms/scenery.");
                if(receipt.prefabTerrainChanges>0){mutated=true;PrefabUtility.SaveAsPrefabAsset(contents,Prefab);}
                PrefabUtility.UnloadPrefabContents(contents);contents=null;
                AssetDatabase.ImportAsset(Prefab,ImportAssetOptions.ForceUpdate);
                var afterIds=PrefabIds(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
                if(!Same(originalIds,afterIds))throw new InvalidOperationException("Original prefab local file IDs changed; all mode prefab overrides must remain valid.");
                receipt.allOriginalPrefabLocalIdsPreserved=true;
                preview=EditorSceneManager.OpenScene(Preview,OpenSceneMode.Single);
                var world=preview.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                string previewPoses=WorldPoseHash(world.gameObject);var previewIds=SceneIds(preview);
                receipt.previewTerrainChanges=ApplyTerrain(world.gameObject,terrainRefs,new List<TerrainReference>());
                if(WorldPoseHash(world.gameObject)!=previewPoses)throw new InvalidOperationException("Shoulder-only adoption changed original preview transforms/scenery.");
                Physics.SyncTransforms();DragonKingdomWorldAuthoring.ValidatePhysicalRoads(world.transform,world.definition.graph);
                DragonKingdomWorldAuthoring.ValidateTruckStarts(world,false);world.ValidateReferences();receipt.originalPhysicalGatesPassed=true;
                if(receipt.previewTerrainChanges>0&&!EditorSceneManager.SaveScene(preview))throw new InvalidOperationException("Narrow original preview update did not save.");
                if(!Same(previewIds,SceneIds(preview)))throw new InvalidOperationException("Original preview local file IDs changed.");
                receipt.allOriginalPreviewLocalIdsPreserved=true;
                var afterModes=modePaths.Select(p=>InspectMode(p,terrainRefs)).ToArray();
                for(int i=0;i<beforeModes.Length;i++)if(JsonUtility.ToJson(beforeModes[i])!=JsonUtility.ToJson(afterModes[i]))
                {File.WriteAllText(Path.Combine(output,"mode-witness-failure-"+Path.GetFileNameWithoutExtension(modePaths[i])+".json"),"{\"before\":"+JsonUtility.ToJson(beforeModes[i])+",\"after\":"+JsonUtility.ToJson(afterModes[i])+"}");throw new InvalidOperationException("Saved mode override/ambient/NWH witness changed: "+modePaths[i]);}
                if(modeBytes.Any(p=>!File.ReadAllBytes(p.Key).SequenceEqual(p.Value)))throw new InvalidOperationException("Functional adoption rewrote an existing mode scene.");
                receipt.allModeSceneBytesOverridesAmbientAndTractorsPreserved=true;receipt.modes=afterModes;
                if(protectedFiles.Any(p=>Hash(File.ReadAllBytes(p.Key))!=p.Value))throw new InvalidOperationException("Functional adoption changed old terrain/road/model/definition/material bytes or asset GUID metadata.");
                if(AssetDatabase.AssetPathToGUID(Prefab)!=receipt.prefabGuid||AssetDatabase.AssetPathToGUID(Preview)!=receipt.previewGuid||AssetDatabase.AssetPathToGUID(Definition)!=receipt.definitionGuid)throw new InvalidOperationException("Original shared-world GUID changed.");
                receipt.oldTerrainModelDefinitionMaterialBytesPreserved=true;receipt.terrainReferences=changes.ToArray();LastReceipt=receipt;
                File.WriteAllText(Path.Combine(output,"shoulders-only-adoption.json"),JsonUtility.ToJson(receipt,true));completed=true;
            }
            finally
            {
                if(contents!=null)PrefabUtility.UnloadPrefabContents(contents);
                EditorSceneManager.RestoreSceneManagerSetup(prior);
                if(!completed&&mutated)
                {
                    // Exact, narrow rollback of the two owned assets. Original
                    // metadata/GUID files and all mode scene bytes stay intact.
                    foreach(var p in originals){File.WriteAllBytes(p.Key,p.Value);AssetDatabase.ImportAsset(p.Key,ImportAssetOptions.ForceUpdate);}
                    foreach(var p in modeBytes)if(!File.ReadAllBytes(p.Key).SequenceEqual(p.Value)){File.WriteAllBytes(p.Key,p.Value);AssetDatabase.ImportAsset(p.Key,ImportAssetOptions.ForceUpdate);}
                }
            }
        }

        static Dictionary<string,TerrainData> BuildFunctionalTerrain(DragonKingdomWorld candidate,Dictionary<string,TerrainData> refs,string tag)
        {
            string folder=Root+"FunctionalShoulders/"+tag+"/Terrain";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var sites=new List<Vector3>();
            foreach(var plan in candidate.definition.closurePlans)
            {
                var edge=candidate.definition.graph.edges.Single(e=>e.edgeId==plan.edgeIds.Single());
                int i=Mathf.Clamp(Mathf.RoundToInt((edge.samples.Count-1)*(plan.id=="harbor-market-collapse"?.8f:.5f)),1,edge.samples.Count-2);sites.Add(edge.samples[i].position);
            }
            if(sites.Count!=4)throw new InvalidOperationException("Four actual closure support sites required.");
            var result=new Dictionary<string,TerrainData>();
            foreach(var terrain in candidate.GetComponentsInChildren<Terrain>(true))
            {
                string key=PathOf(terrain.transform,candidate.transform),name=Path.GetFileName(AssetDatabase.GetAssetPath(refs[key]));
                var original=AssetDatabase.LoadAssetAtPath<TerrainData>(Root+"Terrain/"+name);if(original==null)throw new InvalidOperationException("Exact original terrain baseline missing: "+name);
                int resolution=original.heightmapResolution;var heights=original.GetHeights(0,0,resolution,resolution);var full=refs[key].GetHeights(0,0,resolution,resolution);
                for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
                {
                    Vector3 p=terrain.transform.position+new Vector3((float)x/(resolution-1)*original.size.x,0,(float)z/(resolution-1)*original.size.z);
                    float d=float.PositiveInfinity;foreach(var site in sites)d=Mathf.Min(d,Vector2.Distance(new Vector2(p.x,p.z),new Vector2(site.x,site.z)));
                    if(d>=125)continue;float amount=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(65,125,d));heights[z,x]=Mathf.Lerp(heights[z,x],full[z,x],amount);
                }
                var proposed=UnityEngine.Object.Instantiate(original);proposed.SetHeights(0,0,heights);
                string path=folder+"/"+name;var data=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if(data!=null)
                {
                    bool same=DragonKingdomWorldVisualMigration.HeightHash(data)==DragonKingdomWorldVisualMigration.HeightHash(proposed);UnityEngine.Object.DestroyImmediate(proposed);
                    if(!same)throw new InvalidOperationException("Existing different functional shoulder data preserved; use a new explicit candidate tag: "+path);
                }
                else{data=proposed;data.name=Path.GetFileNameWithoutExtension(name)+" functional hazard shoulders";AssetDatabase.CreateAsset(data,path);DragonKingdomWorldVisualMigration.RepairTerrainAppearance(data);}
                result.Add(key,data);
            }
            return result;
        }

        static int ApplyTerrain(GameObject root,Dictionary<string,TerrainData> refs,List<TerrainReference> changes)
        {
            var terrains=root.GetComponentsInChildren<Terrain>(true);if(terrains.Length!=4)throw new InvalidOperationException("Four original terrains required.");int count=0;
            foreach(var terrain in terrains)
            {
                string path=PathOf(terrain.transform,root.transform);
                if(!refs.TryGetValue(path,out var data)||data.size!=terrain.terrainData.size||data.heightmapResolution!=terrain.terrainData.heightmapResolution)throw new InvalidOperationException("Candidate terrain identity/bounds do not match original: "+path);
                var collider=terrain.GetComponent<TerrainCollider>();if(collider==null)throw new InvalidOperationException("Original terrain collider missing.");
                if(terrain.terrainData==data&&collider.terrainData==data)continue;
                changes.Add(new TerrainReference{objectPath=path,oldData=AssetDatabase.GetAssetPath(terrain.terrainData),newData=AssetDatabase.GetAssetPath(data)});
                terrain.terrainData=data;collider.terrainData=data;EditorUtility.SetDirty(terrain);EditorUtility.SetDirty(collider);count++;
            }
            return count;
        }
        static ModeEvidence InspectMode(string path,Dictionary<string,TerrainData> expected)
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var roots=scene.GetRootGameObjects();
            var world=roots.SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();world.ValidateReferences();
            if(expected!=null)foreach(var terrain in world.GetComponentsInChildren<Terrain>(true))if(terrain.terrainData!=expected[PathOf(terrain.transform,world.transform)]||terrain.GetComponent<TerrainCollider>().terrainData!=terrain.terrainData)throw new InvalidOperationException("Mode retains an obsolete terrain-data override: "+path);
            var match=roots.SelectMany(r=>r.GetComponentsInChildren<SportsEventSession>(true)).Single();
            var field=match.GetType().GetField("trucks");var trucks=field?.GetValue(match) as SportsEventTruck[];
            if(trucks==null||trucks.Length!=4||trucks.Any(t=>t==null||t.tractorMotor==null||t.tractorMotor.vehicle==null))throw new InvalidOperationException("Four actual original NWH crews required for mode preservation: "+path);
            var instance=PrefabUtility.GetOutermostPrefabInstanceRoot(world.gameObject);if(instance==null)throw new InvalidOperationException("Existing mode must retain its actual shared-world prefab instance.");
            var modifications=PrefabUtility.GetPropertyModifications(instance)??Array.Empty<PropertyModification>();
            string overrides=string.Join("\n",modifications.Select(m=>Identity(m.target)+"|"+m.propertyPath+"|"+m.value+"|"+Identity(m.objectReference)).OrderBy(s=>s,StringComparer.Ordinal));
            string ambient=string.Join("\n",world.regions.Select(r=>r.id+"|"+string.Join(";",r.ambientRoutes.Select(t=>t==null?"NULL":PathOf(t,world.transform)+Pose(t)))));
            if(world.regions.Any(r=>r.ambientRoutes.Any(t=>t==null)))throw new InvalidOperationException("Original scene-specific ambient route became missing.");
            var rigidbodies=roots.SelectMany(r=>r.GetComponentsInChildren<Rigidbody>(true)).ToArray();var colliders=roots.SelectMany(r=>r.GetComponentsInChildren<Collider>(true)).ToArray();
            Physics.SyncTransforms();int groundChecks=0;
            foreach(var walker in world.GetComponentsInChildren<DragonAmbientWalker>(true))for(int leg=0;leg<walker.route.Length;leg++)
            {
                var a=walker.route[leg].position;var b=walker.route[(leg+1)%walker.route.Length].position;int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.7f));
                for(int step=0;step<=steps;step++)
                {
                    var point=Vector3.Lerp(a,b,(float)step/steps);var hits=Physics.RaycastAll(point+Vector3.up*2,Vector3.down,4,~0,QueryTriggerInteraction.Ignore);
                    if(!hits.Any(h=>h.normal.y>.72f&&Mathf.Abs(h.point.y-point.y)<.22f))throw new InvalidOperationException("Preserve actual existing ambient ground/navmesh support; route no longer grounded: "+path+" "+walker.name+" "+point);
                    groundChecks++;
                }
            }
            string physics=string.Join("\n",rigidbodies.Select(b=>PathOf(b.transform,null)+Pose(b.transform)+"|"+b.mass.ToString("R")+"|"+b.linearDamping.ToString("R")+"|"+b.angularDamping.ToString("R")+"|"+b.isKinematic+"|"+b.useGravity+"|"+b.constraints+"|"+b.collisionDetectionMode)
                .Concat(colliders.Select(c=>PathOf(c.transform,null)+Pose(c.transform)+ColliderShape(c))).OrderBy(s=>s,StringComparer.Ordinal));
            return new ModeEvidence{scene=path,sceneBytes=Hash(File.ReadAllBytes(path)),overrides=Hash(Encoding.UTF8.GetBytes(overrides)),worldPoses=WorldPoseHash(world.gameObject),ambientRoutes=Hash(Encoding.UTF8.GetBytes(ambient)),bodyAndCollision=Hash(Encoding.UTF8.GetBytes(physics)),trucks=trucks.Length,worldObjects=world.GetComponentsInChildren<Transform>(true).Length,rigidbodies=rigidbodies.Length,colliders=colliders.Length,groundRouteChecks=groundChecks};
        }
        static string ColliderShape(Collider c)
        {
            string common="|"+c.GetType().FullName+"|"+c.enabled+"|"+c.isTrigger+"|"+Identity(c.sharedMaterial)+"|"+c.contactOffset.ToString("R");
            if(c is BoxCollider b)return common+b.center.ToString("R")+b.size.ToString("R");
            if(c is SphereCollider s)return common+s.center.ToString("R")+s.radius.ToString("R");
            if(c is CapsuleCollider k)return common+k.center.ToString("R")+k.radius.ToString("R")+k.height.ToString("R")+k.direction;
            if(c is MeshCollider m)return common+Identity(m.sharedMesh)+m.convex+m.cookingOptions;
            return common; // TerrainData reference change is the sole intended change.
        }
        static Dictionary<string,string> PrefabIds(GameObject root)
        {
            var result=new Dictionary<string,string>();foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {result.Add(PathOf(t,root.transform)+"|GO",Identity(t.gameObject));var components=t.GetComponents<Component>();for(int i=0;i<components.Length;i++)result.Add(PathOf(t,root.transform)+"|"+i,Identity(components[i]));}return result;
        }
        static Dictionary<string,string> SceneIds(Scene scene)
        {var result=new Dictionary<string,string>();foreach(var root in scene.GetRootGameObjects())foreach(var p in PrefabIds(root))result.Add(root.name+"/"+p.Key,p.Value);return result;}
        static bool Same(Dictionary<string,string> a,Dictionary<string,string> b)=>a.Count==b.Count&&a.All(p=>b.TryGetValue(p.Key,out var value)&&value==p.Value);
        static string Identity(UnityEngine.Object o){if(o==null)return "NULL";if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string guid,out long id))return guid+":"+id;return GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();}
        static string WorldPoseHash(GameObject root)=>Hash(Encoding.UTF8.GetBytes(string.Join("\n",root.GetComponentsInChildren<Transform>(true).Select(t=>PathOf(t,root.transform)+Pose(t)).OrderBy(s=>s,StringComparer.Ordinal))));
        static string Pose(Transform t)=>"|"+t.localPosition.ToString("R")+t.localRotation.ToString("R")+t.localScale.ToString("R");
        static string PathOf(Transform t,Transform root)
        {
            if(t==root)return ".";string name=t.name;
            if(t.parent!=null){int ordinal=0;foreach(Transform sibling in t.parent){if(sibling==t)break;if(sibling.name==t.name)ordinal++;}name+="["+ordinal+"]";}
            if(t.parent==null||t.parent==root)return name;return PathOf(t.parent,root)+"/"+name;
        }
        static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","");}
        static string Output(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");return at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):"Logs/TruckSports/DragonKingdom/ShouldersOnly";}
    }
}
