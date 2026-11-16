#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed class DragonKingdomTowerStoneSamplingTests
{
    static Type Owner=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] args)=>Owner.GetMethod(method).Invoke(null,args);
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static void Set(object value,string name,object data)=>value.GetType().GetField(name).SetValue(value,data);
    static string Source()=>File.ReadAllText((string)Owner.GetField("TowerDetailVendorGraph").GetValue(null));
    [Test]
    public void ActualPaintedStoneDependencyUsesVerifiedOriginalTextureRootGuidBytesAndNativeLoad()
    {
        const string expected="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Plugins/Tidal Flask Studios/Fantastic Village Pack/2d/textures/T_stonebrick_02.png";string actual=(string)Owner.GetField("TowerPaintedBrick",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);Assert.That(actual,Is.EqualTo(expected));Assert.That(File.Exists(actual)&&File.Exists(actual+".meta"),Is.True);Assert.That(UnityEditor.AssetDatabase.AssetPathToGUID(actual),Is.EqualTo("94a7b0f7e0cee0e43b38273bbcee2b98"));
        string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}Assert.That(Hash(actual),Is.EqualTo("605187d060fa829fc32dcaed38c5c610c8f2e60ce4cd0f946e18870e6e472d8c"));Assert.That(Hash(actual+".meta"),Is.EqualTo("e2665065aba4fe93c73b59e66efa1bfcc3769e1343d81a7a25a5efefbecd5311"));var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(actual);Assert.That(texture,Is.Not.Null);Assert.That(texture.wrapMode,Is.EqualTo(TextureWrapMode.Repeat));Assert.That(texture.width,Is.EqualTo(2048));Assert.That(texture.height,Is.EqualTo(2048));
    }
    [Test]
    public void ActualSamplingRepairAddsOnlyOneExistingPropertyEdgeAlongsideTheTwoProvenMaskValues()
    {
        string source=Source(),edge=(string)Owner.GetField("TowerSamplingEdge").GetValue(null),repaired=(string)Call("CorrectTowerSecondarySampling",source),without=repaired.Replace(edge,"");Assert.That(without,Is.EqualTo(Call("CorrectDisconnectedTowerMaskInputs",source)));Assert.That(repaired.Split(new[]{edge},StringSplitOptions.None).Length,Is.EqualTo(2));Assert.That(Source(),Is.EqualTo(source));
    }
    [TestCase("already-repaired")][TestCase("connected-UV-scale")][TestCase("missing-source-property")]
    public void SamplingRepairRejectsExistingOrAmbiguousBindingsInsteadOfOverwritingAuthorship(string fault)
    {
        string source=Source(),edge=(string)Owner.GetField("TowerSamplingEdge").GetValue(null);switch(fault){case "already-repaired":source=(string)Call("CorrectTowerSecondarySampling",source);break;case "connected-UV-scale":const string marker="\"m_Edges\": [";source=source.Insert(source.IndexOf(marker,StringComparison.Ordinal)+marker.Length,edge);break;case "missing-source-property":source=source.Replace("b7a72872b1ef4e609cd3853ee46f59dd","11111111111111111111111111111111");break;}var error=Assert.Throws<TargetInvocationException>(()=>Call("CorrectTowerSecondarySampling",source));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test]
    public void FailedVisualResponseRetainsActualPhysicalMeasurementsAndTheFullFailure()
    {
        object review=Activator.CreateInstance(Owner.GetNestedType("TowerStoneSamplingReview")),appearance=Activator.CreateInstance(Owner.GetNestedType("TowerAppearanceReview"));Set(review,"appearance",appearance);Set(review,"stage","ACTUAL_TOWER_PIXEL_RESPONSE");Set(review,"measuredOriginalColliders",123);Set(review,"measuredTerrains",16);foreach(string flag in new[]{"allColliderReferencesAndStateExact","allHeightfieldsExact","allPerchReferencesAndPosesExact"})Set(appearance,flag,true);Call("RecordTowerStoneSamplingFailure",review,new InvalidOperationException("Actual rendered texture response failed its unchanged acceptance"));Assert.That(Field(review,"error"),Does.Contain("Actual rendered texture response failed"));Assert.That(Field(review,"status"),Is.EqualTo("REJECTED_ACTUAL_SAMPLING_OR_RENDER_RESPONSE"));Assert.That(Field(review,"stage"),Is.EqualTo("ACTUAL_TOWER_PIXEL_RESPONSE"));Assert.That(Field(review,"measuredOriginalColliders"),Is.EqualTo(123));Assert.That(Field(review,"measuredTerrains"),Is.EqualTo(16));foreach(string flag in new[]{"allColliderReferencesAndStateExact","allHeightfieldsExact","allPerchReferencesAndPosesExact"})Assert.That(Field(appearance,flag),Is.True);
    }
}
#endif
