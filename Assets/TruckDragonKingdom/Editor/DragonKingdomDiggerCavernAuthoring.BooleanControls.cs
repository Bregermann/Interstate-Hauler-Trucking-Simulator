#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using Digger.Modules.Core.Sources;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomDiggerCavernAuthoring
    {
        [Serializable] public sealed class BooleanUnionControl
        {
            public float[] values;
            public bool originalInputExact, repeatPayloadExact, orderPayloadExact, outsidePayloadExact;
            public int interiorChanges, exteriorChanges, newHoles, markedHoles;
        }

        [Serializable] public sealed class BooleanFloorControl
        {
            public float[] values, worldY;
            public bool[] inside;
            public bool outsidePayloadExact, repeatPayloadExact, originalInputExact;
            public int outsideSamples, modifiedSamples;
        }

        public static BooleanFloorControl ProbeBooleanFloorControl(string mode)
        {
            const int n = 4, count = n * n * n;
            var original = new Voxel[count];
            for (int i = 0; i < count; i++)
            {
                original[i] = new Voxel(mode == "original-solid" ? -80 : mode == "different-void-values" && i % 2 != 0 ? 80 : 30);
                original[i].FirstTextureIndex = 7; original[i].SecondTextureIndex = 5; original[i].NormalizedTextureLerp = .25f;
            }
            var origin = new float3(440, mode == "outside-upper-band" ? 110 : mode == "below-lower-bound" ? 83 : 104.5f, 726);
            var minimum = mode == "outside-footprint" ? new float2(441, 727) : new float2(400, 700);
            var maximum = mode == "outside-footprint" ? new float2(444, 730) : new float2(600, 850);
            var job = new BooleanCavernJob
            {
                SizeVox = n, SizeVox2 = n * n, HeightmapScale = new float3(1.5625f, .5f, 1.5625f),
                ClipAuthoredFloor = true, AbsoluteWorldOrigin = origin, FloorMinimumXZ = minimum, FloorMaximumXZ = maximum,
                FloorMinimumY = 84, FloorHeight = 105, ExteriorBand = 4.6875f, TextureIndex = 3,
                Voxels = new NativeArray<Voxel>(original, Allocator.TempJob),
                InputVoxels = new NativeArray<Voxel>(0, Allocator.TempJob),
                Heights = new NativeArray<float>((n + 2) * (n + 2), Allocator.TempJob),
                Holes = new NativeArray<int>(n * n, Allocator.TempJob),
                NewHolesConcurrentCounter = new NativeArray<int>(1, Allocator.TempJob),
                ChangedSamples = new NativeArray<int>(2, Allocator.TempJob)
            };
            for (int i = 0; i < job.Heights.Length; i++) job.Heights[i] = 1000;
            byte[] before = job.Voxels.Reinterpret<byte>(8).ToArray();
            try
            {
                job.Schedule(count, 1).Complete();
                byte[] after = job.Voxels.Reinterpret<byte>(8).ToArray();
                var result = new BooleanFloorControl
                {
                    values = job.Voxels.ToArray().Select(v => v.Value).ToArray(), worldY = new float[count], inside = new bool[count],
                    outsidePayloadExact = true, modifiedSamples = job.ChangedSamples[0] + job.ChangedSamples[1]
                };
                for (int i = 0; i < count; i++)
                {
                    var p = Utils.IndexToXYZ(i, n, n * n) * job.HeightmapScale + origin;
                    result.worldY[i] = p.y;
                    result.inside[i] = p.x >= minimum.x && p.x <= maximum.x && p.z >= minimum.y && p.z <= maximum.y && p.y >= 84 && p.y <= 109.6875f;
                    if (!result.inside[i])
                    {
                        result.outsideSamples++;
                        for (int b = 0; b < 8; b++) result.outsidePayloadExact &= before[i * 8 + b] == after[i * 8 + b];
                    }
                }
                job.Schedule(count, 1).Complete();
                result.repeatPayloadExact = after.SequenceEqual(job.Voxels.Reinterpret<byte>(8).ToArray());
                using (var check = new NativeArray<Voxel>(original, Allocator.Temp)) result.originalInputExact = before.SequenceEqual(check.Reinterpret<byte>(8).ToArray());
                return result;
            }
            finally { job.Dispose(); }
        }

        // Small native-job controls only: no scene, source mesh, Digger system,
        // voxel file or candidate asset is created or initialized here.
        public static BooleanUnionControl ProbeBooleanUnionControl(string mode)
        {
            const int n = 4, count = n * n * n;
            var original = new Voxel[count];
            var signed = new Voxel[count];
            for (int i = 0; i < count; i++)
            {
                original[i] = new Voxel(i % 2 == 0 ? -10 : -100);
                original[i].FirstTextureIndex = 7;
                original[i].SecondTextureIndex = 5;
                original[i].NormalizedTextureLerp = .25f;
                signed[i] = new Voxel(mode == "outside-band" ? -6 : mode == "negative-band" ? -1 : i % 2 == 0 ? -1 : 1);
            }
            if (mode == "inside-depth") for (int i = 0; i < count; i++) signed[i] = new Voxel(1);
            byte[] InputBytes(Voxel[] v)
            {
                using (var array = new NativeArray<Voxel>(v, Allocator.Temp)) return array.Reinterpret<byte>(8).ToArray();
            }
            var before = InputBytes(original);
            var job = new BooleanCavernJob
            {
                SizeVox = n, SizeVox2 = n * n, HeightmapScale = new float3(1), ChunkAltitude = 0,
                Center = new float3(1), Size = new float3(2, 1, 2), ExteriorBand = 4.6875f,
                Brush = mode == "native-ellipsoid" ? BrushType.Sphere : BrushType.Custom,
                TextureIndex = 3, InputOriginVox = int3.zero, InputSizeVox = new int3(n),
                Voxels = new NativeArray<Voxel>(original, Allocator.TempJob),
                InputVoxels = new NativeArray<Voxel>(signed, Allocator.TempJob),
                Heights = new NativeArray<float>((n + 2) * (n + 2), Allocator.TempJob),
                Holes = new NativeArray<int>(n * n, Allocator.TempJob),
                NewHolesConcurrentCounter = new NativeArray<int>(1, Allocator.TempJob),
                ChangedSamples = new NativeArray<int>(2, Allocator.TempJob)
            };
            if (job.Brush == BrushType.Custom) job.Size = new float3(1);
            for (int i = 0; i < job.Heights.Length; i++) job.Heights[i] = mode == "native-holes" ? .25f : 1000;
            var result = new BooleanUnionControl();
            try
            {
                job.Schedule(count, 1).Complete();
                byte[] first = job.Voxels.Reinterpret<byte>(8).ToArray();
                result.values = job.Voxels.ToArray().Select(v => v.Value).ToArray();
                result.interiorChanges = job.ChangedSamples[0];
                result.exteriorChanges = job.ChangedSamples[1];
                result.newHoles = job.NewHolesConcurrentCounter[0];
                result.markedHoles = job.Holes.ToArray().Count(v => v > 0);
                result.outsidePayloadExact = first.SequenceEqual(before);
                result.originalInputExact = InputBytes(original).SequenceEqual(before);
                job.Schedule(count, 1).Complete();
                result.repeatPayloadExact = first.SequenceEqual(job.Voxels.Reinterpret<byte>(8).ToArray());
                if (mode == "union-order")
                {
                    var other = signed.Select(v => new Voxel(-v.Value)).ToArray();
                    job.InputVoxels.CopyFrom(other);
                    job.Schedule(count, 1).Complete();
                    var forward = job.Voxels.Reinterpret<byte>(8).ToArray();
                    job.Voxels.CopyFrom(original);
                    job.Schedule(count, 1).Complete();
                    job.InputVoxels.CopyFrom(signed);
                    job.Schedule(count, 1).Complete();
                    result.orderPayloadExact = forward.SequenceEqual(job.Voxels.Reinterpret<byte>(8).ToArray());
                }
                return result;
            }
            finally { job.Dispose(); }
        }
    }
}
#endif
