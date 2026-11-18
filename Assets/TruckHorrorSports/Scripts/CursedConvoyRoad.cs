using System;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using LWS.TruckSports;
using TMPro;
namespace LWS.TruckHorrorSports
{
    [Serializable] public sealed class CursedConvoyLeg { public Transform[] points; }
    /// <summary>Optional authored convoy roads/encounters; original match rules own every ward and delivery.</summary>
    public sealed class CursedConvoyRoad : MonoBehaviour
    {
        public HorrorMatch match;
        public GameObject geometry;
        public GameObject[] defaultObjects;
        public CursedConvoyLeg[] legs;
        public Transform[] wards,encounters;
        public Transform receivingGate;
        public GameObject[] apparitions;
        public Light[] apparitionLights;
        public Sequencer[] sequences;
        public AudioSource[] cargoSounds;
        public AudioClip thump,unrest;
        public float laneSpacing=12;
        public Light[] bayLights;
        public TMP_Text[] bayLabels;
        public bool Active {get;private set;}
        readonly int[] stage=new int[4],waypoint=new int[4],encounterCount=new int[4],lastSurge=new int[4],surgeSounds=new int[4];
        readonly bool[,] encountered=new bool[4,3];
        readonly float[] unrestUntil=new float[4],distance=new float[4],soundUntil=new float[4];
        readonly float[] visibleUntil=new float[3];
        readonly Vector3[] last=new Vector3[4];
        bool[] originalStates;
        bool reported;float nextCaption;
        readonly string[] captions=new string[4];
        public string CaptionFor(int slot)=>Active&&match.Age<unrestUntil[slot]?captions[slot]:null;
        public bool DefaultGeometryRestored
        {
            get {if(originalStates==null)return false;for(int i=0;i<defaultObjects.Length;i++)if(defaultObjects[i].activeSelf!=originalStates[i])return false;return true;}
        }
        public float DistanceTravelled(int slot)=>distance[slot];
        public int EncountersSeen(int slot)=>encounterCount[slot];
        public float UnrestFor(int slot)=>Active&&match.Age<unrestUntil[slot]?1.5f:1;
        public Vector3 WardPosition(int slot,int task)=>Lane(wards[task],slot);
        public Vector3 ExitPosition(int slot)=>Lane(receivingGate,slot);
        Vector3 Lane(Transform point,int slot)=>point.position+point.right*((slot-(match.setup.count-1)*.5f)*laneSpacing);
        public void ResetRoute(bool active)
        {
            if(originalStates==null){originalStates=new bool[defaultObjects.Length];for(int i=0;i<defaultObjects.Length;i++)originalStates[i]=defaultObjects[i].activeSelf;}
            Active=active;geometry.SetActive(active);reported=false;nextCaption=0;Array.Clear(captions,0,captions.Length);
            for(int i=0;i<defaultObjects.Length;i++)defaultObjects[i].SetActive(active?false:originalStates[i]);
            Array.Clear(encountered,0,encountered.Length);Array.Clear(encounterCount,0,encounterCount.Length);Array.Clear(lastSurge,0,lastSurge.Length);Array.Clear(surgeSounds,0,surgeSounds.Length);Array.Clear(distance,0,distance.Length);Array.Clear(unrestUntil,0,unrestUntil.Length);Array.Clear(visibleUntil,0,visibleUntil.Length);Array.Clear(soundUntil,0,soundUntil.Length);
            for(int i=0;i<4;i++){stage[i]=-99;waypoint[i]=0;if(i<match.setup.count)last[i]=match.cargo[i].Body.position;cargoSounds[i].Stop();}
            for(int i=0;i<apparitions.Length;i++){sequences[i].Stop();apparitions[i].SetActive(false);apparitionLights[i].enabled=false;}
            if(active)
            {
                foreach(var objective in match.objectives)if(objective.beacon!=null)objective.beacon.enabled=false;
                for(int task=0;task<wards.Length;task++)for(int slot=0;slot<4;slot++)
                {
                    int index=task*4+slot;bool joined=slot<match.setup.count;bayLabels[index].gameObject.SetActive(joined);bayLights[index].gameObject.SetActive(joined);
                    if(!joined)continue;
                    Vector3 p=WardPosition(slot,task);bayLabels[index].transform.SetPositionAndRotation(p+Vector3.up*4,wards[task].rotation);bayLabels[index].text=match.setup.players[slot].name+"\nHOLD TO CONTAIN";bayLabels[index].color=match.setup.players[slot].color;
                    bayLights[index].transform.position=p+Vector3.up*3;bayLights[index].color=match.setup.players[slot].color;
                }
            }
        }
        public bool TryDriveAI(int slot,out Vector2 drive,out bool park)
        {
            drive=Vector2.zero;park=false;if(!Active)return false;
            var truck=match.trucks[slot];if(!match.cargo[slot].Ready)return true;
            int objective=match.NextObjective(slot);
            if(objective>=0 && match.rules.ConvoyArrived(slot,objective)){park=true;return true;}
            int leg=objective<0?3:objective;
            if(stage[slot]!=leg){stage[slot]=leg;waypoint[slot]=0;}
            var points=legs[leg].points;
            while(waypoint[slot]+1<points.Length && Vector3.ProjectOnPlane(truck.body.position-Lane(points[waypoint[slot]],slot),Vector3.up).magnitude<8)waypoint[slot]++;
            Vector3 target=Lane(points[waypoint[slot]],slot),goal=objective<0?ExitPosition(slot):WardPosition(slot,objective);
            Vector3 local=truck.transform.InverseTransformPoint(target);float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            float desired=(7+(int)match.setup.players[slot].difficulty*.5f)*Mathf.Lerp(1,.28f,Mathf.Clamp01(Mathf.Abs(angle)/80));
            if(waypoint[slot]==points.Length-1)
            {
                float remaining=Vector3.ProjectOnPlane(goal-truck.body.position,Vector3.up).magnitude;
                desired=Mathf.Min(desired,Mathf.Sqrt(2*1.3f*Mathf.Max(0,remaining-2)));
                if(remaining<2.6f&&truck.body.linearVelocity.magnitude<.7f){park=true;return true;}
            }
            for(int other=0;other<match.setup.count;other++)if(other!=slot)
            {Vector3 relative=truck.transform.InverseTransformPoint(match.trucks[other].body.position);if(relative.z>0&&relative.z<16&&Mathf.Abs(relative.x)<4)desired=Mathf.Min(desired,Mathf.Max(0,relative.z-7)*.5f);}
            float speed=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);
            drive=new Vector2(Mathf.Clamp(angle/36,-1,1),Mathf.Clamp((desired-speed)*.65f,-1,1));return true;
        }
        public void TickRoad(float dt)
        {
            if(!Active)return;
            for(int slot=0;slot<match.setup.count;slot++)
            {
                var cargo=match.cargo[slot];Vector3 position=cargo.Body.position;float travelled=Vector3.ProjectOnPlane(position-last[slot],Vector3.up).magnitude;last[slot]=position;
                if(travelled<50*dt+1)distance[slot]+=travelled;
                cargoSounds[slot].volume=match.settings.sfx*.42f/Mathf.Sqrt(match.setup.count);
                if(match.Age>soundUntil[slot]&&cargoSounds[slot].isPlaying)cargoSounds[slot].Stop();
                if(match.rules.outcome[slot]!=HorrorOutcome.Active)continue;
                for(int encounter=0;encounter<encounters.Length;encounter++)
                {
                    if(encountered[slot,encounter]||Vector3.ProjectOnPlane(position-encounters[encounter].position,Vector3.up).magnitude>30)continue;
                    encountered[slot,encounter]=true;encounterCount[slot]++;unrestUntil[slot]=match.Age+10;
                    visibleUntil[encounter]=Mathf.Max(visibleUntil[encounter],match.Age+6);apparitions[encounter].SetActive(true);apparitionLights[encounter].enabled=true;
                    if(DialogueManager.hasInstance&&DialogueManager.displaySettings!=null&&DialogueManager.displaySettings.cameraSettings!=null)
                        sequences[encounter].PlaySequence("AnimatorPlay(Still watching,speaker)",apparitions[encounter].transform,null,false,false);
                    else
                    {
                        var animator=apparitions[encounter].GetComponentInChildren<Animator>(true);
                        if(animator!=null)animator.Play("Still watching",0,0);
                    }
                    PlayCargo(slot,unrest,3.5f);
                    captions[slot]=match.setup.players[slot].name+" · "+(encounter==0?"THE SAME LOADING CLERK. THE DEPOT IS CLOSED.":encounter==1?"SOMETHING IS MOVING INSIDE YOUR ORIGINAL LOAD.":"THE LAST RECEIVER KNOWS YOUR REGISTRATION.");
                    if(match.Age>=nextCaption){match.Say(captions[slot],5);nextCaption=match.Age+6;}
                    SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"haunted_road_encounter",match.setup.players[slot].participantId,"physical course trigger="+encounter,encounterCount[slot]);
                }
                if(cargo.PhysicalSurges>lastSurge[slot])
                {lastSurge[slot]=cargo.PhysicalSurges;if(surgeSounds[slot]<8){surgeSounds[slot]++;PlayCargo(slot,thump,1);}}
            }
            for(int i=0;i<apparitions.Length;i++)if(match.Age>visibleUntil[i]){apparitions[i].SetActive(false);apparitionLights[i].enabled=false;}
        }
        void PlayCargo(int slot,AudioClip clip,float seconds)
        {var source=cargoSounds[slot];if(!source.isActiveAndEnabled)return;source.Stop();source.clip=clip;source.pitch=.85f;source.Play();soundUntil[slot]=match.Age+seconds;}
        public void Freeze()
        {if(cargoSounds!=null)foreach(var sound in cargoSounds)if(sound!=null)sound.Stop();if(sequences!=null)foreach(var sequence in sequences)if(sequence!=null)sequence.Stop();if(apparitions!=null)foreach(var apparition in apparitions)if(apparition!=null)apparition.SetActive(false);if(apparitionLights!=null)foreach(var light in apparitionLights)if(light!=null)light.enabled=false;}
        public void Silence(){if(cargoSounds!=null)foreach(var sound in cargoSounds)if(sound!=null)sound.Stop();}
        public void ReportFinal()
        {
            if(!Active||reported)return;reported=true;
            for(int slot=0;slot<match.setup.count;slot++)
            {SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"haunted_convoy_road_distance",match.setup.players[slot].participantId,"Actual original-load movement; unit=M",distance[slot]);SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"haunted_convoy_road_encounters",match.setup.players[slot].participantId,"Actual course triggers; original HauntedCargo owns forces",encounterCount[slot]);}
        }
        public string Diagnostic(int slot)=>"convoy stage="+stage[slot]+" point="+waypoint[slot]+" truck="+match.trucks[slot].body.position.ToString("F1")+" originalLoad="+match.cargo[slot].Body.position.ToString("F1")+" attached="+match.cargo[slot].Ready+" distance="+distance[slot].ToString("F1")+" encounters="+encounterCount[slot]+" ward="+match.NextObjective(slot);
        void OnDisable()
        {
            Freeze();if(originalStates!=null)for(int i=0;i<defaultObjects.Length;i++)if(defaultObjects[i]!=null)defaultObjects[i].SetActive(originalStates[i]);Active=false;
        }
    }
}
