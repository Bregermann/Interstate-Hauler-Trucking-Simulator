#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DragonKingdomModelBoundsTests
{
    [Serializable]sealed class Facts{public string scope="ACTUAL_OWNED_PREFAB_STATIC_MODEL_GEOMETRY_NATIVE_EDITOR_NOT_GAMEPLAY_OR_WINDOWS_REVIEW";public Fact[] houses;public bool ownedDependencyBytesPreserved;}
    [Serializable]sealed class Fact{public string source;public int retainedSteamRenderers,meshFilters;public Bounds modelAtOrigin,modelTranslated,allRendererBoundsTranslated;public Vector3 translation;}
    [Test]public void ActualOwnedHouseModelBoundsExcludeSteamWithoutDeletingEffectsOrChangingSourceBytes()
    {
        const string prefix="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Plugins/Tidal Flask Studios/Fantastic Village Pack/prefabs/buildings/";
        var sources=new[]{prefix+"P_BLD_house_02.prefab",prefix+"P_BLD_house_05.prefab"};
        var files=sources.SelectMany(p=>AssetDatabase.GetDependencies(p,true)).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)).SelectMany(p=>new[]{p,p+".meta"}).Distinct().ToDictionary(p=>p,Hash);
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring, Assembly-CSharp-Editor",true);var method=type.GetMethod("MeasureOwnedModelBounds",new[]{typeof(Transform)});Assert.That(method,Is.Not.Null);
        var rows=new List<Fact>();
        foreach(string source in sources)
        {
            GameObject root=null;
            try
            {
                root=PrefabUtility.LoadPrefabContents(source);var effects=root.GetComponentsInChildren<ParticleSystemRenderer>(true);Assert.That(effects.Length,Is.GreaterThan(0),"This regression exercises the actual house with its original steam effects.");
                var enabled=effects.Select(r=>r.enabled).ToArray();var materials=effects.Select(r=>r.sharedMaterials).ToArray();
                root.transform.position=Vector3.zero;root.transform.rotation=Quaternion.Euler(0,37,0);var first=(Bounds)method.Invoke(null,new object[]{root.transform});
                var delta=new Vector3(-775.4267f,20,-596.0717f);root.transform.position=delta;var translated=(Bounds)method.Invoke(null,new object[]{root.transform});
                Assert.That(Vector3.Distance(translated.center-first.center,delta),Is.LessThan(.001f));Assert.That(Vector3.Distance(translated.size,first.size),Is.LessThan(.001f));
                Assert.That(Mathf.Max(translated.size.x,translated.size.z),Is.LessThan(64),"A complete source house cannot acquire a785m footprint from an empty effect renderer.");
                Assert.That(translated.Contains(Vector3.zero),Is.False,"The distant real house has no model geometry at world origin.");
                Assert.That(root.GetComponentsInChildren<ParticleSystemRenderer>(true),Is.EqualTo(effects));
                for(int i=0;i<effects.Length;i++){Assert.That(effects[i].enabled,Is.EqualTo(enabled[i]));Assert.That(effects[i].sharedMaterials,Is.EqualTo(materials[i]));}
                var renderers=root.GetComponentsInChildren<Renderer>(true);var all=renderers[0].bounds;foreach(var r in renderers.Skip(1))all.Encapsulate(r.bounds);
                rows.Add(new Fact{source=source,retainedSteamRenderers=effects.Length,meshFilters=root.GetComponentsInChildren<MeshFilter>(true).Count(f=>f.sharedMesh!=null),modelAtOrigin=first,modelTranslated=translated,allRendererBoundsTranslated=all,translation=delta});
            }
            finally{if(root!=null)PrefabUtility.UnloadPrefabContents(root);}
        }
        bool exact=files.All(p=>Hash(p.Key)==p.Value);var report=new Facts{houses=rows.ToArray(),ownedDependencyBytesPreserved=exact};
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");string output=at>=0&&at+1<args.Length?args[at+1]:"Logs/TruckSports/DragonKingdom/ModelBounds";Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"actual-house-model-bounds.json"),JsonUtility.ToJson(report,true));
        Assert.That(exact,Is.True,"Every original owned dependency and metadata byte must remain unchanged.");
    }
    static string Hash(string path)
    {string full=Path.GetFullPath(path);if(Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal))full=@"\\?\"+full;using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(full)));}
}
#endif
