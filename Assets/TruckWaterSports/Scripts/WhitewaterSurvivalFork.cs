using System;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckWaterSports
{
    // Whitewater-only route semantics. The original course/current/buoyancy,
    // physical rock colliders and ordinary survival outcomes remain authoritative.
    public sealed class WhitewaterSurvivalFork : MonoBehaviour
    {
        public WhitewaterWipeoutMatch match;
        public Transform equipment;
        public Vector3 center, forward = Vector3.forward, right = Vector3.right;
        public float narrowOffset = -10.2f, narrowHalfWidth = 2, wideOffset = 1.5f;
        public bool stagedWhirlpoolApproach;
        public Vector3 upstreamInlet, narrowOutlet, openOutlet;
        readonly int[] navigationNode = new int[4];
        readonly bool[] navigationActive = new bool[4], navigationReturning = new bool[4];
        public int[] NarrowPassages = new int[4], OpenChannelPassages = new int[4];
        readonly Vector3[] previous = new Vector3[4];
        readonly bool[] armed = new bool[4];
        string matchId;
        public bool Active => match != null && match.option == 3;

        void EnsureMatch()
        {
            if (matchId == match.MatchId) return;
            matchId = match.MatchId;
            Array.Clear(NarrowPassages, 0, NarrowPassages.Length); Array.Clear(OpenChannelPassages, 0, OpenChannelPassages.Length);
            Array.Clear(navigationNode,0,navigationNode.Length);Array.Clear(navigationActive,0,navigationActive.Length);
            for (int i = 0; i < match.vessels.Length; i++) { previous[i] = match.vessels[i].body.position; armed[i] = true; }
        }

        public Vector3 Aim(WaterVessel vessel, int slot, Vector3 ordinaryTarget, bool returning)
        {
            if (!Active || !match.Live) return ordinaryTarget;
            EnsureMatch();
            float direction = returning ? -1 : 1, along = Vector3.Dot(vessel.body.position - center, forward) * direction;
            if (along < -38 || along > (stagedWhirlpoolApproach ? 34 : 18)) { navigationActive[slot]=false; return ordinaryTarget; }
            // The narrow branch leads into the existing lower whirlpool;
            // the open branch follows its outside edge toward the native bend.
            int skill = (int)match.setup.players[slot].difficulty;
            bool narrow = skill >= 2 && slot % 2 == 0;
            float lateral = narrow ? narrowOffset : wideOffset + (slot % 2 == 0 ? -.75f : .75f);
            if(stagedWhirlpoolApproach)return StagedAim(vessel,slot,lateral,returning,narrow,ordinaryTarget);
            // Always aim beyond the crossing plane. A waypoint ON the plane
            // could cause an upstream craft to brake before ever passing it.
            return center + right * lateral + forward * direction * (along < 0 ? 18 : 24);
        }

        Vector3 StagedAim(WaterVessel vessel,int slot,float lateral,bool returning,bool narrow,Vector3 ordinaryTarget)
        {
            if(!navigationActive[slot]||navigationReturning[slot]!=returning)
            {navigationActive[slot]=true;navigationReturning[slot]=returning;navigationNode[slot]=0;}
            // The previous notch aimed directly through a bank. These real
            // ingress/egress points clear both overlapping bank corners before
            // turning into the physical gap; no craft pose or force is replaced.
            while(navigationNode[slot]<4)
            {
                int node=returning?3-navigationNode[slot]:navigationNode[slot];
                Vector3 target=node==0?upstreamInlet:node==1?center+right*lateral-forward*6:
                    node==2?center+right*lateral+forward*6:narrow?narrowOutlet:openOutlet;
                float direction=returning?-1:1;
                bool passed=Vector3.Dot(vessel.body.position-target,forward)*direction>2;
                if(Vector3.ProjectOnPlane(vessel.body.position-target,Vector3.up).magnitude<3||passed)
                {navigationNode[slot]++;continue;}
                return target;
            }
            return ordinaryTarget;
        }

        void FixedUpdate()
        {
            if (match == null || equipment == null) return;
            if (equipment.gameObject.activeSelf != Active) equipment.gameObject.SetActive(Active);
            if (!Active || !match.Live) return;
            EnsureMatch();
            for (int slot = 0; slot < match.setup.count; slot++)
            {
                var vessel = match.vessels[slot]; if (!vessel.gameObject.activeInHierarchy) continue;
                Vector3 position = vessel.body.position;
                float before = Vector3.Dot(previous[slot] - center, forward), after = Vector3.Dot(position - center, forward);
                if (Mathf.Abs(after) > 18) armed[slot] = true;
                if (armed[slot] && (before < 0 && after >= 0 || before > 0 && after <= 0))
                {
                    Vector3 passage = Vector3.Lerp(previous[slot], position, -before / (after - before));
                    float lateral = Vector3.Dot(passage - center, right);
                    bool narrow = Mathf.Abs(lateral - narrowOffset) <= narrowHalfWidth;
                    bool open = lateral >= -4 && lateral <= match.course.width * .5f - 1;
                    if (narrow || open)
                    {
                        if (narrow) NarrowPassages[slot]++; else OpenChannelPassages[slot]++;
                        SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "whitewater-survival-route-passage",
                            match.setup.players[slot].participantId, (narrow ? "physical rock notch into whirlpool" : "open central recovery channel") +
                            (after > before ? "; downstream" : "; upstream"), lateral);
                    }
                    armed[slot] = false;
                }
                previous[slot] = position;
            }
        }
    }
}
