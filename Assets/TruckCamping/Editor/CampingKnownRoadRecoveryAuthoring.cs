using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyRoads3Dv3;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckCamping.Editor
{
    public static partial class CampingWildernessAuthoring
    {
        const string ReviewedRoadAsset = "Assets/TruckCamping/Models/Camp Haul scenic woodland loop EasyRoads mesh 0.asset";
        const string ReviewedRoadSha = "2034fbc5acb869fbae2e177d8c51ede77fee4db50b14d9a64af782641dbb9b2f";
        const string ReviewedRoadMetaSha = "912f571b4ac8bc14bc299b8a77148aea7d6d49a37c6f27cce015d0eb5a011ed6";
        [Serializable] sealed class RoadRecoveryProof
        {
            public string status="EXACT_KNOWN_FAILED_AUTHOR_ROAD_REGENERATED",asset,guid,oldSha256,metaSha256;
            public int vertices,submeshes;public float maximumVerticalDelta,maximumHorizontalDelta;
            public bool originalIndicesColorsExact,originalHorizontalBoundsExact,perVertexHorizontalVariationWithinTwoMillimetres,completeVendorStreamsCopied,originalMetaBytesExact;
        }
        static Transform RegenerateExactReviewedRoad(Material material)
        {
            if(!resumingKnownPartial)throw new InvalidOperationException("Road recovery is exclusive to the explicit exact known partial-world resume.");
            // This gate still requires ALL127 reviewed original bytes, including
            // this road and meta, before any persistent road content is copied.
            VerifyRetainedInputs(false);
            if(Digest(ReviewedRoadAsset)!=ReviewedRoadSha||Digest(ReviewedRoadAsset+".meta")!=ReviewedRoadMetaSha)
                throw new InvalidOperationException("Unknown/custom road bytes preserved; exact failed-author mesh and meta required.");
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(ReviewedRoadAsset);
            if(existing==null)throw new InvalidOperationException("Original retained road Mesh required.");
            string guid=AssetDatabase.AssetPathToGUID(ReviewedRoadAsset);byte[] meta=File.ReadAllBytes(ReviewedRoadAsset+".meta");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab");
            if(prefab==null)throw new InvalidOperationException("Installed EasyRoads public API/network prefab required.");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var result=new GameObject("Camp Haul scenic woodland loop · EasyRoads generated course").transform;
            try
            {
                var owner=instance.GetComponent<ERModularBase>();owner.RoadNetworkInit();var network=new ERRoadNetwork(owner);
                var type=new ERRoadType{roadTypeName="Camp Haul scenic woodland loop",roadWidth=14,hasMeshCollider=true,roadMaterial=material};
                var markers=Trail.Select(p=>Ground(p,.09f)).ToArray();var road=network.CreateRoad(type.roadTypeName,type,markers,Enumerable.Repeat(ERMarkerControlType.StraightXZ,markers.Length).ToArray());
                if(road==null)throw new InvalidOperationException("EasyRoads returned no original course.");
                road.SetMeshCollider(true);road.SetTerrainDeformation(false,false);road.SnapToTerrain(false,false);road.SetResolution(.35f,false);road.Refresh();
                var component=road.GetType().GetField("roadScript").GetValue(road) as Component;
                if(component==null)throw new InvalidOperationException("Vendor course owner required.");
                var colliders=component.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.enabled&&!c.isTrigger&&c.sharedMesh!=null&&c.sharedMesh.vertexCount>2).ToArray();
                if(colliders.Length!=1)throw new InvalidOperationException("Unknown vendor road layout preserved; exact single original physical surface required.");
                var source=colliders[0];
                if(!source.transform.position.Equals(Vector3.zero)||!source.transform.rotation.Equals(Quaternion.identity)||!source.transform.lossyScale.Equals(Vector3.one))
                    throw new InvalidOperationException("Unknown generated road frame preserved.");
                float delta=ValidateReviewedRoadGeometry(source.sharedMesh,existing);float horizontalDelta=HorizontalRoadDelta(source.sharedMesh,existing);
                CopyReviewedRoadGeometry(source.sharedMesh,existing);
                if(AssetDatabase.AssetPathToGUID(ReviewedRoadAsset)!=guid||!meta.SequenceEqual(File.ReadAllBytes(ReviewedRoadAsset+".meta")))
                    throw new InvalidOperationException("Original road GUID/meta changed; retained backup must be reviewed.");
                var piece=new GameObject("EasyRoads physical surface 0");piece.transform.SetParent(result,false);
                piece.AddComponent<MeshFilter>().sharedMesh=existing;piece.AddComponent<MeshRenderer>().sharedMaterial=material;piece.AddComponent<MeshCollider>().sharedMesh=existing;
                string directory="Logs/TruckSports/Impossible07/CampingNavigationResume";Directory.CreateDirectory(directory);
                File.WriteAllText(directory+"/reviewed-road-recovery.json",JsonUtility.ToJson(new RoadRecoveryProof{asset=ReviewedRoadAsset,guid=guid,oldSha256=ReviewedRoadSha,metaSha256=ReviewedRoadMetaSha,vertices=existing.vertexCount,submeshes=existing.subMeshCount,maximumVerticalDelta=delta,maximumHorizontalDelta=horizontalDelta,originalIndicesColorsExact=true,originalHorizontalBoundsExact=true,perVertexHorizontalVariationWithinTwoMillimetres=true,completeVendorStreamsCopied=true,originalMetaBytesExact=true},true));
                return result;
            }
            catch{UnityEngine.Object.DestroyImmediate(result.gameObject);throw;}
            finally{UnityEngine.Object.DestroyImmediate(instance);}
        }
        public static float ValidateReviewedRoadGeometry(Mesh source,Mesh original)
        {
            if(source==null||original==null||!source.isReadable||!original.isReadable||source.vertexCount!=15496||original.vertexCount!=15496||source.indexFormat!=original.indexFormat||source.subMeshCount!=1||original.subMeshCount!=1)
                throw new InvalidOperationException("Exact known readable15496-vertex/one-submesh original road layout required.");
            if(source.GetTopology(0)!=original.GetTopology(0)||!source.GetIndices(0).SequenceEqual(original.GetIndices(0)))
                throw new InvalidOperationException("Original road topology/indices changed.");
            if(source.blendShapeCount!=0||original.blendShapeCount!=0||!source.bindposes.SequenceEqual(original.bindposes)||!source.boneWeights.SequenceEqual(original.boneWeights))
                throw new InvalidOperationException("Unknown road deformation data preserved.");
            if(!source.bounds.center.x.Equals(original.bounds.center.x)||!source.bounds.center.z.Equals(original.bounds.center.z)||!source.bounds.size.x.Equals(original.bounds.size.x)||!source.bounds.size.z.Equals(original.bounds.size.z))
                throw new InvalidOperationException("Original road outer horizontal bounds changed.");
            var a=source.vertices;var b=original.vertices;float maximum=0;
            for(int i=0;i<a.Length;i++)
            {
                                if(!FiniteRoad(a[i]))throw new InvalidOperationException("Nonfinite generated road position.");
                // R17 full15,496-vertex buffers prove X1.0834mm/Z.5951mm
                // variation, even when the same1km recipe is repeated. Permit
                // at most2mm per horizontal axis ONLY after original-file SHA
                // preflight; outer bounds, indices and colors stay exact.
                if(Mathf.Abs(a[i].x-b[i].x)>.002f||Mathf.Abs(a[i].z-b[i].z)>.002f)
                    throw new InvalidOperationException("Generated road exceeds measured two-millimetre horizontal variation at vertex "+i+" dx="+(a[i].x-b[i].x).ToString("R")+" dz="+(a[i].z-b[i].z).ToString("R"));
                maximum=Mathf.Max(maximum,Mathf.Abs(a[i].y-b[i].y));
            }
            // Actual three native regenerations (including same1km repeated)
            // differ vertically by max.038253m, not by terrain scale. Retain
            // the vendor's own surface variation, bounded to5cm; this never
            // admits a customized INPUT because its whole SHA must match first.
            if(maximum>.05f)throw new InvalidOperationException("Generated road exceeds measured bounded vendor surface variation.");
            if(!source.colors.SequenceEqual(original.colors))throw new InvalidOperationException("Original road vertex colors changed.");
            if(source.normals.Length!=a.Length||source.normals.Any(v=>!FiniteRoad(v))||source.tangents.Length!=a.Length||source.tangents.Any(v=>!FiniteRoad(v)))
                throw new InvalidOperationException("Invalid generated road normal/tangent stream.");
            var sourceUv=new List<Vector4>();var originalUv=new List<Vector4>();
            for(int channel=0;channel<8;channel++)
            {
                sourceUv.Clear();originalUv.Clear();source.GetUVs(channel,sourceUv);original.GetUVs(channel,originalUv);
                if(sourceUv.Count!=originalUv.Count||sourceUv.Any(v=>!FiniteRoad(v)))throw new InvalidOperationException("Invalid generated road UV layout/channel "+channel);
            }
            return maximum;
        }
        static float HorizontalRoadDelta(Mesh source,Mesh original)
        {
            var a=source.vertices;var b=original.vertices;float result=0;
            for(int i=0;i<a.Length;i++)result=Mathf.Max(result,Mathf.Abs(a[i].x-b[i].x),Mathf.Abs(a[i].z-b[i].z));
            return result;
        }
        static bool FiniteRoad(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
        static bool FiniteRoad(Vector4 v)=>FiniteRoad((Vector3)v)&&float.IsFinite(v.w);
        static void CopyReviewedRoadGeometry(Mesh source,Mesh target)
        {
            ValidateReviewedRoadGeometry(source,target);string name=target.name;var flags=target.hideFlags;
            // Copies every native vertex channel and submesh. Unlike a generic
            // vertex/uv0 setter, this preserves actual vendor UV3 and future
            // complete already-validated source streams without temporary assets.
            EditorUtility.CopySerialized(source,target);target.name=name;target.hideFlags=flags;
            if(!LWS.TruckSports.Editor.SportsEasyRoadsAuthoring.EquivalentPreservedGeometry(source,target,out var reason))
                throw new InvalidOperationException("Complete vendor road stream copy failed: "+reason);
            EditorUtility.SetDirty(target);
        }
    }
}
