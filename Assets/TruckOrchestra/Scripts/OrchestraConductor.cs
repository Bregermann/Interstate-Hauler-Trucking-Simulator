using UnityEngine;

namespace LWS.TruckOrchestra
{
    // One pose writer: sample the original compatible Generic vendor idle,
    // then apply music-clock conducting to the actual skinned arm chain.
    public sealed class OrchestraConductor : MonoBehaviour
    {
        public Transform leftArm, rightArm, baton, torso, actorSpace;
        public Transform leftForearm,rightForearm,leftHand,rightHand;
        public AnimationClip idleFoundation;
        public int ConductedBeats { get; private set; }
        int previous=-1;
        void SampleFoundation(double clock)
        {
            if(idleFoundation==null||actorSpace==null||idleFoundation.humanMotion)
                throw new System.InvalidOperationException("Original compatible Generic conductor idle required; run explicit presentation author.");
            // A Generic clip may contain an imported root curve. The owned stage
            // anchor remains fixed; all actual skeleton tracks still evaluate.
            Vector3 p=actorSpace.localPosition,s=actorSpace.localScale;Quaternion q=actorSpace.localRotation;
            idleFoundation.SampleAnimation(actorSpace.gameObject,(float)(System.Math.Max(0,clock)%idleFoundation.length));
            actorSpace.localPosition=p;actorSpace.localRotation=q;actorSpace.localScale=s;
        }
        void Aim(Transform bone,Transform child,Vector3 actorDirection)
        {
            Vector3 current=child.position-bone.position,desired=actorSpace.TransformDirection(actorDirection);
            if(current.sqrMagnitude>.000001f)bone.rotation=Quaternion.FromToRotation(current,desired)*bone.rotation;
        }
        public void Conduct(double clock,double beat)
        {
            if(beat<=0)return;SampleFoundation(clock);
            float phase=(float)(System.Math.Max(0,clock)/beat*Mathf.PI*2),a=Mathf.Sin(phase),b=Mathf.Sin(phase*.5f);
            // Elbows stay genuinely bent; hands travel in front of the torso.
            // These target directions are actor-space, independent of bone axes.
            Aim(leftArm,leftForearm,new Vector3(-.45f,-.35f,.8f));
            Aim(rightArm,rightForearm,new Vector3(.45f+.06f*b,-.35f-.1f*a,.85f));
            Aim(leftForearm,leftHand,new Vector3(-.15f,.75f+.15f*b,.8f));
            Aim(rightForearm,rightHand,new Vector3(.15f,.8f+.18f*a,.8f));
            leftHand.localRotation*=Quaternion.Euler(4+a*8,b*6,0);
            rightHand.localRotation*=Quaternion.Euler(8+a*18,-b*8,b*6);
            torso.localRotation*=Quaternion.Euler(a*2,b*3,b);
            int current=(int)(clock/beat);if(current!=previous&&clock>=0){previous=current;ConductedBeats++;}
        }
        public void ResetPose(bool clearStatistics=false)
        {
            SampleFoundation(0);previous=-1;if(clearStatistics)ConductedBeats=0;
        }
    }
}
