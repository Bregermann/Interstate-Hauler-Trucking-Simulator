#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomSettlementFinishSearchTests
{
    static Vector3 Offset(int trial,bool solid,bool dense)
    {
        var integration=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        return (Vector3)integration.GetMethod("FinishPlacementOffset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{trial,solid,dense});
    }
    [TestCase(true)]
    [TestCase(false)]
    public void ExistingSuccessfulSearchOrderAndPositionsAreExactlyPreserved(bool solid)
    {
        for(int i=0;i<288;i++)
        {
            float radius=i==0?0:(solid?7:3)*Mathf.Sqrt(i),angle=i*2.399963f;
            var original=new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
            Assert.That(Offset(i,solid,false),Is.EqualTo(original));
            Assert.That(Offset(i,solid,true),Is.EqualTo(original));
        }
    }
    [Test]
    public void ExtraSettlementCandidatesRemainInsideOriginalBoundedDistrictRadius()
    {
        float originalRadius=7*Mathf.Sqrt(287);
        for(int i=288;i<2336;i++)
        {
            var p=Offset(i,true,true);
            Assert.That(float.IsFinite(p.x)&&float.IsFinite(p.z),Is.True);
            Assert.That(p.y,Is.Zero);
            Assert.That(p.magnitude,Is.LessThanOrEqualTo(originalRadius+.0001f));
        }
    }
    [Test]
    public void DenseSearchCanReachASmallParcelMissedByAllOriginalSparseCandidates()
    {
        var original=Enumerable.Range(0,288).Select(i=>Offset(i,true,false)).ToArray();
        // A small valid parcel between sparse samples is representative of the
        // remaining spaces between completed stalls. The real-world caller
        // must additionally pass unchanged native ground and collision guards.
        var extra=Enumerable.Range(288,2048).Select(i=>Offset(i,true,true)).First(p=>original.All(q=>Vector3.Distance(p,q)>3));
        Assert.That(original.All(p=>Vector3.Distance(p,extra)>3),Is.True);
        Assert.That(Enumerable.Range(288,2048).Any(i=>Vector3.Distance(Offset(i,true,true),extra)<.01f),Is.True);
    }
    [Test]
    public void UndeclaredExtraCandidatesAreRejected()
    {
        foreach(var sample in new[]{new{trial=-1,dense=true},new{trial=288,dense=false},new{trial=2336,dense=true}})
        {
            var error=Assert.Throws<TargetInvocationException>(()=>Offset(sample.trial,true,sample.dense));
            Assert.That(error.InnerException,Is.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
#endif
