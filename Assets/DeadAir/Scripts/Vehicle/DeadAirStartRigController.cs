using System.Collections;
using LWS.InterstateHauler;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-360)]
    [DisallowMultipleComponent]
    public sealed class DeadAirStartRigController : MonoBehaviour
    {
        [SerializeField] private DeadAirStartMarker startMarker;
        [SerializeField] private GameObject playerTruckPrefab;
        [SerializeField] private GameObject deliveryTrailerPrefab;
        [SerializeField] private DeadAirVehicleAdapter playerTruck;
        [SerializeField] private GameObject deliveryTrailer;
        [SerializeField] private bool spawnOnStart;
        [SerializeField] private bool parentRuntimeRigToStartRoot = true;
        [SerializeField] private bool resetRigidbodiesOnRestart = true;
        [SerializeField] private bool requestCouplingOnInitialize = true;
        [SerializeField] private bool considerTrailerConnectedWhileCouplingPending = true;
        [SerializeField, Min(1)] private int couplingRequestAttempts = 3;
        [SerializeField, Min(0f)] private float couplingRetryDelaySeconds = 0.25f;

        private Coroutine _couplingRoutine;
        private bool _trailerConsideredConnected;

        public DeadAirStartMarker StartMarker => startMarker;
        public GameObject PlayerTruckPrefab => playerTruckPrefab;
        public GameObject DeliveryTrailerPrefab => deliveryTrailerPrefab;
        public DeadAirVehicleAdapter PlayerTruck => playerTruck;
        public GameObject DeliveryTrailer => deliveryTrailer;
        public bool TrailerReferenceAvailable => deliveryTrailer != null || deliveryTrailerPrefab != null;
        public bool StartRigConfigured => startMarker != null && (playerTruck != null || playerTruckPrefab != null) && TrailerReferenceAvailable;
        public bool TrailerConsideredConnected => playerTruck != null && playerTruck.TrailerAttachedForDeadAir || _trailerConsideredConnected;
        public bool CouplingRequestEnabled => requestCouplingOnInitialize;

        private void Start()
        {
            if (spawnOnStart)
            {
                InitializeRig();
            }
        }

        public void Configure(GameObject truckPrefab, GameObject trailerPrefab, DeadAirStartMarker marker)
        {
            if (truckPrefab != null)
            {
                playerTruckPrefab = truckPrefab;
            }

            if (trailerPrefab != null)
            {
                deliveryTrailerPrefab = trailerPrefab;
            }

            if (marker != null)
            {
                startMarker = marker;
            }
        }

        public DeadAirVehicleAdapter InitializeRig()
        {
            ResolveReferences();
            playerTruck = EnsureTruck();
            deliveryTrailer = EnsureTrailer();

            if (playerTruck != null)
            {
                ApplyTruckPose(playerTruck.gameObject);
                playerTruck.EnableBasicAutomatic();
            }

            if (deliveryTrailer != null)
            {
                ApplyTrailerPose(deliveryTrailer);
                ConfigureTrailerIdentity(deliveryTrailer);
            }

            if (resetRigidbodiesOnRestart)
            {
                ResetRigidbodyState(playerTruck != null ? playerTruck.gameObject : null);
                ResetRigidbodyState(deliveryTrailer);
            }

            _trailerConsideredConnected = deliveryTrailer != null && considerTrailerConnectedWhileCouplingPending;
            if (playerTruck != null)
            {
                playerTruck.ConfigureDeadAirTrailerState(_trailerConsideredConnected, deliveryTrailer);
            }

            StartCouplingAttempts();
            return playerTruck;
        }

        public DeadAirVehicleAdapter RestartRig()
        {
            return InitializeRig();
        }

        private DeadAirVehicleAdapter EnsureTruck()
        {
            if (playerTruck != null)
            {
                return playerTruck;
            }

            playerTruck = FindFirstObjectByType<DeadAirVehicleAdapter>();
            if (playerTruck != null)
            {
                return playerTruck;
            }

            if (playerTruckPrefab == null)
            {
                return null;
            }

            GetTruckPose(out Vector3 position, out Quaternion rotation);
            Transform parent = ResolveRuntimeParent();
            GameObject truck = Instantiate(playerTruckPrefab, position, rotation, parent);
            truck.name = "Dead Air Player Truck";
            playerTruck = truck.GetComponent<DeadAirVehicleAdapter>();
            if (playerTruck == null)
            {
                playerTruck = truck.AddComponent<DeadAirVehicleAdapter>();
            }

            if (truck.GetComponent<LwsKeyboardGamepadTruckInputSource>() == null)
            {
                truck.AddComponent<LwsKeyboardGamepadTruckInputSource>();
            }

            return playerTruck;
        }

        private GameObject EnsureTrailer()
        {
            if (deliveryTrailer != null)
            {
                return deliveryTrailer;
            }

            deliveryTrailer = FindExistingDeadAirTrailer();
            if (deliveryTrailer != null)
            {
                return deliveryTrailer;
            }

            if (deliveryTrailerPrefab == null)
            {
                return null;
            }

            GetTrailerPose(out Vector3 position, out Quaternion rotation);
            Transform parent = ResolveRuntimeParent();
            deliveryTrailer = Instantiate(deliveryTrailerPrefab, position, rotation, parent);
            deliveryTrailer.name = "Dead Air Delivery Trailer";
            return deliveryTrailer;
        }

        private void ApplyTruckPose(GameObject truck)
        {
            if (truck == null)
            {
                return;
            }

            GetTruckPose(out Vector3 position, out Quaternion rotation);
            truck.transform.SetPositionAndRotation(position, rotation);
        }

        private void ApplyTrailerPose(GameObject trailer)
        {
            if (trailer == null)
            {
                return;
            }

            GetTrailerPose(out Vector3 position, out Quaternion rotation);
            trailer.transform.SetPositionAndRotation(position, rotation);
        }

        private void GetTruckPose(out Vector3 position, out Quaternion rotation)
        {
            if (startMarker != null)
            {
                startMarker.GetTruckPose(out position, out rotation);
                return;
            }

            position = transform.position;
            rotation = transform.rotation;
        }

        private void GetTrailerPose(out Vector3 position, out Quaternion rotation)
        {
            if (startMarker != null)
            {
                startMarker.GetTrailerPose(out position, out rotation);
                return;
            }

            position = transform.position - transform.forward * 13.5f;
            rotation = transform.rotation;
        }

        private Transform ResolveRuntimeParent()
        {
            if (!parentRuntimeRigToStartRoot || startMarker == null)
            {
                return null;
            }

            return startMarker.transform.parent;
        }

        private void StartCouplingAttempts()
        {
            if (_couplingRoutine != null)
            {
                StopCoroutine(_couplingRoutine);
                _couplingRoutine = null;
            }

            if (!requestCouplingOnInitialize || playerTruck == null || deliveryTrailer == null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            _couplingRoutine = StartCoroutine(RequestCouplingUntilAttached());
        }

        private IEnumerator RequestCouplingUntilAttached()
        {
            yield return new WaitForFixedUpdate();
            for (int attempt = 0; attempt < couplingRequestAttempts; attempt++)
            {
                if (playerTruck == null)
                {
                    yield break;
                }

                if (playerTruck.TrailerAttachedForDeadAir)
                {
                    _trailerConsideredConnected = true;
                    playerTruck.ConfigureDeadAirTrailerState(true, deliveryTrailer);
                    yield break;
                }

                playerTruck.RequestTrailerAttachDetach();
                yield return new WaitForSeconds(couplingRetryDelaySeconds);
            }

            if (playerTruck != null)
            {
                _trailerConsideredConnected = playerTruck.TrailerAttachedForDeadAir || deliveryTrailer != null && considerTrailerConnectedWhileCouplingPending;
                playerTruck.ConfigureDeadAirTrailerState(_trailerConsideredConnected, deliveryTrailer);
            }
        }

        private static void ResetRigidbodyState(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.Sleep();
            }
        }

        private static void ConfigureTrailerIdentity(GameObject trailer)
        {
            if (trailer == null)
            {
                return;
            }

            LwsVehicleIdentity identity = trailer.GetComponent<LwsVehicleIdentity>();
            if (identity == null)
            {
                identity = trailer.AddComponent<LwsVehicleIdentity>();
            }

            identity.Configure(
                "deadair.delivery.trailer",
                "deadair.trailer.delivery",
                LwsVehicleRole.Trailer,
                "Dead Air Delivery Trailer",
                false);
        }

        private static GameObject FindExistingDeadAirTrailer()
        {
            LwsVehicleIdentity[] identities = FindObjectsByType<LwsVehicleIdentity>(FindObjectsSortMode.None);
            foreach (LwsVehicleIdentity identity in identities)
            {
                if (identity != null && identity.Role == LwsVehicleRole.Trailer && identity.VehicleId == "deadair.delivery.trailer")
                {
                    return identity.gameObject;
                }
            }

            GameObject named = GameObject.Find("Dead Air Delivery Trailer");
            return named;
        }

        private void ResolveReferences()
        {
            if (startMarker == null)
            {
                startMarker = DeadAirBeatLayoutUtility.FindPreferredRuntimeStartMarker();
            }

            if (playerTruck == null)
            {
                playerTruck = FindFirstObjectByType<DeadAirVehicleAdapter>();
            }
        }
    }
}
