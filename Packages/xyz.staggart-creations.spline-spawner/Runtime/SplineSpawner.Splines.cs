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
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Splines;

#if !SPLINES
#error The Splines package has not been installed, or does not meet the minimum version requirement. The Spline Spawner package relies on this to be the case. To resolve this error install or update the Splines package through the Package Manager.
#endif

namespace sc.splines.spawner.runtime
{
    public partial class SplineSpawner : MonoBehaviour
    {
        [SerializeField]
        private int splineCount; //Change tracking
        /// <summary>
        /// The number of tracked and cached Splines
        /// </summary>
        public int SplineCount => splineCount;
        private int containerID;
        
        //Creating a NativeSpline is costly, and isn't necessary if only spawning parameters are changed
        //Hence they are cached and rebuild when they change.
        private List<NativeSpline> nativeSplines = new List<NativeSpline>();
        public readonly List<NativeBounds> splineBounds = new List<NativeBounds>();
        private const float BOUNDS_SAMPLE_DISTANCE = 2f;

        public enum SplineChangeTrigger
        {
            [InspectorName("Manually")]
            None,
            DuringSplineChanges,
            AfterSplineChanges,
        }
        
        [UnityEngine.Serialization.FormerlySerializedAs("respawningMode")] [Tooltip("When respawning should occur. For best performance in the editor, opt to use \"AfterSplineChanges\"")]
        public SplineChangeTrigger splineChangeTrigger = SplineChangeTrigger.DuringSplineChanges;
        
        private partial void SubscribeSplineCallbacks()
        {
            SplineContainer.SplineAdded += OnSplineAdded;
            SplineContainer.SplineRemoved += OnSplineRemoved;
            Spline.Changed += OnSplineChanged;

            RebuildSplineCache();
        }

        private partial void UnsubscribeSplineCallbacks()
        {
            SplineContainer.SplineAdded -= OnSplineAdded;
            SplineContainer.SplineRemoved -= OnSplineRemoved;
            Spline.Changed -= OnSplineChanged;
            
            DisposeSplineCache();
        }

        /// <summary>
        /// Spline are converted to native arrays for fast parallel read access. The conversion process is fairly slow, use this function to perform it for all splines within the assigned container.
        /// Doing so before respawning makes the first respawning job faster
        /// </summary>
        public void WarmupSplineCache()
        {
            RebuildSplineCache();
        }

        /// <summary>
        /// Sets the source spline container and forces the cache to be rebuilt.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="forceCacheRebuild">Force the cache for all the splines to be rebuilt</param>
        public void SetSplineContainer(SplineContainer container, bool forceCacheRebuild = true)
        {
            this.splineContainer = container;
            if(forceCacheRebuild) RebuildSplineCache();
        }
        
        /// <summary>
        /// Disposes and rebuilds the cached spline data
        /// </summary>
        [ContextMenu("Rebuild Spline Cache")]
        public void RebuildSplineCache()
        {
            if (!splineContainer) return;
            
            //When first adding the component, ensure count is updated
            splineCount = splineContainer.Splines.Count;

            DisposeSplineCache();
            
            nativeSplines = new List<NativeSpline>();
            
            foreach (var spline in splineContainer.Splines)
            {
                CacheSpline(spline);
            }
        }
        
        internal bool HasCachedSpline(int splineIndex)
        {
            //If calling this function directly, yet the component is configured for only manual rebuilding
            //Then no native spline will have been created automatically
            if (RespawnTriggerEnabled(RespawnTriggers.OnSplineChanged) == false || RespawnTriggerEnabled(RespawnTriggers.OnSplineAdded) == false)
            {
                if (splineIndex >= nativeSplines.Count)
                {
                    Debug.LogError($"[Spline Curve Mesher] Manual rebuilding in use. Spline #{splineIndex} in {splineContainer.name} does not exist in cache. Ensure you have added the spline through the AddCacheSpline function.", this);
                    return false;
                }
            }

            return true;
        }
        
        private void DisposeSplineCache()
        {
            if (nativeSplines != null)
            {
                foreach (var nativeSpline in nativeSplines)
                {
                    nativeSpline.Dispose();
                }
                nativeSplines.Clear();
                splineBounds.Clear();
            }
        }

        private void RemoveSpline(int index)
        {
            if (index >= nativeSplines.Count) return;
            
            nativeSplines[index].Dispose();
            nativeSplines.RemoveAt(index);
            
            splineBounds.RemoveAt(index);
        }

        private NativeSpline CreateNativeSpline(ISpline spline)
        {
            return new NativeSpline(spline, spline.Closed, splineContainer.transform.localToWorldMatrix, true, Allocator.Persistent);
        }
        
        private void UpdateSpline(Spline spline, int index)
        {
            if (index >= nativeSplines.Count) return;
            
            nativeSplines[index].Dispose();
            nativeSplines[index] = CreateNativeSpline(spline);
            
            splineBounds[index] = NativeBounds.Create(nativeSplines[index], BOUNDS_SAMPLE_DISTANCE);
        }
        
        private void CacheSpline(Spline spline)
        {
            NativeSpline nativeSpline = CreateNativeSpline(spline);
            nativeSplines.Add(nativeSpline);
            
            NativeBounds nativeBounds = NativeBounds.Create(nativeSpline, BOUNDS_SAMPLE_DISTANCE);
            splineBounds.Add(nativeBounds);
        }
        
