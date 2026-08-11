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
    [Modifier("Cull", "Deletes objects based on a noise pattern")]
    public class Cull : Modifier
    {
        public float noiseFrequency = 1f;
        public Vector2 noiseOffset;

        [Range(0f, 1f)]
        public float noiseCutoff = 0.33f;
        public float noiseFalloff;

        [BurstCompile]
        private struct Job : IJobParallelFor
        {
            public float noiseFrequency;
            public float2 noiseOffset;
            
            public float noiseCutoff;
            public float noiseFalloff;

            //private Unity.Mathematics.Random random;
            
            [NativeDisableParallelForRestriction]
            private NativeList<SpawnPoint> spawnPoints;

            private Random random;

            public Job(Cull settings, ref NativeList<SpawnPoint> spawnPoints)
            {
                this.spawnPoints = spawnPoints;

                this.noiseFrequency = settings.noiseFrequency;
                this.noiseOffset = settings.noiseOffset;
                this.noiseCutoff = settings.noiseCutoff;
                this.noiseFalloff = settings.noiseFalloff;
                
                random = new Unity.Mathematics.Random();
                random.InitState(1337);
            }

            public void Execute(int i)
            {
                SpawnPoint spawnPoint = spawnPoints[i];
                
                //Skip any spawn points already invalidated
                if(spawnPoint.isValid == false) return;
                
                SpawnPoint.Context context = spawnPoint.context;
                
                float2 noiseCoord = (spawnPoint.position.xz + noiseOffset) * noiseFrequency;
                float noise = Unity.Mathematics.noise.cnoise(noiseCoord);
                float r = noise * 0.5f + 0.5f;

                float gradient = math.smoothstep(noiseCutoff, 1f, r);
                
                float randomValue = random.NextFloat(0f, 1f) * noiseFalloff;
                
                if (math.max(gradient, randomValue) < noiseCutoff + randomValue)
                {
                    spawnPoint.isValid = false;
                }
                
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