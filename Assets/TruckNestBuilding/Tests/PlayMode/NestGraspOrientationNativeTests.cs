#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Synthetic finite-joint fixture. This discriminates an orientation-control
// defect; it is not ordinary AI/session/family completion evidence.
public sealed partial class NestNativeTests
{
    [Test] public void RetainedGraspPitchHasNoYawOnlySolutionAtTheOriginalSevenDegreeGate()
    {
        const float pitch=11.2466f;
        float best=180;
        for(int step=-1800;step<=1800;step++)
        {
            var forward=Quaternion.Euler(0,step*.1f,0)*Quaternion.Euler(pitch,0,0)*Vector3.forward;
            best=Mathf.Min(best,Vector3.Angle(forward,Vector3.forward));
        }
        Assert.That(best,Is.EqualTo(pitch).Within(.0002f));
        Assert.That(best,Is.GreaterThan(7),"The original full-angle gate cannot be passed by correcting yaw alone on a tilted retained grasp.");
        var grasp=Quaternion.Euler(pitch,21,-8);var desired=Quaternion.Euler(0,90,0);
        var requestedHand=desired*Quaternion.Inverse(grasp);
        Assert.That(Mathf.Abs(Quaternion.Dot(requestedHand*grasp,desired)),Is.GreaterThan(.999999f));
    }

