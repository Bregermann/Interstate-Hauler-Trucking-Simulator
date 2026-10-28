#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CampingPurchasedDressingTests
{
    const string Shared="Assets/TruckCamping/Prefabs/Camp Haul shared wilderness.prefab";
    const string Owned="Owned purchased campsite dressing V1";
    static Type Author=>Type.GetType("LWS.TruckCamping.Editor.CampingCampsiteDressingAuthoring, Assembly-CSharp-Editor",true);
    static string Call(string name,params object[] args)=>(string)Author.GetMethod(name,BindingFlags.Public|BindingFlags.Static).Invoke(null,args);
    static string Hash(string p){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(File.ReadAllBytes(p)).Select(x=>x.ToString("x2")));}
    static string[] Files()=>AssetDatabase.GetDependencies(Shared,true).Where(File.Exists).SelectMany(p=>File.Exists(p+".meta")?new[]{p,p+".meta"}:new[]{p}).Distinct().ToArray();
    [Test]
    public void ExactPurchasedModelsAddOnlyFourteenRenderSubtreesAndLeaveOriginalWorldAndDependenciesUnchanged()
    {
        Assert.That(AssetDatabase.AssetPathToGUID(Shared),Is.EqualTo("2ec3264f79b522a49bcddfec51fc1cda"));var files=Files();var hashes=files.Select(Hash).ToArray();var root=PrefabUtility.LoadPrefabContents(Shared);
        try
        {
            string before=Call("OriginalGraphDigest",root);var original=root.GetComponentsInChildren<Component>(true).Where(c=>!UnderOwned(c.transform)).Select(c=>c.GetInstanceID()).ToArray();
            Assert.That(Call("ValidateOrBuildDetached",root),Is.EqualTo(before));var owned=root.transform.Find(Owned);Assert.That(owned,Is.Not.Null);Assert.That(owned.childCount,Is.EqualTo(14));
            Assert.That(owned.GetComponentsInChildren<Component>(true).All(c=>c is Transform||c is MeshFilter||c is MeshRenderer||c is LODGroup),Is.True,"No colliders, bodies, scripts, lights or nav modifiers added.");
            Assert.That(root.GetComponentsInChildren<Component>(true).Where(c=>!UnderOwned(c.transform)).Select(c=>c.GetInstanceID()).ToArray(),Is.EqualTo(original));Assert.That(Call("OriginalGraphDigest",root),Is.EqualTo(before));
            int[] identities=owned.GetComponentsInChildren<Transform>(true).Select(c=>c.GetInstanceID()).ToArray();Assert.That(Call("ValidateOrBuildDetached",root),Is.EqualTo(before));Assert.That(owned.GetComponentsInChildren<Transform>(true).Select(c=>c.GetInstanceID()).ToArray(),Is.EqualTo(identities),"Second pass validates in place; never regenerates.");
            Assert.That(files.Select(Hash).ToArray(),Is.EqualTo(hashes));LogAssert.NoUnexpectedReceived();TestContext.WriteLine("CONTROLLED EDITOR PREFLIGHT ONLY: 14 render-only source model wrappers; original graph, activity/terrain/nav/collider dependencies and all bytes unchanged; native visual acceptance pending.");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static bool UnderOwned(Transform t){while(t!=null){if(t.name==Owned)return true;t=t.parent;}return false;}
    [Test]
    public void UnknownExistingDressingOrCustomizedSourceIsRefusedWithoutSavingOrRepairingIt()
    {
        string bytes=Hash(Shared);var root=PrefabUtility.LoadPrefabContents(Shared);
        try
        {
            var owned=root.transform.Find(Owned);if(owned!=null)UnityEngine.Object.DestroyImmediate(owned.gameObject);var unknown=new GameObject(Owned);unknown.transform.SetParent(root.transform,false);
            var error=Assert.Throws<TargetInvocationException>(()=>Call("ValidateOrBuildDetached",root));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(unknown.transform.childCount,Is.Zero);Assert.That(Hash(Shared),Is.EqualTo(bytes));
            UnityEngine.Object.DestroyImmediate(unknown);Call("ValidateOrBuildDetached",root);owned=root.transform.Find(Owned);var mesh=owned.GetComponentInChildren<MeshFilter>(true);var originalMesh=mesh.sharedMesh;mesh.sharedMesh=null;
            error=Assert.Throws<TargetInvocationException>(()=>Call("ValidateOrBuildDetached",root));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(mesh.sharedMesh,Is.Null,"Refusal must preserve custom edits rather than repair them.");mesh.sharedMesh=originalMesh;Assert.That(Hash(Shared),Is.EqualTo(bytes));LogAssert.NoUnexpectedReceived();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
#endif
