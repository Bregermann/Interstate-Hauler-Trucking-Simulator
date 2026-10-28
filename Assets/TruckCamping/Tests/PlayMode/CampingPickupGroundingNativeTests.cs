#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static WildernessNativeScope;
public sealed class CampingPickupGroundingNativeTests
{
    [Test]public void MeasuredTwelveMetreCollectionDeadzoneUsesBoundedApproachAndRetainsOriginalPublicNavigationDefault()
    {
        var type=Runtime("LWS.TruckCamping.CampExplorer");var target=type.GetMethod("CollectionApproachTarget");var speed=type.GetMethod("NavigationDesiredSpeed");
        var actual=new Vector3(-9.098828f,8.198535f,15.530981f);var stopped=new Vector3(-9.165467f,8.10599f,3.196377f);var approach=(Vector3)target.Invoke(null,new object[]{actual});float gap=Vector3.ProjectOnPlane(approach-stopped,Vector3.up).magnitude;
        Assert.That(Vector3.Distance(stopped,actual),Is.GreaterThan(12));Assert.That((float)speed.Invoke(null,new object[]{gap,0f,7f}),Is.Zero,"Original7m stop reproduces actual outside10m gather deadzone.");
        Assert.That((float)speed.Invoke(null,new object[]{gap,0f,2.5f}),Is.GreaterThan(0));
        foreach(float angle in new[]{0f,45f,90f,180f,271f})
        {var offset=Quaternion.Euler(0,angle,0)*Vector3.forward*2.499f;Assert.That(Vector3.Distance(approach+offset,actual),Is.LessThan(9.22f),"Every point inside the tighter stop is inside original sqrt85 trigger and10m actual gather gate.");}
        Assert.That(type.GetMethod("Navigate",new[]{typeof(Vector3)}),Is.Not.Null,"Original one-argument public/reflection contract retained.");
    }
    [UnityTest,Timeout(40000)]public IEnumerator ActualOwnedPickupsStayDynamicHullCollidableAndOutOfWheelSupportAcrossCollectionPauseSetReplacementAndRematch()
    {
        using(var scope=new WildernessNativeScope("camp-pickup-query-ownership"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");scope.Configure(4,true);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);yield return scope.Advance(.4f);Call(scope.match,"SetupScreen");
            var world=Get(scope.match,"world");var explorers=Components(Get(scope.match,"explorers"));var trucks=explorers.Select(e=>(Component)Get(e,"truck")).ToArray();var wheels=trucks.SelectMany(t=>Components(Get(t,"wheels"))).ToArray();Assert.That(wheels.Length,Is.EqualTo(16));var resources=Components(Get(world,"resources"));var pickups=resources.Select(p=>(Collider)Get(p,"physicalPickup")).ToArray();Assert.That(pickups.Length,Is.GreaterThan(40));
            var hulls=trucks.Select(t=>(Collider)Get(t,"shape")).ToArray();var original=wheels.Select(w=>(HashSet<Collider>)Get(w,"vehicleColliders")).ToArray();Assert.That(original.All(s=>s!=null),Is.True,"Actual original NWH initialization precedes ownership baseline.");
            var unrelated=(Collider)Get(Components(Get(world,"hammocks"))[0],"deck");Assert.That(unrelated,Is.Not.Null);
            bool hadPickup=original[0].Contains(pickups[0]),hadOther=original[1].Contains(unrelated);original[0].Add(pickups[0]);original[1].Add(unrelated);var baseline=original.Select(s=>s.ToArray()).ToArray();var identities=trucks.Select(PhysicsIdentity).ToArray();var masks=wheels.Select(w=>(LayerMask)Get(w,"layerMask")).ToArray();var force=wheels.Select(w=>(float)Get(w,"otherBodyForceScale")).ToArray();var layers=pickups.Select(p=>p.gameObject.layer).ToArray();var pairs=hulls.Select(h=>pickups.Select(p=>Physics.GetIgnoreCollision(h,p)).ToArray()).ToArray();var matrix=new bool[32,32];for(int a=0;a<32;a++)for(int b=0;b<32;b++)matrix[a,b]=Physics.GetIgnoreLayerCollision(a,b);
            Action check=()=>
            {
                for(int w=0;w<wheels.Length;w++)
                {var set=(HashSet<Collider>)Get(wheels[w],"vehicleColliders");Assert.That(pickups.All(set.Contains),Is.True);Assert.That((LayerMask)Get(wheels[w],"layerMask"),Is.EqualTo(masks[w]));Assert.That((float)Get(wheels[w],"otherBodyForceScale"),Is.EqualTo(force[w]));var hit=(Collider)Get(wheels[w],"HitCollider");if((bool)Get(wheels[w],"IsGrounded")&&hit!=null)Assert.That(Array.IndexOf(pickups,hit),Is.EqualTo(-1),"A3kg pickup must not receive original heavy-truck suspension ground reaction.");}
            };
            try
            {
                Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);Time.timeScale=4;yield return scope.Advance(.15f);check();
                var replacement=new HashSet<Collider>(baseline[0]);Set(wheels[0],"vehicleColliders",replacement);yield return scope.Advance(.1f);CollectionAssert.AreEquivalent(baseline[0],original[0]);check();
                float end=Time.realtimeSinceStartup+15;while(explorers.All(e=>(int)Get(e,"ActualCollections")==0)&&Time.realtimeSinceStartup<end){check();yield return null;}
                Assert.That(explorers.Any(e=>(int)Get(e,"ActualCollections")>0),Is.True,"Ordinary original4AI must reach and gather real resources through unchanged10m/low-speed gate, without injected inventory or poses.");check();
                Assert.That(resources.Any(p=>(bool)Get(p,"Available")&&!((Rigidbody)Get(p,"body")).isKinematic),Is.True,"Remaining pickups retain actual physical bodies.");
                Call(scope.match,"Pause",0,false);var paused=wheels.Select(w=>((HashSet<Collider>)Get(w,"vehicleColliders")).ToArray()).ToArray();yield return null;yield return null;for(int i=0;i<16;i++)CollectionAssert.AreEquivalent(paused[i],(HashSet<Collider>)Get(wheels[i],"vehicleColliders"));Call(scope.match,"Resume");Call(scope.match,"SetupScreen");
                for(int i=0;i<16;i++)CollectionAssert.AreEquivalent(baseline[i],(HashSet<Collider>)Get(wheels[i],"vehicleColliders"),"Exit removes only owned additions, preserving existing pickup/unrelated members.");
                Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);yield return scope.Advance(.1f);check();Call(scope.match,"SetupScreen");
                for(int i=0;i<16;i++)CollectionAssert.AreEquivalent(baseline[i],(HashSet<Collider>)Get(wheels[i],"vehicleColliders"));CollectionAssert.AreEqual(identities,trucks.Select(PhysicsIdentity).ToArray());CollectionAssert.AreEqual(layers,pickups.Select(p=>p.gameObject.layer).ToArray());
                for(int a=0;a<32;a++)for(int b=0;b<32;b++)Assert.That(Physics.GetIgnoreLayerCollision(a,b),Is.EqualTo(matrix[a,b]));for(int i=0;i<hulls.Length;i++)for(int p=0;p<pickups.Length;p++)Assert.That(Physics.GetIgnoreCollision(hulls[i],pickups[p]),Is.EqualTo(pairs[i][p]),"Original hull/pickup collisions preserved.");
                TestContext.WriteLine("CAMP_PICKUP_QUERY_PASS actual16wheels/"+pickups.Length+" pickups,realAIgather,originalconfiguredphysics,masks,reactionScale,layers/hullpairs/globalmatrix exact; only owned query additions restored across lifecycle.");
            }
            finally{Call(scope.match,"SetupScreen");if(!hadPickup){original[0].Remove(pickups[0]);((HashSet<Collider>)Get(wheels[0],"vehicleColliders")).Remove(pickups[0]);}if(!hadOther)original[1].Remove(unrelated);Set(wheels[0],"vehicleColliders",original[0]);}
        }
    }
}
#endif
