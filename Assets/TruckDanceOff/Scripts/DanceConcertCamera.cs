using LWS.TruckBowling;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDanceOff
{
    // Shot selection over the existing Cinemachine adapter. DSP/input/rules and
    // the complete authored truck choreography remain independent of the view.
    [DefaultExecutionOrder(900)]
    public sealed class DanceConcertCamera : MonoBehaviour
    {
        public DanceMatch match;
        public SportsEventCamera cameraRig;
        [Range(6,16)]public float minimumShotSeconds=7;
        [Range(.5f,3)]public float transitionSeconds=1.2f;
        public int ShotChanges{get;private set;}
        public int CurrentShot{get;private set;}=-1;
        public int FocusedParticipant{get;private set;}=-1;
        Vector3 currentFocus,currentOffset;float currentFov=52;int round=-1,focusSlot=-1;bool initialized,priorResult;
        void LateUpdate()
        {
            if(match==null||cameraRig==null)return;
            if(!match.Running){initialized=false;round=-1;CurrentShot=focusSlot=FocusedParticipant=-1;return;}
            if(match.Paused||match.Song==null)return;
            if(cameraRig.broadcastFraming!=null&&cameraRig.broadcastFraming.OwnsShot)return;
            Vector3 center=Vector3.zero;int humans=0,human=-1,leader=0;
            for(int i=0;i<match.setup.count;i++){center+=match.dancers[i].truck.body.position;if(match.setup.players[i].source!=BowlingSource.AI){humans++;human=i;}int score=match.total[i]+(match.Phase==DancePhase.Song?match.Rules[i].Score:0),best=match.total[leader]+(match.Phase==DancePhase.Song?match.Rules[leader].Score:0);if(score>best)leader=i;}
            center/=Mathf.Max(1,match.setup.count);center.y=2.2f;
            float period=Mathf.Max(minimumShotSeconds,(float)match.Song.Beat*16);double clock=System.Math.Max(0,match.SongTime);int shot=(int)(clock/period)%4;
            bool result=match.Phase==DancePhase.RoundResult||match.Finished;
            // Hold the chosen performer for the musical shot. Near-equal AI
            // scores must not bounce a close-up between opposite stage edges.
            if(CurrentShot!=shot||round!=match.Round||priorResult!=result||focusSlot<0)focusSlot=result?leader:human>=0?human:leader;
            priorResult=result;FocusedParticipant=-1;
            Vector3 focus=center,offset;float fov=52;
            float sweep=Mathf.Sin((float)(clock/period)*Mathf.PI);
            switch(shot)
            {
                case 1:offset=new Vector3(sweep*2.4f,7.5f,-24);break;
                case 2:offset=new Vector3(-2,5.5f,-25);break;
                case 3:offset=new Vector3(0,11,-23);break;
                default:offset=new Vector3(sweep*1.8f,8,-23);break;
            }
            if(result||shot==3&&humans<=1)
            {
                int slot=focusSlot;FocusedParticipant=slot;focus=match.dancers[slot].truck.body.position+Vector3.up*.3f;offset=new Vector3(Mathf.Sin((float)clock*.2f)*2,7,-17);fov=50;
                string routine=match.dancers[slot].Routine;if(routine=="HEADSPIN"||routine=="ROOF SPIN"||routine=="SIX-TON WINDMILL"){offset.y=8;fov=49;}
            }
            if(CurrentShot!=shot){CurrentShot=shot;ShotChanges++;}
            bool snap=!initialized||round!=match.Round;float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*3/Mathf.Max(.1f,transitionSeconds));
            if(snap){currentFocus=focus;currentOffset=offset;currentFov=fov;initialized=true;round=match.Round;}
            else{currentFocus=Vector3.Lerp(currentFocus,focus,blend);currentOffset=Vector3.Lerp(currentOffset,offset,blend);currentFov=Mathf.Lerp(currentFov,fov,blend);}
            cameraRig.Pose(currentFocus,currentOffset,currentFov,snap);
        }
        void OnDisable(){initialized=false;CurrentShot=focusSlot=FocusedParticipant=-1;}
    }
}
