using System;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class CampingHammockTransitionTests
{
    static Type Author => Type.GetType("LWS.TruckCamping.Editor.CampingHammockTransitionAuthoring, Assembly-CSharp-Editor", true);
    static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
    static BoxCollider Box(Transform frame, Vector3 position, Vector3 scale, Quaternion rotation)
    {
        var go = new GameObject("Controlled original box"); go.transform.SetParent(frame,false); go.transform.localPosition=position;go.transform.localScale=scale;go.transform.localRotation=rotation;
        return go.AddComponent<BoxCollider>();
    }
    [TestCase(-1,-.025f)] [TestCase(-1,.025f)] [TestCase(1,-.025f)] [TestCase(1,.025f)]
    public void ActualTerrainToeAndMeasuredInnerEnvelopeFitBothSidesWithoutWritingAnySourceGeometry(int side,float slope)
    {
        var root=new GameObject("Controlled terrain-plane geometry, not native transition acceptance");
        try
        {
            root.transform.position=new Vector3(45,9.7574526f,-65);
            var deck=Box(root.transform,new Vector3(0,.9f,0),new Vector3(10,.35f,16),Quaternion.identity);
            var ramp=Box(root.transform,new Vector3(0,.4f,side*11),new Vector3(10,.25f,5.953912f),Quaternion.Euler(side*7,0,0));
            Func<Vector3,float> terrain=p=>root.transform.position.y+slope*(p.z-root.transform.position.z)+.01f*(p.x-root.transform.position.x);
            Vector3 originalPosition=ramp.transform.localPosition, originalScale=ramp.transform.localScale;Quaternion originalRotation=ramp.transform.localRotation;
            var profile=Author.GetMethod("FitActualToeProfile").Invoke(null,new object[]{root.transform,deck,ramp,terrain});
            Assert.That((float)Field(profile,"innerTop"),Is.EqualTo(.45f).Within(1e-5));
            Assert.That((float)Field(profile,"outerTop")-(float)Field(profile,"toeMin"),Is.EqualTo(.02f).Within(1e-4));
            Assert.That((float)Field(profile,"innerGap"),Is.EqualTo(.03f).Within(1e-5));
            Assert.That(Mathf.Abs((float)Field(profile,"pitch")),Is.LessThan(12));
            Assert.That(((Vector3)Field(profile,"scale")).x,Is.EqualTo(10));Assert.That(((Vector3)Field(profile,"scale")).y,Is.EqualTo(.25f));
            Assert.That(((Vector3)Field(profile,"position")).z,Is.EqualTo(side*(8.03f+(float)Field(profile,"horizontalSpan")*.5f)).Within(1e-5));
            Assert.That((float)Field(profile,"horizontalSpan"),Is.InRange(5.9399f,24));
            Assert.That(ramp.transform.localPosition,Is.EqualTo(originalPosition));Assert.That(ramp.transform.localScale,Is.EqualTo(originalScale));Assert.That(ramp.transform.localRotation,Is.EqualTo(originalRotation));
            Assert.That(deck.transform.localPosition,Is.EqualTo(new Vector3(0,.9f,0)));Assert.That(deck.transform.localScale,Is.EqualTo(new Vector3(10,.35f,16)));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    [Test] public void ActualR49LowFarToeRequiresOutwardExtensionWithoutRaisingTwelveDegreeLimit()
    {
        var root=new GameObject("Measured R49 root and actual flat toe elevation");
        try
        {
            root.transform.position=new Vector3(45,9.7574529648f,-65);
            var deck=Box(root.transform,new Vector3(0,.9f,0),new Vector3(10,.35f,16),Quaternion.identity);
            var ramp=Box(root.transform,new Vector3(0,.4f,-11),new Vector3(10,.25f,5.953912f),Quaternion.Euler(-7,0,0));
            Func<Vector3,float> terrain=p=>7.99853515625f;
            var profile=Author.GetMethod("FitActualToeProfile").Invoke(null,new object[]{root.transform,deck,ramp,terrain});
            Assert.That((float)Field(profile,"horizontalSpan"),Is.GreaterThan(10),"Original5.94m footprint cannot bridge the measured2.19m rise at12degrees.");
            Assert.That((int)Field(profile,"extensionSteps"),Is.GreaterThan(0));
            Assert.That(Mathf.Abs((float)Field(profile,"pitch")),Is.LessThanOrEqualTo(12));
            Assert.That((float)Field(profile,"innerGap"),Is.EqualTo(.03f).Within(1e-5));
            Assert.That((float)Field(profile,"outerTop")+root.transform.position.y,Is.EqualTo(7.99853515625f+.02f).Within(1e-4));
            Assert.That(Mathf.Abs(((Vector3)Field(profile,"position")).z),Is.GreaterThan(13));
            Assert.That(ramp.transform.localPosition.z,Is.EqualTo(-11),"Read-only plan must not apply any transform.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    [Test] public void ActualWidthSlopeUsesLowToeEmbeddingRatherThanFloatingHighestCornerLedge()
    {
        var root=new GameObject("Measured R49 half-metre toe cross-slope");
        try
        {
            root.transform.position=new Vector3(45,9.7574529648f,-65);
            var deck=Box(root.transform,new Vector3(0,.9f,0),new Vector3(10,.35f,16),Quaternion.identity);
            var ramp=Box(root.transform,new Vector3(0,.4f,-11),new Vector3(10,.25f,5.953912f),Quaternion.Euler(-7,0,0));
            Func<Vector3,float> terrain=p=>7.99853515625f+(50-p.x)*.05f;
            var profile=Author.GetMethod("FitActualToeProfile").Invoke(null,new object[]{root.transform,deck,ramp,terrain});
            Assert.That((float)Field(profile,"toeMax")-(float)Field(profile,"toeMin"),Is.EqualTo(.5f).Within(1e-4));
            Assert.That((float)Field(profile,"outerTop")-(float)Field(profile,"toeMin"),Is.EqualTo(.02f).Within(1e-4));
            Vector3 outer=((Vector3)Field(profile,"outerLeft")+(Vector3)Field(profile,"outerRight"))*.5f;
            float actualMiddleGround=terrain(root.transform.TransformPoint(outer));
            Assert.That(actualMiddleGround-(outer.y+root.transform.position.y),Is.GreaterThan(.2f),"Centre toe embeds into actual ground instead of leaving the old max-corner floating ledge.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    [Test] public void UnsupportedDepthStillRefusesWithoutExpandingPitchOrApproachBudget()
    {
        var root=new GameObject("Finite but physically unsupported toe");
        try
        {
            var deck=Box(root.transform,new Vector3(0,.9f,0),new Vector3(10,.35f,16),Quaternion.identity);
            var ramp=Box(root.transform,new Vector3(0,.4f,-11),new Vector3(10,.25f,5.953912f),Quaternion.Euler(-7,0,0));
            Func<Vector3,float> terrain=p=>-1000;
            Assert.That(Assert.Throws<TargetInvocationException>(()=>Author.GetMethod("FitActualToeProfile").Invoke(null,new object[]{root.transform,deck,ramp,terrain})).InnerException,Is.TypeOf<InvalidOperationException>());
            Assert.That(ramp.transform.localPosition,Is.EqualTo(new Vector3(0,.4f,-11)));
            Assert.That((float)Author.GetField("MaximumPitch").GetRawConstantValue(),Is.EqualTo(12));
            Assert.That((float)Author.GetField("MaximumHorizontalSpan").GetRawConstantValue(),Is.EqualTo(24));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    [Test] public void MissingActualTerrainCannotProduceSyntheticToeOrGeometry()
    {
        var root=new GameObject("Missing terrain refusal");
        try
        {
            var deck=Box(root.transform,new Vector3(0,.9f,0),new Vector3(10,.35f,16),Quaternion.identity);
            var ramp=Box(root.transform,new Vector3(0,.4f,11),new Vector3(10,.25f,5.953912f),Quaternion.Euler(7,0,0));
            Func<Vector3,float> noTerrain=p=>float.NaN;
            Assert.That(Assert.Throws<TargetInvocationException>(()=>Author.GetMethod("FitActualToeProfile").Invoke(null,new object[]{root.transform,deck,ramp,noTerrain})).InnerException,Is.TypeOf<InvalidOperationException>());
            Assert.That(ramp.transform.localPosition.y,Is.EqualTo(.4f));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    [TestCase("\n")] [TestCase("\r\n")]
    public void NativeYamlProfilePatchPreservesUnrelatedScalarsAndLineEndings(string newline)
    {
        var old=Quaternion.Euler(7,0,0);var next=Quaternion.Euler(3,0,0);Func<float,string> n=v=>v.ToString("R",CultureInfo.InvariantCulture);
        string before=string.Join(newline,new[]{"--- !u!4 &42","Transform:","  m_LocalRotation: {x: "+n(old.x)+", y: 0, z: 0, w: "+n(old.w)+"}","  m_LocalPosition: {x: 0, y: 0.4, z: 11}","  m_LocalScale: {x: 10, y: 0.25, z: 5.953912}","  m_LocalEulerAnglesHint: {x: 7, y: 0, z: 0}","  unrelated: retain exact text  ",""});
        var method=Author.GetMethod("RewriteKnownRampDocument");
        string after=(string)method.Invoke(null,new object[]{before,new Vector3(0,.1f,12.5f),5.97f,next,3f});
        Assert.That(after,Does.Contain("m_LocalPosition: {x: 0, y: 0.1, z: 12.5}"));Assert.That(after,Does.Contain("m_LocalScale: {x: 10, y: 0.25, z: 5.97}"));
        Assert.That(after,Does.Contain("unrelated: retain exact text  "+newline));
        string reverse=(string)method.Invoke(null,new object[]{after,new Vector3(0,.4f,11),5.953912f,old,7f});Assert.That(reverse,Is.EqualTo(before));
    }
    [Test] public void SavedTransitionChangesOnlyReviewedRampTransformsAndPreservesWholeOriginalWorldAndMeta()
    {
        string result=(string)Author.GetMethod("VerifyLastRepairPreservation").Invoke(null,null);
        Assert.That(result,Is.EqualTo("EIGHT_RAMP_EXTENDED_GROUND_PROFILE_TRANSFORMS_ONLY_NATIVE_TRANSITION_ACCEPTANCE_PENDING"));
    }
}
