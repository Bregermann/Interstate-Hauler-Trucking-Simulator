#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class MooringDepthCandidate
        {public Vector3 hint;public Bounds actualTranslatedHullBounds;public float minimumKeelClearance,departureLateralClearance;public int depthSamples;public Vector3 shallowestPoint;}
        [Serializable]public sealed class MooringDepthProbe
        {public string status="READ_ONLY_SAVED20_BAY_QUERY_OF_ACTUAL21_HULL_NOT_NEW_WORLD_OR_VISUAL_ACCEPTANCE",savedWorld,failureReceipt,failureReceiptSha256;public bool sourceBytesPreserved;public float original21Clearance,saved20BaselineClearance;public MooringDepthCandidate[] candidates;}
        public static MooringDepthProbe LastMooringDepthProbe{get;private set;}
        public static void CaptureActualSavedBayMooringDepths()
        {
            string worldPath=Root+"VisualCandidates/RoundedIntegrated20/Shared Dragon Kingdom.prefab",failurePath=Argument("-tsDragonMooringFailureReceipt",Root+"VisualCandidates/RoundedIntegrated21/Integration Receipt.json");
            var failure=JsonUtility.FromJson<Receipt>(File.ReadAllText(failurePath));var hull=failure.finish?.mooredShip;
            if(failure.tag!="RoundedIntegrated21"||failure.status!="REJECTED_CANDIDATE"||hull==null||hull.passed||Mathf.Abs(hull.hullLength-32)>.01f||hull.waterlineWidth<5||hull.waterlineLength<15||!hull.originalFerryPoseExact||hull.sourceSha256!=AssetSha(hull.source)||hull.sourceMetaSha256!=AssetSha(hull.source+".meta"))throw new InvalidOperationException("Exact actual21 rejected shallow-mooring hull receipt/source required; no guessed dimensions.");
            var world=AssetDatabase.LoadAssetAtPath<GameObject>(worldPath)?.GetComponent<DragonKingdomWorld>();if(world==null)throw new InvalidOperationException("Existing saved passed20 world required; no regeneration.");var terrains=world.GetComponentsInChildren<Terrain>(true);if(terrains.Length!=16)throw new InvalidOperationException("Exact saved sixteen real terrain tiles required.");
            var files=terrains.Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).Concat(new[]{worldPath,hull.source,failurePath}).Distinct().SelectMany(p=>File.Exists(p+".meta")?new[]{p,p+".meta"}:new[]{p}).ToDictionary(p=>p,AssetSha);var ferry=world.extractionSites.Single(s=>s.vessel!=null).vessel;var records=new System.Collections.Generic.List<MooringDepthCandidate>();
            foreach(float x in new[]{805f,830f,855f})foreach(float z in new[]{-735f,-760f,-785f,-810f})
            {
                var bounds=hull.bounds;bounds.center+=new Vector3(x-805,0,z+735);var row=new MooringDepthCandidate{hint=new Vector3(x,0,z),actualTranslatedHullBounds=bounds,minimumKeelClearance=float.PositiveInfinity,departureLateralClearance=bounds.min.x-(ferry.transform.position.x+12)};
                for(int iz=0;iz<5;iz++)for(int ix=0;ix<5;ix++)
                {
                    var point=new Vector3(Mathf.Lerp(bounds.min.x,bounds.max.x,ix/4f),0,Mathf.Lerp(bounds.min.z,bounds.max.z,iz/4f));var terrain=terrains.Single(t=>{var local=point-t.transform.position;var size=t.terrainData.size;return local.x>=0&&local.z>=0&&local.x<size.x&&local.z<size.z;});var local=point-terrain.transform.position;var size=terrain.terrainData.size;float ground=terrain.terrainData.GetInterpolatedHeight(local.x/size.x,local.z/size.z)+terrain.transform.position.y;float clearance=hull.keel-ground;
                    if(float.IsNaN(clearance)||float.IsInfinity(clearance))throw new InvalidOperationException("Actual saved terrain returned invalid depth.");if(clearance<row.minimumKeelClearance){row.minimumKeelClearance=clearance;row.shallowestPoint=new Vector3(point.x,ground,point.z);}row.depthSamples++;
                }
                records.Add(row);
            }
            var result=new MooringDepthProbe{savedWorld=worldPath,failureReceipt=failurePath,failureReceiptSha256=AssetSha(failurePath),original21Clearance=hull.minimumKeelClearance,saved20BaselineClearance=records[0].minimumKeelClearance,candidates=records.ToArray(),sourceBytesPreserved=files.All(p=>AssetSha(p.Key)==p.Value)};LastMooringDepthProbe=result;Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"saved-bay-mooring-depth-probe.json"),JsonUtility.ToJson(result,true));if(!result.sourceBytesPreserved)throw new InvalidOperationException("Readonly bay query changed saved source bytes.");
        }
    }
}
#endif
