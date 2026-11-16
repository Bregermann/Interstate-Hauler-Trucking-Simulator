using LWS.TruckBowling;
using UnityEngine;

namespace LWS.TruckDragonRiders
{
    public sealed partial class DragonRidersMatch
    {
        [Min(0),Tooltip("Straight travel beyond a scored physical gate before AI turns. Zero preserves the original course; the measured large-ring candidate configures55m.")]
        public float aiStraightGateExitDistance;
        readonly DragonRiderGateEgress[] aiGateEgress={new DragonRiderGateEgress(),new DragonRiderGateEgress(),new DragonRiderGateEgress(),new DragonRiderGateEgress()};
        void ResetStraightGateEgress(){foreach(var exit in aiGateEgress)exit.Reset();}
        void ArmStraightGateEgress(int slot,int total)
        {
            if(aiStraightGateExitDistance<=0||setup.players[slot].source!=BowlingSource.AI||rules.Finished(slot))return;
            var gate=rules.Gate((total-1)%rules.GateCount);aiGateEgress[slot].Arm(gate.center,gate.rotation*Vector3.forward,aiStraightGateExitDistance);
        }
        bool TryStraightGateEgress(int slot,Vector3 position,out Vector3 target)=>aiGateEgress[slot].TryTarget(position,out target);
    }
}
