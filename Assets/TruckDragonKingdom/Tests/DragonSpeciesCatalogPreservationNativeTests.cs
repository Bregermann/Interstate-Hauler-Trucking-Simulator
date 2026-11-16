#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DragonSpeciesCatalogPreservationNativeTests
{
    const string CatalogPath="Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset";
    const string Configured="Assets/TruckDragonKingdom/Prefabs/Dragons";
    const string Authored="Assets/TruckDragonKingdom/Animations/Authored";
    const string Raw="Assets/TruckDragonKingdom/PrivateLicensedDragons";
    const string Animal="Assets/TruckDragonKingdom/PrivateLicensedAnimalController";
    static object Read(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static void Write(object value,string name,object data)=>value.GetType().GetField(name).SetValue(value,data);
    static string IoPath(string path)
    {
        string absolute=Path.GetFullPath(path);
        // Unity's Windows Mono File APIs otherwise reject real owned files
        // discovered by Directory.GetFiles once the validation root adds length.
        if(Environment.OSVersion.Platform==PlatformID.Win32NT&&absolute.Length>=248&&!absolute.StartsWith(@"\\?\",StringComparison.Ordinal))
            return absolute.StartsWith(@"\\",StringComparison.Ordinal)?@"\\?\UNC\"+absolute.Substring(2):@"\\?\"+absolute;
        return absolute;
    }
    static string Hash(string path){using(var hash=SHA256.Create())using(var input=File.OpenRead(IoPath(path)))return string.Concat(hash.ComputeHash(input).Select(value=>value.ToString("x2")));}
    static string[] Paths()=>new[]{Configured,Authored,Raw,Animal}.SelectMany(folder=>Directory.GetFiles(folder,"*",SearchOption.AllDirectories)).Concat(new[]{CatalogPath,CatalogPath+".meta"}).OrderBy(path=>path,StringComparer.Ordinal).ToArray();
    static Dictionary<string,string> Snapshot()=>Paths().ToDictionary(path=>path,Hash,StringComparer.Ordinal);
    static void Exact(Dictionary<string,string> expected)
    {
        CollectionAssert.AreEquivalent(expected.Keys,Paths(),"Repeated creation cannot generate/remove clips or configured/raw files.");
        foreach(var item in expected)Assert.That(Hash(item.Key),Is.EqualTo(item.Value),"Existing asset or metadata must stay byte-exact: "+item.Key);
    }
    static object Definition(ScriptableObject catalog,string id)=>((IEnumerable)Read(catalog,"species")).Cast<object>().Single(value=>(string)Read(value,"id")==id);
    static object Mapping(object species,string action)=>((IEnumerable)Read(species,"animations")).Cast<object>().Single(value=>Read(value,"action").ToString()==action);

    [Test,Timeout(180000)]
    public void RepeatedCreateCatalogPreservesAcceptedActionsPersistedDesignerDataAndUnrelatedDirtyAsset()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False,"Explicit stopped native authoring only.");
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);Assert.That(catalog,Is.Not.Null,"Actual accepted catalog must already exist; this regression never regenerates it.");
        Assert.That(EditorUtility.IsDirty(catalog),Is.False,"Save catalog designer edits before running the isolated preservation contract.");
        var create=Type.GetType("LWS.TruckDragonKingdom.DragonSpeciesAuthoring, Assembly-CSharp-Editor",true).GetMethod("CreateCatalog");
        var species=Definition(catalog,"dragon-inferno");var groundGrab=Mapping(species,"GroundGrab");var breath=Mapping(species,"Breath");
        var groundClip=Read(groundGrab,"clip") as AnimationClip;var breathClip=Read(breath,"clip") as AnimationClip;
        Assert.That(groundClip,Is.SameAs(AssetDatabase.LoadAssetAtPath<AnimationClip>(Authored+"/dragon-inferno/GroundGrab.anim")),"Accepted explicit grounded action is a prerequisite.");
        Assert.That(groundClip,Is.Not.Null);Assert.That(breathClip,Is.Not.Null);Assert.That(AssetDatabase.GetAssetPath(breathClip),Is.EqualTo(Authored+"/dragon-inferno/Breath.anim"),"Use accepted directed Breath, not the raw Cast Spell mapping.");
        var replacement=AssetDatabase.LoadAssetAtPath<GameObject>(Configured+"/dragon-inferno/dragon-inferno Dragon Animal.prefab");Assert.That(replacement,Is.Not.Null);
        if(ReferenceEquals(replacement,Read(species,"visualPrefab")))replacement=AssetDatabase.LoadAssetAtPath<GameObject>(Raw+"/Monsters Ultimate Pack 02 Cute Series/Dragon Inferno Cute Series/Prefabs/Dragon Inferno.prefab");
        Assert.That(replacement,Is.Not.Null);Assert.That(replacement,Is.Not.SameAs(Read(species,"visualPrefab")),"A genuine persisted reference customization is exercised.");
        var original=Snapshot();byte[] catalogBytes=File.ReadAllBytes(IoPath(CatalogPath)),metaBytes=File.ReadAllBytes(IoPath(CatalogPath+".meta"));string originalJson=EditorJsonUtility.ToJson(catalog),catalogGuid=AssetDatabase.AssetPathToGUID(CatalogPath);
        string folder="Assets/TruckDragonKingdom/Tests/CatalogPreservationNative_"+Guid.NewGuid().ToString("N"),clonePath=folder+"/Unrelated designer clone.asset";bool customized=false;ScriptableObject clone=null;
        try
        {
            Assert.That(AssetDatabase.CreateFolder("Assets/TruckDragonKingdom/Tests",Path.GetFileName(folder)),Is.Not.Empty);
            clone=UnityEngine.Object.Instantiate(catalog);AssetDatabase.CreateAsset(clone,clonePath);AssetDatabase.SaveAssetIfDirty(clone);string cloneDisk=Hash(clonePath);
            Write(Definition(clone,"dragon-inferno"),"maximumStamina",(float)Read(species,"maximumStamina")+17.25f);EditorUtility.SetDirty(clone);
            customized=true;float stamina=(float)Read(species,"maximumStamina")+.125f;Write(species,"maximumStamina",stamina);Write(species,"visualPrefab",replacement);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
            string customizedJson=EditorJsonUtility.ToJson(catalog);var expected=Snapshot();var entries=Read(catalog,"species");
            for(int repeat=0;repeat<3;repeat++)
            {
                Assert.That(create.Invoke(null,null),Is.SameAs(catalog));Assert.That(Read(catalog,"species"),Is.SameAs(entries));Assert.That(Definition(catalog,"dragon-inferno"),Is.SameAs(species));
                Assert.That(Read(species,"maximumStamina"),Is.EqualTo(stamina));Assert.That(Read(species,"visualPrefab"),Is.SameAs(replacement));
                Assert.That(Mapping(species,"GroundGrab"),Is.SameAs(groundGrab));Assert.That(Read(groundGrab,"clip"),Is.SameAs(groundClip));Assert.That(Mapping(species,"Breath"),Is.SameAs(breath));Assert.That(Read(breath,"clip"),Is.SameAs(breathClip));
                Assert.That(EditorJsonUtility.ToJson(catalog),Is.EqualTo(customizedJson));Assert.That(EditorUtility.IsDirty(catalog),Is.False);Assert.That(AssetDatabase.AssetPathToGUID(CatalogPath),Is.EqualTo(catalogGuid));
                Assert.That(Hash(clonePath),Is.EqualTo(cloneDisk),"Read-only catalog validation cannot save an unrelated designer clone.");Assert.That(EditorUtility.IsDirty(clone),Is.True);Exact(expected);
            }
            TestContext.WriteLine("CATALOG_PRESERVATION acceptedGroundGrab="+AssetDatabase.GetAssetPath(groundClip)+" directedBreath="+AssetDatabase.GetAssetPath(breathClip)+" repeats=3 exactRawConfiguredAuthoredFiles="+expected.Count+" persistedDesignerScalarAndReference=true unrelatedDirtyCloneUnsaved=true");
        }
        finally
        {
            // This contract runs on the native validation copy. Restore both
            // memory and original disk bytes even when an assertion fails.
            if(customized){EditorJsonUtility.FromJsonOverwrite(originalJson,catalog);EditorUtility.ClearDirty(catalog);File.WriteAllBytes(IoPath(CatalogPath),catalogBytes);File.WriteAllBytes(IoPath(CatalogPath+".meta"),metaBytes);AssetDatabase.ImportAsset(CatalogPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);}
            if(AssetDatabase.IsValidFolder(folder))AssetDatabase.DeleteAsset(folder);
        }
        Exact(original);Assert.That(AssetDatabase.AssetPathToGUID(CatalogPath),Is.EqualTo(catalogGuid));Assert.That(EditorJsonUtility.ToJson(catalog),Is.EqualTo(originalJson));
    }
}
#endif
