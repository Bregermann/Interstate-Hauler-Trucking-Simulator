using System;
using UnityEngine;
namespace LWS.TruckHorrorSports
{
    public enum DeadEndExitKind { None, DawnServiceGate, UnlitEscape }
    [Serializable] public sealed class DeadEndPuzzlePath { public Transform[] points; }
    [Serializable] public sealed class DeadEndRoadLine { public Vector3[] points; }
    /// <summary>Optional physical junction puzzle; HorrorMatch remains the only objective/result owner.</summary>
    public sealed class DeadEndJunctionPuzzle : MonoBehaviour
    {
        public HorrorMatch match;
        public GameObject geometry;
        public Transform entry;
        public Transform[] gates,cluePositions,clueViewpoints,falseTurns;
        public Renderer[] litClues,darkClues;
        public DeadEndPuzzlePath[] stagePaths;
        public DeadEndPuzzlePath dawnPath,darkPath;
        public Transform dawnExit,darkExit,shiftingBarrier;
        public DeadEndRoadLine[] roads;
        [Tooltip("Explicit authoring binds only owned forest instances obstructing the new exit roads. Original active states return outside Route Puzzle.")]
        public GameObject[] exitRoadForest=Array.Empty<GameObject>();
        GameObject[] cachedForest;
        bool[] forestStates;
        public bool Active {get;private set;}
        readonly bool[] entered=new bool[12];
        readonly bool[,] seen=new bool[12,4],wrongInside=new bool[12,2];
        readonly int[] pendingWrong=new int[12],wrongTurns=new int[12],pathStage=new int[12],waypoint=new int[12],chosenExit=new int[12];
        readonly DeadEndExitKind[] exits=new DeadEndExitKind[12];
        Quaternion closed;bool captured;
        readonly RaycastHit[] exitProbe=new RaycastHit[16];
        public int WrongTurns(int slot)=>wrongTurns[slot];
        public bool Entered(int slot)=>entered[slot];
        public bool SawClue(int slot,int gate)=>slot>=0&&slot<12&&gate>=0&&gate<4&&seen[slot,gate];
        public DeadEndExitKind ExitKind(int slot)=>exits[slot];
        public void ResetPuzzle(bool active)
        {
            Active=active;geometry.SetActive(active);SetExitForest(active);
            if(!captured){closed=shiftingBarrier.localRotation;captured=true;}
            shiftingBarrier.localRotation=closed;
            Array.Clear(entered,0,entered.Length);Array.Clear(seen,0,seen.Length);Array.Clear(wrongInside,0,wrongInside.Length);Array.Clear(wrongTurns,0,wrongTurns.Length);Array.Clear(pendingWrong,0,pendingWrong.Length);Array.Clear(waypoint,0,waypoint.Length);Array.Clear(exits,0,exits.Length);
            for(int i=0;i<pathStage.Length;i++){pathStage[i]=-99;chosenExit[i]=-1;}
        }
        void RestoreExitForest()
        {
            if(cachedForest==null)return;
            for(int i=0;i<cachedForest.Length;i++)if(cachedForest[i]!=null)cachedForest[i].SetActive(forestStates[i]);
        }
        void SetExitForest(bool active)
        {
            RestoreExitForest();
            bool changed=cachedForest==null||cachedForest.Length!=exitRoadForest.Length;
            if(!changed)for(int i=0;i<cachedForest.Length;i++)changed|=cachedForest[i]!=exitRoadForest[i];
            if(changed)
            {
                cachedForest=(GameObject[])exitRoadForest.Clone();forestStates=new bool[cachedForest.Length];
                for(int i=0;i<cachedForest.Length;i++)if(cachedForest[i]!=null)forestStates[i]=cachedForest[i].activeSelf;
            }
            if(active)foreach(var tree in cachedForest)if(tree!=null)tree.SetActive(false);
        }
        void OnDisable()=>RestoreExitForest();
        public Vector3 GatePosition(int objective)=>gates[Mathf.Clamp(objective,0,gates.Length-1)].position;
        public Vector3 ExitPosition(int slot)=>Vector3.Distance(match.PlayerPosition(slot),dawnExit.position)<Vector3.Distance(match.PlayerPosition(slot),darkExit.position)?dawnExit.position:darkExit.position;
        public bool CanAcceptGate(int slot,int objective)=>Active&&objective>=0&&objective<4&&entered[slot]&&seen[slot,objective];
        public void ObserveParticipant(int slot,bool headlights)
        {
            if(!Active||slot<0||slot>=match.setup.count)return;
            var truck=match.trucks[slot];Vector3 position=truck.body.position;
            if(Vector3.Distance(position,entry.position)<18)entered[slot]=true;
            for(int gate=0;gate<4;gate++)
            {
                Vector3 delta=cluePositions[gate].position-position;
                bool visible=gate%2==0?litClues[gate].enabled:darkClues[gate].enabled;
                if(visible&&headlights==(gate%2==0)&&delta.magnitude<31&&delta.magnitude>.2f&&Vector3.Dot(truck.transform.forward,delta.normalized)>.45f&&!Physics.Linecast(position+Vector3.up,cluePositions[gate].position,1,QueryTriggerInteraction.Ignore))seen[slot,gate]=true;
            }
            for(int fork=0;fork<falseTurns.Length;fork++)
            {
                bool inside=Vector3.Distance(position,falseTurns[fork].position)<8;
                if(inside&&!wrongInside[slot,fork]){wrongTurns[slot]++;pendingWrong[slot]++;}
                wrongInside[slot,fork]=inside;
            }
        }
        public int ConsumeWrongTurns(int slot){int count=pendingWrong[slot];pendingWrong[slot]=0;return count;}
        public void Tick(float dt)
        {
            if(!Active)return;
            for(int gate=0;gate<4;gate++)
            {
                bool illuminated=match.IsIlluminated(cluePositions[gate].position);
                if(litClues[gate]!=null)litClues[gate].enabled=illuminated;
                if(darkClues[gate]!=null)darkClues[gate].enabled=!illuminated;
            }
            bool mayShift=false,occupied=false;
            for(int slot=0;slot<match.setup.count;slot++)
            {mayShift|=match.rules.Done(slot,1);occupied|=Vector3.Distance(match.PlayerPosition(slot),shiftingBarrier.position+shiftingBarrier.right*6)<12;}
            if(mayShift&&!occupied)shiftingBarrier.localRotation=Quaternion.RotateTowards(shiftingBarrier.localRotation,closed*Quaternion.Euler(0,-90,0),dt*45);
        }
        public bool TryDriveAI(int slot,int objective,out Vector2 drive,out bool headlights)
        {
            drive=Vector2.zero;headlights=objective<0?false:objective%2==0;
            if(!Active||!entered[slot])return false;
            var truck=match.trucks[slot];
            if(objective==2&&Quaternion.Angle(shiftingBarrier.localRotation,closed)<80)
            {float speed=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);drive=new Vector2(0,speed>.2f?-.8f:speed<-.2f?.8f:0);return true;}
            int stage=objective<0?4:objective;
            if(pathStage[slot]!=stage){pathStage[slot]=stage;waypoint[slot]=0;}
            if(objective<0&&chosenExit[slot]<0)
            {
                // Pick an actual reachable exit away from the physical pursuer, then commit
                // to that route. This is an AI decision; it never determines a result itself.
                bool dawn=!seen[slot,3]||match.antagonist!=null&&Vector3.Distance(match.antagonist.body.position,dawnExit.position)>Vector3.Distance(match.antagonist.body.position,darkExit.position);
                chosenExit[slot]=dawn?0:1;
            }
            var path=objective<0?(chosenExit[slot]==0?dawnPath:darkPath):stagePaths[objective];
            if(objective<0)headlights=path==dawnPath;
            while(waypoint[slot]+1<path.points.Length&&Vector3.Distance(truck.body.position,path.points[waypoint[slot]].position)<5)waypoint[slot]++;
            Vector3 target=path.points[waypoint[slot]].position;
            if(objective>=0&&waypoint[slot]==path.points.Length-1&&!seen[slot,objective])target=clueViewpoints[objective].position;
            Vector3 local=truck.transform.InverseTransformPoint(target);float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            float desired=(8+(int)match.setup.players[slot].difficulty*.5f)*Mathf.Lerp(1,.25f,Mathf.Clamp01(Mathf.Abs(angle)/85));
            // A read clue does not complete the physical gate. Brake into the final
            // waypoint instead of endlessly orbiting its inside turning circle at cruise.
            if(waypoint[slot]==path.points.Length-1)
            {
                float remaining=Vector3.ProjectOnPlane(target-truck.body.position,Vector3.up).magnitude;
                desired=Mathf.Min(desired,Mathf.Max(.25f,(remaining-.75f)*.15f));
            }
            float signed=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);
            drive=new Vector2(Mathf.Clamp(angle/32,-1,1),Mathf.Clamp((desired-signed)*.55f,-1,1));return true;
        }
        public bool CanExtract(int slot,bool headlights)
        {
            if(!Active||match.NextObjective(slot)>=0)return false;
            Vector3 position=match.PlayerPosition(slot);
            if(headlights&&Vector3.Distance(position,dawnExit.position)<7){exits[slot]=DeadEndExitKind.DawnServiceGate;return true;}
            if(!headlights&&seen[slot,3]&&Vector3.Distance(position,darkExit.position)<7){exits[slot]=DeadEndExitKind.UnlitEscape;return true;}
            return false;
        }
        public string Diagnostic(int slot)
        {
            int objective=match.NextObjective(slot);int gate=Mathf.Clamp(objective,0,3);var truck=match.trucks[slot];Vector3 delta=cluePositions[gate].position-truck.body.position;
            bool blocked=Physics.Linecast(truck.body.position+Vector3.up,cluePositions[gate].position,out var hit,1,QueryTriggerInteraction.Ignore);
            string egress="";
            if(objective<0 && chosenExit[slot]>=0)
            {
                var path=chosenExit[slot]==0?dawnPath:darkPath;Vector3 target=path.points[Mathf.Clamp(waypoint[slot],0,path.points.Length-1)].position;
                Vector3 direction=Vector3.ProjectOnPlane(target-truck.body.position,Vector3.up);var shape=truck.shape;string obstruction="clear";float nearest=float.MaxValue;
                if(direction.sqrMagnitude>.001f)
                {
                    int count=Physics.BoxCastNonAlloc(shape.transform.TransformPoint(shape.center),Vector3.Scale(shape.size,shape.transform.lossyScale)*.49f,direction.normalized,exitProbe,shape.transform.rotation,Mathf.Min(15,direction.magnitude),1,QueryTriggerInteraction.Ignore);
                    for(int i=0;i<count;i++)if(exitProbe[i].distance<nearest && !exitProbe[i].collider.transform.IsChildOf(truck.transform))
                    {nearest=exitProbe[i].distance;obstruction=exitProbe[i].collider.name+" root="+exitProbe[i].collider.transform.root.name+" at="+exitProbe[i].point.ToString("F2")+" normal="+exitProbe[i].normal.ToString("F2")+" gap="+nearest.ToString("F2");}
                }
                egress=" exit="+(chosenExit[slot]==0?"dawn":"unlit")+" target="+target.ToString("F2")+" actualGate="+(chosenExit[slot]==0?dawnExit.position:darkExit.position).ToString("F2")+" remaining="+direction.magnitude.ToString("F2")+" command="+truck.command.ToString("F2")+" velocity="+truck.body.linearVelocity.ToString("F2")+" chassisSweep="+obstruction;
            }
            return "junction objective="+objective+" stage="+pathStage[slot]+" waypoint="+waypoint[slot]+" seen="+seen[slot,0]+","+seen[slot,1]+","+seen[slot,2]+","+seen[slot,3]+" truck="+truck.body.position.ToString("F2")+" forward="+truck.transform.forward.ToString("F2")+" clue="+cluePositions[gate].position.ToString("F2")+" view="+clueViewpoints[gate].position.ToString("F2")+" distance="+delta.magnitude.ToString("F2")+" facing="+Vector3.Dot(truck.transform.forward,delta.normalized).ToString("F2")+" litRenderer="+litClues[gate].enabled+" darkRenderer="+darkClues[gate].enabled+" illuminated="+match.IsIlluminated(cluePositions[gate].position)+" lineHit="+(blocked?hit.collider.name+" root="+hit.collider.transform.root.name+" at="+hit.point.ToString("F2"):"clear")+egress;
        }
        public string EndingCaption(int slot)=>exits[slot]==DeadEndExitKind.DawnServiceGate?"DAWN THROUGH THE SERVICE GATE":exits[slot]==DeadEndExitKind.UnlitEscape?"THE UNLIT EXIT · THE ROAD DID NOT SEE YOU LEAVE":"THE ROAD STILL HAS YOU";
        public float DistanceFromRoad(Vector3 position)
        {
            float best=float.PositiveInfinity;
            if(!Active)return best;
            foreach(var road in roads)for(int i=1;i<road.points.Length;i++)
            {Vector3 a=road.points[i-1],b=road.points[i];a.y=b.y=position.y;Vector3 segment=b-a;float t=segment.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(position-a,segment)/segment.sqrMagnitude):0;best=Mathf.Min(best,Vector3.Distance(position,a+segment*t));}
            return best;
        }
    }
}
