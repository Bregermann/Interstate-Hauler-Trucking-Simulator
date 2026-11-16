#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DragonKingdomBoundedArtDefinitionTests
{
    const string Definition="Assets/TruckDragonKingdom/Configuration/Dragon Kingdom.asset";
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static void Set(object value,string name,object data)=>value.GetType().GetField(name).SetValue(value,data);
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    [TestCase("persistent")][TestCase("certified-clone")][TestCase("uncertified-clone")]
    [TestCase("candidate-edit")][TestCase("stale-original-json")][TestCase("wrong-sha")]
    [TestCase("wrong-meta-sha")][TestCase("wrong-world-definition")][TestCase("null-world")]
    public void BoundedArtUsesExactExistingDefinitionAuthorityAndRejectsUnknownChanges(string mutation)
    {
        string before=Hash(Definition),meta=Hash(Definition+".meta");
        var source=AssetDatabase.LoadAssetAtPath<ScriptableObject>(Definition);
        var candidate=UnityEngine.Object.Instantiate(source);candidate.name=source.name;
        var go=new GameObject("Disposable bounded-art definition control");go.SetActive(false);
        var world=go.AddComponent(Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true));
        try
        {
            object certificate=null;Set(world,"definition",mutation=="persistent"?source:candidate);
            if(mutation!="persistent"&&mutation!="uncertified-clone")
            {
                certificate=Activator.CreateInstance(Integration.GetNestedType("OwnedDefinitionProvenance",BindingFlags.NonPublic),true);
                Set(certificate,"original",source);Set(certificate,"candidate",candidate);
                Set(certificate,"originalJson",JsonUtility.ToJson(source));Set(certificate,"candidateJson",JsonUtility.ToJson(candidate));Set(certificate,"path",Definition);
                var assetSha=Integration.GetMethod("AssetSha",BindingFlags.NonPublic|BindingFlags.Static);
                Set(certificate,"sha",assetSha.Invoke(null,new object[]{Definition}));Set(certificate,"metaSha",assetSha.Invoke(null,new object[]{Definition+".meta"}));
            }
            if(mutation=="candidate-edit")Set(candidate,"minimumRoadWidth",(float)candidate.GetType().GetField("minimumRoadWidth").GetValue(candidate)+.25f);
            if(mutation=="stale-original-json")Set(certificate,"originalJson","stale");
            if(mutation=="wrong-sha")Set(certificate,"sha","stale");
            if(mutation=="wrong-meta-sha")Set(certificate,"metaSha","stale");
            if(mutation=="wrong-world-definition")Set(world,"definition",source);
            var method=Integration.GetMethod("BoundedArtInputDefinition",BindingFlags.Static|BindingFlags.NonPublic);
            object[] args={mutation=="null-world"?null:world,certificate};
            if(mutation=="persistent"||mutation=="certified-clone")Assert.That(method.Invoke(null,args),Is.EqualTo(Definition));
            else Assert.That(Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,args)).InnerException,Is.TypeOf<InvalidOperationException>());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(candidate);
            Assert.That(Hash(Definition),Is.EqualTo(before));Assert.That(Hash(Definition+".meta"),Is.EqualTo(meta));
        }
    }
}
#endif