        private Spline lastEditedSpline;
        private int lastEditedSplineIndex = -1;
        
        private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
        {
            if (!splineContainer) return;

            if (respawnTriggers.HasFlag(RespawnTriggers.OnSplineChanged) == false) return;

            //Spline belongs to the assigned container?
            var splineIndex = Array.IndexOf(splineContainer.Splines.ToArray(), spline);
            if (splineIndex < 0)
                return;

            splineCount = splineContainer.Splines.Count;
            
            int hashCode = splineContainer.GetHashCode();
            if (hashCode != containerID)
            {
                ValidateContainers();
            }
            containerID = splineContainer.GetHashCode();

            lastEditedSpline = spline;
            lastEditedSplineIndex = splineIndex;
            
            if (splineChangeTrigger == SplineChangeTrigger.AfterSplineChanges)
            {
                lastChangeTime = Time.realtimeSinceStartup;

                if (Application.isPlaying)
                {
                    //Coroutines only work in play mode and builds
                    
                    //Cancel any existing debounce coroutine
                    if (debounceCoroutine != null) StopCoroutine(debounceCoroutine);
                
                    debounceCoroutine = StartCoroutine(DebounceCoroutine());
                }
                else
                {
                    if (!isTrackingChanges)
                    {
                        isTrackingChanges = true;
                        
                        #if UNITY_EDITOR
                        UnityEditor.EditorApplication.update += EditorUpdate;
                        #endif
                    }
                    
                }
            }
            else if (splineChangeTrigger == SplineChangeTrigger.DuringSplineChanges)
            {
                ExecuteAfterSplineChanges();
            }
        }
        
        /// <summary>
        /// Time in seconds after the last change to the spline until respawning occurs. Applies only when the <see cref="splineChangeTrigger"/> is set to "After Spline Changes"
        /// </summary>
        private const float splineDebounceTime = 0.1f;

        private float lastChangeTime = -1f;
        private bool isTrackingChanges = false;
        
        private void EditorUpdate()
        {
            if (isTrackingChanges && Time.realtimeSinceStartup - lastChangeTime >= (splineDebounceTime))
            {
                ExecuteAfterSplineChanges();
                
                isTrackingChanges = false;
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.update -= EditorUpdate;
                #endif
            }
            
        }
        
        private Coroutine debounceCoroutine;
        private IEnumerator DebounceCoroutine()
        {
            yield return new WaitForSeconds((splineDebounceTime));
            
            ExecuteAfterSplineChanges();
        }

        private void ExecuteAfterSplineChanges()
        {
            if(lastEditedSplineIndex < 0) return;
            
            Profiler.BeginSample(PROFILER_PREFIX + "Update Spline Data");
            {
                UpdateSpline(lastEditedSpline, lastEditedSplineIndex);
            }
            Profiler.EndSample();
            
            Respawn(lastEditedSplineIndex);
        }
        
        private void OnSplineAdded(SplineContainer container, int index)
        {
            if (RespawnTriggerEnabled(RespawnTriggers.OnSplineAdded) == false) return;
            
            if (!splineContainer) return;
            
            //if (rebuildTriggers.HasFlag(RebuildTriggers.OnSplineAdded) == false) return;

            if (container.GetHashCode() != splineContainer.GetHashCode())
                return;

            //Inserting a new Knot also triggers this function, bail out if no new spline was actually added
            //Should still execute, since it may be that a number of splines were assigned that are totally different
            //if (splineCount == splineContainer.Splines.Count) return;
            
            splineCount = splineContainer.Splines.Count;

            containers.Add(SplineInstanceContainer.Create(this, index));
            
            CacheSpline(container.Splines[index]);
            
            //Causes issues with SendMessage. Adding splines must never be done from an OnValidate function
            Respawn(index);
        }

        private void OnSplineRemoved(SplineContainer targetContainer, int index)
        {
            if (RespawnTriggerEnabled(RespawnTriggers.OnSplineRemoved) == false) return;
            
            if (!splineContainer) return;
            
            if (targetContainer.GetHashCode() != splineContainer.GetHashCode())
                return;
            
            splineCount = splineContainer.Splines.Count;

            if (index <= nativeSplines.Count)
            {
                RemoveSpline(index);

                if (index < containers.Count && index >= 0)
                {
                    //Debug.Log($"Spline removed at index {index}. Container count: {containers.Count}");
                    SplineInstanceContainer container = containers[index];
                    container.Destroy();
                    
                    containers.RemoveAt(index);
                }
                else
                {
                    //throw new Exception($"Error when removing Spline #{index} from {targetContainer.name}. Index out of range ({containers.Count} containers).");
                }
            }

            //No need to respawn, the container for the spline has been removed and all objects with it
        }

        public Bounds CalculateSplineContainerBounds()
        {
            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;
            
            for (int i = 0; i < splineCount; i++)
            {
                //Note: Also includes tangents, but provides a natural amount of padding
                Bounds splineBounds = SplineContainer.Splines[i].GetBounds(SplineContainer.transform.localToWorldMatrix);

                min = Vector3.Min(splineBounds.min, min);
                max = Vector3.Max(splineBounds.max, max);
            }

            Bounds splineSpawnerBounds = new Bounds();
            splineSpawnerBounds.SetMinMax(min, max);

            return splineSpawnerBounds;
        }
    }
}