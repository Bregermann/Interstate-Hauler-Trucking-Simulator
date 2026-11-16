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
        [Serializable]public sealed class BearingHeightDiagnosis
        {
            public string scope="READ_ONLY_FAILED32_EXACT_PERSISTED_NATIVE_HEIGHT_CHECK_NOT_COLLIDER_RAY_OR_SUPPORT_ACCEPTANCE",status="IN_PROGRESS",error,data,expectedHeightSha256,actualHeightSha256,beforeAssetSha256,afterAssetSha256;
            public bool exactFailedNativeHeightfield,sourceBytesExact;public BearingHeightPoint[] samples;
        }
        [Serializable]public sealed class BearingHeightPoint
        {public Vector3 position,interpolatedNormal;public float groundY,pavementY=24,undersideY=19.67f;public bool heightMeetsExistingFilter,interpolatedNormalMeetsExistingFilter;}
        public static BearingHeightDiagnosis LastBearingHeightDiagnosis{get;private set;}
        public static void DiagnoseFailed32ForestBearingFromCommandLine()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonFailedWorldReceipt");
            if(at<0||at+1>=args.Length||!File.Exists(args[at+1]))throw new InvalidOperationException("Exact retained failed32 receipt is required; no guessed heightfield.");
            var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(args[at+1]));
            if(receipt.tag!="RoundedIntegrated32"||receipt.berth==null||!receipt.berth.passed||receipt.berth.tiles==null)throw new InvalidOperationException("Actual failed32 post-berth receipt required.");
            var row=receipt.berth.tiles.Single(t=>t.terrain.EndsWith("/Kingdom 0 0.asset",StringComparison.Ordinal));var proof=new BearingHeightDiagnosis{data=row.terrain,expectedHeightSha256=row.afterHeightSha256,beforeAssetSha256=AssetSha(row.terrain)};LastBearingHeightDiagnosis=proof;
            try
            {
                var data=AssetDatabase.LoadAssetAtPath<TerrainData>(row.terrain);if(data==null||data.heightmapResolution!=513||data.size!=new Vector3(1600,600,1600))throw new InvalidOperationException("Actual failed32 native terrain contract required.");
                proof.actualHeightSha256=HeightArraySha(data.GetHeights(0,0,513,513));proof.exactFailedNativeHeightfield=proof.actualHeightSha256==proof.expectedHeightSha256;
                if(!proof.exactFailedNativeHeightfield)throw new InvalidOperationException("Persisted failed32 data is stale relative to its real post-berth height SHA; instrument next actual candidate instead of inferring ground.");
                proof.samples=new[]{0f,-3f,3f,-4.5f,4.5f}.Select(d=>
                {var p=new Vector3(-780,0,306+d);float x=(p.x+1600)/1600,z=(p.z+800)/1600,ground=-20+data.GetInterpolatedHeight(x,z);var normal=data.GetInterpolatedNormal(x,z);return new BearingHeightPoint{position=p,groundY=ground,interpolatedNormal=normal,heightMeetsExistingFilter=ground<=24-.02f,interpolatedNormalMeetsExistingFilter=normal.y>.25f};}).ToArray();
                proof.status="EXACT_FAILED32_HEIGHTS_MEASURED_COLLIDER_BASE_RAYS_AND_PHYSICAL_REPAIR_PENDING";
            }
            catch(Exception error){proof.status="REJECTED_READ_ONLY_BEARING_DIAGNOSIS";proof.error=error.ToString();throw;}
            finally
            {proof.afterAssetSha256=AssetSha(proof.data);proof.sourceBytesExact=proof.beforeAssetSha256==proof.afterAssetSha256;Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"failed32-forest-bearing-height-diagnosis.json"),JsonUtility.ToJson(proof,true));if(!proof.sourceBytesExact)throw new InvalidOperationException("Read-only bearing diagnosis changed original candidate bytes.");}
        }
    }
}
#endif
