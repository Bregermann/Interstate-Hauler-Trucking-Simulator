using System;
using DG.Tweening;
using LWS.TruckFieldGoal;
using UnityEngine;

namespace LWS.TruckDockDiving
{
    // One small adapter per nearby rig. Original crowd reactions interrupt the
    // ambient performance; compatible vendor Animator clips own the body pose.
    public sealed class DockSpectatorLife : MonoBehaviour
    {
        public DockMatch match;
        public FieldGoalCrowd crowd;
        public Animator actor;
        public Transform anchor,phone;
        public int variety;
        public string[] activities={"Idle","Look","Talk A","Talk B","Listen","Dance","Clap","Cheer","Photo","Laugh","Walk"};
        Vector3 home; Quaternion facing; float next,age; int choice=-1;
        Sequence stroll;
        bool reacting,paused,initialized;
        Transform head,spine,rightUpper,rightElbow,rightHand,leftUpper,leftElbow,leftHand;
        public int ActivityChanges {get;private set;}
        public string Activity {get;private set;}="Idle";
        void Start()
        {
            home=anchor.position;facing=anchor.rotation;initialized=true;next=.2f+variety*.137f;actor.speed=.87f+variety%7*.047f;
            // Only the24 nearby rigs run continuously. Their small post-pose
            // gestures need a fresh base pose even while outside the camera;
            // far fans retain the shared eight-source sampling path.
            actor.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var bone in actor.GetComponentsInChildren<Transform>(true))switch(bone.name)
            {
                case "Bip01 Head":head=bone;break;case "Bip01 Spine":spine=bone;break;
                case "Bip01 R UpperArm":rightUpper=bone;break;case "Bip01 R Forearm":rightElbow=bone;break;case "Bip01 R Hand":rightHand=bone;break;
                case "Bip01 L UpperArm":leftUpper=bone;break;case "Bip01 L Forearm":leftElbow=bone;break;case "Bip01 L Hand":leftHand=bone;break;
            }
        }
        void Update()
        {
            if(match==null||actor==null)return;
            if(match.Paused){if(!paused){actor.speed=0;stroll?.Pause();paused=true;}return;}
            if(paused){paused=false;actor.speed=.87f+variety%7*.047f;stroll?.Play();}
            age+=Time.deltaTime;
            if(crowd.Mood!="IDLE")
            {
                if(!reacting){reacting=true;StopWalk();if(phone!=null)phone.gameObject.SetActive(false);}
                return;
            }
            if(reacting){reacting=false;next=age+.2f+variety%9*.13f;}
            if(age<next)return;
            StopWalk();choice=(choice+3+variety%5)%activities.Length;Activity=activities[choice];
            if(!actor.HasState(0,Animator.StringToHash(Activity)))Activity="Idle";
            actor.CrossFade(Activity,.35f,0,(variety*.139f)%1);ActivityChanges++;
            if(phone!=null)phone.gameObject.SetActive(Activity=="Photo");
            next=age+4.5f+variety%7*.67f;
            if(Activity=="Walk")
            {
                // Stay in the authored spectator seat/aisle. These render-only
                // people never enter the approach, pool or physical truck lane.
                float sign=variety%2==0?1:-1;var destination=home+Vector3.forward*(sign*.58f);
                anchor.rotation=Quaternion.LookRotation(Vector3.forward*sign);
                stroll=DOTween.Sequence().Append(anchor.DOMove(destination,1.15f).SetEase(Ease.Linear))
                    .AppendCallback(()=>anchor.rotation=Quaternion.LookRotation(Vector3.back*sign))
                    .Append(anchor.DOMove(home,1.15f).SetEase(Ease.Linear))
                    .OnComplete(()=>{anchor.rotation=facing;actor.CrossFade("Idle",.3f);stroll=null;});
            }
        }
        void LateUpdate()
        {
            if(!initialized||reacting||match.Paused)return;
            if(Activity=="Laugh"&&spine!=null){spine.localRotation*=Quaternion.Euler(5+Mathf.Sin(age*19)*2,0,0);if(head!=null)head.localRotation*=Quaternion.Euler(Mathf.Sin(age*18)*2,0,0);}
            if(Activity!="Photo"||phone==null||head==null)return;
            var target=head.position+anchor.forward*.33f-Vector3.up*.1f;
            Arm(rightUpper,rightElbow,rightHand,target+anchor.right*.065f,anchor.right);
            Arm(leftUpper,leftElbow,leftHand,target-anchor.right*.065f,-anchor.right);
            phone.SetPositionAndRotation(target,Quaternion.LookRotation(anchor.forward,Vector3.up));
        }
        static void Arm(Transform shoulder,Transform elbow,Transform hand,Vector3 target,Vector3 outside)
        {
            if(shoulder==null||elbow==null||hand==null)return;
            float upper=Vector3.Distance(shoulder.position,elbow.position),lower=Vector3.Distance(elbow.position,hand.position);
            var direction=target-shoulder.position;float reach=Mathf.Clamp(direction.magnitude,.05f,upper+lower-.005f);direction.Normalize();
            float along=(upper*upper-lower*lower+reach*reach)/(2*reach);float side=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            var bend=Vector3.ProjectOnPlane(outside-Vector3.up*.8f,direction).normalized;
            var elbowGoal=shoulder.position+direction*along+bend*side;
            shoulder.rotation=Quaternion.FromToRotation(elbow.position-shoulder.position,elbowGoal-shoulder.position)*shoulder.rotation;
            elbow.rotation=Quaternion.FromToRotation(hand.position-elbow.position,target-elbow.position)*elbow.rotation;
        }
        void StopWalk(){stroll?.Kill();stroll=null;if(initialized&&anchor!=null){anchor.position=home;anchor.rotation=facing;}}
        void OnDisable(){StopWalk();if(phone!=null)phone.gameObject.SetActive(false);}
    }
}
