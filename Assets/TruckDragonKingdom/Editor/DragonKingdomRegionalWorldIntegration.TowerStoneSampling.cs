#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        const string TowerPaintedBrick=DragonKingdomWorldAuthoring.Art+"Tidal Flask Studios/Fantastic Village Pack/2d/textures/T_stonebrick_02.png";
        const string TowerSamplingUvNode="8c4e4eadc0624699b9e992e3595b51f1",TowerSamplingScaleSlot="8fcfd224a97b452ba9deeb3697af9433",TowerSamplingPropertyNode="b7a72872b1ef4e609cd3853ee46f59dd";
        public const string TowerSamplingEdge="{\"m_OutputSlot\":{\"m_Node\":{\"m_Id\":\"b7a72872b1ef4e609cd3853ee46f59dd\"},\"m_SlotId\":0},\"m_InputSlot\":{\"m_Node\":{\"m_Id\":\"8c4e4eadc0624699b9e992e3595b51f1\"},\"m_SlotId\":447017714}},";
        [Serializable]public sealed class TowerStoneGraphProof{public string source,sourceSha256,owned,ownedSha256,scope="OWNED_VENDOR_DERIVED_DEPENDENCY_NOT_VENDOR_EDIT_OR_PUBLIC_PUBLICATION_APPROVAL",addedSamplingEdge;public bool onlyTwoInputsAndOneUvEdgeChanged,originalVendorExact;}
        [Serializable]public sealed class TowerStoneResponse{public string view,texture;public int measuredTowerPixels,lowChangedByThree,highChangedByThree;public double lowMeanAbsoluteChannelDelta,highMeanAbsoluteChannelDelta;public bool originalGlobalResponseThresholdPassed,measuredTowerResponsePassed;public TowerMaskResponse originalGlobalResponse;}
        [Serializable]public sealed class TowerStoneSamplingReview
        {
            public string scope="CORRECTED_ACTUAL_UV_SAMPLING_AND_PAINTED_BRICK_COMPARISON_NOT_FINISHED_APPEARANCE_OR_ADOPTION",status="IN_PROGRESS",stage,error,cleanupError,paintedBrick=TowerPaintedBrick;public int measuredOriginalColliders,measuredTerrains,measuredPerches,sourceMaterials;public TowerStoneGraphProof graph;public TowerAppearanceReview appearance;public TowerStoneResponse[] responses;public TowerAppearanceFrame[] measuredTowerMasks;
        }
        static string TowerSamplingPropertyObject(string source)
        {
            string token="\"m_ObjectId\": \""+TowerSamplingPropertyNode+"\"";int at=source.IndexOf(token,StringComparison.Ordinal);if(at<0||source.IndexOf(token,at+token.Length,StringComparison.Ordinal)>=0)throw new InvalidOperationException("Exactly one existing UV-scale property node required.");int begin=source.LastIndexOf('{',at),end=source.IndexOf("\n}",at,StringComparison.Ordinal);if(begin<0||end<0)throw new InvalidOperationException("Complete original property node required.");string block=source.Substring(begin,end+2-begin);var node=JsonUtility.FromJson<TowerSamplingPropertyBinding>(block);if(node?.m_Property?.m_Id!="55dde98cf416410d914b1749187c050d")throw new InvalidOperationException("Existing property node no longer binds actual UV1 scale authority.");return block;
        }
        [Serializable]sealed class TowerSamplingPropertyBinding{public TowerGraphNode m_Property;}
        public static string CorrectTowerSecondarySampling(string source)
        {
            string corrected=CorrectDisconnectedTowerMaskInputs(source);int headEnd=source.IndexOf("\n}\n",StringComparison.Ordinal);if(headEnd<0)headEnd=source.IndexOf("\r\n}\r\n",StringComparison.Ordinal);if(headEnd<0)throw new InvalidOperationException("Actual graph root missing.");var head=JsonUtility.FromJson<TowerGraphHead>(source.Substring(0,source.IndexOf('}',headEnd)+1));
            if(head.m_Edges.Any(e=>e.m_InputSlot?.m_Node?.m_Id==TowerSamplingUvNode&&e.m_InputSlot.m_SlotId==447017714))throw new InvalidOperationException("Do not overwrite an existing/customized secondary UV scale binding.");
            string slot=TowerGraphInputObject(source,TowerSamplingScaleSlot),property=TowerSamplingPropertyObject(source);
            if(!slot.Contains("\"m_Id\": 447017714")||!slot.Contains("\"m_DisplayName\": \"UV1 Scale\"")||!slot.Contains("\"m_Value\": 0.0,")||!source.Contains("\"m_ObjectId\": \"55dde98cf416410d914b1749187c050d\"")||!property.Contains("55dde98cf416410d914b1749187c050d"))throw new InvalidOperationException("Exact original missing sampling-scale authority required; never guess shader bindings.");
            const string marker="\"m_Edges\": [";int at=corrected.IndexOf(marker,StringComparison.Ordinal);if(at<0)throw new InvalidOperationException("Actual graph edge array required.");corrected=corrected.Insert(at+marker.Length,TowerSamplingEdge);if(corrected.Replace(TowerSamplingEdge,"")!=CorrectDisconnectedTowerMaskInputs(source))throw new InvalidOperationException("Owned graph changed beyond exactly two blend constants and one existing-property UV edge.");return corrected;
        }

        static TowerAppearanceFrame CaptureMeasuredTowerPixels(Camera camera,DragonKingdomWorld world,Renderer[] towers,string output,string view,int width,int height,Vector3 target)
        {
            var states=world.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).ToDictionary(r=>r,r=>r.enabled);var originals=towers.ToDictionary(r=>r,r=>r.sharedMaterials);var clear=camera.clearFlags;var background=camera.backgroundColor;var shader=Shader.Find("Universal Render Pipeline/Unlit");if(shader==null)throw new InvalidOperationException("Installed Unity URP Unlit required for measured tower-pixel isolation.");var white=new Material(shader){hideFlags=HideFlags.HideAndDontSave,name="TEMP actual tower pixel measurement"};white.SetColor("_BaseColor",Color.white);white.SetTexture("_BaseMap",Texture2D.whiteTexture);
            try{foreach(var p in states)p.Key.enabled=originals.ContainsKey(p.Key);foreach(var p in originals)p.Key.sharedMaterials=p.Value.Select(_=>white).ToArray();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;return TowerCapture(camera,output,view+"-measured-tower-pixels",width,height,target,new[]{shader});}
            finally{foreach(var p in originals)p.Key.sharedMaterials=p.Value;foreach(var p in states)p.Key.enabled=p.Value;camera.clearFlags=clear;camera.backgroundColor=background;UnityEngine.Object.DestroyImmediate(white);}
        }
        static TowerStoneResponse MeasureTowerStoneResponse(string view,string texture,TowerAppearanceFrame mask,TowerAppearanceFrame zero,TowerAppearanceFrame low,TowerAppearanceFrame high)
        {
            var frames=new[]{mask,zero,low,high};var images=frames.Select(f=>new Texture2D(2,2,TextureFormat.RGB24,false)).ToArray();
            try
            {
                for(int i=0;i<4;i++)if(!ImageConversion.LoadImage(images[i],File.ReadAllBytes(Io(frames[i].png)),false))throw new InvalidOperationException("Actual saved ROI/response image required.");var pixels=images.Select(t=>t.GetPixels32()).ToArray();if(pixels.Any(p=>p.Length!=pixels[0].Length))throw new InvalidOperationException("Actual equal-size measured pixels required.");var result=new TowerStoneResponse{view=view,texture=texture,originalGlobalResponse=MeasureTowerMaskResponse(view,zero,low,high)};double ld=0,hd=0;
                for(int i=0;i<pixels[0].Length;i++){var id=pixels[0][i];if(id.r<245||id.g<245||id.b<245)continue;result.measuredTowerPixels++;var a=pixels[1][i];var b=pixels[2][i];var c=pixels[3][i];int lr=Math.Abs(a.r-b.r),lg=Math.Abs(a.g-b.g),lb=Math.Abs(a.b-b.b),hr=Math.Abs(a.r-c.r),hg=Math.Abs(a.g-c.g),hb=Math.Abs(a.b-c.b);ld+=lr+lg+lb;hd+=hr+hg+hb;if(Math.Max(lr,Math.Max(lg,lb))>=3)result.lowChangedByThree++;if(Math.Max(hr,Math.Max(hg,hb))>=3)result.highChangedByThree++;}
                if(result.measuredTowerPixels==0)throw new InvalidOperationException("No actual isolated native tower pixels; never substitute a hand-picked rectangle.");result.lowMeanAbsoluteChannelDelta=ld/(result.measuredTowerPixels*3);result.highMeanAbsoluteChannelDelta=hd/(result.measuredTowerPixels*3);result.originalGlobalResponseThresholdPassed=result.originalGlobalResponse.readableNonzeroMaskResponse;result.measuredTowerResponsePassed=result.measuredTowerPixels>=1000&&result.lowChangedByThree>=result.measuredTowerPixels*.05&&result.highChangedByThree>=result.measuredTowerPixels*.1&&result.lowMeanAbsoluteChannelDelta>=.75&&result.highMeanAbsoluteChannelDelta>=result.lowMeanAbsoluteChannelDelta*1.1;return result;
            }
            finally{foreach(var image in images)UnityEngine.Object.DestroyImmediate(image);}
        }
        public static void RecordTowerStoneSamplingFailure(TowerStoneSamplingReview review,Exception error)
        {if(review==null||error==null)throw new ArgumentNullException();review.error=error.ToString();review.status="REJECTED_ACTUAL_SAMPLING_OR_RENDER_RESPONSE";if(review.appearance!=null){review.appearance.status=review.status;review.appearance.error=review.error;}}



    }
}
#endif
