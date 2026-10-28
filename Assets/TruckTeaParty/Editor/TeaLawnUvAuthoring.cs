using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using LWS.TruckCamping.Editor;

namespace LWS.TruckTeaParty.Editor
{
    // Explicit repair of the one saved lawn render binding. Furniture, source
    // geometry, colliders, materials and every other scene byte stay intact.
    public static class TeaLawnUvAuthoring
    {
        public const string Scene = "Assets/TruckTeaParty/Scenes/TruckTeaParty.unity";
        public const string Source = "Assets/TruckTeaParty/Models/Original bevelled serving furniture.asset";
        public const string Output = "Assets/TruckTeaParty/Models/Tea garden lawn valid UV tangent surface.asset";
        public const string OriginalSceneSha = "d287b0f871a00c4492723ba6863a965876a446a7bdf92944ddb83ac3178fb5a8";
        const string SourceSha = "e429843bf746649c332550770af5ae13058f6eac4d736e45855b97754c67d04b";
        const string SourceMetaSha = "aca189b734b4e35a48534f40debaa170e017ed86268ad2457363d19dd96126ec";
        const string OriginalGuid = "80c1566fe1e6fbf47aef1dbe1d495fc2";
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        public static string Hash(string path)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Tea lawn-only UV repair: " + message + "; existing customization preserved."); }
        static void OriginalInputs()
        { Require(Hash(Source)==SourceSha && Hash(Source+".meta")==SourceMetaSha,"exact original serving mesh and GUID"); }
        public static Mesh CreateTransient()
        {
            OriginalInputs();
            // Reuse the already tested project-owned seam/normal/tangent repair
            // for this identical rounded-block topology; no second algorithm.
            var mesh=CampingWaterPresentationAuthoring.CreateTransientWaterMesh(AssetDatabase.LoadAssetAtPath<Mesh>(Source));
            mesh.name="Tea garden lawn valid planar UV tangent surface";
            return mesh;
        }
        public static string RewriteOnlyLawnMesh(string text,string from,string to)
        {
            Require(Regex.IsMatch(from,"\\A[0-9a-f]{32}\\z") && Regex.IsMatch(to,"\\A[0-9a-f]{32}\\z"),"exact mesh GUIDs");
            var matches=Regex.Matches(text,@"(?ms)^--- !u!33 &213497243\r?\n.*?(?=^--- !u!|\z)");
            Require(matches.Count==1,"one original lawn MeshFilter document");var doc=matches[0];
            Require(doc.Value.Contains("m_GameObject: {fileID: 213497239}"),"original lawn object identity");
            string field="m_Mesh: {fileID: 4300000, guid: "+from+", type: 2}";
            Require(doc.Value.Split(new[]{field},StringSplitOptions.None).Length==2,"one exact lawn mesh reference");
            string replaced=doc.Value.Replace(field,field.Replace(from,to));
            return text.Substring(0,doc.Index)+replaced+text.Substring(doc.Index+doc.Length);
        }
        public static void InstallReviewedLawnUv()
        {
            Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling,"stopped compiled Editor");OriginalInputs();
            Require(Hash(Scene)==OriginalSceneSha,"original saved scene stage");
            Require(!File.Exists(Output) && !File.Exists(Output+".meta") && AssetDatabase.LoadMainAssetAtPath(Output)==null,"new owned derivative only");
            byte[] before=File.ReadAllBytes(Scene),meta=File.ReadAllBytes(Scene+".meta");string text=Utf8.GetString(before);
            Require(before.SequenceEqual(Utf8.GetBytes(text)),"lossless original UTF8 bytes");
            // Preflight the exact field and inverse before creating any asset.
            string trial=RewriteOnlyLawnMesh(text,OriginalGuid,new string('a',32));
            Require(RewriteOnlyLawnMesh(trial,new string('a',32),OriginalGuid)==text,"whole-scene inverse preflight");
            string folder="Logs/TruckSports/Impossible07/TeaLawnUvRepair/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,"original-scene.bytes"),before);File.WriteAllBytes(Path.Combine(folder,"original-scene.meta.bytes"),meta);
            var mesh=CreateTransient();AssetDatabase.CreateAsset(mesh,Output);AssetDatabase.SaveAssetIfDirty(mesh);
            AssetDatabase.ImportAsset(Output,ImportAssetOptions.ForceSynchronousImport);
            string guid=AssetDatabase.AssetPathToGUID(Output);Require(!string.IsNullOrEmpty(guid),"real new asset GUID");
            byte[] after=Utf8.GetBytes(RewriteOnlyLawnMesh(text,OriginalGuid,guid));
            Require(File.ReadAllBytes(Scene).SequenceEqual(before) && File.ReadAllBytes(Scene+".meta").SequenceEqual(meta),"unchanged scene before reference write");
            File.WriteAllBytes(Scene,after);AssetDatabase.ImportAsset(Scene,ImportAssetOptions.ForceSynchronousImport);
            Require(File.ReadAllBytes(Scene+".meta").SequenceEqual(meta),"original scene GUID/importer");
            VerifyInstalled();
            File.WriteAllText(Path.Combine(folder,"receipt.txt"),"ONE_LAWN_RENDER_MESH_REFERENCE_ONLY\nOriginalScene="+OriginalSceneSha+"\nCurrentScene="+Hash(Scene)+"\nSourceMesh="+Hash(Source)+"\nOutputMesh="+Hash(Output)+"\nOutputMeta="+Hash(Output+".meta")+"\nNo collider, transform, material, rules, source mesh or vendor changes.\n",Utf8);
            Debug.Log("TEA_LAWN_UV_REPAIR "+Path.GetFullPath(folder));
        }
        public static void VerifyInstalled()
        {
            OriginalInputs();var source=AssetDatabase.LoadAssetAtPath<Mesh>(Source);var derived=AssetDatabase.LoadAssetAtPath<Mesh>(Output);
            Require(source!=null && derived!=null,"original and separate derived meshes");CampingWaterPresentationAuthoring.ValidateDerivedMesh(source,derived);
            string guid=AssetDatabase.AssetPathToGUID(Output);string restored=RewriteOnlyLawnMesh(File.ReadAllText(Scene,Utf8),guid,OriginalGuid);
            using(var sha=SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(Utf8.GetBytes(restored))).Replace("-","").ToLowerInvariant()==OriginalSceneSha,"every other scene byte including physics/transforms/materials exact");
        }
    }
}
