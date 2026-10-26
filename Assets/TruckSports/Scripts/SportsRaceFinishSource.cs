using System;
using System.Collections.Generic;
using UnityEngine;
using LWS.TruckMattressRacing;
using LWS.TruckWifeCarrying;
using LWS.TruckBogSnorkelling;
using LWS.TruckTerrierRacing;
using LWS.TruckAvalancheEscape;
using LWS.TruckRedLightGreenLight;
using LWS.TruckMinefield;
using LWS.TruckFrogger;

namespace LWS.TruckSports
{
    public struct SportsRaceFinishReceipt
    {
        public string matchId,scope,id,name;public int slot,place,gate;public float seconds;public bool winner,gag;
        public Color color;public Vector3 position;public Quaternion rotation;public SportsEventTruck truck;
        public string Key=>matchId+"|"+scope+"|"+id;
    }
    [Serializable] public sealed class SportsRaceFinishGate
    {
        public Transform root,announcement;public TMPro.TMP_Text caption;public MeshFilter checks;public float width=48;
        [NonSerialized]public Mesh ownedChecks;
    }
    // Presentation reads existing locked native outcomes. It never confirms a finish,
    // awards a point, changes a winner or republishes a score-journal receipt.
    public sealed class SportsRaceFinishSource:MonoBehaviour
    {
        public SportsEventSession session;public SportsRaceFinishGate[] gates;
        readonly Dictionary<int,SportsRaceFinishReceipt> frogSuccess=new Dictionary<int,SportsRaceFinishReceipt>();int frogHistory;string cachedMatch;
        readonly List<SportsRaceFinishReceipt> verification=new List<SportsRaceFinishReceipt>(12);
        public static bool Supports(Type type)=>type==typeof(MattressMatch)||type==typeof(WifeMatch)||type==typeof(BogMatch)||type==typeof(TerrierMatch)||type==typeof(AvalancheMatch)||type==typeof(RedLightMatch)||type==typeof(MinefieldMatch)||type==typeof(FrogMatch);
        public string Scope=>session is MattressMatch m?"race:"+m.series.round:session is WifeMatch w?"race:"+w.series.round:session is BogMatch b?"race:"+b.series.round:session is TerrierMatch t?"heat:"+t.Heat:session is MinefieldMatch?"mile":session is FrogMatch?"championship":"race";
        public int GateIndex=>session is FrogMatch f?f.world.Current:0;
        public bool Ready=>session!=null&&session.Running&&!string.IsNullOrEmpty(session.MatchId)&&Supports(session.GetType());
        public bool TieWithoutWinner=>session is TerrierMatch t&&t.HeatWinner<0&&t.Heat>0&&AnyTerrierFinish(t);
        public bool FinishBatchSettled(List<SportsRaceFinishReceipt> rows)
        {
            bool active=session is MattressMatch m?m.Phase==MattressPhase.Race:session is WifeMatch w?w.Phase==WifePhase.Race:session is BogMatch b?b.Phase==BogPhase.Racing:session is AvalancheMatch a?a.rules.Phase==AvalanchePhase.Racing:session is RedLightMatch r?r.rules.Phase!=RedLightPhase.Complete:false;
            if(!active||Time.timeScale<=0)return true;float first=float.PositiveInfinity;foreach(var row in rows)if(row.winner)first=Mathf.Min(first,row.seconds);
            return session.RaceStartSeconds-first>=Time.fixedDeltaTime+.0002f;
        }
        public bool IsConfirmedLoser(SportsRaceFinishReceipt receipt)
        {
            Read(verification);foreach(var row in verification)if(row.Key==receipt.Key&&row.seconds==receipt.seconds&&row.truck==receipt.truck&&row.slot==receipt.slot&&!row.winner&&row.gag)return true;return false;
        }
        static bool AnyTerrierFinish(TerrierMatch t){for(int i=0;i<t.setup.count;i++)if(float.IsFinite(t.ConfirmedHeatFinishSeconds(i)))return true;return false;}
        SportsRaceFinishReceipt Receipt(int slot,float seconds,int place,bool winner,bool gag,SportsEventTruck truck,int gate=0)
        {
            var player=session.setup.players[slot];return new SportsRaceFinishReceipt{matchId=session.MatchId,scope=Scope,id=((ISportsParticipantSlot)player).ParticipantId,name=player.name,slot=slot,seconds=seconds,place=place,winner=winner,gag=gag,truck=truck,color=player.color,position=truck.body.position,rotation=truck.body.rotation,gate=gate};
        }
        public int Read(List<SportsRaceFinishReceipt> output)
        {
            output.Clear();if(!Ready)return 0;
            if(cachedMatch!=session.MatchId){cachedMatch=session.MatchId;frogSuccess.Clear();frogHistory=0;}
            if(session is MattressMatch mattress)
            {
                for(int i=0;i<mattress.setup.count;i++){var r=mattress.racers[i];if(r.Phase==MattressAttempt.Finished&&float.IsFinite(r.FinishTime)){var row=Receipt(i,r.FinishTime,mattress.RunningPlace(i),false,true,r.truck);row.position=r.FinishOrigin;output.Add(row);}}
                FirstTimes(output,.0001f);
            }
            else if(session is WifeMatch wife)
            {
                for(int pair=0;pair<4;pair++){var r=wife.pairs[pair];if(r.State!=WifeState.Finished||!float.IsFinite(r.FinishTime))continue;var row=Receipt(wife.DriverRole(pair),r.FinishTime,wife.RunningPlace(pair),false,true,r.driver);row.id=wife.PairScoreId(pair);row.name=wife.teams[pair];row.color=wife.ScoreCardColor(pair);output.Add(row);}
                FirstTimes(output,.0001f);
            }
            else if(session is BogMatch bog)
            {
                for(int i=0;i<bog.setup.count;i++){var r=bog.racers[i];if(r.State==BogState.Finished&&float.IsFinite(r.FinishTime))output.Add(Receipt(i,r.FinishTime,0,false,true,r.truck));}
                FirstTimes(output,.0001f);
            }
            else if(session is TerrierMatch terrier)
            {
                int winner=terrier.HeatWinner;if(winner>=0){float seconds=terrier.ConfirmedHeatFinishSeconds(winner);if(float.IsFinite(seconds))output.Add(Receipt(winner,seconds,1,true,false,terrier.racers[winner].truck));}
                // Native tunnel authority accepts no later losing finishes. A tied heat reruns.
            }
            else if(session is AvalancheMatch avalanche)
            {
                for(int i=0;i<avalanche.setup.count;i++)if(avalanche.rules.finished[i]&&float.IsFinite(avalanche.rules.finishTime[i]))output.Add(Receipt(i,avalanche.rules.finishTime[i],avalanche.rules.Place(i),false,true,avalanche.trucks[i]));
                FirstTimes(output,.001f);
            }
            else if(session is RedLightMatch red)
            {
                for(int i=0;i<red.setup.count;i++)if(red.rules.outcomes[i]==RedLightOutcome.Finished&&float.IsFinite(red.rules.finishTimes[i]))output.Add(Receipt(i,red.rules.finishTimes[i],red.ResultPlace(i),red.rules.Winner==i,true,red.trucks[i]));
            }
            else if(session is MinefieldMatch mine)
            {
                int winner=mine.Winner;if(winner>=0&&mine.racers[winner].Phase==MineAttempt.DrivenFinish&&float.IsFinite(mine.racers[winner].FinishTime))output.Add(Receipt(winner,mine.racers[winner].FinishTime,1,true,false,mine.racers[winner].truck));
                // Farthest-rest heat wins and same-step mile ties are not unique mile finishes.
            }
            else if(session is FrogMatch frog)
            {
                var history=frog.rules.History;
                while(frogHistory<history.Count){var attempt=history[frogHistory++];if(attempt.success&&float.IsFinite(attempt.seconds)){var row=Receipt(attempt.participant,attempt.seconds,0,false,true,frog.trucks[attempt.participant],attempt.challenge);frogSuccess[attempt.participant]=row;}}
                // Sequential attempts cannot crown the first attempted clear. Wait for the
                // native championship comparison, then use successful recorded finishes only.
                if(frog.rules.Complete&&frog.rules.Winner>=0&&frogSuccess.ContainsKey(frog.rules.Winner))
                    foreach(var saved in frogSuccess.Values){var row=saved;row.winner=row.slot==frog.rules.Winner;row.place=frog.rules.Place(row.slot);output.Add(row);}
            }
            output.Sort((a,b)=>a.seconds.CompareTo(b.seconds));return output.Count;
        }
        static void FirstTimes(List<SportsRaceFinishReceipt> rows,float epsilon)
        {
            float first=float.PositiveInfinity;foreach(var row in rows)first=Mathf.Min(first,row.seconds);
            for(int i=0;i<rows.Count;i++){var row=rows[i];row.winner=Mathf.Abs(row.seconds-first)<=epsilon;if(row.place==0){row.place=1;foreach(var other in rows)if(other.seconds<row.seconds-epsilon)row.place++;}rows[i]=row;}
        }
        public void GatePose(int index,out Vector3 point,out Quaternion rotation,out float width)
        {
            point=Vector3.zero;rotation=Quaternion.identity;width=gates!=null&&index<gates.Length&&gates[index]!=null?gates[index].width:48;
            if(session is MattressMatch m){point=m.course.Surface(new Vector3(m.course.Center(MattressCourse.Finish),0,MattressCourse.Finish));rotation=m.course.Rotation(MattressCourse.Finish);width=92;}
            else if(session is WifeMatch w){point=w.course.loopCircuit?w.course.Point(WifeCourse.Finish):w.course.Surface(new Vector3(w.course.Center(WifeCourse.Finish),0,WifeCourse.Finish));rotation=w.course.Rotation(WifeCourse.Finish);width=48;}
            else if(session is BogMatch b){point=b.course.Surface(new Vector3(BogCourse.Center(b.course.FinishDistance),0,b.course.FinishDistance));width=48;}
            else if(session is TerrierMatch t){point=t.course.World(new Vector3(0,0,t.course.Length));rotation=t.course.Heading;width=8;}
            else if(session is AvalancheMatch a){point=a.world.finish.position;width=70;}
            else if(session is RedLightMatch r){point=new Vector3(0,r.startY-.65f,r.finishZ);width=r.trackHalfWidth*2;}
            else if(session is MinefieldMatch mine){point=mine.Surface(new Vector3(0,0,MinefieldRules.Mile));width=110;}
            else if(session is FrogMatch f){point=f.world.courses[index].finish;width=f.world.courses[index].halfWidth*2;}
            // Finish decoration follows the track tangent but never tilts a banner into the road.
            var forward=Vector3.ProjectOnPlane(rotation*Vector3.forward,Vector3.up);rotation=Quaternion.LookRotation(forward.sqrMagnitude>.01f?forward:Vector3.forward,Vector3.up);
        }
        public float GroundHeight(Vector3 point)
        {
            if(session is MattressMatch m)return m.course.Height(point);
            if(session is WifeMatch w)return w.course.Height(point);
            if(session is BogMatch b)return b.course.Surface(point).y;
            if(session is AvalancheMatch a)return a.world.Height(point);
            if(session is MinefieldMatch mine)return mine.Surface(point).y;
            if(session is TerrierMatch t){var local=t.course.Local(point);local.y=0;return t.course.World(local).y;}
            return 0;
        }
        public void ProjectChecks()
        {
            foreach(var gate in gates)
            {
                if(gate.checks==null)continue;if(gate.ownedChecks==null){gate.ownedChecks=Instantiate(gate.checks.sharedMesh);gate.ownedChecks.name="Runtime terrain-fitted finish checkers";gate.checks.sharedMesh=gate.ownedChecks;}
                var vertices=gate.ownedChecks.vertices;for(int i=0;i<vertices.Length;i++){var world=gate.checks.transform.TransformPoint(vertices[i]);world.y=GroundHeight(world)+.045f;vertices[i]=gate.checks.transform.InverseTransformPoint(world);}gate.ownedChecks.vertices=vertices;gate.ownedChecks.RecalculateNormals();gate.ownedChecks.RecalculateBounds();
            }
        }
        void OnDestroy(){if(gates!=null)foreach(var gate in gates)if(gate.ownedChecks!=null)Destroy(gate.ownedChecks);}
    }
}
