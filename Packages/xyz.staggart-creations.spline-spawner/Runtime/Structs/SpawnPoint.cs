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

using Unity.Mathematics;

namespace sc.splines.spawner.runtime
{
    public struct SpawnPoint
    {
        public bool isValid;
            
        public int prefabIndex;
        public float3 pivotOffset;
        public float3 position;
        public quaternion rotation;
        /// <summary>
        /// Scale of the object's bounds
        /// </summary>
        public float3 scale;
            
        public struct Context
        {
            public float t;
            public float splineLength;
            public float random01;
            public float2 noiseCoord;
            public float curvature;

            public float3 position;
            public float3 forward;
            public float3 right;
            public float3 up;
            
            public bool invertDistance;
        }
        public Context context;
    }
}