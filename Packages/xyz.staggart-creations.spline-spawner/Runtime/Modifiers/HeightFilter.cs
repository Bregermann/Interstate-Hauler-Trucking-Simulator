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
using Random = Unity.Mathematics.Random;

namespace sc.splines.spawner.runtime
{
    [Serializable]
    [Modifier("Height Filter", "Deletes objects that fall outside of the configured height range.")]
    public class HeightFilter : Modifier
    {
        public float minHeight = -1000f;
        [Min(0f)]
        public float minFalloff;
        public float maxHeight = 3000f;
        [Min(0f)]
        public float maxFalloff;

        [BurstCompile]
        private struct Job : IJobParallelFor
        {
            private float minHeight;
            private float minFalloff;
            private float maxHeight;
            private float maxFalloff;
            
            [NativeDisableParallelForRestriction]
            private NativeList<SpawnPoint> spawnPoints;

            private Random rng;

            public Job(HeightFilter settings, ref NativeList<SpawnPoint> spawnPoints)
            {
                this.spawnPoints = spawnPoints;
                
                minHeight = settings.minHeight;
                minFalloff = settings.minFalloff + 0.01f;
                maxHeight = settings.maxHeight;
                maxFalloff = settings.maxFalloff + 0.01f;
                
                rng = new Random(1234); // or seed per frame
            }
            
            float HeightRangeMask(float height)
            {
                float minStart = minHeight + minFalloff;
                float min = math.saturate((height - minHeight) / (minStart - minHeight));

                float maxStart = maxHeight - maxFalloff;
                float max = math.saturate((maxHeight - height) / (maxHeight - maxStart));

                return math.saturate(min * max);
            }
            
            public void Execute(int i)
            {
                SpawnPoint spawnPoint = spawnPoints[i];
                
                //Skip any spawn points already invalidated
                if(spawnPoint.isValid == false) return;
                
                float height = spawnPoint.position.y;

                float gradient = HeightRangeMask(height);
                
                //If full in within range, keep
                if (gradient >= 1f) return;
                
                bool remove = gradient == 0f;

                if (remove == false && gradient < 1f)
                {
                    Random rand = Random.CreateFromIndex((uint)i);
                    float r = rand.NextFloat(0f, 1f);
                    float noise = math.saturate(gradient + r) * gradient;
                    remove = noise < gradient;
                }
                
                spawnPoint.isValid = !remove;
                
                spawnPoints[i] = spawnPoint;
            }
        }

        public override JobHandle CreateJob(SplineSpawner spawner, ref NativeList<SpawnPoint> spawnPoints)
        {
            Job job = new Job(this, ref spawnPoints);
            
            JobHandle jobHandle = job.Schedule(spawnPoints.Length, DEFAULT_BATCHSIZE);

            return jobHandle;
        }
        
        public override void Dispose()
        {
            
        }
    }
}