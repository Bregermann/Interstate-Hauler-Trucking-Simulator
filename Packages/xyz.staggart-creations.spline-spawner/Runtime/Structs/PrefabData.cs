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

using System.Collections.Generic;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;
using ForwardDirection = sc.splines.spawner.runtime.SplineSpawner.SpawnableObject.ForwardDirection;

namespace sc.splines.spawner.runtime
{
    //Unmanaged version of prefab information
    public struct PrefabData
    {
        public int index;
        public SplineSpawner.SpawnableObject.Pivot pivot;
        public ForwardDirection forwardDirection;
        
        public float probability;
        public float distanceSelectionWeight;
        
        public float3 boundsMin;
        public float3 boundsMax;
        public float3 boundsSize;
        public float3 gameObjectScale;
        
        public float3 pivotOffset;

        public static PrefabData Create(SplineSpawner.SpawnableObject input, int index)
        {
            PrefabData pd = new PrefabData();
            pd.pivot = input.pivot;
            pd.forwardDirection = input.forwardDirection;
            pd.probability = input.probability;
            pd.distanceSelectionWeight = input.selectBySplineDistance;
            pd.index = index;

            if (input.prefab)
            {
                pd.CalculateBoundsSize(input.prefab);

                pd.gameObjectScale = input.prefab.transform.localScale;
                pd.gameObjectScale *= input.baseScale;
                
                if (math.length(pd.boundsSize * pd.gameObjectScale) < SplineSpawner.MIN_OBJECT_SIZE)
                {
                    throw new WarningException($"Prefab bounds size of \"{input.prefab.name}\" is less than {SplineSpawner.MIN_OBJECT_SIZE}m! Check that no Meshes are missing");
                }
            }

            return pd;
        }
        
        /// <summary>
        /// Automatically grab a starting point for the area size, based on the attached mesh(es) or collider(s)
        /// </summary>
        public void CalculateBoundsSize(GameObject target)
        {
            //Default size, considering that the prefab doesn't have any geometry or colliders
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 0.5f);
            
            Vector3 minSum = Vector3.one * Mathf.Infinity;
            Vector3 maxSum = Vector3.one * Mathf.NegativeInfinity;

            List<MeshFilter> meshes = new List<MeshFilter>(target.GetComponentsInChildren<MeshFilter>());
            
            //If a LOD group is present, consider LOD0 as the target object
            LODGroup lodGroup = target.GetComponent<LODGroup>();
            if (lodGroup && lodGroup.lodCount > 0)
            {
                LOD[] lods = lodGroup.GetLODs();
                
				if(lods[0].renderers.Length > 0)
				{
					meshes.Clear(); //Only use LOD0, the last LOD might be an impostor (thus larger)
					for (int i = 0; i < lods[0].renderers.Length; i++)
					{
						Renderer renderer = lods[0].renderers[i];
						
						MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
						if (meshFilter) meshes.Add(meshFilter);
					}
				}
            }
            
            int meshCount = meshes.Count;
            if (meshCount > 0)
            {
                for (int i = 0; i < meshCount; i++)
                {
                    if (meshes[i].sharedMesh == null)
                    {
                        Debug.LogError($"Object \"{target.name}\" has one or more missing meshes. Failed to calculate bounds size.", target);
                        return;
                    }

                    minSum = Vector3.Min(minSum, meshes[i].transform.TransformVector(meshes[i].sharedMesh.bounds.min));
                    maxSum = Vector3.Max(maxSum, meshes[i].transform.TransformVector(meshes[i].sharedMesh.bounds.max));
                }
                bounds.SetMinMax(minSum, maxSum);
            }
            else
            {
                Collider[] colliders = target.GetComponentsInChildren<Collider>();

                if (colliders.Length > 0)
                {
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        if (colliders[i].isTrigger) continue;

                        minSum = Vector3.Min(minSum, colliders[i].transform.TransformVector(colliders[i].bounds.min));
                        maxSum = Vector3.Max(maxSum, colliders[i].transform.TransformVector(colliders[i].bounds.max));
                    }
                    bounds.SetMinMax(minSum, maxSum);
                }
            }

            boundsMin = bounds.min;
            boundsMax = bounds.max;
            
            //Counter the scale, since this'll be multiplied with the bounds size at later stages.
            Vector3 objectScale = target.transform.localScale;
            boundsSize = new float3(bounds.size.x / objectScale.x, bounds.size.y / objectScale.y, bounds.size.z / objectScale.z);
            
            pivotOffset = -(float3)bounds.center;
        }
        
        private int GetLengthAxis()
        {
            return (int)forwardDirection / 2;
        }

        public float GetForwardPivotOffset()
        {
            int component = GetLengthAxis();
            
            return pivotOffset[component];
        }
        
        public float3 GetPivotOffset()
        {
            float3 m_pivotOffset = pivotOffset;
            
            if (forwardDirection == ForwardDirection.PositiveZ || forwardDirection == ForwardDirection.NegativeZ)
            {
                m_pivotOffset *= math.forward();
            }

            //etc..
            
            return m_pivotOffset;
        }

        public float GetObjectBoundsMin()
        {
            int component = GetLengthAxis();
            
            return boundsMin[component];
        }
        
        public float GetObjectLength()
        {
            int component = GetLengthAxis();
            
            return (boundsSize[component]) * gameObjectScale[component];
        }

        public float GetRadiusXZ()
        {
            //return math.min(boundsSize.x * gameObjectScale.x, boundsSize.z * gameObjectScale.z);
            return math.max(boundsSize.x * gameObjectScale.x, boundsSize.z * gameObjectScale.z);
            //Average
            //return ((boundsSize.x * gameObjectScale.x) + (boundsSize.z * gameObjectScale.z)) * 0.5f;
        }

        public quaternion GetForwardRotation(float3 forward, float3 right, float3 up)
        {
            if (forwardDirection == ForwardDirection.PositiveX)
            {
                return quaternion.LookRotation(right, up);
            }
            if (forwardDirection == ForwardDirection.NegativeX)
            {
                return quaternion.LookRotation(-right, up);
            }
            if (forwardDirection == ForwardDirection.PositiveY)
            {
                return quaternion.LookRotation(up, forward);
            }
            if (forwardDirection == ForwardDirection.NegativeY)
            {
                return quaternion.LookRotation(-up, forward);
            }
            if (forwardDirection == ForwardDirection.PositiveZ)
            {
                return quaternion.LookRotation(forward, up);
            }
            if (forwardDirection == ForwardDirection.NegativeZ)
            {
                return quaternion.LookRotation(-forward, up);
            }
            
            return quaternion.identity;
        }
    }
}