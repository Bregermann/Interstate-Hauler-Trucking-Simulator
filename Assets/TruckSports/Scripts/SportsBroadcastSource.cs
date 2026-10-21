using UnityEngine;

namespace LWS.TruckSports
{
    // Small read-only bridge for the older sports which predate SportsEventSession.
    // It owns no setup, input, score, match lifecycle or participant state.
    public abstract class SportsBroadcastSource:MonoBehaviour
    {
        public abstract bool Playing{get;}
        public abstract int Count{get;}
        public abstract bool IsHuman(int slot);
        public abstract string ParticipantName(int slot);
        public abstract Color ParticipantColor(int slot);
        public virtual bool Valid(int slot)=>true;
        public abstract void Describe(SportsBroadcastAction action,SportsBroadcastFraming framing);
    }
}
