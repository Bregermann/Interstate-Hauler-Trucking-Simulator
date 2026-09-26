using System.Reflection;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Uses UTS's existing run animation and movement state; does not steer the pedestrian.
    public sealed class TruckTaxiPedestrianGesture : MonoBehaviour
    {
        private Component passersby;
        private PropertyInfo animationState;
        private object previousState;
        private float restoreAt;
        private TruckTaxiPedestrian pedestrian;

        private void Awake()
        {
            pedestrian = GetComponent<TruckTaxiPedestrian>();
            foreach (var component in GetComponents<MonoBehaviour>())
                if (component.GetType().Name == "Passersby") { passersby = component; break; }
            if (passersby != null) animationState = passersby.GetType().GetProperty("ANIMATION_STATE");
        }

        public bool ReactToBadDriver()
        {
            if (pedestrian == null || pedestrian.IsRagdoll || passersby == null || animationState == null ||
                Time.time < restoreAt) return false;
            previousState = animationState.GetValue(passersby);
            animationState.SetValue(passersby, System.Enum.Parse(animationState.PropertyType, "run"));
            restoreAt = Time.time + 2;
            return true;
        }

        private void Update()
        {
            if (restoreAt <= 0 || Time.time < restoreAt) return;
            Restore();
        }

        private void OnDisable() { Restore(); }

        private void Restore()
        {
            if (restoreAt <= 0) return;
            restoreAt = 0;
            if (passersby != null && animationState != null && previousState != null && pedestrian != null && !pedestrian.IsRagdoll)
                animationState.SetValue(passersby, previousState);
        }
    }
}
