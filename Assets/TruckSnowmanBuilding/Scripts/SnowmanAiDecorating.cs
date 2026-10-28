using System;
using System.Linq;
using UnityEngine;
using LWS.TruckBowling;

namespace LWS.TruckSnowmanBuilding
{
    public sealed partial class SnowmanMatch
    {
        // The rack is closed at its sides/back. Its actual open mouths face
        // south; the cabinet and statue are joined by the clear east aisle.
        // One AI courier keeps parked rolling crews out of that narrow aisle.
        // NWH and the existing pick/place gates still own all physical work.
        public readonly int[] AiDecorationRoute = { -1, -1, -1, -1 };
        readonly float[] decorationExitLane = new float[4];

        void ResetAiDecorating()
        {
            for (int i = 0; i < AiDecorationRoute.Length; i++) AiDecorationRoute[i] = -1;
            Array.Clear(decorationExitLane, 0, decorationExitLane.Length);
        }

        Vector2 DecorateWithOriginalTruck(int owner)
        {
            if (CountDecor() == 6) { FinishBuild(); return new Vector2(0, -1); }
            int courier = Enumerable.Range(0, setup.count).Where(i => setup.players[i].source == BowlingSource.AI && carried[i] >= 0).DefaultIfEmpty(-1).First();
            if (courier < 0) courier = Enumerable.Range(0, setup.count).Where(i => setup.players[i].source == BowlingSource.AI).DefaultIfEmpty(-1).Last();
            if (owner != courier) return new Vector2(0, -1);
            var truck = trucks[owner]; Vector3 position = truck.body.position;
            int phase = AiDecorationRoute[owner];
            if (phase < 0)
            {
                bool insideRack = Mathf.Abs(position.x) < 16.5f && position.z > -12 && position.z < 9;
                decorationExitLane[owner] = Mathf.Clamp(Mathf.Round(position.x / 10) * 10, -10, 10);
                phase = AiDecorationRoute[owner] = insideRack ? 0 : carried[owner] >= 0 ? 3 : 1;
            }
            Vector3 target;
            switch (phase)
            {
                case 0: target = new Vector3(decorationExitLane[owner], position.y, -30); break;
                case 1: target = new Vector3(22, position.y, -30); break;
                case 2:
                    if (carried[owner] >= 0) { AiDecorationRoute[owner] = 3; return new Vector2(0, -1); }
                    // The entire2m stopping disk stays inside the unchanged
                    //7m interaction sphere (including body/cabinet height).
                    target = supply.position + Vector3.left * 4.75f;
                    if (Vector3.ProjectOnPlane(position - target, Vector3.up).magnitude < 2)
                    {
                        if (truck.body.linearVelocity.magnitude > .7f) return new Vector2(0, -1);
                        int next = Enumerable.Range(0, 6).Where(f => (Rules.Decorations & (1 << f)) == 0 && !carried.Contains(f)).DefaultIfEmpty(-1).First();
                        if (next < 0) return new Vector2(0, -1);
                        family[owner] = next; style[owner] = owner % decorations[next].variants.Length;
                        if (PickDecoration(owner)) AiDecorationRoute[owner] = 3;
                        return new Vector2(0, -1);
                    }
                    return DriveTo(truck, target, 2);
                case 3: target = new Vector3(22, position.y, buildCenter.position.z); break;
                case 4:
                    target = buildCenter.position + Vector3.right * 9;
                    if (PlaceDecoration(owner)) { AiDecorationRoute[owner] = 5; return new Vector2(0, -1); }
                    return DriveTo(truck, target, 2);
                case 5:
                    if (placements[owner] == null && carried[owner] < 0) AiDecorationRoute[owner] = 6;
                    return new Vector2(0, -1);
                case 6: target = new Vector3(22, position.y, buildCenter.position.z); break;
                default: throw new InvalidOperationException("Unknown Snowman decoration route phase.");
            }
            if (Vector3.ProjectOnPlane(position - target, Vector3.up).magnitude < 3)
            {
                AiDecorationRoute[owner] = phase == 6 ? 2 : phase + 1;
                return new Vector2(0, -1);
            }
            return DriveTo(truck, target, 2);
        }
    }
}
