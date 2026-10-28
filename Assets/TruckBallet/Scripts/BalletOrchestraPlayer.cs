using UnityEngine;
namespace LWS.TruckBallet
{
    // Original missing instrument-playing routine over the real clothed UTS
    // Generic bones. The Animator is explicitly disabled: one bone writer.
    public sealed class BalletOrchestraPlayer:MonoBehaviour
    {
        public BalletMatch match;
        public Animator actor;
        public Transform leftUpper,rightUpper,leftForearm,rightForearm,head;
        public bool cello;
        public float phase;
        Quaternion lu,ru,lf,rf,h;
        public int AnimatedFrames{get;private set;}
        void Awake(){actor.enabled=false;actor.applyRootMotion=false;lu=leftUpper.localRotation;ru=rightUpper.localRotation;lf=leftForearm.localRotation;rf=rightForearm.localRotation;h=head.localRotation;}
        void LateUpdate()
        {
            if(!match.Live||Time.deltaTime<=0)return;AnimatedFrames++;phase+=Time.deltaTime*2.8f;float bow=Mathf.Sin(phase)*14;
            Pose(leftUpper,lu,new Vector3(cello?-25:-35,0,-60));Pose(leftForearm,lf,new Vector3(-65,Mathf.Sin(phase*.5f)*4,0));
            Pose(rightUpper,ru,new Vector3(-30,bow,60));Pose(rightForearm,rf,new Vector3(-70+bow*.7f,0,0));Pose(head,h,new Vector3(Mathf.Sin(phase*.5f)*2,0,Mathf.Cos(phase*.4f)*2));
        }
        void Pose(Transform bone,Quaternion rest,Vector3 angles){var basis=Quaternion.Inverse(bone.parent.rotation)*actor.transform.rotation;bone.localRotation=basis*Quaternion.Euler(angles)*Quaternion.Inverse(basis)*rest;}
        void OnDisable(){if(leftUpper==null)return;leftUpper.localRotation=lu;rightUpper.localRotation=ru;leftForearm.localRotation=lf;rightForearm.localRotation=rf;head.localRotation=h;}
    }
}
