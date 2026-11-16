#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonForestMaterialAuthoring
    {
        public const string Vendor=DragonMaterialFamilyAuthoring.Vendor;
        public const string Tree=Vendor+"Prefabs/Trees/P_ENV_tree_ancient_03.prefab";
        public const string Leaf=Vendor+"Materials/M_ENV_tree_leaves_ancient.mat";
        public const string Wood=Vendor+"Materials/M_ENV_wood_ancient_01.mat";
        public const string CanonicalLeaf="Assets/TruckDragonKingdom/Materials/Environment/a35168ec5f8b41447ba5916c93191534 URP.mat";
        public const string CanonicalWood="Assets/TruckDragonKingdom/Materials/Environment/d768d77458b842644b856380df9a044f URP.mat";
        public const string Proof="Assets/TruckDragonKingdom/VisualCandidates/ForestMaterialProof";
        [Serializable] sealed class Receipt
        {
            public string scope="Explicit two owned forest material configurations only; original vendor sources, scene/prefab geometry and LOD settings unchanged. Native three-LOD render/view-range proof remains separate.";
            public bool repeatByteExact,originalBytesExact,scenePrefabBytesExact,materialGuidsExact;
            public string[] targets,before,after,sourcePaths,sourceHashes,preservedPaths,preservedHashes;
        }
        public static void PrepareForestProofMaterials()
        {
            Guard();var source=Originals();Directory.CreateDirectory(Proof);AssetDatabase.Refresh();
            var originals=new[]{Leaf,Wood};var canonical=new[]{CanonicalLeaf,CanonicalWood};
            for(int i=0;i<2;i++)
            {
                string label=i==0?"leaf":"wood",observed=Proof+"/Observed original Lit "+label+".mat";
                if(AssetDatabase.LoadAssetAtPath<Material>(observed)==null)
                {
                    if(File.Exists(observed))throw new InvalidOperationException("Preserve existing proof file: "+observed);
                    var old=Require(canonical[i]);if(old.shader==null||old.shader.name!="Universal Render Pipeline/Lit")throw new InvalidOperationException("Snapshot the genuine existing generic forest material before explicit canonical migration; no invented broken baseline.");
                    AssetDatabase.CreateAsset(new Material(old){name="Observed original Lit "+label},observed);
                }
                string target=Proof+"/Configured vendor "+label+".mat";
                DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(Require(originals[i]),target,i==0,out _);string first=Hash(target);
                DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(Require(originals[i]),target,i==0,out bool changed);
                if(changed||Hash(target)!=first)throw new InvalidOperationException("Repeated forest proof configuration changed bytes.");
            }
            Write("prepared",new Receipt{repeatByteExact=true,originalBytesExact=Exact(source),scenePrefabBytesExact=true,materialGuidsExact=true,sourcePaths=source.Keys.ToArray(),sourceHashes=source.Values.ToArray()});
        }
        public static void UpgradeForestMaterials()
        {
            Guard();var sources=Originals();var preserved=ScenePrefabs();string[] targets={CanonicalLeaf,CanonicalWood},originals={Leaf,Wood};
            foreach(string target in targets)Require(target);
            var metadata=targets.Select(target=>Hash(target+".meta")).ToArray();var before=targets.Select(Hash).ToArray();
            for(int i=0;i<targets.Length;i++)DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(Require(originals[i]),targets[i],i==0,out _);
            var first=targets.Select(Hash).ToArray();for(int i=0;i<targets.Length;i++)
            {
                DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(Require(originals[i]),targets[i],i==0,out bool changed);
                if(changed||first[i]!=Hash(targets[i]))throw new InvalidOperationException("Explicit forest upgrade is not byte-idempotent.");
            }
            bool guids=metadata.SequenceEqual(targets.Select(target=>Hash(target+".meta")));if(!guids||!Exact(preserved)||!Exact(sources))throw new InvalidOperationException("Forest migration changed original vendor bytes, material GUIDs or scene/prefab geometry/LOD bytes.");
            Write("upgraded",new Receipt{targets=targets,before=before,after=first,repeatByteExact=true,originalBytesExact=true,scenePrefabBytesExact=true,materialGuidsExact=true,sourcePaths=sources.Keys.ToArray(),sourceHashes=sources.Values.ToArray(),preservedPaths=preserved.Keys.ToArray(),preservedHashes=preserved.Values.ToArray()});
        }
        static Dictionary<string,string> Originals()=>AssetDatabase.GetDependencies(Tree,true).Where(path=>path.StartsWith(Vendor,StringComparison.Ordinal)&&File.Exists(path)).SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).Distinct().OrderBy(path=>path,StringComparer.Ordinal).ToDictionary(path=>path,Hash);
        static Dictionary<string,string> ScenePrefabs()
        {
            var paths=new List<string>{"Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab","Assets/TruckDragonKingdom/Scenes/DragonKingdomPreview.unity"};
            foreach(string mode in new[]{"TruckHoardHeist","TruckDragonEggDelivery","TruckDragonEscape","TruckDragonRiders"})paths.Add("Assets/"+mode+"/Scenes/"+mode+".unity");
            return paths.Where(File.Exists).SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).ToDictionary(path=>path,Hash);
        }
        static bool Exact(Dictionary<string,string> sources)=>sources.All(pair=>Hash(pair.Key)==pair.Value);
        static Material Require(string path)=>AssetDatabase.LoadAssetAtPath<Material>(path)??throw new InvalidOperationException("Actual existing owned forest material required: "+path);
        static void Guard(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Explicit stopped forest material configuration only.");}
        static string Hash(string path){string full=Path.GetFullPath(path);if(Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal))full=@"\\?\"+full;using(var sha=SHA256.Create())using(var stream=File.OpenRead(full))return string.Concat(sha.ComputeHash(stream).Select(value=>value.ToString("x2")));}
        static void Write(string stage,Receipt receipt)
        {
            if(!receipt.originalBytesExact)throw new InvalidOperationException("Original licensed forest source changed.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new ArgumentException("Unique explicit dragon evidence output required.");string path=Path.Combine(args[at+1],"ForestMaterialProof",stage+".json");Directory.CreateDirectory(Path.GetDirectoryName(path));if(File.Exists(path))throw new IOException("Preserve previous forest material receipt.");File.WriteAllText(path,JsonUtility.ToJson(receipt,true)+"\n");Debug.Log("DRAGON_FOREST_MATERIAL_"+stage+" "+path);
        }
    }
}
#endif
