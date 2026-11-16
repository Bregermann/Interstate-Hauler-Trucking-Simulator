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

namespace LWS.TruckDragonKingdom.Editor
{
    // Review-only derivative of the accepted scene. Never reruns Vista/EasyRoads
    // or overwrites the accepted prefab, scene, terrain, graph or road meshes.
    public static class DragonKingdomCosmeticGeometryAuthoring
    {
        const string Root="Assets/TruckDragonKingdom/";
        const string AcceptedPreview=Root+"Scenes/DragonKingdomPreview.unity";
        const string AcceptedPrefab=Root+"Prefabs/Shared Dragon Kingdom.prefab";
        const string AcceptedDefinition=Root+"Configuration/Dragon Kingdom.asset";
        const string Mountain=DragonKingdomWorldAuthoring.Art+"Polyart Studio/HandpaintedEnvironmentVol1/Meshes/Prefab/Mountains/";
        [Serializable]public sealed class Placement {public string name,source;public Bounds bounds;public Vector3 scale;public int trial;}
        [Serializable]public sealed class Receipt
        {
            public string scope="SAVED_COSMETIC_REVIEW_CANDIDATE_NOT_APPROVED_WORLD_OR_VISUAL_ACCEPTANCE",tag,prefab,preview,definition;
            public int removedStretchedPeakRoots;public Placement[] placements;
            public bool acceptedAssetsExact,acceptedTerrainAssetsExact,roadMeshesExact,startsAndExtractionsExact,originalPhysicalGatesPassed;
            public DragonKingdomTerrainShoulderAuthoring.Evidence candidateShoulders;
        }
        public static Receipt LastReceipt{get;private set;}

