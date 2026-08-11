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
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace sc.splines.spawner.runtime
{
    public partial class SplineSpawner
    {
        public ComputeShader terrainMaskingComputeShader;
        [SerializeField] [HideInInspector]
        private Shader terrainSplatShader; //Serialize to ensure build inclusion
        
        [Serializable]
        public class TerrainLayerMask
        {
            public enum FilterMode
            {
                Include,
                Exclude
            }
            public bool enabled = true;
            public FilterMode filterMode = FilterMode.Exclude;
            
            public TerrainLayer terrainLayer;
            
            [Tooltip("The minimum weight a terrain layer must have to be considered for the filter.")]
            [Range(0f, 1f)]
            public float threshold = 0f;
            [Tooltip("The falloff of the threshold, within that gradient objects are randomly allowed to spawn.")]
            [Range(0f, 1f)]
            public float falloff;

            public TerrainLayerMask(){}
            public TerrainLayerMask(TerrainLayer terrainLayer1)
            {
                terrainLayer = terrainLayer1;
            }
        }
        public List<TerrainLayerMask> terrainLayerMasks = new List<TerrainLayerMask>();
        [Tooltip("Respawns splines that overlap with terrain areas where the splatmap is being modified." +
                 "\n\nUndo/redo operations will not trigger this.")]
        public bool respawnOnTerrainTextureChange = true;
        
        private const float terrainRespawnDebounceDelay = 0.1f; //Time in seconds

        private readonly HashSet<int> pendingTerrainRespawnIndices = new HashSet<int>();
        private double lastTerrainChangeTime = -1f;
        private bool isTrackingTerrainChanges = false;
        private Coroutine terrainDebounceCoroutine;
        
        //Culling states for each spawn point after terrain layer processing
        private int[] spawnPointCullStates;

        private void SubscribeToTerrainEvents()
        {
            #if TERRAIN
            TerrainCallbacks.textureChanged += OnTerrainSplatmapChanged;
            #endif
        }
        
        private void UnsubscribeFromTerrainEvents()
        {
            #if TERRAIN
            CancelDebouncedTerrainRespawns();
            
            TerrainCallbacks.textureChanged -= OnTerrainSplatmapChanged;
            #endif
        }

#if TERRAIN
        void OnTerrainSplatmapChanged(Terrain terrain, string textureName, RectInt texelRegion, bool cpuDataSynced)
        {
            if (!respawnOnTerrainTextureChange || TerrainMaskingEnabled() == false) return;
            
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPosition = terrain.GetPosition();
            Vector3 terrainSize = terrainData.size;
            
            int splatMapResolution = terrainData.alphamapResolution;
            Vector2 texelSize = new Vector2(terrainSize.x / splatMapResolution, terrainSize.z / splatMapResolution);
            
            //texelRegion denotes the area in the splatmap that's being modified, convert it to a world-space bounds
            Vector3 worldMin = new Vector3(terrainPosition.x + texelRegion.xMin * texelSize.x, terrainPosition.y, terrainPosition.z + texelRegion.yMin * texelSize.y);
            Vector3 worldMax = new Vector3(terrainPosition.x + texelRegion.xMax * texelSize.x, terrainPosition.y, terrainPosition.z + texelRegion.yMax * texelSize.y);

            Bounds changedTerrainBounds = new Bounds();
            changedTerrainBounds.SetMinMax(worldMin, worldMax);

            //Debug.Log($"Terrain texture ({textureName}) changed on {terrain.name} in {changedTerrainBounds}");
            
            //Check if any of the splines overlap with the modified area
            for (int i = 0; i < splineBounds.Count; i++)
            {
                NativeBounds curveBounds = this.splineBounds[i];
                
                //Intersection check on the XZ plane
                bool intersects = curveBounds.min.x <= changedTerrainBounds.max.x && curveBounds.max.x >= changedTerrainBounds.min.x && 
                                  curveBounds.min.z <= changedTerrainBounds.max.z && curveBounds.max.z >= changedTerrainBounds.min.z;
                    
                if (intersects)
                {
                    //Callback is fired frequently, use debouncing behaviour to call Respawn(i)
                    RequestDebouncedTerrainRespawn(i);
                }
            }
        }
        
        internal void RequestDebouncedTerrainRespawn(int splineIndex)
        {
            pendingTerrainRespawnIndices.Add(splineIndex);
            lastTerrainChangeTime = Time.realtimeSinceStartup;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                lastTerrainChangeTime = UnityEditor.EditorApplication.timeSinceStartup;
            }
#endif
            if (Application.isPlaying)
            {
                //Coroutines only work in play mode and builds

                //Cancel any existing debounce coroutine
                if (terrainDebounceCoroutine != null) StopCoroutine(terrainDebounceCoroutine);

                terrainDebounceCoroutine = StartCoroutine(TerrainDebounceCoroutine());
            }
            else
            {
                if (!isTrackingTerrainChanges)
                {
                    isTrackingTerrainChanges = true;

#if UNITY_EDITOR
                    UnityEditor.EditorApplication.update += TerrainEditorUpdate;
#endif
                }
            }
        }
        
        internal void TerrainEditorUpdate()
        {
#if UNITY_EDITOR
            double t = UnityEditor.EditorApplication.timeSinceStartup;
            if (isTrackingTerrainChanges && t - lastTerrainChangeTime >= (terrainRespawnDebounceDelay))
            {
                ExecuteAfterTerrainChanges();

                isTrackingTerrainChanges = false;
                UnityEditor.EditorApplication.update -= TerrainEditorUpdate;
            }
#endif
        }

        internal IEnumerator TerrainDebounceCoroutine()
        {
            yield return new WaitForSeconds(terrainRespawnDebounceDelay);

            ExecuteAfterTerrainChanges();
        }

        internal void ExecuteAfterTerrainChanges()
        {
            if (pendingTerrainRespawnIndices.Count == 0)
            {
                return;
            }

            int[] indices = new int[pendingTerrainRespawnIndices.Count];
            pendingTerrainRespawnIndices.CopyTo(indices);

            pendingTerrainRespawnIndices.Clear();
            isTrackingTerrainChanges = false;
            terrainDebounceCoroutine = null;

            //Array.Sort(indices);
            
            //Debug.Log($"Terrain changes detected, respawning Splines #{string.Join(", ", indices)}");

            for (int i = 0; i < indices.Length; i++)
            {
                Respawn(indices[i]);
            }
        }

        private void CancelDebouncedTerrainRespawns()
        {
            pendingTerrainRespawnIndices.Clear();
            isTrackingTerrainChanges = false;

            if (terrainDebounceCoroutine != null)
            {
                StopCoroutine(terrainDebounceCoroutine);
                terrainDebounceCoroutine = null;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= TerrainEditorUpdate;
#endif
        }

        private bool TerrainMaskingEnabled()
        {
            if(terrainLayerMasks.Count == 0) return false;
            
            var terrainMaskingEnabled = false;
            
            //If any layer mask is enabled
            foreach (var mask in terrainLayerMasks)
            {
                terrainMaskingEnabled |= mask.enabled;
            }

            return terrainMaskingEnabled;
        }
        
        //Entry point called during Respawning, processes the spawn points and culls them
        private void ProcessTerrainLayers()
        {
            if (!terrainMaskingComputeShader)
            {
                Debug.LogError("[Spline Spawner] Terrain layer masking is enabled, but no terrain layer masking compute shader was found. Terrain layer masking will be skipped.");
                return;
            }

            if (!TerrainMaskingEnabled())
            {
                return;
            }
            
            if (spawnPoints.Length > 0)
            {
                NativeBounds combinedBounds = NativeBounds.Combine(splineBounds);
                Bounds splineSpawnerBounds = new Bounds(combinedBounds.center, combinedBounds.size);
                
                RenderTexture splatmapData = TerrainRenderer.RenderTerrainSplatmaps(splineSpawnerBounds);

                //RT may be null if no terrains were found within the bounds
                if (splatmapData != null)
                {
                    SampleSplatWeightsAtPositions(spawnPoints, splineSpawnerBounds, splatmapData);

                    RemoveMaskedSpawnPoints();
                }
            }
        }
        
        internal void SampleSplatWeightsAtPositions(NativeList<SpawnPoint> m_spawnPoints, Bounds bounds, RenderTexture splatmapData)
        {
            var spawnPointCount = m_spawnPoints.Length;
            //Compose a list of spawn positions
            Vector3[] spawnPositions = new Vector3[spawnPointCount];

            //An unsafe pointer is used here to eliminate the overhead of accessing a native array in managed code.
            //It remains completely safe since the two arrays are of identical size.
            unsafe
            {
                SpawnPoint* spawnPointPtr = (SpawnPoint*)m_spawnPoints.GetUnsafeReadOnlyPtr();
                for (int i = 0; i < spawnPointCount; i++)
                {
                    spawnPositions[i] = spawnPointPtr[i].position;
                }
            }

            CommandBuffer cmd = new CommandBuffer();
            int kernel = terrainMaskingComputeShader.FindKernel("CalculateLayerWeights");
            if (kernel < 0)
            {
                Debug.LogError("Could not find kernel 'CalculateLayerWeights' in compute shader!");
                return;
            }

			//Texture array containing the RGBA splatmaps (previously rendered out)
            cmd.SetComputeTextureParam(terrainMaskingComputeShader, kernel, "_SplatData", splatmapData);
            cmd.SetComputeVectorParam(terrainMaskingComputeShader, "_SplatDataResolution", new Vector2(splatmapData.width, splatmapData.height));
            
			//The bounds in which the splatmaps were rasterized
            cmd.SetComputeVectorParam(terrainMaskingComputeShader, "_BoundsCenter", bounds.center);
            cmd.SetComputeVectorParam(terrainMaskingComputeShader, "_BoundsSize", bounds.size);
            
            int layerCount = terrainLayerMasks.Count;
            cmd.SetComputeIntParam(terrainMaskingComputeShader, "_LayerCount", layerCount);

			//Settings for each configured masking layer, packed into a Float4
            ComputeBuffer layerSettings = new ComputeBuffer(layerCount, sizeof(float) * 4);
            Vector4[] layerSettingsData = new Vector4[layerCount];
            for (int i = 0; i < layerCount; i++)
            {
                if (terrainLayerMasks[i].enabled == false)
                {
                    layerSettingsData[i] = new Vector3(-1, 0, 0);
                    continue;
                }
                int layerIndex = TerrainRenderer.GetTerrainLayerIndex(terrainLayerMasks[i].terrainLayer);
                
                //Debug.Log($"Terrain layer {terrainLayerMasks[i].terrainLayer.name} found at index {layerIndex}. Splatmap #{TerrainRenderer.GetSplatmapID(layerIndex)}, channel #{layerIndex % 4}");
                
                float minWeight = Mathf.Max(0.01f, terrainLayerMasks[i].threshold);
                float falloff = Mathf.Max(0.01f, terrainLayerMasks[i].falloff);
                TerrainLayerMask.FilterMode filterMode = terrainLayerMasks[i].filterMode;
                
                layerSettingsData[i] = new Vector4(layerIndex, minWeight, falloff, (int)filterMode);
            }
            layerSettings.SetData(layerSettingsData);
            cmd.SetComputeBufferParam(terrainMaskingComputeShader, kernel, "_LayerSettings", layerSettings);

            //Spawn point positions to sample the splat map data at
            ComputeBuffer positionBuffer = new ComputeBuffer(spawnPointCount, sizeof(float) * 3);
            positionBuffer.SetData(spawnPositions);
            cmd.SetComputeBufferParam(terrainMaskingComputeShader, kernel, "_SamplePositions", positionBuffer);
            cmd.SetComputeIntParam(terrainMaskingComputeShader, "_SamplePositionCount", spawnPointCount);
            
			//The binary culling state of the spawn point, after the layers are processed against their settings
            ComputeBuffer resultBuffer = new ComputeBuffer(spawnPointCount, sizeof(int));
            spawnPointCullStates = new int[spawnPointCount];
            resultBuffer.SetData(spawnPointCullStates);
            cmd.SetComputeBufferParam(terrainMaskingComputeShader, kernel, "_SpawnPointCullStates", resultBuffer);

			//Dispatch the compute shader now
            cmd.DispatchCompute(terrainMaskingComputeShader, kernel, Mathf.CeilToInt(spawnPointCount / 64.0f), 1, 1);
            Graphics.ExecuteCommandBuffer(cmd);
            
            //Get results syncronously
			//This point may introduce a CPU spike as the GPU/CPU need to sync, but the data should be small enough to avoid this.
            resultBuffer.GetData(spawnPointCullStates);
            
			//Cleanup, result now stored in `spawnPointCullStates`
            cmd.Clear();
            layerSettings.Dispose();
            positionBuffer.Dispose();
            resultBuffer.Dispose();
        }
        
        private void RemoveMaskedSpawnPoints()
        {
            int removed = 0;
            int spawnPointCount = spawnPoints.Length;

            unsafe
            {
                SpawnPoint* spawnPointPtr = (SpawnPoint*)spawnPoints.GetUnsafePtr();

                for (int i = 0; i < spawnPointCount; i++)
                {
                    SpawnPoint* point = spawnPointPtr + i;

                    //Previously invalidated
                    if (point->isValid == false) continue;

                    point->isValid &= spawnPointCullStates[i] == 1;

                    if (spawnPointCullStates[i] == 0) removed++;
                }
            }

            if (removed > 0)
            {
                //Debug.Log($"Removed {removed}/{spawnPoints.Length} spawn points through terrain layer masking.");
            }
        }
        #endif
        
        internal void EnsureTerrainResources()
        {
            #if TERRAIN
            if (!terrainSplatShader)
            {
                terrainSplatShader = Shader.Find(TerrainRenderer.TERRAIN_SPLAT_RENDER_SHADER_NAME);
            }

            if (!terrainMaskingComputeShader)
            {
                #if UNITY_EDITOR
                terrainMaskingComputeShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(
                    $"{SplineSpawner.kPackageRoot}/Shaders/TerrainLayerProcessor.compute");
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
            #endif
        }
    }
    
#if TERRAIN
    public static class TerrainRenderer
    {
        private static readonly int _InputSplatmap = Shader.PropertyToID("_InputSplatmap");
        private static readonly int _MainTex = Shader.PropertyToID("_MainTex");
        private static RenderTexture splatmapData;
        public const string TERRAIN_SPLAT_RENDER_SHADER_NAME = "Hidden/TerrainSplatRenderer";
        //private const string TERRAIN_SPLAT_RENDER_SHADER = "Hidden/BlitCopy";

        private static Shader _terrainSplatShader;
        private static Shader terrainSplatShader => _terrainSplatShader ? _terrainSplatShader : _terrainSplatShader = Shader.Find(TERRAIN_SPLAT_RENDER_SHADER_NAME);
        
        /// <summary>
        /// Renders the splatmaps of all active terrains, within the bounds, into a texture array. Where each slice contains the next splatmap (up to 4)
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="texelSize"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static RenderTexture RenderTerrainSplatmaps(Bounds bounds, float texelSize = -1f)
        {
			//Cache values to avoid access overhead on bounds.
            Vector3 boundsSize = bounds.size;
            Vector3 boundsCenter = bounds.center;
            Vector3 boundsMin = bounds.min;
            Vector3 boundsMax = bounds.max;
            Vector3 extents = bounds.extents;
			
            #region Terrains
            Terrain[] activeTerrains = Terrain.activeTerrains;
            
            List<Terrain> terrains = new List<Terrain>();
            List<TerrainData> terrainDatas = new List<TerrainData>();
            float minTexelSize = float.MaxValue;

            int layerCount = 0;
            int splatmapCount = 0;
			
			//First collect all terrains that intersect with the given bounds on the XZ plane
            for (int i = 0; i < activeTerrains.Length; i++)
            {
                TerrainData terrainData = activeTerrains[i].terrainData;

                //Avoid undersampling by using the largest texel size available
                minTexelSize = Mathf.Max(texelSize, terrainData.size.x / terrainData.alphamapResolution);
                
                Bounds terrainBounds = new Bounds(activeTerrains[i].GetPosition() + terrainData.bounds.extents, terrainData.bounds.size);
                
                //Intersection check on the XZ plane
                bool intersects = boundsMin.x <= terrainBounds.max.x && boundsMax.x >= terrainBounds.min.x && boundsMin.z <= terrainBounds.max.z && boundsMax.z >= terrainBounds.min.z;
                
                if (intersects)
                {
                    layerCount = Mathf.Max(layerCount, terrainData.terrainLayers.Length);
                    terrains.Add(activeTerrains[i]);
                    terrainDatas.Add(terrainData);
                }
            }
            texelSize = minTexelSize;
            
            var terrainCount = terrains.Count;

            if (terrainCount == 0)
            {
                Debug.LogWarning("[Spline Spawner] No terrains found within the bounds");
                return null;
            }

            if (layerCount == 0)
            {
                throw new Exception("[Spline Spawner] Terrains in the scene have no terrain layers on them");
            }

            splatmapCount = (layerCount / 4) + 1;
            #endregion
            
            //Debug.Log($"Rendering {terrainCount} terrains with {splatmapCount} splatmaps");
            
            #region Render targets
            Vector2Int resolution = new Vector2Int(Mathf.Min(2048, Mathf.CeilToInt(boundsSize.x / texelSize)), Mathf.Min(2048, Mathf.CeilToInt(boundsSize.z / texelSize)));

            //Cache the RT and recreate only when resized
            if (splatmapData == null || splatmapData.width != resolution.x || splatmapData.height != resolution.y || splatmapData.volumeDepth != splatmapCount)
            {
                if(splatmapData != null) splatmapData.Release();
                
                splatmapData = new RenderTexture(resolution.x, resolution.y, 0, GraphicsFormat.R8G8B8A8_UNorm)
                {
                    name = "Terrain Splatmaps",
                    depthStencilFormat = GraphicsFormat.None,
                    dimension = TextureDimension.Tex2DArray,
                    volumeDepth = splatmapCount,
                    enableRandomWrite = true //Compute shader support
                };
            }
            #endregion
            
            #region Command Buffer
            CommandBuffer cmd = new CommandBuffer();

			//Construct a top-down orthographic project 
            var frustumHeight = 5000f;
            Matrix4x4 view = Matrix4x4.TRS(boundsCenter + (Vector3.up * (frustumHeight * 0.5f)), Quaternion.Euler(Vector3.right * 90f), new Vector3(1, 1, -1)).inverse;
			Matrix4x4 projection = Matrix4x4.Ortho(-extents.x, extents.x, -extents.z, extents.z, 0.02f, frustumHeight);
            
            cmd.SetViewProjectionMatrices(view, projection);
            cmd.SetViewport(new Rect(0, 0, resolution.x, resolution.y));

            //Shader has ZTest Always, Cull Off and ZClip Off. Ensures it is always rendered on top of everything else.
            //Input texture is a global resource, so command buffer compatible
            Shader shader = terrainSplatShader;

            if (!shader)
            {
                throw new Exception("[Spline Spawner] Terrain data shader not found. This may happen (in a build) if the Spline Spawner component is created entirely through script. Or if the file was missing.");
            }
            Material terrainDataMaterial = new Material(shader);
			
            //Debug.Log($"Rendering {terrainCount} terrains");
            
			//Render a quad as a terrain proxy to visualize its splatmaps
            for (int i = 0; i < terrainCount; i++)
            {
                TerrainData terrainData = terrainDatas[i];

                Vector3 position = terrains[i].GetPosition();
                //position.y = bounds.center.y;
                
                //Terrain quad is a unit-scale mesh, so scale it by the terrain size
                Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, terrainData.size);

                //Quite possible a terrain has fewer splatmaps, clamp for safety
                int splats = Mathf.Min(splatmapCount, terrainData.alphamapTextureCount);
                
                //Render splatmap into each slice of the texture array
                for (int x = 0; x < splats; x++)
                {
                    //Set the target to the array slice
                    cmd.SetRenderTarget(new RenderTargetIdentifier(splatmapData, 0, CubemapFace.Unknown, x));
                    
                    //Flood fill as default values. Only clear for the first call!
                    if(i == 0) cmd.ClearRenderTarget(false, true, Color.white, x);

                    cmd.SetGlobalTexture(_MainTex, terrainData.alphamapTextures[x]);
                    
                    cmd.DrawMesh(TerrainProxyMesh, matrix, terrainDataMaterial, 0, 0);
                }
            }

			//Execute
            Graphics.ExecuteCommandBuffer(cmd);
			
			//Cleanup, contents now written into `splatmapData`
            cmd.Clear();
            terrainDataMaterial = null;
            #endregion
            
            return splatmapData;
        }

        //Find which index this terrain layer holds in the terrain layers array.
        //Uses the first available terrain, as it's conventional for all terrains to have an identical set up
        public static int GetTerrainLayerIndex(TerrainLayer layer)
        {
            if (!layer) return -1;
            
            Terrain mainTerrain = Terrain.activeTerrain;
            
            if(mainTerrain == null) return -1;
            
            TerrainLayer[] terrainLayers = mainTerrain.terrainData.terrainLayers;
            int layerCount = terrainLayers.Length;
            
            for (int i = 0; i < layerCount; i++)
            {
                if (terrainLayers[i].Equals(layer)) return i;
            }

            return -1;
        }
        
        private static Mesh _TerrainProxyMesh;
        private static Mesh TerrainProxyMesh
        {
            get
            {
                if (_TerrainProxyMesh == null) _TerrainProxyMesh = CreateTerrainProxyMesh();

                return _TerrainProxyMesh;
            }
        }
        
        /// <summary>
        /// A quad mesh with its origin in the bottom-left, so that it can be scaled up to match a terrain's XZ size
        /// </summary>
        /// <returns></returns>
        private static Mesh CreateTerrainProxyMesh()
        {
            const float baseScale = 1f;
            Vector3[] vertices = new Vector3[4]
            {
                new(0f, 0f, 0f),
                new (baseScale, 0, 0),
                new (0f, 0, baseScale),
                new (baseScale, 0, baseScale)
            };
            
            Mesh mesh = new Mesh
            {
                name = "TerrainProxyQuad",
                vertices = vertices,
                //Not needed
                //normals = new Vector3[4] { Vector3.up, Vector3.up, Vector3.up, Vector3.up },
                triangles = new int[6] { 0, 2, 1, 2, 3, 1 },
                uv = new Vector2[4] { new (0, 0), new (1, 0), new (0, 1), new (1, 1) }
            };
            
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(baseScale, 0f, baseScale));

            return mesh;
        }
    }
#endif
}