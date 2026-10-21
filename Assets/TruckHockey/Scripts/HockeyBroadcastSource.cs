using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckHockey
{
    public sealed class HockeyBroadcastSource:SportsBroadcastSource
    {
        public HockeyMatch match;
        public override bool Playing=>match.InMatch&&match.menus.Screen==HockeyScreen.Match;
        public override int Count=>match.players.Length;
        public override bool IsHuman(int slot)=>match.players[slot].source!=HockeySource.AI;
        public override string ParticipantName(int slot)=>match.players[slot].name;
        public override Color ParticipantColor(int slot)=>match.players[slot].color;
        public override bool Valid(int slot)=>match.trucks[slot].gameObject.activeInHierarchy;
        public override void Describe(SportsBroadcastAction action,SportsBroadcastFraming framing)
        {
            action.Objective(match.puck.position,.5f);int human=framing.Nearest(match.puck.position,true);
            action.primary=human>=0?human:framing.Nearest(match.puck.position);action.reason="PUCK / INVOLVED HOCKEY TRUCKS";
        }
    }
}
