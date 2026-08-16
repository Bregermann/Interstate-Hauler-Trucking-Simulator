using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(
        fileName = "IH_RoadConditionPhysics",
        menuName = "Interstate Hauler/Road Conditions/Physics Profile")]
    public sealed class LwsRoadConditionPhysicsProfile : ScriptableObject
    {
        [SerializeField] private LwsRoadConditionProfile profile = LwsRoadConditionProfile.Default();

        public LwsRoadConditionProfile Profile => profile ?? LwsRoadConditionProfile.Default();

        private void OnValidate()
        {
            profile ??= LwsRoadConditionProfile.Default();
            profile.Validate(out _);
        }

        public bool ValidateProfile(out string message)
        {
            return Profile.Validate(out message);
        }
    }
}
