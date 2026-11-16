#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using System.Text;
using UnityEngine;
namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRoadSurfaceProbe
    {
        [Serializable]public sealed class RejectedWorldSignature
        {public string geometry,providers,rendererBindings,worldConfiguration;public int meshReferences,colliders,terrains;}
        // The already qualified saved-rock signature remains sole geometry,
        // terrain/payload and provider authority. This adds no new comparator.
        public static RejectedWorldSignature DescribeRejectedWorldWithoutMutation(DragonKingdomWorld world)
        {
            if(world==null)throw new ArgumentNullException(nameof(world));
            return new RejectedWorldSignature{geometry=StableSavedRockGeometry(world),providers=StableSavedRockProviders(world),rendererBindings=Hash(Encoding.UTF8.GetBytes(string.Join("\n",world.GetComponentsInChildren<Renderer>(true).Select(renderer=>RockStableJson(renderer,world.transform))))),worldConfiguration=Hash(Encoding.UTF8.GetBytes(RockStableJson(world,world.transform))),meshReferences=world.GetComponentsInChildren<MeshFilter>(true).Count(f=>f.sharedMesh!=null),colliders=world.GetComponentsInChildren<Collider>(true).Length,terrains=world.GetComponentsInChildren<Terrain>(true).Length};
        }
    }
}
#endif