    [UnityTest,Timeout(20000)] public IEnumerator SyntheticActualFiniteGripRetainsTiltUnderYawOnlyButBoundedHandCanAlignItsCapturedOffset()
    {
        float oldScale=Time.timeScale;var root=new GameObject("Synthetic original finite-grip orientation fixture");
        string path=Path.GetFullPath("Logs/TruckSports/Impossible07/NestGraspOrientationDiagnostic/synthetic-actual-joint.json");Directory.CreateDirectory(Path.GetDirectoryName(path));
        try
        {
            Time.timeScale=1;
            var handObject=new GameObject("Actual kinematic hand");handObject.transform.SetParent(root.transform,false);
            var hand=handObject.AddComponent<Rigidbody>();hand.isKinematic=true;hand.position=new Vector3(10000,20,10000);hand.rotation=Quaternion.identity;
            Action<string,Rigidbody> record=(stage,actual)=>File.AppendAllText(path+".inputs.jsonl",JsonUtility.ToJson(new GraspInput{stage=stage,frame=Time.frameCount,fixedTime=Time.fixedTime,body=actual==null?Quaternion.identity:actual.rotation,bodyTransform=actual==null?Quaternion.identity:actual.transform.rotation,hand=hand.rotation,handTransform=hand.transform.rotation,forward=actual==null?Vector3.zero:actual.rotation*Vector3.forward})+"\n");
            var pieceObject=new GameObject("Actual dynamic tilted stick");pieceObject.transform.SetParent(root.transform,false);
            var body=pieceObject.AddComponent<Rigidbody>();record("after-body-component",body);body.mass=3;body.position=hand.position+Vector3.down*.5f;body.rotation=Quaternion.Euler(12,0,0);record("requested-rigidbody-pose-before-collider",body);
            var shape=pieceObject.AddComponent<BoxCollider>();shape.size=new Vector3(.25f,.25f,3);record("after-collider-component",body);
            var part=pieceObject.AddComponent(Type.GetType("LWS.TruckNestBuilding.NestMaterial, Assembly-CSharp",true));Set(part,"body",body);Set(part,"shape",shape);record("after-material-component-before-reserve",body);
            // Original live grabs occur after initialized PhysX poses. Creating a
            // FixedJoint in the same frame as the initial Rigidbody pose setter
            // instead binds the still-identity Transform frame (actualV14b).
            yield return new WaitForFixedUpdate();record("actual-initial-fixed-pose-before-reserve",body);
            Assert.That(Mathf.Abs(Quaternion.Dot(body.rotation,body.transform.rotation)),Is.GreaterThan(.999999f),"Synthetic initial pose must be physically published before the original joint captures its reference frame.");
            Assert.That(Vector3.Angle(body.rotation*Vector3.forward,Vector3.forward),Is.EqualTo(12).Within(.05f),"Actual free initialized cargo must still have the intended12deg pitch before grasp; never rotate it after adding the joint.");
            Assert.That((bool)Call(part,"Reserve",0),Is.True);record("reserved-before-grab",body);Call(part,"Grab",hand,0);record("immediately-after-original-grab",body);
            var joint=(FixedJoint)Get(part,"Grip");Assert.That(joint,Is.Not.Null);Assert.That(joint.connectedBody,Is.SameAs(hand));
            var relative=Quaternion.Inverse(hand.rotation)*body.rotation;var desired=Quaternion.identity;
            float yaw=0,bestOld=180,maxForce=0,maxTorque=0;int oldSamples=0,newSamples=0;
            float stop=Time.realtimeSinceStartup+2;
            while(Time.realtimeSinceStartup<stop)
            {
                Assert.That(joint,Is.Not.Null,"Original finite grip must remain genuine throughout the synthetic control comparison.");
                float error=Vector3.SignedAngle(body.rotation*Vector3.forward,Vector3.forward,Vector3.up);
                yaw=Mathf.DeltaAngle(0,yaw+Mathf.Clamp(error*2,-65,65)*Time.fixedDeltaTime);
                hand.MoveRotation(Quaternion.RotateTowards(hand.rotation,Quaternion.Euler(0,yaw,0),65*Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();
                if(oldSamples<12||oldSamples%20==0)record("yaw-only-fixed-"+oldSamples,body);
                bestOld=Mathf.Min(bestOld,Vector3.Angle(body.rotation*Vector3.forward,Vector3.forward));oldSamples++;
                maxForce=Mathf.Max(maxForce,joint.currentForce.magnitude);maxTorque=Mathf.Max(maxTorque,joint.currentTorque.magnitude);
            }
            record("yaw-only-control-ended",body);
            var settledRelative=Quaternion.Inverse(hand.rotation)*body.rotation;
            File.AppendAllText(path+".inputs.jsonl",JsonUtility.ToJson(new GraspInput{stage="initial-versus-settled-grasp-offset",body=relative,bodyTransform=settledRelative})+"\n");
            float finalAngle=180;stop=Time.realtimeSinceStartup+3;
            while(Time.realtimeSinceStartup<stop)
            {
                var target=(Quaternion)Type.GetType("LWS.TruckNestBuilding.NestBuilder, Assembly-CSharp",true).GetMethod("PlacementHandTarget").Invoke(null,new object[]{desired,settledRelative});
                hand.MoveRotation(Quaternion.RotateTowards(hand.rotation,target,65*Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();newSamples++;Assert.That(joint,Is.Not.Null);
                if(newSamples<12||newSamples%20==0)record("offset-control-fixed-"+newSamples,body);
                finalAngle=Quaternion.Angle(body.rotation,desired);maxForce=Mathf.Max(maxForce,joint.currentForce.magnitude);maxTorque=Mathf.Max(maxTorque,joint.currentTorque.magnitude);
            }
            record("offset-control-ended",body);
            File.WriteAllText(path,JsonUtility.ToJson(new GraspOrientationObservation{initialRelative=relative,finalCargo=body.rotation,finalHand=hand.rotation,minimumYawOnlyAngle=bestOld,finalFullAngle=finalAngle,maximumForce=maxForce,maximumTorque=maxTorque,oldSamples=oldSamples,newSamples=newSamples},true));
            Assert.That(bestOld,Is.GreaterThan(7),"Changing yaw alone must reproduce the unachievable original full-angle alignment gate; pre/after-component/Grab/fixed-phase quaternions are retained even if this negative control fails.");
            Assert.That(finalAngle,Is.LessThan(7),"The same bounded hand must physically align the real dynamic cargo without writing cargo pose.");
            Assert.That(body.isKinematic,Is.False);Assert.That(body.detectCollisions,Is.True);Assert.That(joint.breakForce,Is.EqualTo(2500));Assert.That(joint.breakTorque,Is.EqualTo(1800));
            Assert.That(joint.connectedBody,Is.SameAs(hand));Assert.That(maxForce,Is.LessThan(2500));Assert.That(maxTorque,Is.LessThan(1800));
            File.WriteAllText(path,JsonUtility.ToJson(new GraspOrientationObservation{initialRelative=relative,finalCargo=body.rotation,finalHand=hand.rotation,minimumYawOnlyAngle=bestOld,finalFullAngle=finalAngle,maximumForce=maxForce,maximumTorque=maxTorque,oldSamples=oldSamples,newSamples=newSamples},true));
            TestContext.WriteLine("SYNTHETIC_FINITE_GRASP minimumYawOnlyAngle="+bestOld.ToString("R")+" actualAlignedAngle="+finalAngle.ToString("R")+" maxForce="+maxForce.ToString("R")+" maxTorque="+maxTorque.ToString("R")+"; no ordinary-AI/session/hatching claim; original2500N/1800Nm/65deg-per-second retained.");
        }
        finally {UnityEngine.Object.DestroyImmediate(root);Time.timeScale=oldScale;}
    }
    static void VerifyActualPlacementGraspTargets(Component match,System.Collections.Generic.Dictionary<string,Quaternion> offsets,System.Collections.Generic.HashSet<string> tilted,System.Collections.Generic.HashSet<string> corrected)
    {
        foreach(var builder in Components(Get(match,"builders")))
        {
            var item=(Component)Get(builder,"Carried");if(item==null)continue;var grip=(FixedJoint)Get(item,"Grip");if(grip==null)continue;
            Assert.That((bool)Get(builder,"GraspOffsetCaptured"),Is.True,"Actual successful live grasp must capture its original initialized frame.");
            var offset=(Quaternion)Get(builder,"CapturedGraspOffset");string generation=Get(builder,"slot")+":"+Get(builder,"Grasps");
            if(offsets.TryGetValue(generation,out var original))Assert.That(Mathf.Abs(Quaternion.Dot(offset,original)),Is.GreaterThan(.999999f),"An intact physical grasp offset stays fixed until an actual drop/break/new grasp.");else offsets.Add(generation,offset);
            if(Get(builder,"Mode").ToString()!="Placing")continue;
            var site=(Component)Get(builder,"GoalSite");int socket=(int)Get(builder,"GoalSocket");Assert.That(site,Is.Not.Null);Assert.That(socket,Is.GreaterThanOrEqualTo(0));
            var desired=(Quaternion)Call(site,"TargetRotation",socket);var requested=(Quaternion)Get(builder,"RequestedHandRotation");
            Assert.That(Mathf.Abs(Quaternion.Dot(requested*offset,desired)),Is.GreaterThan(.99999f),"Actual AI uses a grasp-compensated hand target, not a yaw-only target for already-tilted cargo.");
            Assert.That(grip.connectedBody,Is.SameAs(Get(builder,"hand")));Assert.That(grip.breakForce,Is.EqualTo(2500));Assert.That(grip.breakTorque,Is.EqualTo(1800));Assert.That(((Rigidbody)Get(item,"body")).isKinematic,Is.False);
            float pitch=Mathf.Abs(Mathf.Asin(Mathf.Clamp((offset*Vector3.forward).y,-1,1))*Mathf.Rad2Deg);
            if(pitch>7){tilted.Add(generation);if((int)Get(builder,"PlacementStage")>0&&Vector3.Angle(((Rigidbody)Get(item,"body")).rotation*Vector3.forward,desired*Vector3.forward)<7)corrected.Add(generation);}
        }
    }
    [Serializable] sealed class GraspInput {public string stage;public int frame;public float fixedTime;public Quaternion body,bodyTransform,hand,handTransform;public Vector3 forward;}
    [Serializable] sealed class GraspOrientationObservation
    {
        public string scope="SYNTHETIC_ACTUAL_FINITE_JOINT_NOT_GAMEPLAY_ACCEPTANCE";
        public Quaternion initialRelative,finalCargo,finalHand;
        public float minimumYawOnlyAngle,finalFullAngle,maximumForce,maximumTorque;
        public int oldSamples,newSamples;
    }
}
#endif
