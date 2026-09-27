using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Owns only rail-crossing policy. UTS remains the vehicle drive/signal authority.
    [DefaultExecutionOrder(600), DisallowMultipleComponent]
    public sealed class TruckTaxiTransitCrossing : MonoBehaviour
    {
        public TruckTaxiRailTrain train;
        public TruckTaxiTrafficAdapter traffic;
        public TruckTaxiBusService buses;
        public Transform player;
        public Transform[] gateArms;
        public Renderer[] warningLamps;
        public float approachMeters = 105;
        public bool Closed { get; private set; }
        public bool PedestrianMayCross => !Closed;
        private sealed class VehicleControl
        {
            public Component Ai;
            public FieldInfo StopField;
            public WheelCollider[] Wheels;
        }
        private readonly Dictionary<GameObject, VehicleControl> controls = new Dictionary<GameObject, VehicleControl>();
        private readonly HashSet<VehicleControl> heldCars = new HashSet<VehicleControl>();
        private readonly Dictionary<Behaviour, bool> heldPedestrians = new Dictionary<Behaviour, bool>();
        private float nextPeopleScan;

        private void Update()
        {
            if (player == null) player = TruckTaxiBootstrap.Instance?.Player?.transform;
            bool near = player != null && (player.position - transform.position).sqrMagnitude < 250 * 250;
            bool closed = false;
            if (near && train != null && train.route != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 car = train.CarPosition(i);
                    if ((car - transform.position).sqrMagnitude < approachMeters * approachMeters)
                    { closed = true; break; }
                }
            }
            Closed = closed;
            if (gateArms != null)
                for (int i = 0; i < gateArms.Length; i++)
                    if (gateArms[i] != null) gateArms[i].localRotation = Quaternion.RotateTowards(gateArms[i].localRotation,
                        Quaternion.Euler(0, 0, Closed ? 0 : (i == 0 ? 80 : -80)), Time.deltaTime * 65);
            if (warningLamps != null)
                foreach (var lamp in warningLamps) if (lamp != null) lamp.enabled = Closed && Mathf.Repeat(Time.time, .7f) < .35f;
            if (!Closed) ReleaseAll();
            else if (Time.time >= nextPeopleScan)
            {
                nextPeopleScan = Time.time + .2f;
                HoldPedestrians();
            }
        }

        private void FixedUpdate()
        {
            if (!Closed) return;
            if (traffic != null) foreach (var vehicle in traffic.Vehicles)
                if (vehicle != null && vehicle.activeInHierarchy) HoldCar(vehicle);
            if (buses != null) for (int i = 0; i < buses.RouteCount; i++)
            { var bus = buses.GetLiveBus(i); if (bus != null) HoldCar(bus); }
        }

        public bool IsApproachingRoad(Vector3 position, Vector3 forward)
        {
            Vector3 local = position - transform.position;
            return Mathf.Abs(local.z) < 10 && Mathf.Abs(local.x) > 15 && Mathf.Abs(local.x) < 65 &&
                local.x * forward.x < -.2f;
        }

        private void HoldCar(GameObject vehicle)
        {
            if (!IsApproachingRoad(vehicle.transform.position, vehicle.transform.forward)) return;
            if (!controls.TryGetValue(vehicle, out var control) || control.Ai == null)
            {
                var ai = FindUts(vehicle, "CarAIController");
                if (ai == null) return;
                control = new VehicleControl { Ai = ai,
                    StopField = ai.GetType().GetField("tempStop", BindingFlags.Instance | BindingFlags.Public),
                    Wheels = ai.GetComponentsInChildren<WheelCollider>(true) };
                controls[vehicle] = control;
            }
            if (control.StopField == null) return;
            control.StopField.SetValue(control.Ai, true);
            heldCars.Add(control);
            foreach (var wheel in control.Wheels) if (wheel != null)
                wheel.brakeTorque = Mathf.Max(wheel.brakeTorque, 18000);
        }

        private void HoldPedestrians()
        {
            var people = TruckTaxiBootstrap.Instance?.pedestrians?.People;
            if (people == null) return;
            foreach (var pedestrian in people)
            {
                if (pedestrian == null || !pedestrian.gameObject.activeInHierarchy || pedestrian.IsRagdoll) continue;
                Vector3 offset = pedestrian.transform.position - transform.position;
                if (Mathf.Abs(offset.x) < 11 || Mathf.Abs(offset.x) > 29 || Mathf.Abs(offset.z) > 18) continue;
                foreach (var component in pedestrian.GetComponentsInChildren<MonoBehaviour>())
                {
                    if (component == null || (component.GetType().Name != "Passersby" && component.GetType().Name != "PeopleController")) continue;
                    if (component is Behaviour behaviour && !heldPedestrians.ContainsKey(behaviour))
                    { heldPedestrians.Add(behaviour, behaviour.enabled); behaviour.enabled = false; }
                }
            }
        }

        private void ReleaseAll()
        {
            foreach (var control in heldCars)
            {
                if (control.Ai == null) continue;
                control.StopField?.SetValue(control.Ai, false);
                foreach (var wheel in control.Wheels)
                    if (wheel != null && Mathf.Approximately(wheel.brakeTorque, 18000)) wheel.brakeTorque = 0;
            }
            heldCars.Clear();
            controls.Clear();
            foreach (var held in heldPedestrians) if (held.Key != null) held.Key.enabled = held.Value;
            heldPedestrians.Clear();
        }

        private void OnDisable() { Closed = false; ReleaseAll(); }

        private static Component FindUts(GameObject owner, string name)
        {
            foreach (var component in owner.GetComponentsInChildren<MonoBehaviour>())
                if (component != null && component.GetType().Name == name) return component;
            return null;
        }
    }
}
