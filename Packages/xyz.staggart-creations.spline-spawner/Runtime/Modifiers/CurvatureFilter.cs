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

namespace sc.splines.spawner.runtime
{
    [Serializable]
    [Modifier("Curvature Filter", "Restricts object to a areas on the curve that are curved or straight enough", 
        new [] { DistributionSettings.DistributionMode.Grid, DistributionSettings.DistributionMode.OnKnots, DistributionSettings.DistributionMode.Radial, DistributionSettings.DistributionMode.InsideArea })]
    public class CurvatureFilter : Modifier
    {
        public float minAngle;
        public float maxAngle = 90f;

        [BurstCompile]
        private struct Job : IJobParallelFor
        {
            private readonly float minAngle;
            private readonly float maxAngle;
            
            [NativeDisableParallelForRestriction]
            private NativeList<SpawnPoint> spawnPoints;

            public Job(CurvatureFilter settings, ref NativeList<SpawnPoint> spawnPoints)
            {
                this.spawnPoints = spawnPoints;
                
                this.minAngle = settings.minAngle;
                this.maxAngle = settings.maxAngle;
            }

            public void Execute(int i)
            {
                SpawnPoint spawnPoint = spawnPoints[i];
                                
                //Skip any spawn points invalidated
                if(spawnPoint.isValid == false) return;
                
                SpawnPoint.Context context = spawnPoint.context;
                
                if (context.curvature < minAngle || context.curvature > maxAngle) spawnPoint.isValid = false;
                
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