        [MenuItem("Truck Sports/Dragon Kingdom/Create cosmetic mountain review candidate")]
        public static void CreateReviewCandidate()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonWorldCandidate");
            CreateReviewCandidate(at>=0&&at+1<args.Length?args[at+1]:"Mountains01");
        }
        public static void CreateReviewCandidate(string tag)
        {
            if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("A short alphanumeric candidate tag is required.");
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for explicit review candidate authoring.");
            string folder=Root+"VisualCandidates/"+tag;
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve existing review candidate; choose a new explicit tag: "+folder);
            foreach(var p in new[]{AcceptedPrefab,AcceptedPreview,AcceptedDefinition})if(!File.Exists(p))throw new InvalidOperationException("The accepted saved world is required: "+p);
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.path==AcceptedPreview||s.isDirty||string.IsNullOrEmpty(s.path))throw new InvalidOperationException("Save/close user scenes before isolated candidate authoring.");}
            var preserved=Directory.GetFiles(Root+"Terrain","*",SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Root+"Models","*",SearchOption.AllDirectories))
                .Concat(new[]{AcceptedPrefab,AcceptedPreview,AcceptedDefinition}).ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(p)));
            var prior=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;GameObject probe=null;
            var records=new List<Placement>();var receipt=new Receipt{tag=tag,prefab=folder+"/Shared Dragon Kingdom.prefab",preview=folder+"/DragonKingdomPreview.unity",definition=folder+"/Dragon Kingdom.asset"};
            try
            {
                Directory.CreateDirectory(folder+"/Materials");AssetDatabase.Refresh();
                scene=EditorSceneManager.OpenScene(AcceptedPreview,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
                var world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                var peaks=new List<Transform>();
                foreach(string id in new[]{"mountains","volcano"})
                    foreach(Transform child in world.Region(id).root)
                        if(child.name.StartsWith("RockFormation_",StringComparison.Ordinal)||child.name.StartsWith("Owned basalt cliff composition ",StringComparison.Ordinal))peaks.Add(child);
                if(peaks.Count!=18)throw new InvalidOperationException("Expected the18 audited stretched cosmetic peak roots; preserve all other architecture.");
                var protectedTransforms=world.GetComponentsInChildren<Transform>(true).Where(t=>!peaks.Any(p=>t==p||t.IsChildOf(p))).ToDictionary(t=>t,t=>TransformPose(t));
                var protectedColliders=world.GetComponentsInChildren<Collider>(true).Where(c=>!(c is TerrainCollider)&&!peaks.Any(p=>c.transform==p||c.transform.IsChildOf(p))).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
                var originalDefinitions=JsonUtility.ToJson(world.definition.graph);
                var feet=peaks.Select(p=>new{region=p.parent,position=BoundsOf(p).center,baseY=BoundsOf(p).min.y}).ToArray();
                foreach(var peak in peaks)UnityEngine.Object.DestroyImmediate(peak.gameObject);
                receipt.removedStretchedPeakRoots=peaks.Count;
                receipt.candidateShoulders=DragonKingdomTerrainShoulderAuthoring.CloneAndBlend(world,folder);
                probe=new GameObject("Temporary candidate clearance shape"){hideFlags=HideFlags.HideAndDontSave};var box=probe.AddComponent<BoxCollider>();box.isTrigger=true;
                // Full real mountain meshes use UNIFORM scale. Their source
                // slopes/silhouettes stay intact instead of stretching a small
                // cliff hundreds of meters vertically.
                PlaceMountain(world,"mountains","Terrain_01.prefab",new Vector3(-1800,-20,1950),540,0,box,folder,records);
                PlaceMountain(world,"mountains","Terrain_02.prefab",new Vector3(-100,-20,2450),440,35,box,folder,records);
                PlaceMountain(world,"volcano","Terrain_01.prefab",new Vector3(1680,-20,1750),500,-35,box,folder,records);
                PlaceMountain(world,"volcano","Terrain_02.prefab",new Vector3(2200,-20,650),440,80,box,folder,records);
                // Keep each former peak location dressed with naturally
                // proportioned complete rock clusters, with accurate collision.
                int index=0;
                foreach(var foot in feet)
                {
                    string source=DragonKingdomWorldAuthoring.Cliff+"RockFormation_"+(index%4+1).ToString("00")+".prefab";
                    var piece=Instantiate(source,foot.region,"Natural cliff foothill "+index,folder);
                    var bounds=BoundsOf(piece);float factor=Mathf.Min(65/bounds.size.y,145/Mathf.Max(bounds.size.x,bounds.size.z));piece.localScale*=factor;
                    piece.rotation=Quaternion.Euler(0,index*137.508f,0);
                    bool placed=false;
                    for(int trial=0;trial<24;trial++)
                    {
                        float radius=trial==0?0:18*Mathf.Sqrt(trial);float angle=trial*2.399963f;
                        Vector3 center=foot.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                        try{center.y=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,center)-1;}catch(InvalidOperationException){center.y=foot.baseY-1;}
                        AlignBase(piece,center);Physics.SyncTransforms();
                        if(ClearOfRoutes(piece,world,box)){records.Add(new Placement{name=piece.name,source=source,bounds=BoundsOf(piece),scale=piece.localScale,trial=trial});placed=true;break;}
                    }
                    if(!placed)throw new InvalidOperationException("No bounded clear placement for retained natural foothill "+index);
                    index++;
                }
                UnityEngine.Object.DestroyImmediate(probe);probe=null;
                foreach(var p in protectedTransforms)if(p.Key==null||TransformPose(p.Key)!=p.Value)throw new InvalidOperationException("Candidate changed an accepted non-cosmetic transform: "+(p.Key==null?"missing":p.Key.name));
                foreach(var c in protectedColliders)if(c.Key==null||EditorJsonUtility.ToJson(c.Key)!=c.Value)throw new InvalidOperationException("Candidate changed an accepted road/start/extraction collider.");
                var definition=UnityEngine.Object.Instantiate(world.definition);definition.name="Dragon Kingdom cosmetic candidate "+tag;
                foreach(var region in world.regions)
                {
                    var record=definition.FindRegion(region.id);var bounds=BoundsOf(region.root);
                    foreach(var start in region.truckStarts)bounds.Encapsulate(start.position);
                    bounds.Encapsulate(record.center);
                    record.bounds=bounds;
                }
                if(JsonUtility.ToJson(definition.graph)!=originalDefinitions)throw new InvalidOperationException("Candidate changed the accepted navigation graph.");
                AssetDatabase.CreateAsset(definition,receipt.definition);world.definition=definition;
                Physics.SyncTransforms();DragonKingdomWorldAuthoring.ValidatePhysicalRoads(world.transform,definition.graph);
                DragonKingdomWorldAuthoring.ValidateTruckStarts(world,false);world.ValidateReferences();
                receipt.originalPhysicalGatesPassed=true;
                PrefabUtility.SaveAsPrefabAsset(world.gameObject,receipt.prefab);
                if(!EditorSceneManager.SaveScene(scene,receipt.preview))throw new InvalidOperationException("Candidate preview did not save.");
                receipt.acceptedAssetsExact=preserved.All(p=>Hash(File.ReadAllBytes(p.Key))==p.Value);
                receipt.acceptedTerrainAssetsExact=receipt.roadMeshesExact=receipt.acceptedAssetsExact;receipt.startsAndExtractionsExact=true;receipt.placements=records.ToArray();
                if(!receipt.acceptedAssetsExact)throw new InvalidOperationException("A review candidate changed accepted source asset bytes.");
                LastReceipt=receipt;string output=Output();Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"cosmetic-candidate-"+tag+".json"),JsonUtility.ToJson(receipt,true));
            }
            finally{if(probe!=null)UnityEngine.Object.DestroyImmediate(probe);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(prior);}
        }

        static void PlaceMountain(DragonKingdomWorld world,string region,string name,Vector3 baseCenter,float height,float yaw,BoxCollider probe,string folder,List<Placement> records)
        {
            string source=Mountain+name;var piece=Instantiate(source,world.Region(region).root,"Natural mountain "+region+" "+name,folder);var bounds=BoundsOf(piece);
            piece.localScale*=height/bounds.size.y;piece.rotation=Quaternion.Euler(0,yaw,0);
            for(int trial=0;trial<20;trial++)
            {
                Vector3 outward=new Vector3(baseCenter.x,0,baseCenter.z-700).normalized;
                AlignBase(piece,baseCenter+outward*(trial*60));Physics.SyncTransforms();
                if(ClearOfRoutes(piece,world,probe)){records.Add(new Placement{name=piece.name,source=source,bounds=BoundsOf(piece),scale=piece.localScale,trial=trial});return;}
            }
            throw new InvalidOperationException("No bounded route-clear placement for whole natural mountain "+name);
        }
        static Transform Instantiate(string source,Transform parent,string name,string folder)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(asset==null)throw new InvalidOperationException("Audited actual mountain/cliff asset required: "+source);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);go.name=name;
            foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh!=null&&filter.GetComponent<Collider>()==null)filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
            foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var original=materials[i];if(original==null)throw new InvalidOperationException("Missing actual candidate source material.");
                    string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
                    materials[i]=DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(original,folder+"/Materials/"+guid+" URP.mat",false,out _);
                }
                renderer.sharedMaterials=materials;
            }
            return go.transform;
        }
        static bool ClearOfRoutes(Transform piece,DragonKingdomWorld world,BoxCollider box)
        {
            var colliders=piece.GetComponentsInChildren<Collider>(true);Bounds bounds=BoundsOf(piece);bounds.Expand(20);
            box.size=new Vector3(3.2f,6,3.6f);
            foreach(var edge in world.definition.graph.edges)foreach(var sample in edge.samples)
            {
                var right=Vector3.Cross(Vector3.up,sample.forward).normalized;
                foreach(float offset in new[]{-.35f,0,.35f})
                {
                    var p=sample.position+right*sample.roadWidthMeters*offset;
                    if(p.x<bounds.min.x||p.x>bounds.max.x||p.z<bounds.min.z||p.z>bounds.max.z)continue;
                    foreach(var collider in colliders)
                    {
                        // Reject burying a route beneath a mountain surface,
                        // including surfaces above the body gate's finite ray.
                        if(collider.Raycast(new Ray(p+Vector3.up*1200,Vector3.down),out var hit,2400)&&hit.point.y>p.y+.2f)return false;
                        if(Physics.ComputePenetration(box,p+Vector3.up*3.3f,Quaternion.LookRotation(sample.forward),collider,collider.transform.position,collider.transform.rotation,out _,out _))return false;
                    }
                }
            }
            box.size=new Vector3(4.4f,6,16);
            foreach(var region in world.regions)foreach(var start in region.truckStarts)
                foreach(var collider in colliders)if(Physics.ComputePenetration(box,start.position+Vector3.up*2, start.rotation,collider,collider.transform.position,collider.transform.rotation,out _,out _))return false;
            foreach(var site in world.extractionSites)foreach(var collider in colliders)
                if(Physics.ComputePenetration(site.secureZone,site.secureZone.transform.position,site.secureZone.transform.rotation,collider,collider.transform.position,collider.transform.rotation,out _,out _))return false;
            return true;
        }
        static Bounds BoundsOf(Transform t)=>DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(t);
        static void AlignBase(Transform t,Vector3 center){var b=BoundsOf(t);t.position+=new Vector3(center.x-b.center.x,center.y-b.min.y,center.z-b.center.z);}
        static string TransformPose(Transform t)=>t.localPosition.ToString("R")+t.localRotation.ToString("R")+t.localScale.ToString("R");
        static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes));}
        static string Output(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");return at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):"Logs/TruckSports/DragonKingdom/CosmeticCandidates";}
    }
}
