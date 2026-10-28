#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public sealed class OrchestraPresentationGuardTests
{
    [Test]public void ExactActualAuthoredLegIdentityPassesWhileChangedPositionScaleAndRotationRefuse()
    {
        var type=Type.GetType("LWS.TruckOrchestra.Editor.OrchestraPresentationAuthoring, Assembly-CSharp-Editor",true);var method=type.GetMethod("Original",BindingFlags.Static|BindingFlags.NonPublic);Assert.That(method,Is.Not.Null);var g=new GameObject("Instrument carved leg");
        try
        {
            Vector3 position=new Vector3(-3,1,1),scale=new Vector3(.35f,2.1f,.5f);g.transform.localPosition=position;g.transform.localScale=scale;g.transform.localRotation=Quaternion.identity;
            Action check=()=>method.Invoke(null,new object[]{g.transform,position,scale,default(Quaternion)});
            Assert.DoesNotThrow(()=>check(),"Actual saved original identity leg must pass the omitted-rotation sentinel");
            g.transform.localPosition+=Vector3.right*.001f;Assert.That(Assert.Throws<TargetInvocationException>(()=>check()).InnerException,Is.TypeOf<InvalidOperationException>());g.transform.localPosition=position;
            g.transform.localScale+=Vector3.right*.001f;Assert.That(Assert.Throws<TargetInvocationException>(()=>check()).InnerException,Is.TypeOf<InvalidOperationException>());g.transform.localScale=scale;
            g.transform.localRotation=Quaternion.Euler(0,1,0);Assert.That(Assert.Throws<TargetInvocationException>(()=>check()).InnerException,Is.TypeOf<InvalidOperationException>());g.transform.localRotation=Quaternion.identity;
            Assert.DoesNotThrow(()=>check());
        }
        finally{UnityEngine.Object.DestroyImmediate(g);}
    }
}
#endif
