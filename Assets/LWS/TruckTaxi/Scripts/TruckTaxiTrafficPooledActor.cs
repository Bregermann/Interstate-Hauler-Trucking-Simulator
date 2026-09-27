using System;
using System.Reflection;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiTrafficPooledActor : MonoBehaviour
    {
        private Component ai;
        private Component path;
        private Component move;
        private Rigidbody body;
        private Behaviour aiBehaviour;
        private Behaviour moveBehaviour;
        private TruckTaxiTrafficBehaviour taxi;
        private TruckTaxiRoadRage rage;
        private TruckTaxiImpactTarget impact;
        private TruckTaxiMissionTarget mission;
        private Collider[] colliders;
        private bool[] colliderDefaults;
        private WheelCollider[] wheels;
        private Animator[] animators;
        private AudioSource[] sounds;
        private bool[] animatorDefaults;
        private bool bodyWasKinematic;
        private bool bodyUsedGravity;
        private RigidbodyConstraints bodyConstraints;
        private Component[] optionalBehaviours;
        private bool[] optionalDefaults;
        public int PrefabIndex { get; private set; }
        public int Generation { get; private set; }
        public bool FullPhysics { get; private set; }
        public bool MissionReserved => mission != null && !string.IsNullOrEmpty(mission.StableId);
        public Rigidbody Body => body;

        public void Configure(int prefabIndex)
        {
            PrefabIndex = prefabIndex;
            foreach (var component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                switch (component.GetType().Name)
                {
                    case "CarAIController": ai = component; break;
                    case "MovePath": path = component; break;
                    case "CarMove": move = component; break;
                }
            }
            aiBehaviour = ai as Behaviour; moveBehaviour = move as Behaviour;
            body = ai != null ? ai.GetComponent<Rigidbody>() : GetComponentInChildren<Rigidbody>();
            taxi = GetComponent<TruckTaxiTrafficBehaviour>();
            rage = GetComponent<TruckTaxiRoadRage>();
            impact = GetComponent<TruckTaxiImpactTarget>();
            colliders = GetComponentsInChildren<Collider>(true);
            colliderDefaults = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++) colliderDefaults[i] = colliders[i].enabled;
            wheels = GetComponentsInChildren<WheelCollider>(true);
            animators = GetComponentsInChildren<Animator>(true);
            sounds = GetComponentsInChildren<AudioSource>(true);
            animatorDefaults = new bool[animators.Length];
            for (int i = 0; i < animators.Length; i++) animatorDefaults[i] = animators[i].enabled;
            if (body != null) { bodyWasKinematic = body.isKinematic; bodyUsedGravity = body.useGravity; bodyConstraints = body.constraints; }
            optionalBehaviours = GetComponentsInChildren<MonoBehaviour>(true);
            optionalDefaults = new bool[optionalBehaviours.Length];
            for (int i = 0; i < optionalBehaviours.Length; i++)
                optionalDefaults[i] = optionalBehaviours[i] is Behaviour behaviour && behaviour.enabled;
        }

        public void PutAway()
        {
            Generation++;
            GetComponent<TruckTaxiRivalVehicleVisual>()?.ResetForPool();
            taxi?.ResetForPool();
            gameObject.SetActive(false);
            if (rage != null)
            {
                SetField(rage, "nextAllowed", 0f);
                SetField(rage, "endsAt", 0f);
            }
            mission = GetComponent<TruckTaxiMissionTarget>();
            mission?.Clear();
            if (impact != null) { SetField(impact, "<Damaged>k__BackingField", false); impact.targetId = null; }
            ResetVendorState();
            FullPhysics = false;
            if (body != null) { body.isKinematic = true; body.useGravity = false; }
            foreach (var wheel in wheels) if (wheel != null) wheel.enabled = false;
            foreach (var animator in animators) if (animator != null) animator.enabled = false;
            foreach (var sound in sounds) if (sound != null) sound.Stop();
        }

        public void Place(Component lanePath, int point, Vector3 position, string stableId, bool full)
        {
            Generation++;
            ResetVendorState();
            if (body != null) body.isKinematic = true;
            transform.position = position;
            SynchronizePath(lanePath, point);
            if (impact != null)
            {
                SetField(impact, "<Damaged>k__BackingField", false);
                impact.kind = TaxiImpactKind.Traffic; impact.targetId = stableId;
            }
            mission = GetComponent<TruckTaxiMissionTarget>();
            mission?.Clear();
            gameObject.SetActive(true);
            SetFullPhysics(full);
        }

        public void SetCruiseSpeed(float speed)
        {
            SetProperty(ai, "MOVE_SPEED", speed);
            SetField(ai, "startSpeed", speed);
        }

        public void SynchronizePath(Component lanePath, int point)
        {
            SetField(path, "walkPath", lanePath);
            path?.GetType().GetMethod("InitStartPosition")?.Invoke(path, new object[] { 0, point, true, true });
            path?.GetType().GetMethod("SetLookPosition")?.Invoke(path, null);
        }

        public void SetFullPhysics(bool full)
        {
            if (body == null) return;
            FullPhysics = full;
            if (!full)
            {
                taxi?.ResetForPool();
                for (int i = 0; i < optionalBehaviours.Length; i++)
                    if (optionalBehaviours[i] is Behaviour behaviour && behaviour != this) behaviour.enabled = false;
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.isKinematic = true; body.useGravity = false;
                foreach (var collider in colliders) if (collider != null) collider.enabled = false;
                foreach (var wheel in wheels) if (wheel != null) wheel.enabled = false;
                foreach (var animator in animators) if (animator != null) animator.enabled = false;
                foreach (var sound in sounds) if (sound != null) sound.Stop();
            }
            else
            {
                body.isKinematic = bodyWasKinematic; body.useGravity = bodyUsedGravity; body.constraints = bodyConstraints;
                for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = colliderDefaults[i];
                for (int i = 0; i < optionalBehaviours.Length; i++)
                    if (optionalBehaviours[i] is Behaviour behaviour && behaviour != this && behaviour != aiBehaviour)
                        behaviour.enabled = optionalDefaults[i];
                for (int i = 0; i < animators.Length; i++) if (animators[i] != null) animators[i].enabled = animatorDefaults[i];
                if (moveBehaviour != null) moveBehaviour.enabled = true;
                // CarAIController is enabled by the adapter on the next frame after Start.
                if (aiBehaviour != null) aiBehaviour.enabled = false;
            }
        }

        public void EnableAi(int generation)
        {
            if (generation == Generation && FullPhysics && gameObject.activeInHierarchy && aiBehaviour != null)
                aiBehaviour.enabled = true;
        }

        public void MoveReduced(Vector3 position, Vector3 forward)
        {
            if (FullPhysics || !gameObject.activeSelf) return;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        }

        private void ResetVendorState()
        {
            if (body != null && !body.isKinematic)
            { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.Sleep(); }
            foreach (var wheel in wheels)
                if (wheel != null) { wheel.motorTorque = 0; wheel.brakeTorque = 0; wheel.steerAngle = 0; }
            SetField(ai, "curMoveSpeed", 0f);
            SetField(ai, "angleBetweenPoint", 0f);
            SetField(ai, "targetSteerAngle", 0f);
            SetField(ai, "upTurnTimer", 0f);
            SetField(ai, "moveBrake", false);
            SetField(ai, "tempStop", false);
            SetField(ai, "insideSemaphore", false);
            SetProperty(ai, "INCREASE", 1.5f);
            SetProperty(ai, "DECREASE", 2.5f);
            SetProperty(ai, "TO_CAR", 28f);
            SetProperty(ai, "TO_SEMAPHORE", 30f);
            SetField(path, "nextFinishPos", Vector3.zero);
            SetField(path, "randXFinish", 0f);
            SetField(path, "randZFinish", 0f);
            SetField(move, "m_GearNum", 0);
            SetField(move, "m_GearFactor", 0f);
            SetField(move, "m_CurrentTorque", 0f);
            SetField(move, "m_OldRotation", transform.eulerAngles.y);
        }

        private static void SetField(Component component, string name, object value) =>
            component?.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(component, value);

        private static void SetProperty(Component component, string name, object value) =>
            component?.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.SetValue(component, value);
    }
}
