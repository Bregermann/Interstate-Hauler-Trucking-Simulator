using System;
using UnityEngine;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Polygonizers;
namespace LWS.TruckArchaeology
{
    // Configuration adapter for the installed vendor extension point only.
    // Identical to VoxelChunk's built-in null-provider fallback, including
    // default smooth style. All terrain/voxel/mesh/jobs remain vendor code.
    [DisallowMultipleComponent]
    public sealed class ArchaeologyDefaultPolygonizerProvider:APolygonizerProvider
    {
        // The vendor default allocates its native job data lazily in BuildMesh;
        // this factory has no additional initialization or mesh state.
        public override void Init(){}
        public override IPolygonizer NewPolygonizer(DiggerSystem digger)
        {
            if(digger==null)throw new ArgumentNullException(nameof(digger));
            return new MarchingCubesPolygonizer();
        }
        // Null deliberately preserves DiggerSystemEditor's ordinary URP material
        // path. A nonnull array selects its distinct block-material pipeline.
        public override Material[] GetMaterials()=>null;
    }
}
