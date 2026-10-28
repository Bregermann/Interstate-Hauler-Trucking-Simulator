using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LWS.TruckCamping.Editor
{
    // Explicit presentation-only upgrade of the four already-reviewed owned
    // TerrainData assets. Never regenerate/reseed terrain or touch vendor data.
    public static class CampingLivingForestAuthoring
    {
        public const string LivingPrefab="Assets/TruckCamping/Prefabs/Camp Haul living Vista pine.prefab";
        public const string SourcePrefab="Assets/PinwheelStudio/Vista/Personal/Samples/Prefabs/VistaSample_PineTree.prefab";
        public const string OldPrefab="Assets/Toby Fredson/Rocky Hills Environment - Whitebark Pine/RHEWP_Demo/Prefabs/Prefabs_Vegetation/PV_Trees/WhitebarkPineRHEP_B.prefab";
        public static readonly string[] TerrainPaths={"Assets/TruckCamping/Models/Camp Haul Vista terrain 0 0.asset","Assets/TruckCamping/Models/Camp Haul Vista terrain 1 0.asset","Assets/TruckCamping/Models/Camp Haul Vista terrain 0 1.asset","Assets/TruckCamping/Models/Camp Haul Vista terrain 1 1.asset"};
        static readonly string[] ReviewedHashes={"a6545790fa5ab3746539fe237fcf0819eee69b00b128a63930b82a2aeebc5a5f","96bb672cf7b4104175f29f3c2341f79935d0ded5c94d0b26bcbeb352dfbe80cb","2d4331bf1b9588f2aa4419c76cb5b74018eedbbb36b14da009fc3c90b00a931c","1bea70d99fcb2d107252f4579029d11ab2879c31cf2e83d7dbbe22e66d3e7915"};
        public static string DigestFile(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static string Hash(byte[] data){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
        static void V(BinaryWriter w,Vector3 v){w.Write(v.x);w.Write(v.y);w.Write(v.z);}
        public static string PreservedTerrainDigest(TerrainData data)
        {
            using(var memory=new MemoryStream())using(var writer=new BinaryWriter(memory))
            {
                writer.Write(data.name);V(writer,data.size);writer.Write(data.heightmapResolution);writer.Write(data.alphamapResolution);writer.Write(data.baseMapResolution);writer.Write(data.detailResolution);writer.Write(data.detailResolutionPerPatch);
                foreach(var value in data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution))writer.Write(value);
                foreach(var value in data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight))writer.Write(value);
                foreach(var value in data.GetHoles(0,0,data.holesResolution,data.holesResolution))writer.Write(value);
                writer.Write(data.terrainLayers.Length);foreach(var layer in data.terrainLayers)writer.Write(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(layer)));
                writer.Write(data.detailPrototypes.Length);for(int layer=0;layer<data.detailPrototypes.Length;layer++){writer.Write(EditorJsonUtility.ToJson(data.detailPrototypes[layer]));foreach(var value in data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,layer))writer.Write(value);}
                writer.Write(data.treeInstanceCount);foreach(var tree in data.treeInstances){V(writer,tree.position);writer.Write(tree.widthScale);writer.Write(tree.heightScale);writer.Write(tree.rotation);writer.Write(tree.prototypeIndex);writer.Write(tree.color.r);writer.Write(tree.color.g);writer.Write(tree.color.b);writer.Write(tree.color.a);writer.Write(tree.lightmapColor.r);writer.Write(tree.lightmapColor.g);writer.Write(tree.lightmapColor.b);writer.Write(tree.lightmapColor.a);}
                writer.Flush();return Hash(memory.ToArray());
            }
        }
        static Bounds BoundsOf(GameObject root){var rs=root.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)throw new InvalidOperationException("Original tree rendering missing.");var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        static GameObject CreateOwnedVariant()
        {
            if(File.Exists(LivingPrefab)||File.Exists(LivingPrefab+".meta")||AssetDatabase.LoadAssetAtPath<GameObject>(LivingPrefab)!=null)throw new InvalidOperationException("Existing/custom living tree prefab preserved; verify its prior explicit upgrade instead of overwriting it.");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);var old=AssetDatabase.LoadAssetAtPath<GameObject>(OldPrefab);if(source==null||old==null)throw new InvalidOperationException("Reviewed installed living Vista and old Whitebark prototypes required.");
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var original=(GameObject)PrefabUtility.InstantiatePrefab(old,scene);var clone=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);original.transform.position=clone.transform.position=Vector3.zero;
                if(clone.GetComponentsInChildren<Rigidbody>(true).Length!=0||clone.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Living prototype must remain render-only.");
                var before=clone.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray();var wanted=BoundsOf(original);var bounds=BoundsOf(clone);
                if(clone.transform.localScale!=Vector3.one||clone.transform.rotation!=Quaternion.identity)throw new InvalidOperationException("Reviewed original Vista identity root required.");
                // Tree rendering may replace the prototype root transform with
                // TreeInstance pose. Bake the same uniform fit into the existing
                // render-only child transforms, keeping this variant root unit.
                float fit=wanted.size.y/bounds.size.y;foreach(Transform child in clone.transform){child.localPosition*=fit;child.localScale*=fit;}
                bounds=BoundsOf(clone);var offset=new Vector3(wanted.center.x-bounds.center.x,wanted.min.y-bounds.min.y,wanted.center.z-bounds.center.z);foreach(Transform child in clone.transform)child.localPosition+=offset;
                clone.name="Camp Haul living Vista pine";
                var after=clone.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray();if(!before.SequenceEqual(after))throw new InvalidOperationException("Vendor materials must remain exact references.");
                var actual=BoundsOf(clone);if(Mathf.Abs(actual.size.y-wanted.size.y)>.001f||Mathf.Abs(actual.min.y-wanted.min.y)>.001f)throw new InvalidOperationException("Owned prototype must match original tree height/root offset.");
                Directory.CreateDirectory(Path.GetDirectoryName(LivingPrefab));var result=PrefabUtility.SaveAsPrefabAsset(clone,LivingPrefab);if(result==null)throw new InvalidOperationException("Owned prefab variant save failed.");return result;
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        public static void UpgradeExactReviewedWhitebarkPrototypes()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Explicit stopped author operation only.");
            var data=TerrainPaths.Select(AssetDatabase.LoadAssetAtPath<TerrainData>).ToArray();
            for(int i=0;i<data.Length;i++)
                if(data[i]==null||DigestFile(TerrainPaths[i])!=ReviewedHashes[i]||data[i].treePrototypes.Length!=1||AssetDatabase.GetAssetPath(data[i].treePrototypes[0].prefab)!=OldPrefab||data[i].treePrototypes[0].bendFactor!=.2f)throw new InvalidOperationException("Unknown/customized original forest preserved: "+TerrainPaths[i]);
            if(data.Sum(t=>t.treeInstanceCount)!=2297)throw new InvalidOperationException("Exact reviewed2297instances required.");
            if(File.Exists(LivingPrefab)||File.Exists(LivingPrefab+".meta"))throw new InvalidOperationException("Existing/custom living prefab preserved before any terrain mutation.");
            string backup=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingLivingForestUpgrade/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(backup);
            var before=data.Select(PreservedTerrainDigest).ToArray();var metas=TerrainPaths.Select(p=>DigestFile(p+".meta")).ToArray();
            for(int i=0;i<data.Length;i++){File.Copy(TerrainPaths[i],Path.Combine(backup,i+"-original.asset"));File.Copy(TerrainPaths[i]+".meta",Path.Combine(backup,i+"-original.asset.meta"));}
            var living=CreateOwnedVariant();
            // Verify the actual native TerrainData setter on detached clones
            // before touching any persistent original payload.
            for(int i=0;i<data.Length;i++)
            {
                var clone=UnityEngine.Object.Instantiate(data[i]);try{clone.name=data[i].name;clone.treePrototypes=new[]{new TreePrototype{prefab=living,bendFactor=.2f}};if(PreservedTerrainDigest(clone)!=before[i])throw new InvalidOperationException("Native prototype setter altered original tree/terrain payload.");}finally{UnityEngine.Object.DestroyImmediate(clone);}
            }
            for(int i=0;i<data.Length;i++){data[i].treePrototypes=new[]{new TreePrototype{prefab=living,bendFactor=.2f}};if(PreservedTerrainDigest(data[i])!=before[i])throw new InvalidOperationException("Original terrain payload unexpectedly changed; exact backup retained: "+backup);EditorUtility.SetDirty(data[i]);}
            AssetDatabase.SaveAssets();
            var report=new UpgradeReceipt{backup=backup,source=SourcePrefab,owned=LivingPrefab,files=TerrainPaths.Select((p,i)=>new TerrainReceipt{path=p,guid=AssetDatabase.AssetPathToGUID(p),originalSha256=ReviewedHashes[i],afterSha256=DigestFile(p),metaSha256=DigestFile(p+".meta"),preservedDigest=PreservedTerrainDigest(data[i]),trees=data[i].treeInstanceCount}).ToArray()};
            for(int i=0;i<data.Length;i++)if(before[i]!=report.files[i].preservedDigest||metas[i]!=report.files[i].metaSha256)throw new InvalidOperationException("Post-save original terrain/GUID invariants changed; backup retained.");
            File.WriteAllText(Path.Combine(backup,"upgrade-receipt.json"),JsonUtility.ToJson(report,true));File.WriteAllText("Logs/TruckSports/Impossible07/CampingLivingForestUpgrade/latest-upgrade-receipt.json",JsonUtility.ToJson(report,true));
            Debug.Log("CAMP_LIVING_FOREST_UPGRADE exact4TerrainData/2297instances/heights/paint/holes/layers/details/GUIDs retained; prototype-only vendor Vista variant. Backup="+backup);
        }
        [Serializable]public sealed class UpgradeReceipt{public string status="EXACT_REVIEWED_PROTOTYPE_ONLY_UPGRADE",backup,source,owned;public TerrainReceipt[] files;}
        [Serializable]public sealed class TerrainReceipt{public string path,guid,originalSha256,afterSha256,metaSha256,preservedDigest;public int trees;}
    }
}
