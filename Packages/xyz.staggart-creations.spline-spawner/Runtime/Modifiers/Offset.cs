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
    [Modifier("Offset", "Offsets the object's position")]
    public class Offset : Modifier
    {
        public Space direction = Space.SplineCurve;
        
        public Vector3 offset;
        
        public Vector3 noiseAmplitude;
        public Vector3 noiseFrequency = new Vector3(1f, 1f, 1f);
        public Vector3 noiseOffset;

        public RandomMode randomMode = RandomMode.RandomBetween;
        public Vector3 randomMin;
        public Vector3 randomMax;
        public float randomnessFrequency = 10f;
        
        [BurstCompile]
        private struct Job : IJobParallelFor
        {
            private readonly float3 offset;
            
            private readonly float3 noiseAmplitude;
            private float3 noiseFrequency;
            private readonly float3 noiseOffset;
            
            private readonly RandomMode randomMode;
            private readonly float3 randomMin;
            private readonly float3 randomMax;
            private readonly float randomnessFrequency;
            private readonly Space direction;
            
            [NativeDisableParallelForRestriction]
            private NativeList<SpawnPoint> spawnPoints;

            private Random random;
            
            public Job(Offset settings, ref NativeList<SpawnPoint> spawnPoints)
            {
                this.spawnPoints = spawnPoints;

                this.offset = settings.offset;
                
                this.noiseAmplitude = settings.noiseAmplitude;
                this.noiseFrequency = settings.noiseFrequency;
                this.noiseOffset = settings.noiseOffset;
                
                this.randomMin = settings.randomMin;
                this.randomMax = settings.randomMax;
                this.randomnessFrequency = settings.randomnessFrequency;
                this.direction = settings.direction;
                this.randomMode = settings.randomMode;

                random = new Random(1337);
            }
            
            public void Execute(int i)
            {
                SpawnPoint spawnPoint = spawnPoints[i];
                                
                //Skip any spawn points invalidated
                if(spawnPoint.isValid == false) return;
                
                SpawnPoint.Context context = spawnPoint.context;
                
                float noise = Unity.Mathematics.noise.cnoise(context.noiseCoord * randomnessFrequency);
                float r = noise * 0.5f + 0.5f;

                float3 offsetNoise = 0f;

                if (math.any(noiseAmplitude))
                {
                    if(noiseAmplitude.x > 0) offsetNoise.x = Unity.Mathematics.noise.cnoise((context.noiseCoord.xyy * noiseFrequency.x) + noiseOffset.x) * noiseAmplitude.x;
                    if(noiseAmplitude.y > 0) offsetNoise.y = Unity.Mathematics.noise.cnoise((context.noiseCoord.xyy * noiseFrequency.y) + noiseOffset.y) * noiseAmplitude.y;
                    if(noiseAmplitude.z > 0) offsetNoise.z = Unity.Mathematics.noise.cnoise((context.noiseCoord.xyy * noiseFrequency.z) + noiseOffset.z) * noiseAmplitude.z;
                }

                if (randomMode == RandomMode.Alternate) r = (i/(int)math.max(1, randomnessFrequency)) % 2 == 0 ? 0 : 1;
                
                float3 randomOffset = math.lerp(randomMin, randomMax, r);
                //X
                {
                    float3 right = direction == Space.SplineCurve ? spawnPoint.context.right : math.mul(spawnPoint.rotation, math.right()).xyz;
                    if (direction == Space.World) right = math.right();
                    
                    float x = offset.x + (randomOffset.x);
                    x += offsetNoise.x;
                    spawnPoint.position += right * x;
                }
                
                //Y
                {
                    float3 up = direction == Space.SplineCurve ? spawnPoint.context.up : math.mul(spawnPoint.rotation, math.up()).xyz;
                    if (direction == Space.World) up = math.up();
                    
                    float y = offset.y + (randomOffset.y);
                    y += offsetNoise.y;
                    spawnPoint.position += up * y;
                }
                
                //Z
                {
                    float3 forward = direction == Space.SplineCurve ? spawnPoint.context.forward : math.mul(spawnPoint.rotation, math.forward()).xyz;
                    if (direction == Space.World) forward = math.forward();
                    
                    float z = offset.z + (randomOffset.z);
                    z += offsetNoise.z;
                    spawnPoint.position += forward * z;
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