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
    // Explicit owned configuration only. Original shader/material/texture/mesh
    // bytes and accepted world geometry remain untouched.
    public static class DragonMaterialFamilyAuthoring
    {
        public const string Vendor="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Fantastic Ancient Forest/";
        public const string Column=Vendor+"Materials/M_PROP_column_ancient_02.mat";
        public const string Leaf=Vendor+"Materials/M_ENV_tree_leaves_ancient.mat";
        public const string ExistingColumn="Assets/TruckDragonKingdom/Materials/Environment/6045b0d731b32e345a193bf7cff0b759 URP.mat";
        public const string Proof="Assets/TruckDragonKingdom/VisualCandidates/MaterialFamilyProof";
        [Serializable] sealed class Receipt
        {
            public string scope="Explicit material-family configuration; native controlled rendering/live wind/human acceptance still require separate proof.";
            public bool changed,repeatByteExact,originalsPreserved;
            public string[] paths,before,after;
        }
        public static void PrepareControlledProofMaterials()
        {
            Guard();var before=Originals();Directory.CreateDirectory(Proof);AssetDatabase.Refresh();
            var observed=AssetDatabase.LoadAssetAtPath<Material>(Proof+"/Observed pre-repair column.mat");
            if(observed==null)
            {
                if(File.Exists(Proof+"/Observed pre-repair column.mat"))throw new InvalidOperationException("Preserve existing proof file; no replacement of a differing/non-Material asset.");
                var prior=AssetDatabase.LoadAssetAtPath<Material>(ExistingColumn);
                if(prior==null||prior.shader==null||!prior.shader.name.StartsWith("Universal Render Pipeline/",StringComparison.Ordinal)||prior.GetTexture("_BaseMap")!=null)
                    throw new InvalidOperationException("Preserve the genuine observed white project variant before product migration; no invented baseline.");
                observed=new Material(prior){name="Observed pre-repair column"};AssetDatabase.CreateAsset(observed,Proof+"/Observed pre-repair column.mat");
            }
            var source=Require(Column);DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(source,Proof+"/Configured original triplanar.mat",false,out _);
            DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(Require(Leaf),Proof+"/Configured existing foliage.mat",true,out _);
            string fixedPath=Proof+"/Configured original triplanar.mat",first=Hash(fixedPath);
            DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(source,fixedPath,false,out bool secondChanged);
            if(secondChanged||first!=Hash(fixedPath))throw new InvalidOperationException("Explicit repeated triplanar proof configuration changed existing bytes.");
            Write("prepared",before,false,true);
        }
        public static void UpgradeAncientColumnMaterial()
        {
            Guard();var before=Originals();if(AssetDatabase.LoadAssetAtPath<Material>(ExistingColumn)==null)throw new InvalidOperationException("Existing selected project-owned column material required; no world regeneration.");
            DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(Require(Column),ExistingColumn,false,out bool changed);
            string first=Hash(ExistingColumn);DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(Require(Column),ExistingColumn,false,out bool secondChanged);
            if(secondChanged||first!=Hash(ExistingColumn))throw new InvalidOperationException("Repeated selected material migration changed bytes.");
            Write("upgraded",before,changed,true);
        }
        static Material Require(string path)=>AssetDatabase.LoadAssetAtPath<Material>(path)??throw new InvalidOperationException("Actual owned source material required: "+path);
        static Dictionary<string,string> Originals()
        {
            return AssetDatabase.GetDependencies(Vendor+"Prefabs/Props/P_PROP_column_ancient_02.prefab",true).Concat(AssetDatabase.GetDependencies(Leaf,true)).Where(File.Exists).SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).Distinct().OrderBy(path=>path,StringComparer.Ordinal).ToDictionary(path=>path,Hash);
        }
        static string Hash(string path){using(var sha=SHA256.Create())using(var input=File.OpenRead(path))return string.Concat(sha.ComputeHash(input).Select(value=>value.ToString("x2")));}
        static void Guard(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Explicit stopped material authoring only.");}
        static void Write(string stage,Dictionary<string,string> originals,bool changed,bool repeat)
        {
            var after=originals.Keys.Select(Hash).ToArray();if(!after.SequenceEqual(originals.Values))throw new InvalidOperationException("Original licensed source changed during owned material configuration.");
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");
            string path=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:"Logs/TruckSports/DragonKingdom","MaterialFamilyProof",stage+".json"));Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path,JsonUtility.ToJson(new Receipt{changed=changed,repeatByteExact=repeat,originalsPreserved=true,paths=originals.Keys.ToArray(),before=originals.Values.ToArray(),after=after},true)+"\n");Debug.Log("DRAGON_VENDOR_TRIPLANAR_"+stage+" receipt="+path+"; no visual/human acceptance inferred.");
        }
    }
}
#endif
