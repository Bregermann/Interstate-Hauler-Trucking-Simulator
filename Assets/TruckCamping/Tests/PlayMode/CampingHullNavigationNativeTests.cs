#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static WildernessNativeScope;
public sealed class CampingHullNavigationNativeTests
{
    [Test]public void ActualPineconeBenchAvoidanceCornersCannotBeDiscardedByTheFormerEighteenMetreLookahead()
    {
        var actualPosition=new Vector3(-155.6464f,8.1988f,-86.975f);
        var corners=new[]{new Vector3(-155.6464f,8.375f,-86.975f),new Vector3(-151.45f,8.375f,-86.45f),new Vector3(-146.25f,8.375f,-81.25f),new Vector3(-89.37008f,8.375f,97.65455f)};
        Assert.That(Vector3.ProjectOnPlane(corners[1]-actualPosition,Vector3.up).magnitude,Is.InRange(4,4.5f));
        Assert.That(Vector3.ProjectOnPlane(corners[2]-actualPosition,Vector3.up).magnitude,Is.LessThan(18));
        var method=Runtime("LWS.TruckCamping.CampExplorer").GetMethod("FirstUnreachedNavigationCorner");
        Assert.That((int)method.Invoke(null,new object[]{actualPosition,corners}),Is.EqualTo(1),"Actual NavMesh furniture-avoidance nodes remain available to steering instead of cutting straight through the measured bench0.42m ahead.");
        Assert.That((int)method.Invoke(null,new object[]{corners[1],corners}),Is.EqualTo(2));
    }
    [UnityTest,Timeout(60000)]public IEnumerator OrdinaryOwnerTwoPhysicallyLeavesPineconeFurnitureAndCollectsWithoutChangingAnyHullCollision()
    {
        using(var scope=new WildernessNativeScope("camp-pinecone-hull-route"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");scope.Configure(4,true);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);Time.timeScale=4;yield return scope.Advance(.5f);
            var world=(Component)Get(scope.match,"world");var explorers=Components(Get(scope.match,"explorers"));var trucks=explorers.Select(e=>(Component)Get(e,"truck")).ToArray();var hulls=trucks.Select(t=>(Collider)Get(t,"shape")).ToArray();var physical=trucks.Select(PhysicsIdentity).ToArray();
            var benches=world.GetComponentsInChildren<Collider>(true).Where(c=>c.name=="Bound cedar camp bench").ToArray();Assert.That(benches.Length,Is.GreaterThanOrEqualTo(4));var enabled=benches.Select(c=>c.enabled).ToArray();var poses=benches.Select(c=>c.transform.localToWorldMatrix).ToArray();var ignored=hulls.Select(h=>benches.Select(b=>Physics.GetIgnoreCollision(h,b)).ToArray()).ToArray();
            float end=Time.realtimeSinceStartup+45;while((int)Get(explorers[2],"ActualCollections")==0&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That((int)Get(explorers[2],"ActualCollections"),Is.GreaterThan(0),"Original owner2 must actually approach/gather a real resource after its original campsite visit; no scene/cargo/vehicle pose or inventory injection.");
            Assert.That(explorers.Sum(e=>(int)Get(e,"NavigationShortcutRejections")+(int)Get(e,"NavigationRecoveries")),Is.GreaterThan(0),"Exercise actual clear-shortcut or bounded original NWH-control recovery branch.");
            CollectionAssert.AreEqual(physical,trucks.Select(PhysicsIdentity).ToArray());CollectionAssert.AreEqual(enabled,benches.Select(c=>c.enabled).ToArray());CollectionAssert.AreEqual(poses,benches.Select(c=>c.transform.localToWorldMatrix).ToArray());
            for(int t=0;t<hulls.Length;t++)for(int b=0;b<benches.Length;b++)Assert.That(Physics.GetIgnoreCollision(hulls[t],benches[b]),Is.EqualTo(ignored[t][b]));
            scope.Capture("camp-owner-two-actual-bench-clearance-and-resource",1920,1080);
            TestContext.WriteLine("CAMP_HULL_ROUTE actualcollections="+string.Join(",",explorers.Select(e=>Get(e,"ActualCollections")))+" shortcutRejections="+string.Join(",",explorers.Select(e=>Get(e,"NavigationShortcutRejections")))+" ordinaryReverseRecoveries="+string.Join(",",explorers.Select(e=>Get(e,"NavigationRecoveries")))+" originalHullFurnitureCollisionsUnchanged=true; full80second all4/fire/cooking remains separate unchanged case.");
        }
    }
}
#endif
