#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Numerical controls for development observers, not a production restore path.
public sealed class ArchaeologyRestorePrecisionTests
{
    static void Place(Rigidbody body,Vector3 position,Quaternion rotation)
    {
        body.transform.SetPositionAndRotation(position,rotation);
        Physics.SyncTransforms(); // Test-owned throwaway body, no production restore path.
        body.position=position;body.rotation=rotation;
    }
    [TestCase("ArchaeologyWindowsPersistenceProbe")]
    [TestCase("ArchaeologyNonzeroPersistenceWindowsProbe")]
    public void ActualWorldScaleQuantizationPassesButDisplacementVelocityAndRotationStillFail(string observer)
    {
        var actor=new GameObject("Isolated restored-assembly numerical control");
        try
        {
            var body=actor.AddComponent<Rigidbody>();body.useGravity=false;Place(body,new Vector3(54,.107097f,-17.99032f),Quaternion.identity);
            var savedType=Type.GetType("LWS.TruckArchaeology.ArchaeologyMatch+SavedBody, Assembly-CSharp",true);
            var state=Activator.CreateInstance(savedType);savedType.GetField("position").SetValue(state,body.position);savedType.GetField("rotation").SetValue(state,body.rotation);savedType.GetField("velocity").SetValue(state,Vector3.zero);savedType.GetField("angular").SetValue(state,Vector3.zero);savedType.GetField("kinematic").SetValue(state,false);
            var compare=Type.GetType("LWS.TruckArchaeology."+observer+", Assembly-CSharp",true).GetMethod("SameBody",BindingFlags.Static|BindingFlags.NonPublic);
            var origin=body.position;var rotation=body.rotation;Func<bool> matches=()=>(bool)compare.Invoke(null,new[]{(object)body,state});
            Place(body,origin+Vector3.right*.00002f,rotation);Assert.That(matches(),Is.True,"Observed actual Windows19micrometre round-trip remains within0.1mm.");
            Place(body,origin+Vector3.right*.001f,rotation);Assert.That(matches(),Is.False,"A real1mm displacement must not pass the observer.");
            Place(body,origin,rotation*Quaternion.Euler(0,1,0));Assert.That(Quaternion.Angle(body.rotation,rotation),Is.GreaterThan(.01f),"The control must represent a detectable full-degree rotation.");Assert.That(matches(),Is.False,"Rotation tolerance is unchanged.");
            Place(body,origin,rotation);body.linearVelocity=Vector3.right*.001f;Assert.That(matches(),Is.False,"Velocity tolerance is unchanged.");
            body.linearVelocity=Vector3.zero;body.isKinematic=true;Assert.That(matches(),Is.False,"Wrong physical support state must fail.");
        }
        finally{UnityEngine.Object.DestroyImmediate(actor);}
    }
}
#endif
