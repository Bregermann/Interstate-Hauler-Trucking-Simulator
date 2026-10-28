#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public sealed class OrchestraPodiumClearanceTests
{
 [Test]public void ExactInstalledV2RaisesFeetAndPodiumTopTogetherWithoutFloatingAndRefusesCustomizedOrPartialPoses()
 {
  var type=Type.GetType("LWS.TruckOrchestra.Editor.OrchestraPodiumClearanceAuthoring, Assembly-CSharp-Editor",true);var validate=type.GetMethod("ValidateInstalledPose",BindingFlags.NonPublic|BindingFlags.Static);var apply=type.GetMethod("ApplyNewPresentation",BindingFlags.NonPublic|BindingFlags.Static);var root=new GameObject("Original podium group");var actor=new GameObject("Actual Generic actor");var podium=new GameObject("Ornate conductor podium");podium.transform.SetParent(root.transform,false);
  try
  {
   Action reset=()=>{actor.transform.SetPositionAndRotation(new Vector3(0,1.05f,-3),Quaternion.identity);actor.transform.localScale=Vector3.one*2;podium.transform.SetPositionAndRotation(new Vector3(0,.5f,-3),Quaternion.identity);podium.transform.localScale=new Vector3(5,1,4);};reset();
   Action check=()=>validate.Invoke(null,new object[]{actor.transform,podium.transform});Assert.DoesNotThrow(()=>check());
   actor.transform.position+=Vector3.right*.001f;Assert.That(Assert.Throws<TargetInvocationException>(()=>check()).InnerException,Is.TypeOf<InvalidOperationException>());reset();
   podium.transform.localScale+=Vector3.up*.001f;Assert.That(Assert.Throws<TargetInvocationException>(()=>check()).InnerException,Is.TypeOf<InvalidOperationException>());reset();
   podium.transform.rotation=Quaternion.Euler(0,1,0);Assert.That(Assert.Throws<TargetInvocationException>(()=>check()).InnerException,Is.TypeOf<InvalidOperationException>());reset();
   var collider=podium.AddComponent<BoxCollider>();Assert.That(Assert.Throws<TargetInvocationException>(()=>check()).InnerException,Is.TypeOf<InvalidOperationException>());UnityEngine.Object.DestroyImmediate(collider);Assert.DoesNotThrow(()=>check());
   apply.Invoke(null,new object[]{actor.transform,podium.transform});Assert.That(actor.transform.position,Is.EqualTo(new Vector3(0,1.4f,-3)));Assert.That(actor.transform.localScale,Is.EqualTo(Vector3.one*2));float bottom=podium.transform.position.y-podium.transform.localScale.y*.5f,top=podium.transform.position.y+podium.transform.localScale.y*.5f;Assert.That(bottom,Is.EqualTo(0).Within(1e-6f));Assert.That(top,Is.EqualTo(1.35f).Within(1e-6f));Assert.That(actor.transform.position.y-top,Is.EqualTo(.05f).Within(1e-6f));Assert.That(Assert.Throws<TargetInvocationException>(()=>check()).InnerException,Is.TypeOf<InvalidOperationException>(),"Already-applied/partial repairs cannot silently reapply or overwrite custom work");
  }
  finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(actor);}
 }
}
#endif
