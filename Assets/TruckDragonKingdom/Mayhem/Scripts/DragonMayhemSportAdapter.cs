using System;
using LWS.TruckSports;
using LWS.TruckKeepaway;
using LWS.TruckBalloonBattle;
using LWS.TruckCornhole;
using LWS.TruckCheeseRolling;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Mayhem
{
    public enum DragonMayhemSport { Keepaway, BalloonBattle, Cornhole, CheeseRolling }

    // Read-only lifecycle/target adapter. The original controller remains the only
    // owner of possession, balloons, board points, catches, and winner selection.
    public sealed class DragonMayhemSportAdapter : MonoBehaviour
    {
        public DragonMayhemSport sport;
        public KeepawayMatch keepaway;
        public BalloonMatch balloons;
        public CornholeMatch cornhole;
        public CheeseMatch cheese;
        public string SportId => sport.ToString();
        public SportsSettings Settings => sport == DragonMayhemSport.Keepaway ? keepaway.settings : sport == DragonMayhemSport.BalloonBattle ? balloons.settings : sport == DragonMayhemSport.Cornhole ? cornhole.settings : cheese.settings;
        public int Count => sport == DragonMayhemSport.Keepaway ? keepaway.setup.count : sport == DragonMayhemSport.BalloonBattle ? balloons.setup.count : sport == DragonMayhemSport.Cornhole ? cornhole.setup.count : cheese.setup.count;
        public bool InMatch => sport == DragonMayhemSport.Keepaway ? keepaway.Running : sport == DragonMayhemSport.BalloonBattle ? balloons.Running : sport == DragonMayhemSport.Cornhole ? cornhole.InMatch : cheese.InMatch;
        public bool Paused => sport == DragonMayhemSport.Keepaway ? keepaway.Paused : sport == DragonMayhemSport.BalloonBattle ? balloons.Paused : sport == DragonMayhemSport.Cornhole ? cornhole.Paused : cheese.Paused;
        public bool Finished => sport == DragonMayhemSport.Keepaway ? keepaway.Finished : sport == DragonMayhemSport.BalloonBattle ? balloons.Finished : sport == DragonMayhemSport.Cornhole ? cornhole.Finished : cheese.Finished;
        public bool Live => InMatch && !Paused && !Finished && (sport != DragonMayhemSport.BalloonBattle || balloons.Countdown <= 0) && (sport != DragonMayhemSport.CheeseRolling || cheese.Live) && (sport != DragonMayhemSport.Cornhole || !cornhole.RoundBreak);
        public string MatchKey => sport == DragonMayhemSport.Keepaway ? keepaway.MatchId : sport == DragonMayhemSport.BalloonBattle ? balloons.MatchId : sport == DragonMayhemSport.Cornhole ? cornhole.MatchId : cheese.MatchId;
        public string ParticipantName(int slot) => sport == DragonMayhemSport.Keepaway ? keepaway.setup.players[slot].name : sport == DragonMayhemSport.BalloonBattle ? balloons.setup.players[slot].name : sport == DragonMayhemSport.Cornhole ? cornhole.setup.players[slot].name : cheese.setup.players[slot].name;
        public Rigidbody Body(int slot)
        {
            if (slot < 0 || slot >= Count) return null;
            if (sport == DragonMayhemSport.Keepaway) return keepaway.trucks[slot].body;
            if (sport == DragonMayhemSport.BalloonBattle) return balloons.trucks[slot].body;
            if (sport == DragonMayhemSport.CheeseRolling) return cheese.trucks[slot].body;
            return cornhole.courses[slot].Current != null ? cornhole.courses[slot].Current.body : null;
        }
        public bool Eligible(int slot)
        {
            var body = Body(slot);
            if (!Live || body == null || !body.gameObject.activeInHierarchy || body.isKinematic) return false;
            if (sport == DragonMayhemSport.Keepaway) return keepaway.CanAttack(slot);
            if (sport == DragonMayhemSport.BalloonBattle) return balloons.rules.Alive(slot);
            if (sport == DragonMayhemSport.CheeseRolling) return cheese.trucks[slot].Scoring && cheese.trucks[slot].GridPrepared && cheese.trucks[slot].RecoveryCooldown <= 0;
            var course = cornhole.courses[slot]; var truck = course.Current;
            // No additional force, collision, or pickup is applied in flight,
            // landing, settling, replay, or the board/hole scoring region.
            return course.Phase == CornholePhase.Approach && !truck.Launched && !truck.Landed && !truck.Holed && !truck.Excluded && !Protected(body.position);
        }
        public bool TryParticipant(Collider contact, out int slot)
        {
            slot = -1;
            if (contact == null) return false;
            var body = contact.attachedRigidbody;
            if (body == null) return false;
            for (int i = 0; i < Count; i++) if (Body(i) == body && CanCollect(i)) { slot = i; return true; }
            return false;
        }
        public bool CanCollect(int slot)
        {
            if(sport!=DragonMayhemSport.Cornhole)return Eligible(slot);
            if(!Live || slot<0 || slot>=Count)return false;
            var course=cornhole.courses[slot];var truck=course.Current;
            // Trigger-only pickup may touch a genuinely airborne truck. It does
            // not enable solid collisions or alter the landing coordinator.
            return truck!=null && truck.gameObject.activeInHierarchy && !truck.body.isKinematic && !truck.Landed && !truck.Holed && !truck.Excluded && !truck.LandingCollisionReady && (course.Phase==CornholePhase.Approach || course.Phase==CornholePhase.Flight);
        }
        public bool InsidePlayArea(Vector3 point)
        {
            if(sport==DragonMayhemSport.Keepaway)return KeepawayBoundary.Contains(point,4);
            if(sport==DragonMayhemSport.BalloonBattle)return new Vector2(point.x,point.z).sqrMagnitude<49*49;
            if(sport==DragonMayhemSport.CheeseRolling)return Mathf.Abs(point.x)<45 && point.z>34 && point.z<300;
            foreach(var course in cornhole.courses){var p=course.ApproachLocalPoint(point);if(Mathf.Abs(p.x)<5 && p.z> -18 && p.z< -7)return true;}
            return false;
        }
        public Vector3 DropCandidate(int slot)
        {
            if(sport==DragonMayhemSport.Cornhole)return cornhole.courses[Mathf.Clamp(slot,0,Count-1)].ApproachPoint(new Vector3(0,0,-11));
            var body=Body(slot);var point=(body!=null?body.position:Center)+Vector3.right*5;
            // The original Cheese hill is a 2:1 slope. Its authored flat runout
            // offers actual valid support without weakening any release slope,
            // race protection, or play-area check. The later ray remains final.
            return sport==DragonMayhemSport.CheeseRolling?CheeseRules.Surface(Mathf.Clamp(point.x,-30,30),Mathf.Clamp(point.z+80,225,260)):point;
        }
        public Vector3 Center
        {
            get { Vector3 sum = Vector3.zero; int n = 0; for (int i = 0; i < Count; i++) { var b = Body(i); if (b != null && b.gameObject.activeInHierarchy) { sum += b.position; n++; } } return n > 0 ? sum / n : transform.position; }
        }
        public bool Protected(Vector3 point)
        {
            if (sport == DragonMayhemSport.Keepaway) return Vector3.Distance(point, keepaway.ball.position) < 5;
            if (sport == DragonMayhemSport.CheeseRolling) return point.z < 22 || Vector3.Distance(point, cheese.cheese.body.position) < 12;
            if (sport != DragonMayhemSport.Cornhole) return false;
            foreach (var c in cornhole.courses)
            {
                var p = c.board.InverseTransformPoint(point);
                if (Mathf.Abs(p.x) < 14 && Mathf.Abs(p.z) < 19) return true;
                if (Vector3.Distance(point, c.trampoline.position) < 9) return true;
                // Start space and first acceleration section stay clear.
                if (Vector3.Distance(point, c.StartPosition) < 7) return true;
            }
            return false;
        }
        public bool TryGround(Vector3 near, out Vector3 point)
        {
            point = default;
            var hits = Physics.RaycastAll(near + Vector3.up * 70, Vector3.down, 140, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.attachedRigidbody != null || Vector3.Dot(hit.normal, Vector3.up) < .7f || Protected(hit.point) || !InsidePlayArea(hit.point)) continue;
                point = hit.point; return true;
            }
            return false;
        }
        public Collider[] OriginalColliders() => sport == DragonMayhemSport.Keepaway ? keepaway.GetComponentsInChildren<Collider>(true) : sport == DragonMayhemSport.BalloonBattle ? balloons.GetComponentsInChildren<Collider>(true) : sport == DragonMayhemSport.Cornhole ? cornhole.GetComponentsInChildren<Collider>(true) : cheese.GetComponentsInChildren<Collider>(true);
        public void Validate()
        {
            int references = (keepaway != null ? 1 : 0) + (balloons != null ? 1 : 0) + (cornhole != null ? 1 : 0) + (cheese != null ? 1 : 0);
            if (references != 1 || sport == DragonMayhemSport.Keepaway && keepaway == null || sport == DragonMayhemSport.BalloonBattle && balloons == null || sport == DragonMayhemSport.Cornhole && cornhole == null || sport == DragonMayhemSport.CheeseRolling && cheese == null) throw new InvalidOperationException("Mayhem requires exactly one matching original sport controller.");
        }
    }
}
