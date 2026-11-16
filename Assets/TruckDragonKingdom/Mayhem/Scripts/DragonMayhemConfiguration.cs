using UnityEngine;

namespace LWS.TruckDragonKingdom.Mayhem
{
    public sealed class DragonMayhemConfiguration : ScriptableObject
    {
        public DragonMayhemSport sport;
        public string variantName, sideBonusLabel;
        [Range(1,6)] public int maximumSideGold = 6;
        [Range(.1f,1.2f)] public float gustVelocityChange = 1.15f;
        [Range(12,28)] public float approachHeight = 13;
        [Range(12,40)] public float goldLifetime = 24;
        [Range(12,30)] public float passCooldown = 14;
        [Range(0,2)] public int temporarySheep = 2;
        public Vector3 spawnOffset = new Vector3(-90,60,-75);
        public Vector3 departureOffset = new Vector3(100,55,100);
        public string dropSpeciesId;
        public AnimationClip dropContactClip;
        public float dropContactNormalizedTime;
        public Vector3 dropMouthLocalOffset;
        public string groundGrabSpeciesId;
        public AnimationClip groundGrabContactClip;
        public float groundGrabContactNormalizedTime;
        public Vector3 groundGrabGripLocalOffset;
    }
}
