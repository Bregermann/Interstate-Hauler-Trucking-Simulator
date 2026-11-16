#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using sc.modeling.river.runtime;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class WaterFinish
        {
            public string scope="CANDIDATE_ONLY_EXISTING_RIVER_MODELER_LAVA_MESH_SEATED_ON_ACTUAL_ROCK_BED_NOT_NEW_TERRAIN_OR_GAMEPLAY";
            public int actualBedSamples,lavaVertices;public float maximumOriginalSurfaceGap,maximumSeatedSurfaceGap;public string sourceMesh,candidateMesh,material;
            public bool originalHeightmapsExact,originalColliderGeometryExact;public BedSample[] samples;
        }
        [Serializable]public sealed class BedSample{public Vector3 position;public float bed,originalSurface,seatedSurface;}
        static void InstallOwnedLanterns(DragonKingdomWorld world,string folder,LandmarkFinish evidence,List<Placement> records,IEnumerable<string> regionIds=null,Func<Bounds,bool> additionalClearance=null)
        {
            foreach(string id in regionIds??new[]{"caverns","village","harbor","refuge"})
            {
                var region=world.Region(id);int count=id=="caverns"?12:id=="refuge"?4:6;
                for(int i=0;i<count;i++)
                {
                    Vector3 p;
                    if(id=="caverns"){float a=(i+.25f)*Mathf.PI*2/count;p=new Vector3(520+Mathf.Cos(a)*70,105,780+Mathf.Sin(a)*45);}
                    else if(id=="refuge")p=region.root.position+new Vector3(i%2==0?-37:37,0,100+i/2*45);
                    else if(id=="harbor")p=new Vector3(i%2==0?666:770,0,-579-i/2*16);
                    else p=region.root.position+new Vector3(i%2==0?-58:58,0,-50-i/2*28);
                    var lamp=FinishPlace(world,folder,region,"props/P_PROP_lantern_04.prefab",p,2.8f,i*60,true,id=="caverns"?2:2.5f,evidence,records,"Owned working lantern "+id+" "+(i+1),false,id=="caverns",additionalClearance);
                    // The vendor documentation explicitly supplies one nested
                    // Light_lantern per PROP_lantern. Configure that actual
                    // source-backed light; a second copy doubled all28lamps.
                    var light=lamp.GetComponentsInChildren<Light>(true).SingleOrDefault();if(light==null)throw new InvalidOperationException("The actual source PROP_lantern must retain its one embedded Light_lantern.");
                    light.gameObject.name="Original vendor warm lantern light";
                    light.type=LightType.Point;light.lightmapBakeType=LightmapBakeType.Realtime;light.intensity=id=="caverns"?60:12;light.range=id=="caverns"?34:14;light.shadows=LightShadows.None;
                    evidence.sources.Add(DragonKingdomWorldAuthoring.Village+"light/Light_lantern.prefab");evidence.lanterns++;
                }
            }
            evidence.singleSourceLightPerLantern=true;
        }
        static WaterFinish FinishActualWaterways(DragonKingdomWorld world,string folder)
        {
            var flow=world.transform.Find("Ashen Crown molten river");if(flow==null)throw new InvalidOperationException("The original real RiverModeler molten channel must remain.");
            var model=flow.GetComponent<RiverModeler>();var filter=flow.GetComponent<MeshFilter>();var renderer=flow.GetComponent<MeshRenderer>();var presentation=flow.GetComponent<LWS.TruckSports.SportsCarnivalLavaSurface>();
            if(model==null||filter==null||filter.sharedMesh==null||renderer==null||presentation==null)throw new InvalidOperationException("Actual original vendor river and existing molten presentation required.");
            var evidence=new WaterFinish{sourceMesh=AssetDatabase.GetAssetPath(filter.sharedMesh)};var records=new List<BedSample>();
            var heights=world.GetComponentsInChildren<Terrain>(true).ToDictionary(t=>t,t=>HashHeights(t.terrainData));string physics=PhysicalColliderHash(world);
            // Retain the vendor-generated actual topology/UVs/flow direction.
            // Seat each visible molten vertex on its measured bed. The old
            // straight control heights floated up to15m over the real valley.
            // This is a thin flowing crust on solid basalt; no terrain or
            // collision is altered, and no original mesh asset is modified.
            var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="Measured actual molten bed surface";var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++)
            {
                Vector3 p=flow.TransformPoint(vertices[i]);float bed=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,p);float old=p.y;p.y=bed+.12f;vertices[i]=flow.InverseTransformPoint(p);
                evidence.maximumOriginalSurfaceGap=Mathf.Max(evidence.maximumOriginalSurfaceGap,old-bed);evidence.maximumSeatedSurfaceGap=Mathf.Max(evidence.maximumSeatedSurfaceGap,p.y-bed);evidence.actualBedSamples++;
                if(i%Mathf.Max(1,vertices.Length/128)==0)records.Add(new BedSample{position=p,bed=bed,originalSurface=old,seatedSurface=p.y});
            }
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();Directory.CreateDirectory(folder+"/Models");AssetDatabase.Refresh();evidence.candidateMesh=folder+"/Models/Measured molten bed surface.asset";AssetDatabase.CreateAsset(mesh,evidence.candidateMesh);filter.sharedMesh=mesh;evidence.lavaVertices=mesh.vertexCount;evidence.samples=records.ToArray();
            var material=new Material(presentation.moltenMaterial){name="Owned existing molten currents restrained exposure"};
            if(material.HasProperty("_HotColor"))material.SetColor("_HotColor",new Color(2.6f,.65f,.055f));if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",new Color(1.5f,.22f,.018f));
            evidence.material=folder+"/Materials/Finish/Actual molten rock flow.mat";AssetDatabase.CreateAsset(material,evidence.material);renderer.sharedMaterial=material;presentation.moltenMaterial=material;presentation.originalMaterials=new[]{material};presentation.worldEnvelope=renderer.bounds;EditorUtility.SetDirty(presentation);
            evidence.originalHeightmapsExact=heights.All(p=>HashHeights(p.Key.terrainData)==p.Value);evidence.originalColliderGeometryExact=physics==PhysicalColliderHash(world);
            if(!evidence.originalHeightmapsExact||!evidence.originalColliderGeometryExact||evidence.maximumSeatedSurfaceGap>.13f)throw new InvalidOperationException("Molten surface coordination changed original terrain/collision or remained unseated.");
            return evidence;
        }
        static string HashHeights(TerrainData data)
        {var values=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);var bytes=new byte[values.Length*sizeof(float)];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);return Hash(bytes);}
        static string PhysicalColliderHash(DragonKingdomWorld world)
        {var values=world.GetComponentsInChildren<Transform>(true).Select(t=>EditorJsonUtility.ToJson(t)).Concat(world.GetComponentsInChildren<Collider>(true).Select(c=>EditorJsonUtility.ToJson(c))).Concat(world.GetComponentsInChildren<Rigidbody>(true).Select(b=>EditorJsonUtility.ToJson(b)));return Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",values)));}
    }
}
#endif
