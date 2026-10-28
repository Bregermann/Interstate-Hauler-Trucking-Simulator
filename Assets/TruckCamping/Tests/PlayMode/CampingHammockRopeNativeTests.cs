#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static WildernessNativeScope;

public sealed class CampingHammockRopeNativeTests
{
    static string PhysicsIdentity(Component hammock)
    {
        var body=(Rigidbody)Get(hammock,"platform");var deck=(BoxCollider)Get(hammock,"deck");
        return body.GetInstanceID()+":"+body.mass+":"+body.linearDamping+":"+body.angularDamping+":"+deck.size.ToString("R")+":"+deck.transform.localScale.ToString("R")+string.Join("/",body.GetComponents<SpringJoint>().Select(s=>s.GetInstanceID()+":"+s.anchor.ToString("R")+":"+s.connectedAnchor.ToString("R")+":"+s.spring+":"+s.damper+":"+s.minDistance+":"+s.maxDistance));
    }
    static void AssertObserved(CampingHammockRopeLateFrameObserver.Statistics samples, bool moving)
    {
        Assert.That(samples.failure, Is.Empty, "Read-only late-frame observer must complete.");
        Assert.That(samples.frames, Is.GreaterThanOrEqualTo(8));
        Assert.That(samples.endpoints, Is.EqualTo(samples.frames * 16), "All original sixteen endpoints observed each actual frame.");
        Assert.That(samples.maxStartError, Is.LessThan(.0001f), "Actual fixed-anchor rope endpoints in the SAME LateUpdate frame.");
        Assert.That(samples.maxEndError, Is.LessThan(.0001f), "Actual physical spring endpoints after the original rope writer, with no phase mismatch.");
        Assert.That(samples.maxDeckDistance, Is.LessThan(.001f));
        Assert.That(samples.maxLength, Is.LessThan(8), "No doubled-scale woodland-spanning cable.");
        Assert.That(samples.physicsExact, Is.True, "Original platform/deck/spring identity and parameters exact in every observed frame.");
        if(moving)Assert.That(samples.movingFrames, Is.GreaterThan(0), "Live endpoint proof must include actual moving anchor frames.");
        TestContext.WriteLine("ACTUAL_ROPE_FRAME_SAMPLES frames="+samples.frames+" endpoints="+samples.endpoints+" movingFrames="+samples.movingFrames+" maxStartError="+samples.maxStartError.ToString("R")+" maxEndError="+samples.maxEndError.ToString("R")+" maxAnchorMovement="+samples.maxAnchorMovement.ToString("R"));
    }
    [UnityTest,Timeout(45000)]public IEnumerator AllSixteenRopesFollowActualSpringAnchorsAtSetupAndDuringSwayWithoutChangingPhysics()
    {
        using(var scope=new WildernessNativeScope("camp-real-rope-anchors"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");
            var hammocks=Components(Get(Get(scope.match,"world"),"hammocks"));Assert.That(hammocks.Length,Is.EqualTo(4));
            var original=hammocks.Select(PhysicsIdentity).ToArray();
            var observerRoot=new GameObject("Test-owned actual post-rope LateUpdate observation");
            var observer=observerRoot.AddComponent<CampingHammockRopeLateFrameObserver>();
            string output=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(scope.progress),"rope-late-"+System.Guid.NewGuid().ToString("N")+".jsonl");
            try
            {
                Assert.That(UnityEditor.MonoImporter.GetExecutionOrder(UnityEditor.MonoScript.FromMonoBehaviour(observer)),Is.EqualTo(30000));
                Assert.That(UnityEditor.MonoImporter.GetExecutionOrder(UnityEditor.MonoScript.FromMonoBehaviour((MonoBehaviour)hammocks[0])),Is.Zero,"Original rope writer remains default order0.");
                observer.Initialize(hammocks,original,PhysicsIdentity,output);observer.BeginPhase("Setup");
                float deadline=Time.realtimeSinceStartup+4;
                while(observer.Observed("Setup").frames<8&&Time.realtimeSinceStartup<deadline)yield return null;
                AssertObserved(observer.Observed("Setup"),false);
                scope.Configure(1,true);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);observer.BeginPhase("Live");
                yield return scope.Advance(2);deadline=Time.realtimeSinceStartup+4;
                while(observer.Observed("Live").frames<8&&Time.realtimeSinceStartup<deadline)yield return null;
                AssertObserved(observer.Observed("Live"),true);
                CollectionAssert.AreEqual(original,hammocks.Select(PhysicsIdentity).ToArray());
                TestContext.WriteLine("ACTUAL_ROPE_SAME_FRAME_OUTPUT "+output);
            }
            finally { UnityEngine.Object.DestroyImmediate(observerRoot); }
            TestContext.WriteLine("ACTUAL16_ROPE_ENDPOINTS setup/live sway follow original SpringJoint anchors; mass/damping/deck scale/spring identity and parameters unchanged. Windows visual acceptance remains separate.");
        }
    }
}
#endif
