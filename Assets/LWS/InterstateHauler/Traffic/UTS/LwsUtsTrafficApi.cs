using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public sealed class LwsUtsTrafficApi
    {
        private readonly Type _walkPathType;
        private readonly Type _carWalkPathType;
        private readonly Type _movePathType;
        private readonly Type _carAiControllerType;
        private readonly Type _carMoveType;
        private readonly Type _carWheelsType;
        private readonly Type _addTrailerType;
        private readonly Type _walkDirectionType;
        private readonly Type _vehiclesAllowType;

        public LwsUtsTrafficApi()
        {
            _walkPathType = ResolveType("WalkPath");
            _carWalkPathType = ResolveType("CarWalkPath");
            _movePathType = ResolveType("MovePath");
            _carAiControllerType = ResolveType("CarAIController");
            _carMoveType = ResolveType("CarMove");
            _carWheelsType = ResolveType("CarWheels");
            _addTrailerType = ResolveType("AddTrailer");
            _walkDirectionType = ResolveType("WalkPath+EnumDir");
            _vehiclesAllowType = ResolveType("VehiclesAllow");
        }

        public bool IsAvailable =>
            _walkPathType != null &&
            _carWalkPathType != null &&
            _movePathType != null &&
            _carAiControllerType != null &&
            _carMoveType != null &&
            _carWheelsType != null;

        public bool WalkPathAvailable => _walkPathType != null;
        public bool CarWalkPathAvailable => _carWalkPathType != null;
        public bool MovePathAvailable => _movePathType != null;
        public bool CarAiControllerAvailable => _carAiControllerType != null;
        public bool CarMoveAvailable => _carMoveType != null;
        public bool CarWheelsAvailable => _carWheelsType != null;

        public string AvailabilitySummary => IsAvailable
            ? "UTS CarWalkPath, MovePath, CarAIController, CarMove, and CarWheels are available."
            : $"Missing UTS types: WalkPath={_walkPathType != null}, CarWalkPath={_carWalkPathType != null}, MovePath={_movePathType != null}, CarAIController={_carAiControllerType != null}, CarMove={_carMoveType != null}, CarWheels={_carWheelsType != null}.";

        public string TypeAvailabilityReport =>
            "UTS Traffic Initialization:\n" +
            $"CarMove: {FormatAvailability(_carMoveType)}\n" +
            $"CarAIController: {FormatAvailability(_carAiControllerType)}\n" +
            $"CarWalkPath: {FormatAvailability(_carWalkPathType)}\n" +
            $"WalkPath: {FormatAvailability(_walkPathType)}\n" +
            $"MovePath: {FormatAvailability(_movePathType)}\n" +
            $"CarWheels: {FormatAvailability(_carWheelsType)}";

        public bool PrefabLooksLikeUtsVehicle(GameObject prefab)
        {
            return TryDescribePrefabSupport(prefab, out _);
        }

        public bool TryDescribePrefabSupport(GameObject prefab, out string message)
        {
            if (prefab == null)
            {
                message = "Prefab rejected: reference is null.";
                return false;
            }

            if (!IsAvailable)
            {
                message = $"{prefab.name} rejected: {AvailabilitySummary}";
                return false;
            }

            Component wheels = FindComponent(prefab, _carWheelsType);
            if (wheels == null)
            {
                message = $"{prefab.name} rejected: CarWheels was not found on the prefab root or children.";
                return false;
            }

            Rigidbody body = wheels.GetComponent<Rigidbody>();
            if (body == null)
            {
                message = $"{prefab.name} rejected: Rigidbody was not found on the CarWheels GameObject.";
                return false;
            }

            Array wheelColliders = GetMember(wheels, "WheelColliders") as Array;
            if (wheelColliders == null || wheelColliders.Length == 0)
            {
                message = $"{prefab.name} rejected: CarWheels has no wheel collider references.";
                return false;
            }

            bool hasPreauthoredCarMove = FindComponent(prefab, _carMoveType) != null;
            bool hasPreauthoredMovePath = FindComponent(prefab, _movePathType) != null;
            bool hasPreauthoredCarAi = FindComponent(prefab, _carAiControllerType) != null;
            message = $"{prefab.name} accepted: CarWheels and Rigidbody are present on {wheels.gameObject.name}; " +
                      $"CarMove={(hasPreauthoredCarMove ? "preauthored" : "added at spawn")}, " +
                      $"MovePath={(hasPreauthoredMovePath ? "preauthored" : "added at spawn")}, " +
                      $"CarAIController={(hasPreauthoredCarAi ? "preauthored" : "added at spawn")}.";
            return true;
        }

        public bool PrefabUsesUtsTrailer(GameObject prefab)
        {
            return prefab != null && _addTrailerType != null && prefab.GetComponentInChildren(_addTrailerType, true) != null;
        }

        public Component CreatePath(GameObject owner, LwsTrafficLaneDefinition lane, GameObject[] prefabs, LwsTrafficSpawnPolicy policy, out string message)
        {
            return CreatePath(owner, lane, prefabs, policy, null, out message);
        }

        public Component CreatePath(
            GameObject owner,
            LwsTrafficLaneDefinition lane,
            GameObject[] prefabs,
            LwsTrafficSpawnPolicy policy,
            Func<Vector3, Vector3> globalToLocal,
            out string message)
        {
            message = string.Empty;
            if (!IsAvailable)
            {
                message = AvailabilitySummary;
                return null;
            }

            if (owner == null || lane == null || prefabs == null || prefabs.Length == 0)
            {
                message = "UTS path creation requires an owner, lane, and traffic prefabs.";
                return null;
            }

            bool restoreActive = owner.activeSelf;
            if (restoreActive)
            {
                owner.SetActive(false);
            }

            try
            {
                Component path = owner.AddComponent(_carWalkPathType);
                SetMember(path, "walkingPrefabs", prefabs);
                SetMember(path, "numberOfWays", 1);
                SetMember(path, "lineSpacing", Mathf.Max(0.6f, lane.laneWidthMeters));
                SetMember(path, "Density", ResolveDensity(policy));
                SetMember(path, "_minimalObjectLength", 8f);
                SetMember(path, "loopPath", true);
                SetMember(path, "highToSpawn", 1.0f);
                SetMember(path, "PathType", ResolveEnum("PathType", "VehiclePath"));
                SetMember(path, "moveSpeed", ResolveTrafficSpeed(lane, policy));
                SetMember(path, "speadDecrease", 2.5f);
                SetMember(path, "speadIncrease", 1.5f);
                SetMember(path, "distanceToCar", 28f);
                SetMember(path, "distanceToSemaphore", 30f);
                SetMember(path, "nextPointThreshold", 4f);
                SetMember(path, "maxAngleToMoveBreak", 8f);
                SetMember(path, "disableLineDraw", true);

                if (_walkDirectionType != null)
                {
                    object forwardDirection = Enum.Parse(_walkDirectionType, "Forward");
                    SetMember(path, "direction", forwardDirection, BindingFlags.Instance | BindingFlags.NonPublic);
                    Invoke(path, "DrawCurved", false, forwardDirection);
                }

                PopulatePathPoints(owner.transform, path, lane, globalToLocal);
                if (_walkDirectionType != null)
                {
                    Invoke(path, "DrawCurved", false, Enum.Parse(_walkDirectionType, "Forward"));
                }
                else
                {
                    Invoke(path, "DrawCurved", false);
                }

                message = $"Created UTS CarWalkPath for {lane.laneId}.";
                return path;
            }
            catch (Exception ex)
            {
                message = $"UTS path creation failed for {lane.laneId}: {ex.GetType().Name}: {ex.Message}";
                return null;
            }
            finally
            {
                if (owner != null && restoreActive)
                {
                    owner.SetActive(true);
                }
            }
        }

        public GameObject SpawnVehicle(
            GameObject prefab,
            Component path,
            LwsTrafficLaneDefinition lane,
            int pointIndex,
            Transform parent,
            LwsTrafficSpawnPolicy policy,
            out string message)
        {
            return SpawnVehicle(prefab, path, lane, pointIndex, parent, policy, null, out message);
        }

        public GameObject SpawnVehicle(
            GameObject prefab,
            Component path,
            LwsTrafficLaneDefinition lane,
            int pointIndex,
            Transform parent,
            LwsTrafficSpawnPolicy policy,
            Func<Vector3, Vector3> globalToLocal,
            out string message)
        {
            message = string.Empty;
            if (!IsAvailable)
            {
                message = AvailabilitySummary;
                return null;
            }

            if (prefab == null || path == null || lane == null || lane.centerline == null || lane.centerline.Length < 3)
            {
                message = "UTS spawn requires a prefab, path, and valid lane.";
                return null;
            }

            if (!TryDescribePrefabSupport(prefab, out string supportMessage))
            {
                message = supportMessage;
                return null;
            }

            pointIndex = Mathf.Clamp(pointIndex, 1, lane.centerline.Length - 2);
            Vector3 spawnPosition = globalToLocal != null
                ? globalToLocal(lane.centerline[pointIndex])
                : lane.centerline[pointIndex];
            GameObject instance = UnityEngine.Object.Instantiate(prefab, spawnPosition, Quaternion.identity, parent);
            instance.name = $"IH UTS Traffic {prefab.name}";
            Component wheels = FindComponent(instance, _carWheelsType);
            GameObject controlledObject = wheels != null ? wheels.gameObject : instance;
            Component movePath = controlledObject.GetComponent(_movePathType) ?? controlledObject.AddComponent(_movePathType);
            if (controlledObject.GetComponent(_carMoveType) == null)
            {
                controlledObject.AddComponent(_carMoveType);
            }

            Component carAi = controlledObject.GetComponent(_carAiControllerType) ?? controlledObject.AddComponent(_carAiControllerType);

            SetMember(movePath, "walkPath", path);
            SetMember(movePath, "_walkPointThreshold", 4f);
            Invoke(movePath, "InitStartPosition", 0, pointIndex, true, true);
            Invoke(movePath, "SetLookPosition");

            SetProperty(carAi, "MOVE_SPEED", ResolveTrafficSpeed(lane, policy));
            SetProperty(carAi, "INCREASE", 1.5f);
            SetProperty(carAi, "DECREASE", 2.5f);
            SetProperty(carAi, "TO_CAR", 28f);
            SetProperty(carAi, "TO_SEMAPHORE", 30f);
            SetProperty(carAi, "MaxAngle", 8f);
            if (_vehiclesAllowType != null)
            {
                SetMember(carAi, "allow", Enum.Parse(_vehiclesAllowType, "Forward"));
            }

            try
            {
                Invoke(carAi, "GetBoxSize");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"UTS traffic vehicle {instance.name} could not precompute box size: {ex.GetType().Name}: {ex.Message}", instance);
            }

            message = $"Spawned UTS traffic vehicle {instance.name} on {lane.laneId}.";
            return instance;
        }

        public float ResolveTrafficSpeed(LwsTrafficLaneDefinition lane, LwsTrafficSpawnPolicy policy)
        {
            float scale = policy != null ? Mathf.Clamp(policy.targetCruiseSpeedScale, 0.2f, 1.1f) : 0.72f;
            float max = policy != null ? Mathf.Max(8f, policy.maximumTrafficSpeedMetersPerSecond) : 22f;
            return Mathf.Min(max, LwsTrafficLaneBuilder.MilesPerHourToMetersPerSecond(lane != null ? lane.speedLimitMph : 45f) * scale);
        }

        public bool RefreshPathPointCache(Component path)
        {
            if (path == null)
            {
                return false;
            }

            IList positions = GetMember(path, "pathPoint") as IList;
            IList transforms = GetMember(path, "pathPointTransform") as IList;
            if (positions == null || transforms == null)
            {
                return false;
            }

            positions.Clear();
            for (int i = 0; i < transforms.Count; i++)
            {
                Transform point = transforms[i] as Transform;
                if (point != null)
                {
                    positions.Add(point.position);
                }
            }

            return true;
        }

        private static void PopulatePathPoints(
            Transform owner,
            Component path,
            LwsTrafficLaneDefinition lane,
            Func<Vector3, Vector3> globalToLocal)
        {
            IList positions = GetMember(path, "pathPoint") as IList;
            IList transforms = GetMember(path, "pathPointTransform") as IList;
            positions?.Clear();
            transforms?.Clear();

            Transform pointsRoot = owner.Find("points");
            if (pointsRoot == null)
            {
                var root = new GameObject("points");
                pointsRoot = root.transform;
                pointsRoot.SetParent(owner, false);
            }

            for (int i = 0; i < lane.centerline.Length; i++)
            {
                var point = new GameObject($"p{i:000}");
                point.transform.SetParent(pointsRoot, false);
                point.transform.position = globalToLocal != null
                    ? globalToLocal(lane.centerline[i])
                    : lane.centerline[i];
                positions?.Add(point.transform.position);
                transforms?.Add(point);
            }
        }

        private static float ResolveDensity(LwsTrafficSpawnPolicy policy)
        {
            if (policy == null)
            {
                return 0.05f;
            }

            switch (policy.densityTier)
            {
                case LwsTrafficDensityTier.Off:
                    return 0.01f;
                case LwsTrafficDensityTier.Dense:
                    return 0.18f;
                case LwsTrafficDensityTier.Normal:
                    return 0.10f;
                default:
                    return 0.04f;
            }
        }

        private static object ResolveEnum(string typeName, string memberName)
        {
            Type type = ResolveType(typeName);
            return type != null && Enum.IsDefined(type, memberName) ? Enum.Parse(type, memberName) : null;
        }

        private static Type ResolveType(string typeName)
        {
            Type type = Type.GetType(typeName) ?? Type.GetType($"{typeName}, Assembly-CSharp");
            if (type != null)
            {
                return type;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static string FormatAvailability(Type type)
        {
            return type != null ? "FOUND" : "MISSING";
        }

        private static Component FindComponent(GameObject owner, Type componentType)
        {
            if (owner == null || componentType == null)
            {
                return null;
            }

            return owner.GetComponent(componentType) ?? owner.GetComponentInChildren(componentType, true);
        }

        private static object GetMember(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            Type type = target.GetType();
            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(target);
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property != null && property.CanRead ? property.GetValue(target) : null;
        }

        private static bool SetProperty(object target, string propertyName, object value)
        {
            if (target == null)
            {
                return false;
            }

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
            {
                return false;
            }

            property.SetValue(target, value);
            return true;
        }

        private static bool SetMember(object target, string memberName, object value, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            if (target == null || value == null)
            {
                return false;
            }

            Type type = target.GetType();
            FieldInfo field = type.GetField(memberName, flags);
            if (field != null)
            {
                field.SetValue(target, value);
                return true;
            }

            PropertyInfo property = type.GetProperty(memberName, flags);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
                return true;
            }

            return false;
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            if (target == null)
            {
                return null;
            }

            MethodInfo method = FindMethod(target.GetType(), methodName, args);
            return method != null ? method.Invoke(target, args) : null;
        }

        private static MethodInfo FindMethod(Type type, string methodName, object[] args)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                bool match = true;
                for (int p = 0; p < parameters.Length; p++)
                {
                    if (args[p] != null && !parameters[p].ParameterType.IsInstanceOfType(args[p]))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return method;
                }
            }

            return null;
        }
    }
}
