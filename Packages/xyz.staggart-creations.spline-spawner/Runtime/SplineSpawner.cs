// Spline Spawner © Staggart Creations (http://staggart.xyz)
// COPYRIGHT PROTECTED UNDER THE UNITY ASSET STORE EULA (https://unity.com/legal/as-terms)
//
// ⚠️ WARNING: UNAUTHORIZED USE OR DISTRIBUTION IS STRICTLY PROHIBITED
// • Copying, referencing, or reverse-engineering this source code for the creation of new Asset Store or derivative products,
//   or any other publicly distributed content is strictly forbidden and will result in legal action.
// • Studying this file for the purpose of reproducing its functionality in your own assets or tools is not permitted.
// • If you are viewing this file as a reference, please close it immediately to avoid unintentional design influence or potential EULA violations.
// • Uploading this file or any derivative of it to a public GitHub or similar repository will trigger an automated DMCA takedown request.
// • Studying to understand for personal, educational or integration purposes is allowed, studying to reproduce is not.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace sc.splines.spawner.runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [Icon(kPackageRoot + "/Editor/Resources/Icons/spline-spawner-icon-64px.psd")]
    [SelectionBase] //Select this object when selecting spawned objects instead
    [HelpURL("https://staggart.xyz/support/documentation/spline-spawner/")]
    public partial class SplineSpawner : MonoBehaviour, IDisposable
    {
        public const string kPackageRoot = "Packages/xyz.staggart-creations.spline-spawner";
        public const int CAPACITY = 8192;
        private const string PROFILER_PREFIX = "Spline Spawner: ";
        
        public const float MIN_CURVE_LENGTH = 0.25f;
        public const float MIN_OBJECT_SIZE = 0.05f;
        
        [Tooltip("The spline container that defines the curves for spawning")]
        [SerializeField]
        private SplineContainer splineContainer;
        public SplineContainer SplineContainer
        {
            get => splineContainer;
            set => SetSplineContainer(value);
        }
        
        [Flags]
        public enum RespawnTriggers
        {
            [InspectorName("Via scripting")]
            None = 0,
            [InspectorName("On Spline Change")]
            OnSplineChanged = 1,
            OnSplineAdded = 2,
            OnSplineRemoved = 4,
            OnUIChange = 16,
            OnTransformChange = 32,
        }

        [Tooltip("Control which sort of events cause a respawn operation." +
                 "\n\n" +
                 "For instance when the spline changes (default) or is moved." +
                 "\n\n" +
                 "If none are selected you need to call the Rebuild() function through script.")]
        public RespawnTriggers respawnTriggers = RespawnTriggers.OnSplineAdded | RespawnTriggers.OnSplineRemoved | RespawnTriggers.OnSplineChanged | RespawnTriggers.OnUIChange | RespawnTriggers.OnTransformChange;

        public bool RespawnTriggerEnabled(RespawnTriggers trigger)
        {
            return (respawnTriggers & trigger) != 0;
        }
        
        [Tooltip("The root transform under which spawned objects will be placed")]
        public Transform root;

        [Tooltip("Whether to hide spawned instances in the hierarchy")]
        public bool hideInstances;

        [Serializable]
        //Managed, front end
        public class SpawnableObject
        {
            public GameObject prefab;

            public enum Pivot
            {
                Original,
                Center,
                Back
            }
            public Pivot pivot;
            public ForwardDirection forwardDirection = ForwardDirection.PositiveZ;

            [Min(0)]
            public float selectBySplineDistance;
            [Range(0f, 100f)]
            public float probability = 100f;
            public Vector3 baseScale = Vector3.one;

            [NonSerialized]
            public Texture thumbnail;

            public SpawnableObject(GameObject prefab, float probability = 100f)
            {
                this.prefab = prefab;
                this.probability = probability;
            }

            public SpawnableObject() { }

            public enum ForwardDirection
            {
                [InspectorName("+X")]
                PositiveX,
                [InspectorName("-X")]
                NegativeX,
                [InspectorName("+Y")]
                PositiveY,
                [InspectorName("-Y")]
                NegativeY,
                [InspectorName("+Z")]
                PositiveZ,
                [InspectorName("-Z")]
                NegativeZ,
            }
        }

        public enum StartupBehaviour
        {
            None,
            Respawn,
            RespawnRandomized
        }
        [Tooltip("Action executed in Start()")]
        public StartupBehaviour startupBehaviour = StartupBehaviour.None;

        [UnityEngine.Serialization.FormerlySerializedAs("prefabs")]
        [Tooltip("List of prefab objects to spawn along the spline")]
        public List<SpawnableObject> inputObjects = new List<SpawnableObject>();

        /// <summary>
        /// Settings for all of the distribution modes. This class contains several sub-classes, one for each mode.
        /// </summary>
        public DistributionSettings distributionSettings = new DistributionSettings();
        
        [Tooltip("List of modifiers that affect the spawned objects")]
        [SerializeReference]
        public List<Modifier> modifiers = new List<Modifier>();

        [SerializeField]
        private List<SplineInstanceContainer> containers = new List<SplineInstanceContainer>();

        public int InstanceCount { get; private set; }

        private NativeList<PrefabData> prefabData;
        private NativeList<SpawnPoint> spawnPoints;

#pragma warning disable CS0067
        public delegate void Action(SplineSpawner instance, int splineIndex);

        /// <summary>
        /// Pre- and post-respawn callbacks. The instance being passed is the Spline Spawner being respawned.
        /// </summary>
        public static event Action onPreRespawn, onPostRespawn;

        public delegate void SpawnPointModifyEvent(NativeList<SpawnPoint> spawnPoints);
        public event SpawnPointModifyEvent onAfterDistribution, onAfterModifiers;
#pragma warning restore CS0067

        /// <summary>
        /// Disable only when using the created spawn points and instantiating objects using custom code!
        /// </summary>
        [Tooltip("Whether to actually spawn GameObjects (disable to use spawn points only)")]
        public bool spawnObjects = true;

        private readonly List<string> warnings = new();
        public List<string> Warnings => warnings;

        private void UpdatePrefabData()
        {
            if (prefabData.IsCreated) prefabData.Dispose();

            prefabData = new NativeList<PrefabData>(Allocator.Persistent);

            for (int i = 0; i < inputObjects.Count; i++)
            {
                SpawnableObject input = inputObjects[i];

                PrefabData pd = PrefabData.Create(input, i);

                if (input.prefab)
                {
                    SplineSpawnerMask[] masks = input.prefab.GetComponentsInChildren<SplineSpawnerMask>(true);

                    if (masks.Length > 0)
                    {
                        throw new Exception($"Prefab \"{input.prefab.name}\" contains {masks.Length} spline spawner mask(s). Masks may never be spawned by a Spline Spawner component, as this can lead to infinite respawning loops.");
                    }

                    prefabData.Add(pd);
                }
            }
        }

        private void Reset()
        {
            splineContainer = GetComponentInParent<SplineContainer>();
            root = this.transform;
            
#if UNITY_EDITOR
            SplineSpawner component = GetComponent<SplineSpawner>();
            if (component && component != this && component.root == root)
            {
                UnityEditor.EditorUtility.DisplayDialog("Spline Spawner", $"Unable to add a Spline Spawner component. Another Spline Spawner is already targeting to the same root transform ({root.name})." +
                                                                          $"\n\n" +
                                                                          $"Multiple Spline Spawner components cannot use the same Root transform: They’d both detect rogue objects and will try to adopt them, leading to a continuous cycle of errors.", "OK");
                EditorApplication.delayCall += () => DestroyImmediate(this);
                return;
            }
#endif

            Validate();

            InstanceCount = 0;
        }

        private partial void SubscribeSplineCallbacks();

        private partial void UnsubscribeSplineCallbacks();

        private void Start()
        {
            if (startupBehaviour == StartupBehaviour.None) return;
            
            if(startupBehaviour == StartupBehaviour.RespawnRandomized) distributionSettings.RandomizeSeed();
            if (startupBehaviour == StartupBehaviour.RespawnRandomized || startupBehaviour == StartupBehaviour.Respawn)
            {
                Respawn();
            }
        }

        private void OnEnable()
        {
            SubscribeSplineCallbacks();
            SubscribeToTerrainEvents();
        }

        private void OnDisable()
        {
            UnsubscribeSplineCallbacks();
            UnsubscribeFromTerrainEvents();

            Dispose();
        }

        public void Validate()
        {
            ValidateContainers();
            
            EnsureTerrainResources();
        }
        
        private SpawnOnCurve spawnOnCurveJob;
        private SpawnInArea spawnInSplineJob;
        private SpawnOnKnots spawnOnKnotsJob;
        private RadialInsideSpline radialJob;
        private SpawnOnGrid gridJob;

        SplineCache splineCache = new SplineCache();
        private JobHandle spawnPointJobHandle;

        private readonly System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
        public float lastDistributionTime { get; private set; }
        public float lastMaskingTime { get; private set; }
        public float lastTerrainLayerTime { get; private set; }
        public float lastModifierStackTime { get; private set; }
        public float lastInstantiateTime { get; private set; }

        public float LastRespawnTime => lastDistributionTime + lastMaskingTime + lastTerrainLayerTime + lastModifierStackTime + lastInstantiateTime;

        /// <summary>
        /// Respawns from all splines using the current settings
        /// </summary>
        public void Respawn()
        {
            if (!splineContainer || !IsAllowedToSpawn()) return;

            //if(Application.isPlaying) Debug.Log($"{this.name} respawned", this);
            
            splineCount = splineContainer.Splines.Count;

            //If the container transform was altered, the cached native splines will be required to update
            if (splineCount != nativeSplines.Count || splineContainer.transform.hasChanged)
            {
                splineContainer.transform.hasChanged = false;
                
                RebuildSplineCache();
            }

            for (int splineIndex = 0; splineIndex < splineCount; splineIndex++)
            {
                Respawn(splineIndex);
            }
        }
        
        /// <summary>
        /// Respawn using a specific spline
        /// </summary>
        /// <param name="splineIndex"></param>
        public void Respawn(int splineIndex)
        {
            //if (splineIndex >= containers.Count)
            {
                //Debug.LogError($"No container present for spline #{splineIndex}.");
                Validate();
                //return;
            }
            
            if (HasCachedSpline(splineIndex) == false) return;

            //Required, since disabling the component disposes of resources. But disabling a Mask may attempt to respawn this instance, leading to NativeArray leaks
            if (this.enabled == false) return;
            
            if (IsAllowedToSpawn() == false) return;
            
            onPreRespawn?.Invoke(this, splineIndex);
            
            SplineInstanceContainer container = containers[splineIndex];

            #if UNITY_EDITOR
            var flags = GameObjectUtility.GetStaticEditorFlags(this.gameObject);
            GameObjectUtility.SetStaticEditorFlags(container.gameObject, flags);
            #endif

            if (inputObjects.Count == 0)
            {
                Debug.LogWarning("Cannot spawn anything, 0 prefabs assigned...", this);
                return;
            }

            if (spawnPointJobHandle.IsCompleted == false)
            {
                Debug.LogWarning("Previous spawning job hasn't completed yet...", this);
                return;
            }

            Profiler.BeginSample($"{PROFILER_PREFIX} Setup");
            {
                InstanceCount -= container.InstanceCount;
                InstanceCount = Mathf.Max(InstanceCount, 0);

                //Delete current first, to ensure no colliders are in the way
                container.DestroyInstances();

                UpdatePrefabData();

                //No prefab objects assigned
                if (prefabData.Length == 0)
                {
                    Debug.LogWarning($"None of the {inputObjects.Count} assigned objects were suitable for spawning. Either no prefabs were assigned, or they are miniscule in size...", this);
                    Profiler.EndSample();

                    return;
                }
                
                if (spawnPoints.IsCreated == false)
                {
                    spawnPoints = new NativeList<SpawnPoint>(CAPACITY, Allocator.Persistent);
                }
                else
                {
                    spawnPoints.Clear();
                }
            }
            Profiler.EndSample();

            #region Distribution
            stopWatch.Restart();
            int knotCount = splineContainer.Splines[splineIndex].Count;

            Profiler.BeginSample($"{PROFILER_PREFIX} Distribution ({distributionSettings.mode})");
            {
                NativeBounds splineBounds = this.splineBounds[splineIndex];
                NativeSpline spline = nativeSplines[splineIndex];

                DistributionSettings.Accuracy accuracy = DistributionSettings.Accuracy.HighestAccuracy;
                if (distributionSettings.mode == DistributionSettings.DistributionMode.InsideArea)
                    accuracy = distributionSettings.insideArea.borderAccuracy;
                else if (distributionSettings.mode == DistributionSettings.DistributionMode.Radial)
                    accuracy = distributionSettings.radial.borderAccuracy;
                else if (distributionSettings.mode == DistributionSettings.DistributionMode.Grid)
                    accuracy = distributionSettings.grid.accuracy;
                
                splineCache.Create(spline, 0.5f, accuracy);

                warnings.Clear();
                
                //Debug.Log($"Calculate bounds: Center: {splineBounds.center}, Size: {splineBounds.size}. Length: {nativeSplines[splineIndex].GetLength()}");
                
                if (distributionSettings.mode == DistributionSettings.DistributionMode.OnCurve)
                {
                    if (spline.GetLength() < MIN_CURVE_LENGTH || knotCount <= 1)
                    {
                            warnings.Add($"No spawn points were generated for Spline #{splineIndex}.\n\nThe spline's length ({spline.GetLength()}) is less than 1m long or only has 1 knot");
                            return;
                    }
                    
                    //Spawn on spline
                    spawnOnCurveJob = new SpawnOnCurve(spline, splineCache.Points, splineContainer.transform.localToWorldMatrix, distributionSettings, prefabData, ref spawnPoints);

                    spawnPointJobHandle = spawnOnCurveJob.Schedule();
                }
                else if (distributionSettings.mode == DistributionSettings.DistributionMode.InsideArea)
                {
                    if (knotCount < 3)
                    {
                        warnings.Add($"Cannot spawn within Spline #{splineIndex} area. It requires more than 3 knots (has {knotCount}).");
                        return;
                    }

                    //Safety clamping
                    distributionSettings.insideArea.spacing = Mathf.Max(0.5f, distributionSettings.insideArea.spacing);
                    
                    spawnInSplineJob = new SpawnInArea(spline, splineCache.Points, splineBounds, distributionSettings, prefabData, ref spawnPoints);

                    spawnPointJobHandle = spawnInSplineJob.Schedule();
                }
                else if (distributionSettings.mode == DistributionSettings.DistributionMode.Radial)
                {
                    if (knotCount < 3)
                    {
                        warnings.Add($"Cannot spawn within Spline #{splineIndex}. It requires more than 3 knots (has {knotCount}).");
                        return;
                    }

                    radialJob = new RadialInsideSpline(spline, splineCache.Points, splineContainer.transform.localToWorldMatrix, distributionSettings, prefabData, ref spawnPoints);
                    spawnPointJobHandle = radialJob.Schedule();
                }
                else if (distributionSettings.mode == DistributionSettings.DistributionMode.Grid)
                {
                    if (knotCount < 3)
                    {
                        warnings.Add($"Cannot spawn within Spline #{splineIndex}. It requires more than 3 knots (has {knotCount}).");
                        return;
                    }

                    gridJob = new SpawnOnGrid(spline, splineCache.Points, splineContainer.transform.localToWorldMatrix, distributionSettings, prefabData, ref spawnPoints);
                    spawnPointJobHandle = gridJob.Schedule();
                }
                else if (distributionSettings.mode == DistributionSettings.DistributionMode.OnKnots)
                {
                    if (knotCount <= 1)
                    {
                        warnings.Add($"Cannot spawn within Spline #{splineIndex}. It requires more than 1 knot (has {knotCount})");
                        return;
                    }
                    
                    spawnOnKnotsJob = new SpawnOnKnots(spline, splineContainer, splineIndex, splineContainer.transform.localToWorldMatrix, distributionSettings, prefabData, ref spawnPoints);

                    spawnPointJobHandle = spawnOnKnotsJob.Schedule();
                }

                //Complete and sync
                spawnPointJobHandle.Complete();

                if (distributionSettings.mode == DistributionSettings.DistributionMode.OnCurve)
                {
                    spawnOnCurveJob.Dispose();
                }
                else if (distributionSettings.mode == DistributionSettings.DistributionMode.InsideArea)
                {
                    spawnInSplineJob.Dispose();

                    if (spawnPoints.Length == 0)
                    {
                        warnings.Add($"No spawn points were generated for Spline #{splineIndex}.\n\nThe spline area is likely too small to fit any objects, the spacing too large" +
                                   $" or the Border Accuracy is too low.");
                    }
                }
                else if (distributionSettings.mode == DistributionSettings.DistributionMode.Radial)
                {
                    radialJob.Dispose();
                }
                else if (distributionSettings.mode == DistributionSettings.DistributionMode.Grid)
                {
                    gridJob.Dispose();
                }
                else if (distributionSettings.mode == DistributionSettings.DistributionMode.OnKnots)
                {
                    spawnOnKnotsJob.Dispose();
                }
            }
            Profiler.EndSample();

            onAfterDistribution?.Invoke(spawnPoints);

            stopWatch.Stop();
            lastDistributionTime = (float)stopWatch.Elapsed.TotalMilliseconds;
            #endregion

            //Masking

            stopWatch.Restart();

            ProcessMasks();

            stopWatch.Stop();

            lastMaskingTime = (float)stopWatch.Elapsed.TotalMilliseconds;

            //Terrain
            stopWatch.Restart();

            ProcessTerrainLayers();

            stopWatch.Stop();

            lastTerrainLayerTime = (float)stopWatch.Elapsed.TotalMilliseconds;
            
            //Modifier stack
            stopWatch.Restart();

            Profiler.BeginSample($"{PROFILER_PREFIX} Modifier Stack");
            for (int j = 0; j < modifiers.Count; j++)
            {
                if (modifiers[j].enabled == false) continue;

                JobHandle jobHandle = modifiers[j].CreateJob(this, ref spawnPoints);
                jobHandle.Complete();
            }
            Profiler.EndSample();

            onAfterModifiers?.Invoke(spawnPoints);

            stopWatch.Stop();
            lastModifierStackTime = (float)stopWatch.Elapsed.TotalMilliseconds;

            stopWatch.Restart();
            if (spawnObjects)
            {
                Profiler.BeginSample($"{PROFILER_PREFIX} Instantiating");

                SpawnObjects(splineIndex);

                Profiler.EndSample();

            }
            stopWatch.Stop();
            lastInstantiateTime = (float)stopWatch.Elapsed.TotalMilliseconds;

            if (container.InstanceCount == 0)
            {
                //Debug.LogWarning("Spawning amounted to 0 instances");
            }

            splineCache.Dispose();
            DisposeJobs();

            onPostRespawn?.Invoke(this, splineIndex);
        }

        /// <summary>
        /// Adds the GameObject (prefab or not) as a spawnable object
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="probability"></param>
        public SpawnableObject AddObject(GameObject prefab, float probability = 100f)
        {
            SpawnableObject spawnableObject = new SpawnableObject(prefab, probability);
            
            inputObjects.Add(spawnableObject);

            return spawnableObject;
        }
        
        /// <summary>
        /// Removes an object from the list of spawnable objects at the given index
        /// </summary>
        /// <param name="index"></param>
        public void RemoveObject(int index)
        {
            if (inputObjects == null || index < 0 || index >= inputObjects.Count)
            {
                Debug.LogWarning($"RemoveObject: Index {index} is out of range.");
                return;
            }

            inputObjects.RemoveAt(index);
        }

        private void SpawnObjects(int splineIndex)
        {
            #if UNITY_EDITOR
            if (root && EditorUtility.IsPersistent(root))
            {
                Debug.LogWarning($"[Spline Spawner] Cannot spawn objects for Spline #{splineIndex}. The root object ({root.name}) does not exist in the scene.");
                return;
            }
            #endif
            
            int rootStaticFlags = 0;
            bool applyRootStaticFlags = root && root.gameObject.isStatic;
            if (applyRootStaticFlags)
            {
#if UNITY_EDITOR
                rootStaticFlags = (int)GameObjectUtility.GetStaticEditorFlags(root.gameObject);
#endif
            }

            SplineInstanceContainer container = containers[splineIndex];
            int spawnPointCount = spawnPoints.Length;
            
            //Unsafe context is used to avoid the overhead of accessing a native array item
            unsafe
            {
                SpawnPoint* spawnPointPtr = (SpawnPoint*)spawnPoints.GetUnsafeReadOnlyPtr();

                for (int index = 0; index < spawnPointCount; index++)
                {
                    SpawnPoint* point = spawnPointPtr + index;

                    if (point->isValid == false)
                        continue;
                    
#if UNITY_EDITOR
                    if (float.IsNaN(point->position.x))
                    {
                        Debug.LogError($"[Spline Spawner] Spawnpoint at index #{index} is NaN.");
                        continue;
                    }
#endif

                    int prefabIndex = point->prefabIndex;
                    SpawnableObject prefab = inputObjects[prefabIndex];
                    
                    container.SpawnObject(*point, prefab.prefab, root, hideInstances, applyRootStaticFlags, rootStaticFlags);

                    InstanceCount++;
                }
            }
        }
        
        private void DisposeJobs()
        {
            for (int j = 0; j < modifiers.Count; j++)
            {
                modifiers[j].Dispose();
            }
            
            prefabData.Dispose();
        }
        
        /// <summary>
        /// Dispose of any allocated resources/instances when finished with any spawning. This is normally done when the component is disabled.
        /// </summary>
        public void Dispose()
        {
            if(spawnPoints.IsCreated) spawnPoints.Dispose();
            if(prefabData.IsCreated) prefabData.Dispose();
            
            //Mark as disposed
            spawnPoints = default;
            prefabData = default;
            
            ClearObjectPools();
        }
        
        public void AddModifier(Modifier modifier)
        {
            modifiers.Add(modifier);
        }

        public void RemoveModifier(Modifier modifier)
        {
            modifiers.Remove(modifier);
        }
        
        public void RemoveModifier(int index)
        {
            modifiers.RemoveAt(index);
        }

        public Modifier AddModifier(Type type)
        {
            Modifier modifier = Modifier.Create(type);

            modifiers.Add(modifier);

            return modifier;
        }
        
        public Modifier AddModifier(string typeName)
        {
            Type type = Type.GetType(typeName);

            return AddModifier(type);
        }

        public void OverwriteSpawnpoints(NativeList<SpawnPoint> points)
        {
            this.spawnPoints.CopyFrom(points);
        }
        
        public static void WarmUpAllObjectPools(int capacity = 1000)
        {
#if UNITY_6000_4_OR_NEWER
            SplineSpawner[] instances = FindObjectsByType<SplineSpawner>(FindObjectsInactive.Exclude);
#else
            SplineSpawner[] instances = FindObjectsByType<SplineSpawner>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#endif

            for (int i = 0; i < instances.Length; i++)
            {
                instances[i].WarmUpObjectPools(capacity);
            }
        }
        
        /// <summary>
        /// Objects are spawning using Object Pooling, this can lead to a ~20 slower first respawn. Use this function to pre-initialize the object pool for each spline
        /// </summary>
        /// <param name="capacity"></param>
        public void WarmUpObjectPools(int capacity = 1000)
        {
            foreach (SplineInstanceContainer container in containers)
            {
                if(!container) continue;
                
                foreach (var inputObject in inputObjects)
                {
                    if(inputObject.prefab) container.WarmUpObjectPool(inputObject.prefab, capacity);
                }
            }
        }

        /// <summary>
        /// Clears all object pools. Use this to ensure no unused objects are left in memory. Also called in Dispose().
        /// </summary>
        [ContextMenu("Clear Object Pools")]
        public void ClearObjectPools()
        {
            foreach (SplineInstanceContainer container in containers)
            {
                if(container) container.ClearUnusedObjects();
            }
        }

        public int CountInstances()
        {
            InstanceCount = 0;
            foreach (SplineInstanceContainer container in containers)
            {
                if(container) InstanceCount += container.InstanceCount;
            }
            
            return InstanceCount;
        }
        
        [NonSerialized]
        private bool firstTransformChange = true;
        /// <summary>
        /// Checks for changes to the Spline Container or Root's transform. If so, a rebuild is triggered.
        /// </summary>
        public void ListenForTransformChanges()
        {
            if (!RespawnTriggerEnabled(RespawnTriggers.OnTransformChange)) return;
            
            var hasSplineChange = false;
            if (splineContainer)
            {
                hasSplineChange = splineContainer.transform.hasChanged;
                splineContainer.transform.hasChanged = false;
            }
            
            var hasRootChange = false;
            if (root)
            {
                hasRootChange = root.hasChanged;
                root.hasChanged = false;
            }
            
            if (!firstTransformChange && (hasSplineChange || hasRootChange))
            {
                if(hasSplineChange) RebuildSplineCache();
                Respawn();
                
                //Debug.Log($"[Spline Spawner] {name} rebuilt due to transform change. Root:{hasRootChange}. Spline:{hasSplineChange}", this);
            }

            if (firstTransformChange) firstTransformChange = false;
        }

        private void OnDrawGizmosSelected()
        {
            ListenForTransformChanges();
            
            /*
            if (bounds != null)
            {
                foreach (NativeBounds m_bounds in bounds)
                {
                    Gizmos.DrawWireCube(m_bounds.center, m_bounds.size);
                }
            }
            */
        }
    }
}