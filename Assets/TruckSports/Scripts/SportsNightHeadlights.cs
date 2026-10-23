using UnityEngine;

namespace LWS.TruckSports
{
    // Match-time semantics only. NWH retains light-source and emissive-optic
    // control on complete tractors; wheel-only arcade bodies have authored beams.
    public sealed class SportsNightHeadlights : MonoBehaviour
    {
        [System.Serializable] public sealed class Beam
        {
            public Transform target;
            public Light light;
            public Vector3 position;
            public Quaternion rotation = Quaternion.identity;
        }
        public SportsMatchTime time;
        public SportsEventTruck[] trucks = System.Array.Empty<SportsEventTruck>();
        public Light[] wheelOnlyBeams = System.Array.Empty<Light>();
        public Beam[] beams = System.Array.Empty<Beam>();
        public MonoBehaviour alternateSession;
        [HideInInspector] public int layoutVersion;
        float nextRefresh;

        void LateUpdate()
        {
            // Keep presentation objects outside recorded truck hierarchies. Use
            // the interpolated render pose so beams follow smoothly at speed.
            foreach (var beam in beams)
                if (beam.target != null && beam.light != null) beam.light.transform.SetPositionAndRotation(beam.target.TransformPoint(beam.position), beam.target.rotation * beam.rotation);
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .2f;
            bool night = time != null && time.Revision > 0 &&
                (time.Applied == SportsTimeChoice.Night || time.Applied == SportsTimeChoice.Evening);
            if (alternateSession is ISportsWeatherLifecycle alternate)
                foreach (var beam in beams)
                    if (beam.light != null) beam.light.enabled = night && alternate.WeatherMatchActive && beam.target != null && beam.target.gameObject.activeInHierarchy;
            for (int i = 0; i < trucks.Length; i++)
            {
                var truck = trucks[i];
                Apply(i, night && truck != null && truck.gameObject.activeInHierarchy && truck.body.detectCollisions &&
                    truck.session != null && truck.session.Running && !truck.session.Finished);
            }
        }

        void Apply(int index, bool on)
        {
            var truck = trucks[index];
            if (truck != null && truck.tractorMotor != null && truck.tractorMotor.vehicle != null)
            {
                var lights = truck.tractorMotor.vehicle.effectsManager.lightsManager;
                lights.lowBeamLights.SetState(on);
                lights.tailLights.SetState(on);
            }
            else if (index < wheelOnlyBeams.Length && wheelOnlyBeams[index] != null) wheelOnlyBeams[index].enabled = on;
        }

        void OnDisable()
        {
            for (int i = 0; i < trucks.Length; i++) Apply(i, false);
            if (alternateSession != null) foreach (var beam in beams) if (beam.light != null) beam.light.enabled = false;
        }
    }
}
