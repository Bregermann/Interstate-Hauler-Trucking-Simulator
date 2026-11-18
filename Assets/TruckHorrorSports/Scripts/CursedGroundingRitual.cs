using System;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckHorrorSports
{
    /// <summary>Exorcism-only physical ground leads. HorrorMatch retains all objective/outcome authority.</summary>
    public sealed class CursedGroundingRitual : MonoBehaviour
    {
        public HorrorMatch match;
        public GameObject geometry;
        public Transform[] posts;
        public LineRenderer[] leads;
        public AudioSource[] sockets;
        public AudioClip connectSound,releaseSound;
        public float maximumReach=12,settleSeconds=1;
        readonly SpringJoint[] cables=new SpringJoint[4];
        readonly int[] connectedWard={-1,-1,-1,-1},connections=new int[4],completed=new int[4];
        readonly float[] settled=new float[4],cooldown=new float[4],peakTension=new float[4];
        bool reported;
        public bool Active {get;private set;}
        public bool Attached(int slot)=>slot>=0&&slot<cables.Length&&cables[slot]!=null;
        public int Connections(int slot)=>connections[slot];
        public int GroundedWards(int slot)=>completed[slot];
        public float PeakTension(int slot)=>peakTension[slot];
        public float SettledSeconds(int slot)=>settled[slot];
        public bool AllowsChannel(int slot,int ward)=>!Active||Attached(slot)&&connectedWard[slot]==ward&&settled[slot]>=settleSeconds;
        public string CaptionFor(int slot)
        {
            if(!Active||slot<0||slot>=match.setup.count||match.Escaped(slot))return null;
            return Attached(slot)?settled[slot]>=settleSeconds?"GROUND LEAD CONNECTED · HOLD ACTION TO EXORCISE":"GROUND LEAD CONNECTED · STOP BOTH VEHICLES AND LET THE LOAD SETTLE":"PARK BESIDE THE WARD · HOLD ACTION TO CONNECT THE GROUND LEAD";
        }
        public void ResetRitual()
        {
            Freeze();Active=match.sport==HorrorSport.CursedCargo&&match.option==2;reported=false;geometry.SetActive(Active);
            Array.Clear(connections,0,connections.Length);Array.Clear(completed,0,completed.Length);Array.Clear(peakTension,0,peakTension.Length);Array.Clear(cooldown,0,cooldown.Length);
        }
        public void ObserveParticipant(int slot,bool held,float dt)
        {
            if(!Active||!match.Live||slot<0||slot>=match.setup.count)return;
            int ward=match.NextObjective(slot);var cargo=match.cargo[slot];
            if(ward<0||ward>=posts.Length||match.rules.outcome[slot]!=HorrorOutcome.Active||!cargo.Ready||cargo.Condition<=0)
            {Release(slot,false);return;}
            if(Attached(slot)&&connectedWard[slot]!=ward)Release(slot,false);
            if(connectedWard[slot]>=0&&!Attached(slot))
            {
                // A genuinely broken joint is a failed attempt, not a successful ward.
                Release(slot,true);cooldown[slot]=match.Age+2;
            }
            if(!held){Release(slot,false);return;}
            var body=cargo.Body;Vector3 eye=cargo.trailer.Kingpin.position;Vector3 post=posts[ward].position;
            bool stopped=body.linearVelocity.magnitude<1.4f&&match.trucks[slot].body.linearVelocity.magnitude<1.4f&&body.angularVelocity.magnitude<.55f;
            if(!Attached(slot))
            {
                if(match.Age<cooldown[slot]||!stopped||Vector3.Distance(eye,post)>maximumReach||Vector3.Distance(match.trucks[slot].body.position,match.objectives[ward].transform.position)>=match.interactionRadius)return;
                // Native NWH kingpin/fifth-wheel stays coupled. The additional lead
                // is a compliant world tether; it never repositions either assembly.
                var cable=body.gameObject.AddComponent<SpringJoint>();cable.autoConfigureConnectedAnchor=false;
                cable.anchor=body.transform.InverseTransformPoint(eye);cable.connectedAnchor=post;
                cable.minDistance=0;cable.maxDistance=Mathf.Max(.25f,Vector3.Distance(eye,post)-.15f);
                cable.spring=body.mass*2;cable.damper=body.mass*.65f;cable.tolerance=.03f;
                cable.breakForce=body.mass*20;cable.breakTorque=float.PositiveInfinity;cable.enableCollision=false;
                cables[slot]=cable;connectedWard[slot]=ward;connections[slot]++;settled[slot]=0;
                Play(ward,connectSound,.45f);
                match.Say(match.setup.players[slot].name+" · GROUND LEAD CONNECTED · HOLD STEADY",4);
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"cursed_ground_lead_connected",match.setup.players[slot].participantId,"Physical kingpin-to-ward SpringJoint; ward="+ward,connections[slot]);
            }
            float tension=cables[slot].currentForce.magnitude;
            if(float.IsFinite(tension))peakTension[slot]=Mathf.Max(peakTension[slot],tension);
            bool inside=Vector3.Distance(cargo.trailer.Kingpin.position,post)<=maximumReach+1;
            settled[slot]=stopped&&inside?settled[slot]+dt:0;
        }
        public void ObserveCompletedWard(int slot,int ward)
        {
            if(!Active||!AllowsChannel(slot,ward)||!match.rules.Done(slot,ward))return;
            completed[slot]++;
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"cursed_grounded_ward_complete",match.setup.players[slot].participantId,"Original task receipt after physical grounding; ward="+ward,completed[slot]);
            Release(slot,false);
        }
        void Release(int slot,bool broken)
        {
            bool wasConnected=connectedWard[slot]>=0;int ward=connectedWard[slot];
            if(cables[slot]!=null){cables[slot].spring=cables[slot].damper=0;Destroy(cables[slot]);}cables[slot]=null;connectedWard[slot]=-1;settled[slot]=0;
            if(slot<leads.Length&&leads[slot]!=null)leads[slot].enabled=false;
            if(wasConnected&&Active&&match!=null&&match.Live)
            {Play(ward,releaseSound,.28f);if(broken)match.Say(match.setup.players[slot].name+" · GROUND LEAD BROKE · STOP AND RECONNECT",4);}
        }
        void LateUpdate()
        {
            for(int slot=0;slot<leads.Length;slot++)
            {
                var line=leads[slot];if(line==null)continue;bool visible=Active&&Attached(slot);line.enabled=visible;if(!visible)continue;
                Vector3 a=posts[connectedWard[slot]].position,b=match.cargo[slot].trailer.Kingpin.position;
                for(int point=0;point<line.positionCount;point++){float t=point/(float)(line.positionCount-1);line.SetPosition(point,Vector3.Lerp(a,b,t)+Vector3.down*(Mathf.Sin(t*Mathf.PI)*.25f));}
            }
        }
        void Play(int ward,AudioClip clip,float gain)
        {if(ward<0||ward>=sockets.Length)return;var source=sockets[ward];if(source!=null&&source.isActiveAndEnabled&&clip!=null)source.PlayOneShot(clip,match.settings.sfx*gain);}
        public string Diagnostic(int slot)=>"ward="+connectedWard[slot]+" attached="+Attached(slot)+" stable="+settled[slot].ToString("F2")+" connections="+connections[slot]+" receipts="+completed[slot]+" peakN="+peakTension[slot].ToString("F1")+" next="+match.NextObjective(slot)+" load="+match.cargo[slot].Body.position;
        public void ReportFinal()
        {if(reported||!Active)return;reported=true;for(int slot=0;slot<match.setup.count;slot++)SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"cursed_grounding_result",match.setup.players[slot].participantId,"connections="+connections[slot]+";actual_grounded_wards="+completed[slot],peakTension[slot]);}
        public void Silence(){foreach(var source in sockets)if(source!=null)source.Stop();}
        public void Freeze(){Active=false;for(int slot=0;slot<cables.Length;slot++)Release(slot,false);Silence();}
        void OnDisable()=>Freeze();
    }
}
