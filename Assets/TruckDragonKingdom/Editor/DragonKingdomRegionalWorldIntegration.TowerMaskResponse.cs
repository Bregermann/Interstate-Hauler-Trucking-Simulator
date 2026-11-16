#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class TowerMaskGraphProof{public string source,sourceSha256,owned,ownedSha256,scope="OWNED_VENDOR_DERIVED_DEPENDENCY_NOT_VENDOR_EDIT_OR_PUBLIC_PUBLICATION_APPROVAL";public bool onlyTwoDisconnectedInputValuesChanged,originalVendorExact;public string[] changedSlotIds;}
        [Serializable]public sealed class TowerMaskResponse{public string view,zeroFrame,lowFrame,highFrame;public int pixels,lowChangedByThree,highChangedByThree;public double lowMeanAbsoluteChannelDelta,highMeanAbsoluteChannelDelta;public bool readableNonzeroMaskResponse;}
        [Serializable]public sealed class TowerMaskReview{public string scope="TEMPORARY_CORRECTED_VENDOR_MASK_RESPONSE_NOT_FINISHED_APPEARANCE_OR_WORLD_ADOPTION",status="IN_PROGRESS",error;public TowerMaskGraphProof graph;public TowerAppearanceReview appearance;public TowerMaskResponse[] responses;}
        const string TowerMaskInstance="240416e0d5bb4e21934ffc6e19c5457e";
        static readonly string[] TowerMaskInputIds={"c3f6552eb2eb418580fe0dbfbe058d09","e5d2026b498c45eb9ad5e3d7d6471738"};
        [Serializable]sealed class TowerGraphHead{public TowerGraphEdge[] m_Edges;}
        [Serializable]sealed class TowerGraphEdge{public TowerGraphEndpoint m_InputSlot;}
        [Serializable]sealed class TowerGraphEndpoint{public TowerGraphNode m_Node;public int m_SlotId;}
        [Serializable]sealed class TowerGraphNode{public string m_Id;}
        static string TowerGraphInputObject(string source,string id)
        {
            string token="\"m_ObjectId\": \""+id+"\"";int at=source.IndexOf(token,StringComparison.Ordinal);if(at<0||source.IndexOf(token,at+token.Length,StringComparison.Ordinal)>=0)throw new InvalidOperationException("Exactly one actual vendor graph slot required: "+id);
            int start=source.LastIndexOf('{',at),end=source.IndexOf('}',at);if(start<0||end<0)throw new InvalidOperationException("Actual scalar graph slot required.");return source.Substring(start,end-start+1);
        }
        public static string CorrectDisconnectedTowerMaskInputs(string source)
        {
            if(string.IsNullOrEmpty(source))throw new InvalidOperationException("Actual installed vendor graph text required.");int headerEnd=source.IndexOf("\n}\n",StringComparison.Ordinal);if(headerEnd<0)headerEnd=source.IndexOf("\r\n}\r\n",StringComparison.Ordinal);if(headerEnd<0)throw new InvalidOperationException("Actual graph edge header missing.");
            // Read only the first graph JSON object; never reserialize vendor
            // graph contents or regenerate any original references/nodes.
            int close=source.IndexOf('}',headerEnd);var graph=JsonUtility.FromJson<TowerGraphHead>(source.Substring(0,close+1));if(graph?.m_Edges==null)throw new InvalidOperationException("Actual serialized graph edges required.");
            if(graph.m_Edges.Any(e=>e.m_InputSlot?.m_Node?.m_Id==TowerMaskInstance&&(e.m_InputSlot.m_SlotId==1043644200||e.m_InputSlot.m_SlotId==-1024115990)))throw new InvalidOperationException("Do not overwrite a connected/customized blend input.");
            string result=source;foreach(string id in TowerMaskInputIds)
            {
                string block=TowerGraphInputObject(result,id);string expectedName=id==TowerMaskInputIds[0]?"Blend Mask Amount":"Blend Mask Contrast";
                if(!block.Contains("\"m_DisplayName\": \""+expectedName+"\"")||!block.Contains("\"m_SlotType\": 0")||!block.Contains("\"m_Value\": 0.0,")||!block.Contains("\"m_DefaultValue\": 0.0,"))throw new InvalidOperationException("Only the actual unchanged disconnected zero-valued source inputs are eligible.");
                string corrected=block.Replace("\"m_Value\": 0.0,","\"m_Value\": 1.0,");result=result.Replace(block,corrected);
            }
            string restored=result;foreach(string id in TowerMaskInputIds){string block=TowerGraphInputObject(restored,id);restored=restored.Replace(block,block.Replace("\"m_Value\": 1.0,","\"m_Value\": 0.0,"));}
            if(restored!=source)throw new InvalidOperationException("Owned graph change exceeded exactly two scalar input values.");return result;
        }

        static TowerMaskResponse MeasureTowerMaskResponse(string view,TowerAppearanceFrame zero,TowerAppearanceFrame low,TowerAppearanceFrame high)
        {
            var textures=new[]{zero,low,high}.Select(f=>new Texture2D(2,2,TextureFormat.RGB24,false)).ToArray();
            try
            {
                var frames=new[]{zero,low,high};for(int i=0;i<3;i++)if(!ImageConversion.LoadImage(textures[i],File.ReadAllBytes(Io(frames[i].png)),false))throw new InvalidOperationException("Actual saved comparison PNG required.");var pixels=textures.Select(t=>t.GetPixels32()).ToArray();if(pixels.Any(p=>p.Length!=pixels[0].Length))throw new InvalidOperationException("Actual equal-size response images required.");
                var proof=new TowerMaskResponse{view=view,zeroFrame=zero.png,lowFrame=low.png,highFrame=high.png,pixels=pixels[0].Length};double lowTotal=0,highTotal=0;
                for(int i=0;i<proof.pixels;i++){var a=pixels[0][i];var b=pixels[1][i];var c=pixels[2][i];int lr=Math.Abs(a.r-b.r),lg=Math.Abs(a.g-b.g),lb=Math.Abs(a.b-b.b),hr=Math.Abs(a.r-c.r),hg=Math.Abs(a.g-c.g),hb=Math.Abs(a.b-c.b);lowTotal+=lr+lg+lb;highTotal+=hr+hg+hb;if(Math.Max(lr,Math.Max(lg,lb))>=3)proof.lowChangedByThree++;if(Math.Max(hr,Math.Max(hg,hb))>=3)proof.highChangedByThree++;}
                proof.lowMeanAbsoluteChannelDelta=lowTotal/(proof.pixels*3);proof.highMeanAbsoluteChannelDelta=highTotal/(proof.pixels*3);proof.readableNonzeroMaskResponse=proof.lowChangedByThree>=proof.pixels*.01&&proof.highChangedByThree>=proof.pixels*.01&&proof.lowMeanAbsoluteChannelDelta>=.35&&proof.highMeanAbsoluteChannelDelta>=proof.lowMeanAbsoluteChannelDelta*1.1;return proof;
            }
            finally{foreach(var texture in textures)UnityEngine.Object.DestroyImmediate(texture);}
        }

    }
}
#endif
