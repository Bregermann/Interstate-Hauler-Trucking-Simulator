#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

// Runs after the original session FixedUpdate and before that step's PhysX solve.
// The next step supplies the corresponding actual solved velocity/position.
// Transition-only output is bounded; no mechanics or solver settings are changed.
[DefaultExecutionOrder(31000)]
public sealed class DragonEggJointTransitionObservation:MonoBehaviour
{
    object match;
    string output;
    readonly string[] signatures=new string[4];
    readonly int[] remaining=new int[4];
    readonly bool[] previouslyGripped=new bool[4];
    readonly StringBuilder evidence=new StringBuilder();
    int fixedSteps,captures;
    bool dirty;
    public int UnsafeDisconnectedDriveSteps {get;private set;}
    public int InertReleasedJointSteps {get;private set;}
    public int ActualGripHandovers {get;private set;}
    public int ActualPrematureGripLosses {get;private set;}
    public float MaximumHandoverAnchorError {get;private set;}
    static object Get(object item,string name)=>item.GetType().GetField(name) is FieldInfo field?field.GetValue(item):item.GetType().GetProperty(name).GetValue(item);
    public void Configure(object originalMatch,string path){match=originalMatch;output=path;}
    void FixedUpdate()
    {
        if(match==null||!(bool)Get(match,"Running")||(bool)Get(match,"Paused")||(float)Get(match,"Intro")>0||captures>=1000)return;
        fixedSteps++;var carriers=(Array)Get(match,"carriers");
        for(int slot=0;slot<4;slot++)
        {
            var carrier=carriers.GetValue(slot);var cargo=(Component)Get(carrier,"cargo");var restraint=Get(cargo,"Restraint") as Joint;var crane=Get(carrier,"crane");var joints=cargo.GetComponents<Joint>();
            var head=(Rigidbody)Get(crane,"spreader");bool gripped=Array.Exists(joints,joint=>joint is FixedJoint&&joint.connectedBody==head);
            string stage=Get(crane,"Stage").ToString();if(previouslyGripped[slot]&&!gripped&&stage!="Rest"&&stage!="Release")ActualPrematureGripLosses++;
            if(gripped&&!previouslyGripped[slot]&&restraint==null)ActualGripHandovers++;previouslyGripped[slot]=gripped;
            foreach(var joint in joints)
            {
                if(!(joint is ConfigurableJoint released)||released.connectedBody!=null)continue;
                bool inert=!Active(released.xDrive)&&!Active(released.yDrive)&&!Active(released.zDrive)&&!Active(released.angularXDrive)&&!Active(released.angularYZDrive)&&!Active(released.slerpDrive)&&
                    released.linearLimitSpring.spring==0&&released.linearLimitSpring.damper==0&&released.angularXLimitSpring.spring==0&&released.angularXLimitSpring.damper==0&&released.angularYZLimitSpring.spring==0&&released.angularYZLimitSpring.damper==0&&
                    released.xMotion==ConfigurableJointMotion.Free&&released.yMotion==ConfigurableJointMotion.Free&&released.zMotion==ConfigurableJointMotion.Free&&released.angularXMotion==ConfigurableJointMotion.Free&&released.angularYMotion==ConfigurableJointMotion.Free&&released.angularZMotion==ConfigurableJointMotion.Free;
                if(inert)InertReleasedJointSteps++;else UnsafeDisconnectedDriveSteps++;
                if(gripped)MaximumHandoverAnchorError=Mathf.Max(MaximumHandoverAnchorError,Vector3.Distance(cargo.transform.TransformPoint(released.anchor),released.connectedAnchor));
            }
            string signature=Get(crane,"Stage")+":"+(restraint==null?0:restraint.GetEntityId())+":"+joints.Length;
            if(signature!=signatures[slot]){signatures[slot]=signature;remaining[slot]=8;}
            if(remaining[slot]--<=0)continue;
            evidence.AppendLine("AFTER_SESSION_BEFORE_PHYSX step="+fixedSteps+" fixedTime="+Time.fixedTimeAsDouble+" actualClock="+Get(match,"Clock")+" slot="+slot+" signature="+signature+" postTransitionRemaining="+remaining[slot]);
            DragonEggPhysicsObservation.Append(match,slot,evidence,false);captures++;dirty=true;
        }
    }
    static bool Active(JointDrive drive)=>drive.maximumForce>0&&(drive.positionSpring!=0||drive.positionDamper!=0);
    void LateUpdate(){Flush();}
    void OnDisable(){Flush();}
    void Flush(){if(!dirty||output==null)return;File.WriteAllText(output,evidence+"\nACTUAL_RELEASE_COUNTERS unsafeDrivenWorldSteps="+UnsafeDisconnectedDriveSteps+" inertReleasedSteps="+InertReleasedJointSteps+" actualGripHandovers="+ActualGripHandovers+" prematureGripLosses="+ActualPrematureGripLosses+" maximumHandoverAnchorError="+MaximumHandoverAnchorError);dirty=false;}
}
#endif
