using UnityEngine;
using UnityEngine.AI;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckHorrorSports
{
    /// <summary>False-headlight strategy over the existing physical stalker/navigation, never a second AI.</summary>
    public sealed class ScarecrowHeadlightLure : MonoBehaviour
    {
        public HorrorMatch match;
        public HorrorPursuer actor;
        public HorrorAnomaly retiredStaticEvent;
        public Transform[] lureStations;
        public Light[] decoyLights;
        public AudioSource lureSound,revealSound;
        public bool Luring {get;private set;}
        public int LuresStarted {get;private set;}
        public int Reveals {get;private set;}
        public float LureDistance {get;private set;}
        public int LureOwner {get;private set;}=-1;
        public int Station {get;private set;}=-1;
        SportsUniversalHorn horn;
        NavMeshPath path;
        float nextLure,expires,stationHold;
        Vector3 last;
        readonly float[] captionUntil=new float[4];
        readonly string[] captions=new string[4];
        public string CaptionFor(int slot)=>slot>=0&&slot<match.setup.count&&match.Age<captionUntil[slot]?captions[slot]:null;
        public void ResetDeception(HorrorMatch owner)
        {
            Freeze();match=owner;horn=owner.GetComponent<SportsUniversalHorn>();path=new NavMeshPath();LuresStarted=Reveals=0;LureDistance=0;LureOwner=Station=-1;nextLure=25;stationHold=0;last=actor.body.position;
            System.Array.Clear(captionUntil,0,captionUntil.Length);System.Array.Clear(captions,0,captions.Length);
        }
        public float AttentionAdjustment(int slot)
        {
            if(match==null||!actor.scarecrow||slot<0||slot>=match.setup.count)return 0;
            float distance=Vector3.Distance(actor.body.position,match.PlayerPosition(slot));var truck=match.trucks[slot];
            float engine=distance<45?Mathf.Max(0,truck.command.y)*10:0;
            var player=match.setup.players[slot];
            bool honking=distance<90&&horn!=null&&horn.InputReady(slot)&&player.source!=BowlingSource.AI&&SportsUniversalHorn.Held(new SportsUniversalHorn.Driver{source=player.source,pad=player.pad},horn.HornKey(slot));
            // Reuse actual owner-approved horn input, so muting audio never changes stealth.
            return -engine-(honking?35:0);
        }
        public Vector3 DestinationFor(int target,Vector3 ordinaryDestination)=>Luring&&target==LureOwner?lureStations[Station].position:ordinaryDestination;
        public void TickDeception(float dt)
        {
            if(match==null||!match.Live)return;
            if(actor.State==HorrorPursuitState.Repelled){if(Luring)EndLure();return;}
            if(Luring)
            {
                float moved=Vector3.ProjectOnPlane(actor.body.position-last,Vector3.up).magnitude;last=actor.body.position;if(moved<40*dt+1)LureDistance+=moved;
                if(match.IsIlluminated(actor.body.position))
                {
                    Reveals++;int observer=Illuminator();
                    if(observer>=0){captions[observer]=match.setup.players[observer].name+" · YOUR HEADLIGHTS REVEALED THE SCARECROW";captionUntil[observer]=match.Age+6;match.Say(captions[observer],5);}
                    if(revealSound!=null&&revealSound.isActiveAndEnabled){revealSound.volume=match.settings.sfx*.35f;revealSound.Play();}
                    SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"scarecrow_headlight_lure_revealed",observer>=0?match.setup.players[observer].participantId:null,"Actual shared headlight visibility cancelled the physically approached lure",Reveals);EndLure();return;
                }
                if(Vector3.ProjectOnPlane(actor.body.position-lureStations[Station].position,Vector3.up).magnitude<3)stationHold+=dt;
                bool playerClose=LureOwner>=0&&Vector3.Distance(actor.body.position,match.PlayerPosition(LureOwner))<18;
                if(match.Age>=expires||stationHold>=3||playerClose||actor.Target!=LureOwner){EndLure();return;}
                foreach(var light in decoyLights)if(light!=null){light.enabled=true;light.intensity=5+Mathf.Sin(match.Age*1.2f)*.25f;}
                if(lureSound!=null){lureSound.volume=match.settings.sfx*.13f;if(lureSound.isActiveAndEnabled&&!lureSound.isPlaying)lureSound.Play();}
                return;
            }
            if(match.Age<nextLure||match.Escalation<1||actor.Target<0||match.IsIlluminated(actor.body.position))return;
            nextLure=match.Age+2;int target=actor.Target;
            if(Vector3.Distance(actor.body.position,match.PlayerPosition(target))<35)return;
            int selected=-1;float best=float.MaxValue;
            for(int i=0;i<lureStations.Length;i++)
            {
                if(i==Station)continue;Vector3 location=lureStations[i].position;float fromPlayer=Vector3.ProjectOnPlane(location-match.PlayerPosition(target),Vector3.up).magnitude;if(fromPlayer<24||fromPlayer>70)continue;
                bool fair=true;for(int p=0;p<match.setup.count;p++)if(match.rules.outcome[p]==HorrorOutcome.Active&&Vector3.Distance(location,match.PlayerPosition(p))<24)fair=false;
                if(!fair||!NavMesh.SamplePosition(actor.body.position,out var start,5,NavMesh.AllAreas)||!NavMesh.SamplePosition(location,out var end,3,NavMesh.AllAreas)||!NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                float length=0;var corners=path.corners;for(int c=1;c<corners.Length;c++)length+=Vector3.Distance(corners[c-1],corners[c]);
                if(length<8||length>90)continue;float score=length+fromPlayer*.2f;if(score<best){best=score;selected=i;}
            }
            if(selected<0)return;
            Station=selected;LureOwner=target;Luring=true;LuresStarted++;expires=match.Age+12;stationHold=0;last=actor.body.position;
            captions[target]=match.setup.players[target].name+" · HEADLIGHTS IN THE CORN · LOOK FOR THE BODY, NOT THE BEAMS";captionUntil[target]=match.Age+6;
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"scarecrow_physical_headlight_lure",match.setup.players[target].participantId,"Existing stalker navigating to authored station="+selected,LuresStarted);
        }
        int Illuminator()
        {
            int result=-1;float nearest=38;
            for(int i=0;i<match.setup.count;i++)
            {
                if(match.headlights[i]==null||!match.headlights[i].enabled||match.rules.outcome[i]!=HorrorOutcome.Active)continue;
                Vector3 delta=actor.body.position-match.trucks[i].body.position;float distance=delta.magnitude;
                if(distance<nearest&&Vector3.Dot(match.trucks[i].transform.forward,delta.normalized)>.65f&&!Physics.Linecast(match.trucks[i].body.position+Vector3.up,actor.body.position+Vector3.up,1)) {nearest=distance;result=i;}
            }
            return result;
        }
        void EndLure()
        {Luring=false;nextLure=match.Age+35;stationHold=0;foreach(var light in decoyLights)if(light!=null)light.enabled=false;if(lureSound!=null)lureSound.Stop();}
        public void Silence(){if(lureSound!=null)lureSound.Stop();if(revealSound!=null)revealSound.Stop();}
        void OnApplicationFocus(bool focused){if(!focused)Silence();}
        public void Freeze()
        {Luring=false;if(decoyLights!=null)foreach(var light in decoyLights)if(light!=null)light.enabled=false;if(lureSound!=null)lureSound.Stop();if(revealSound!=null)revealSound.Stop();}
        void OnDisable()=>Freeze();
    }
}
