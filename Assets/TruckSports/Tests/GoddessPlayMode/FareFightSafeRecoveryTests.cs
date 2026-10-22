#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FareFightSafeRecoveryTests
{
    static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    static bool Overlap(Collider first,Collider second)=>Physics.ComputePenetration(first,first.transform.position,first.transform.rotation,second,second.transform.position,second.transform.rotation,out _,out _);
    [UnityTest]
    public IEnumerator ChassisSupportFixtureRetainsExistingPhysicalWreckRecovery()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"FareHighCenteredRecovery"}))
        {
            Component match=null;GameObject support=null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFareFight/Scenes/TruckFareFight.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckFareFight.FareFightMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=3;
                float until=Time.realtimeSinceStartup+5;while((float)match.GetType().GetProperty("Intro").GetValue(match)>0&&Time.realtimeSinceStartup<until)yield return null;
                Assert.That((float)match.GetType().GetProperty("Intro").GetValue(match),Is.LessThanOrEqualTo(0));
                Field(match,"setup").GetType().GetField("count").SetValue(Field(match,"setup"),1);
                var trucks=(Array)Field(match,"trucks");for(int i=1;i<4;i++)Call(trucks.GetValue(i),"Park",new Vector3(0,-100-i*20,0));
                var truck=trucks.GetValue(0);var body=(Rigidbody)Field(truck,"body");var shape=(BoxCollider)Field(truck,"shape");var at=new Vector3(2,2.5f,-110);
                Call(truck,"Prepare",at,Quaternion.identity,true);Physics.SyncTransforms();
                // Narrow solid support between the axles: genuine native body
                // contact, while none of the wheels can obtain propulsion.
                support=new GameObject("Explicit chassis-high-centering regression fixture");var block=support.AddComponent<BoxCollider>();block.size=new Vector3(.8f,2,.8f);support.transform.position=new Vector3(shape.bounds.center.x,shape.bounds.min.y-1,shape.bounds.center.z);Physics.SyncTransforms();
                yield return new WaitForSeconds(.5f);
                var motor=Field(truck,"tractorMotor");bool grounded=(bool)motor.GetType().GetProperty("Grounded").GetValue(motor);
                TestContext.WriteLine("Initial high-centered body="+body.position+" grounded wheels="+grounded+" speed="+body.linearVelocity.magnitude);
                Assert.That(grounded,Is.False,"Fixture must isolate actual chassis support with no wheel traction.");
                int before=(int)match.GetType().GetProperty("Recoveries").GetValue(match);until=Time.realtimeSinceStartup+6;
                while((int)match.GetType().GetProperty("Recoveries").GetValue(match)==before&&Time.realtimeSinceStartup<until)yield return null;
                Assert.That((int)match.GetType().GetProperty("Recoveries").GetValue(match),Is.GreaterThan(before),"Existing physical wreck recovery must survive the safe-road placement change.");
                Assert.That(Vector3.Distance(body.position,at),Is.GreaterThan(2));
            }
            finally{if(match!=null)Call(match,"SetupScreen");if(support!=null)UnityEngine.Object.Destroy(support);Time.timeScale=1;}
        }
    }
    [UnityTest]
    public IEnumerator WreckRecoveryNeverMaterializesInsideTheCompetitorOccupyingTheNearestRoadNode()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"FareSafeRoadRecovery"}))
        {
            Component match=null;GameObject blockade=null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFareFight/Scenes/TruckFareFight.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckFareFight.FareFightMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;Time.timeScale=0;
                var district=Field(match,"district");var bounds=(Bounds)Field(district,"playableBounds");var outside=new Vector3(bounds.max.x+30,.4f,bounds.max.z+30);
                object nearest=null;float distance=float.PositiveInfinity;
                foreach(var node in (IEnumerable)Field(Field(district,"graph"),"nodes")){float d=((Vector3)Field(node,"position")-outside).sqrMagnitude;if(d<distance){nearest=node;distance=d;}}
                Assert.That(nearest,Is.Not.Null);var road=(Vector3)Field(nearest,"position")+Vector3.up*.3f;
                var trucks=(Array)Field(match,"trucks");var victim=trucks.GetValue(0);var blocker=trucks.GetValue(1);
                for(int i=2;i<4;i++)Call(trucks.GetValue(i),"Park",new Vector3(0,-100-i*20,0));
                Call(blocker,"Prepare",road,Quaternion.identity,true);((Rigidbody)Field(blocker,"body")).isKinematic=true;
                Call(victim,"Prepare",road,Quaternion.identity,true);Physics.SyncTransforms();
                var first=(Collider)Field(victim,"shape");var second=(Collider)Field(blocker,"shape");Assert.That(Overlap(first,second),Is.True,"Fixture must occupy the old recovery node with a real competitor footprint.");
                Call(victim,"Prepare",outside,Quaternion.identity,true);Physics.SyncTransforms();Assert.That(Overlap(first,second),Is.False);
                Call(match,"DestroyTruck",0);
                // Invoke the actual delayed-placement handler at its scheduled
                // boundary while physics is frozen to inspect its chosen pose.
                match.GetType().GetMethod("Recover",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(match,new object[]{0});Physics.SyncTransforms();
                Assert.That((int)match.GetType().GetProperty("Recoveries").GetValue(match),Is.EqualTo(1));
                Assert.That((bool)Call(district,"Contains",((Rigidbody)Field(victim,"body")).position),Is.True);
                Assert.That(Overlap(first,second),Is.False,"Nearest-node placement must not respawn a wreck directly inside another full NWH truck.");
                var safePosition=((Rigidbody)Field(victim,"body")).position;
                var safeForward=Vector3.ProjectOnPlane(((Component)victim).transform.forward,Vector3.up).normalized;
                bool roadAligned=false;foreach(var edge in (IEnumerable)Field(Field(district,"graph"),"edges"))foreach(var sample in (IEnumerable)Field(edge,"samples"))if(Vector3.Dot(safeForward,Vector3.ProjectOnPlane((Vector3)Field(sample,"forward"),Vector3.up).normalized)>.99f)roadAligned=true;
                Assert.That(roadAligned,Is.True,"Recovery must adopt an actual authored road tangent instead of retaining the wreck's heading.");
                blockade=new GameObject("Explicit all-road-blocked placement fixture");var closed=blockade.AddComponent<BoxCollider>();closed.size=new Vector3(bounds.size.x+100,8,bounds.size.z+100);blockade.transform.position=new Vector3(bounds.center.x,4,bounds.center.z);Physics.SyncTransforms();
                Call(match,"DestroyTruck",0);match.GetType().GetMethod("Recover",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(match,new object[]{0});
                Assert.That((int)match.GetType().GetProperty("Recoveries").GetValue(match),Is.EqualTo(1),"When every sampled road is occupied, wait instead of using an unsafe fallback.");
                Assert.That(((Rigidbody)Field(victim,"body")).position,Is.EqualTo(safePosition));
                Assert.That(((float[])match.GetType().GetField("recoverAt",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(match))[0],Is.GreaterThan(Time.time));
            }
            finally{if(match!=null)Call(match,"SetupScreen");if(blockade!=null)UnityEngine.Object.Destroy(blockade);Time.timeScale=1;}
        }
    }
}
#endif
