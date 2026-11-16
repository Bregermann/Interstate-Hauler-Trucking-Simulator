#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonRiderCourseCenterTests
{
    static Type Authoring=>Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
    [TestCase(false,false,true)][TestCase(true,false,true)][TestCase(false,true,true)][TestCase(false,false,false)]
    public void CurrentAuthoredBoxCenterRemainsCorrectWhenPhysicsBoundsAreDisabledInactiveOrUnsynchronized(bool disabled,bool inactive,bool detectCollisions)
    {
        var root=new GameObject("Read-only native crossing center control");var body=root.AddComponent<Rigidbody>();body.isKinematic=true;body.detectCollisions=detectCollisions;var truck=new GameObject("Existing source box stand-in");truck.transform.SetParent(root.transform,false);var shape=truck.AddComponent<BoxCollider>();shape.center=new Vector3(1,2,3);shape.size=new Vector3(2,4,6);
        try
        {
            root.transform.position=new Vector3(100,200,300);truck.transform.localPosition=new Vector3(4,5,6);Physics.SyncTransforms();
            shape.enabled=!disabled;root.SetActive(!inactive);
            // Deliberately update the represented current pose without a physics step/sync.
            // The reference is a literal affine result, not another Collider.bounds query.
            root.transform.position=new Vector3(1000,2000,3000);truck.transform.localPosition=new Vector3(7,8,9);
            var row=Authoring.GetMethod("MeasureCurrentRiderTruckCrossingCenter").Invoke(null,new object[]{shape,"native-source-center-control"});object Field(string name)=>row.GetType().GetField(name).GetValue(row);
            Assert.That(((Vector3)Field("currentMatrixCenter")).Equals(new Vector3(1008,2010,3012)),Is.True);Assert.That(shape.center.Equals(new Vector3(1,2,3)),Is.True);Assert.That(shape.size.Equals(new Vector3(2,4,6)),Is.True);Assert.That((bool)Field("shapeEnabled"),Is.EqualTo(!disabled));Assert.That((bool)Field("shapeActiveInHierarchy"),Is.EqualTo(!inactive));Assert.That((bool)Field("hasAttachedBody"),Is.EqualTo(shape.attachedRigidbody!=null));Assert.That((bool)Field("bodyDetectCollisions"),Is.EqualTo(shape.attachedRigidbody!=null&&shape.attachedRigidbody.detectCollisions));Assert.That(body.detectCollisions,Is.EqualTo(detectCollisions));
            // Unity may retain a cached nonempty AABB after disabling an actor.
            // Validate the recorded native witness, never require one cache policy.
            var native=shape.bounds;Assert.That((Vector3)Field("physicsBoundsCenter"),Is.EqualTo(native.center));Assert.That((Vector3)Field("physicsBoundsSize"),Is.EqualTo(native.size));Assert.That((bool)Field("physicsBoundsEmpty"),Is.EqualTo(native.size.Equals(Vector3.zero)));Assert.That((float)Field("centerDelta"),Is.EqualTo(Vector3.Distance(new Vector3(1008,2010,3012),native.center)));
            TestContext.WriteLine("ACTUAL_CENTER_CACHE_WITNESS disabled="+disabled+" inactive="+inactive+" cacheSize="+native.size.ToString("R")+" delta="+Field("centerDelta"));
            Assert.That((Matrix4x4)Field("currentLocalToWorld"),Is.EqualTo(truck.transform.localToWorldMatrix));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test]
    public void CurrentCenterUsesActualRotatedBoxTransformAndPreservesShapeConfiguration()
    {
        var root=new GameObject("Exact rotated box center control");var shape=root.AddComponent<BoxCollider>();shape.center=new Vector3(1,2,3);shape.size=new Vector3(2,4,6);shape.isTrigger=true;
        try
        {
            root.transform.SetPositionAndRotation(new Vector3(100,200,300),new Quaternion(0,1,0,0));
            var row=Authoring.GetMethod("MeasureCurrentRiderTruckCrossingCenter").Invoke(null,new object[]{shape,"native-rotated-control"});var actual=(Vector3)row.GetType().GetField("currentMatrixCenter").GetValue(row);
            Assert.That(actual.Equals(new Vector3(99,202,297)),Is.True);Assert.That(shape.center.Equals(new Vector3(1,2,3)),Is.True);Assert.That(shape.isTrigger,Is.True);Assert.That(root.transform.localScale.Equals(Vector3.one),Is.True);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
