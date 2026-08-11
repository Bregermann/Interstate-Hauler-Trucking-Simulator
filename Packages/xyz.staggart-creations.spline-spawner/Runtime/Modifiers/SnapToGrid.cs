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
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace sc.splines.spawner.runtime
{
    [Serializable]
    [Modifier("Grid Snap", "Snaps the position of the object to a virtual grid")]
    public class SnapToGrid : Modifier
    {
        [Min(0f)]
        public float gridSize = 1f;
        [Tooltip(("Editor Only: Use the grid snapping values from the Scene View"))]
        public bool useEditorGridSnapping;

        public Axis axis = Axis.X | Axis.Y | Axis.Z;

        [BurstCompile]
        private struct Job : IJobParallelFor
        {
            private float3 gridSize;
            private Axis axis;

            [NativeDisableParallelForRestriction]
            private NativeList<SpawnPoint> spawnPoints;

            public Job(SnapToGrid settings, ref NativeList<SpawnPoint> spawnPoints)
            {
                this.gridSize = Mathf.Max(0.01f, settings.gridSize);
                this.axis = settings.axis;
                
                #if UNITY_EDITOR
                if (settings.useEditorGridSnapping && EditorSnapSettings.gridSnapEnabled)
                {
                    gridSize = EditorSnapSettings.gridSize;
                }
                #endif
                
                this.spawnPoints = spawnPoints;
            }
            
            private bool IsAxisEnabled(Axis flag)
            {
                return (axis & flag) == flag;
            }
            
            private float SnapToGrid(float position, float cellSize)
            {
                return math.floor(position / cellSize) * (cellSize) + (cellSize * 0.5f);
            }
            
            public void Execute(int i)
            {
                SpawnPoint spawnPoint = spawnPoints[i];
                
                //Skip any spawn points invalidated
                if(spawnPoint.isValid == false) return;
                
                if(IsAxisEnabled(Axis.X)) spawnPoint.position.x = SnapToGrid(spawnPoint.position.x, gridSize.x);
                if(IsAxisEnabled(Axis.Y)) spawnPoint.position.y = SnapToGrid(spawnPoint.position.y, gridSize.y);
                if(IsAxisEnabled(Axis.Z)) spawnPoint.position.z = SnapToGrid(spawnPoint.position.z, gridSize.z);

                spawnPoints[i] = spawnPoint;
            }
        }
        
        public override JobHandle CreateJob(SplineSpawner spawner, ref NativeList<SpawnPoint> spawnPoints)
        {
            Job job = new Job(this, ref spawnPoints);
            
            JobHandle jobHandle = job.Schedule(spawnPoints.Length, DEFAULT_BATCHSIZE);

            return jobHandle;
        }
    }
}