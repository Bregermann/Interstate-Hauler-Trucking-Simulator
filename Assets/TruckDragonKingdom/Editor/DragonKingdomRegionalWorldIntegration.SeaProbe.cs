#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using sc.modeling.river.runtime;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class SeaTangentVariant
        {public string configuration,geometryError;public CoastalBand geometry;public float minimumTangentMagnitude,maximumTangentZ;}
        [Serializable]public sealed class SeaTangentProbe
        {public string scope="ACTUAL_VENDOR_SEA_ONLY_TWO_ENDPOINT_HANDLE_AB_NOT_WORLD_OR_RENDER_ACCEPTANCE";public SeaTangentVariant zeroHandleLinear,uniformCollinearHandles;public bool sourceBytesPreserved;}
        public static SeaTangentProbe LastSeaTangentProbe{get;private set;}
        static Spline UniformStraightSeaSpline(float seaLevel)
        {
            // Zero LINEAR handles leave tiny endpoint derivatives. At12.4km,
            // cancellation in the constant800m Z coordinate perturbs the frame.
            // Public explicit collinear cubic controls give the SAME straight
            // segment, with a nonzero constant derivative over its whole span.
            const float handle=12400f/3;var spline=new Spline();
            foreach(float x in new[]{-6200f,6200f})spline.Add(new BezierKnot(new float3(x,seaLevel,800),new float3(-handle,0,0),new float3(handle,0,0),quaternion.identity),TangentMode.Broken);
            return spline;
        }
        public static void CaptureActualVendorSeaTangentAb()
        {
            const string materialPath="Assets/RiverModeler/ShaderGraph/Default River.mat";var files=AssetDatabase.GetDependencies(materialPath,true).SelectMany(p=>new[]{p,p+".meta"}).Where(File.Exists).Distinct().ToDictionary(p=>p,AssetSha);var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var a=GenerateSeaTangentVariant(scene,false);var b=GenerateSeaTangentVariant(scene,true);var result=new SeaTangentProbe{zeroHandleLinear=a,uniformCollinearHandles=b,sourceBytesPreserved=files.All(p=>AssetSha(p.Key)==p.Value)};LastSeaTangentProbe=result;Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"actual-vendor-sea-tangent-ab.json"),JsonUtility.ToJson(result,true));
                if(!result.sourceBytesPreserved||!b.geometry.exactStraightSeaGeometry)throw new InvalidOperationException("Actual public uniform-tangent sea failed strict geometry/source guards: "+JsonUtility.ToJson(result));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        static SeaTangentVariant GenerateSeaTangentVariant(Scene scene,bool uniform)
        {
            var root=new GameObject("Temporary actual vendor sea handle probe");SceneManager.MoveGameObjectToScene(root,scene);var container=root.AddComponent<SplineContainer>();container.Spline=new Spline();foreach(float x in new[]{0f,2f})container.Spline.Add(new BezierKnot(new float3(x,-2,800)),TangentMode.Linear);
            var filter=root.AddComponent<MeshFilter>();var renderer=root.AddComponent<MeshRenderer>();var material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/RiverModeler/ShaderGraph/Default River.mat"));renderer.sharedMaterial=material;var river=root.AddComponent<RiverModeler>();
            try
            {
                river.rebuildTriggers=RiverModeler.RebuildTriggers.None;river.meshFilter=filter;river.splineContainer=container;river.settings.shape.width=12400;river.settings.triangulation.vertexDistance=160;river.settings.triangulation.vertexDistanceWidth=160;
                var spline=uniform?UniformStraightSeaSpline(-2):new Spline();if(!uniform)foreach(float x in new[]{-6200f,6200f})spline.Add(new BezierKnot(new float3(x,-2,800)),TangentMode.Linear);container.Spline=spline;river.Rebuild();var proof=new SeaTangentVariant{configuration=uniform?"TWO_ENDPOINTS_PUBLIC_EXPLICIT_COLLINEAR_ONE_THIRD_HANDLES":"TWO_ENDPOINTS_LINEAR_ZERO_HANDLES_ACTUAL22_REGRESSION",geometry=new CoastalBand{seaLevel=-2,seaVertices=filter.sharedMesh.vertexCount},minimumTangentMagnitude=float.PositiveInfinity};
                for(int i=0;i<=78;i++){float t=Mathf.Clamp(i/78f,.00001f,.99999f);var tangent=spline.EvaluateTangent(t);proof.minimumTangentMagnitude=Mathf.Min(proof.minimumTangentMagnitude,math.length(tangent));proof.maximumTangentZ=Mathf.Max(proof.maximumTangentZ,math.abs(tangent.z));}
                try{ValidateStraightSeaGeometry(root.transform,filter.sharedMesh,spline.Count,proof.geometry);}catch(InvalidOperationException error){proof.geometryError=error.Message;}
                return proof;
            }
            finally{var mesh=filter.sharedMesh;UnityEngine.Object.DestroyImmediate(root);if(mesh!=null&&!EditorUtility.IsPersistent(mesh))UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);}
        }
    }
}
#endif
