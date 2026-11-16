#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
public sealed class DragonKingdomCanopyMaskedTintTests
{
    static Type Owner=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] args)=>Owner.GetMethod(method).Invoke(null,args);
    static object Field(object data,string name)=>data.GetType().GetField(name).GetValue(data);
    static string BaselineFolder(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonCanopyColorBaseline");Assert.That(at,Is.GreaterThanOrEqualTo(0));var review=JsonUtility.FromJson(File.ReadAllText(args[at+1]),Owner.GetNestedType("ReadableCanopyReview"));Assert.That(Field(review,"status"),Is.EqualTo("SAVED_REAL_CANOPIES_CAPTURED_REQUIRES_DIRECT_VISUAL_REVIEW_NOT_ADOPTION"));return(string)Field(Field(review,"dressing"),"folder");}
    [TestCase(0f)][TestCase(.5f)][TestCase(1f)]
    public void AuthoredPerChannelMaskFormulaPreservesUntintedRegionsAndOriginalAlpha(float mask)
    {var a=new Color(.8f,.6f,.4f,.37f);var m=new Color(mask,1-mask,.25f,0);var t=new Color(.2f,.7f,.1f,.8f);var result=(Color)Call("EvaluateAuthoredCanopyMaskedTint",a,m,t);Assert.That(result.r,Is.EqualTo(.8f*(1-mask+mask*.2f)).Within(.00001f));Assert.That(result.g,Is.EqualTo(.6f*(mask+(1-mask)*.7f)).Within(.00001f));Assert.That(result.b,Is.EqualTo(.4f*(.75f+.25f*.1f)).Within(.00001f));Assert.That(result.a,Is.EqualTo(.37f));}
    [TestCase("a14832c88e3baf949ba52aad936b0070")][TestCase("f7d4bd122228e4e4495bebc0fb49277d")][TestCase("963f7b145f1e9144192f96aaa76f7221")][TestCase("ff5f626497301f741b9f81291d3ec3e6")]
    public void AllFourActualV533FinishesHaveExplicitSourceSemanticsAndBarkIsNotMistakenForLostMask(string guid)
    {
        var source=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));var old=AssetDatabase.LoadAssetAtPath<Material>(BaselineFolder()+"/Materials/Finish/"+guid+" URP.mat");var proof=Call("InspectAuthoredCanopyTintSource",source,old);bool bark=guid=="a14832c88e3baf949ba52aad936b0070";Assert.That(Field(proof,"authoredBarkKept"),Is.EqualTo(bark));Assert.That(Field(proof,"authoredMaskedTint"),Is.EqualTo(!bark));if(!bark){Assert.That(Field(proof,"oldMaskedBindingsMissing"),Is.True);Assert.That((string)Field(proof,"maskGuid"),Has.Length.EqualTo(32));var tint=(Color)Field(proof,"authoredTint");Assert.That(tint.g,Is.GreaterThan(tint.b));string shader=File.ReadAllText(AssetDatabase.GetAssetPath(source.shader));Assert.That(shader,Does.Contain("lerp( ( _AlbedoTint * tex2DNode14 ) , tex2DNode14 , ( 1.0 - tex2D( _AlbedoMask, uv_AlbedoMask ) ))"));Assert.That(shader,Does.Contain("clip( tex2DNode14.a - _Cutoff )"));}TestContext.WriteLine(JsonUtility.ToJson(proof,true));
    }
    [UnityTest,Timeout(900000)]
    public IEnumerator ActualSaved166PartsKeep26TreesAndAllPhysicalGeometryWhileAuthoredMaskedColorsRender()
    {
        yield return(IEnumerator)Call("CaptureCanopyMaskedTintReview");var review=Owner.GetProperty("LastCanopyTintReview").GetValue(null);Assert.That(Field(review,"status"),Is.EqualTo("SAVED_AUTHORED_MASKED_COLOR_CAPTURED_REQUIRES_DIRECT_STYLE_REVIEW_NOT_ADOPTION"));Assert.That(Field(review,"error"),Is.Null.Or.Empty);Assert.That(Field(review,"instances"),Is.EqualTo(166));Assert.That(Field(review,"canopies"),Is.EqualTo(26));foreach(string flag in new[]{"all166PlacementGeometryExact","allLodAndPhysicsExact","originalWorldExact","savedReloaded","originalSourcesExact","incomingSceneContextRestored"})Assert.That(Field(review,flag),Is.True,flag);var materials=(Array)Field(review,"materials");Assert.That(materials.Length,Is.EqualTo(4));int fixedCount=0,bark=0;foreach(var material in materials){if((bool)Field(material,"authoredBarkKept")){bark++;Assert.That(Field(material,"ownedMaterial"),Is.EqualTo(Field(material,"oldOwned")));continue;}fixedCount++;Assert.That(Field(material,"maskRestored"),Is.True);Assert.That(Field(material,"allPixelsRecomputed"),Is.True);Assert.That(Field(material,"alphaMismatches"),Is.EqualTo(0));Assert.That(Field(material,"outputPixelMismatches"),Is.EqualTo(0));Assert.That((int)Field(material,"outputMaximumChannelDelta"),Is.LessThanOrEqualTo(2));Assert.That((double)Field(material,"meanGreenOverBlue"),Is.GreaterThan(1.1));Assert.That(Field(material,"ownedTextureSha256"),Has.Length.EqualTo(64));}Assert.That(fixedCount,Is.EqualTo(3));Assert.That(bark,Is.EqualTo(1));Assert.That(((Array)Field(review,"frames")).Length,Is.EqualTo(12));TestContext.WriteLine("ACTUAL_AUTHORED_MASKED_TINT_AND_SAVED_PLACEMENT_ONLY_DIRECT_STYLE_REVIEW_REMAINS_SEPARATE");
    }
}
#endif
