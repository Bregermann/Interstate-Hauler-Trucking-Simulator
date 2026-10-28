#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public sealed class ArchaeologySpreaderColliderTests
{
 [Test]public void SameActualSphereMeetsOriginalTopAnchorWithoutPenetrationInUprightAndTiltedLoadFrames()
 {
  var type=Type.GetType("LWS.TruckArchaeology.Editor.ArchaeologySpreaderColliderAuthoring, Assembly-CSharp-Editor",true);var validate=type.GetMethod("ValidateUnmodifiedSphere",BindingFlags.Static|BindingFlags.NonPublic);var apply=type.GetMethod("ConfigureNewHead",BindingFlags.Static|BindingFlags.NonPublic);var a=new GameObject("Original physical artifact");var b=new GameObject("Actual kinematic excavation head / crane spreader 0");
  try
  {
   var box=a.AddComponent<BoxCollider>();box.size=new Vector3(1.5f,1.3f,4);var head=b.AddComponent<Rigidbody>();head.isKinematic=true;head.useGravity=false;var sphere=b.AddComponent<SphereCollider>();sphere.radius=.25f;Assert.DoesNotThrow(()=>validate.Invoke(null,new object[]{sphere}));sphere.center=Vector3.up*.01f;Assert.That(Assert.Throws<TargetInvocationException>(()=>validate.Invoke(null,new object[]{sphere})).InnerException,Is.TypeOf<InvalidOperationException>());sphere.center=Vector3.zero;sphere.radius=.3f;Assert.That(Assert.Throws<TargetInvocationException>(()=>validate.Invoke(null,new object[]{sphere})).InnerException,Is.TypeOf<InvalidOperationException>());sphere.radius=.25f;float mass=head.mass,offset=sphere.contactOffset;
   foreach(Quaternion q in new[]{Quaternion.identity,Quaternion.Euler(23,31,17)})
   {
    a.transform.SetPositionAndRotation(new Vector3(-54,1.8f,5),q);Vector3 anchor=a.transform.TransformPoint(Vector3.up*(box.size.y*.5f));b.transform.SetPositionAndRotation(anchor,q);sphere.center=Vector3.zero;Assert.That(Physics.ComputePenetration(sphere,b.transform.position,b.transform.rotation,box,a.transform.position,a.transform.rotation,out _,out float oldDepth),Is.True);Assert.That(oldDepth,Is.GreaterThan(.24f),"Old sphere center at pickup anchor geometrically penetrates the actual load by its radius");apply.Invoke(null,new object[]{sphere});bool overlap=Physics.ComputePenetration(sphere,b.transform.position,b.transform.rotation,box,a.transform.position,a.transform.rotation,out _,out float newDepth);Assert.That(!overlap||newDepth<1e-5f,Is.True,"New sphere bottom touches unchanged top anchor without body overlap");Assert.That(Vector3.Distance(sphere.transform.TransformPoint(sphere.center-Vector3.up*sphere.radius),anchor),Is.LessThan(1e-5f));Assert.That(head.mass,Is.EqualTo(mass));Assert.That(head.isKinematic,Is.True);Assert.That(head.useGravity,Is.False);Assert.That(sphere.enabled&&!sphere.isTrigger,Is.True);Assert.That(sphere.radius,Is.EqualTo(.25f));Assert.That(sphere.contactOffset,Is.EqualTo(offset));Assert.That(Assert.Throws<TargetInvocationException>(()=>validate.Invoke(null,new object[]{sphere})).InnerException,Is.TypeOf<InvalidOperationException>());
   }
  }
  finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
 }
}
#endif
