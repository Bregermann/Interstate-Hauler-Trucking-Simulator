#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckSports;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class DeckWheelSeating
        {
            public string scope="READ_ONLY_ACTUAL_RENDERED_WHEEL_AND_TIMBER_FACE_GEOMETRY_NO_SUSPENSION_POSE_OR_SUPPORT_CHANGE",context;
            public int visibleDeckMeshes,inactiveDeckMeshes;
            public float visibleDeckTopOffset,allIncludingInactiveDeckTopOffset;
            public Vector3 supportPosition,supportUp,deckLossyScale;
            public string[] actualDeckSources;
            public DeckWheel[] wheels;
        }
        [Serializable]sealed class DeckWheel
        {
            public string name,visual,visualAuthority,vendorVisual,deckFaceSource;
            public bool controllerEnabled,actualVisualFound,deckFaceFound;
            public int sampledVisibleVertices;
            public float radius,springLength,maximumSpringLength,actualGap,theoreticalDroopToActualWheelBottom;
            public Vector3 actualRenderedBottom,theoreticalFullDroopBottom,actualDeckFace,deckFaceNormal;
        }
        struct ActualDeckTriangle{public Vector3 a,b,c;public string source;}
        public static string ObserveActualTruckDeckSeating(DragonRiderSaddle frame,SportsEventTruck truck)
        {
            if(frame==null||truck==null||frame.supportPresentation==null)throw new InvalidOperationException("Actual original truck and saved timber presentation required for seating observation.");
            return JsonUtility.ToJson(MeasureActualDeckSeating(truck,frame.supportPresentation,frame.ReadPose().rotation*Vector3.up,Application.isPlaying?"ACTUAL_NATIVE_PLAYMODE_ORIGINAL_PARTICIPANT":"READ_ONLY_EDITOR_ORIGINAL_GEOMETRY"),true);
        }
        static DeckWheelSeating MeasureActualDeckSeating(SportsEventTruck truck,Transform deck,Vector3 up,string context)
        {
            up.Normalize();var report=new DeckWheelSeating{context=context,supportPosition=deck.position,supportUp=up,deckLossyScale=deck.lossyScale};var triangles=new List<ActualDeckTriangle>();var sources=new HashSet<string>();
            float visibleTop=float.NegativeInfinity,allTop=float.NegativeInfinity;
            foreach(var filter in deck.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||filter.sharedMesh==null)continue;
                bool visible=VisibleWithin(filter.transform,deck);if(visible)report.visibleDeckMeshes++;else report.inactiveDeckMeshes++;
                string source=AssetDatabase.GetAssetPath(filter.sharedMesh);sources.Add(source);var mesh=filter.sharedMesh;
                using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
                using(var values=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp))
                {
                    data[0].GetVertices(values);var world=new Vector3[values.Length];
                    for(int index=0;index<world.Length;index++){world[index]=filter.transform.TransformPoint(values[index]);float height=Vector3.Dot(world[index]-deck.position,up);allTop=Mathf.Max(allTop,height);if(visible)visibleTop=Mathf.Max(visibleTop,height);}
                    if(!visible)continue;
                    for(int part=0;part<data[0].subMeshCount;part++)
                    {
                        var sub=data[0].GetSubMesh(part);if(sub.topology!=MeshTopology.Triangles)continue;
                        for(int index=0;index+2<sub.indexCount;index+=3)
                        {
                            int a=ReadActualIndex(data[0],mesh.indexFormat,sub.indexStart+index)+sub.baseVertex,b=ReadActualIndex(data[0],mesh.indexFormat,sub.indexStart+index+1)+sub.baseVertex,c=ReadActualIndex(data[0],mesh.indexFormat,sub.indexStart+index+2)+sub.baseVertex;
                            triangles.Add(new ActualDeckTriangle{a=world[a],b=world[b],c=world[c],source=source+"/"+filter.name});
                        }
                    }
                }
            }
            if(report.visibleDeckMeshes==0||!float.IsFinite(visibleTop)||!float.IsFinite(allTop))throw new InvalidOperationException("Actual visible timber mesh geometry is missing; bounds or inactive alternatives cannot substitute.");
            report.visibleDeckTopOffset=visibleTop;report.allIncludingInactiveDeckTopOffset=allTop;report.actualDeckSources=sources.OrderBy(value=>value,StringComparer.Ordinal).ToArray();var wheels=new List<DeckWheel>();
            for(int wheelIndex=0;wheelIndex<truck.wheels.Length;wheelIndex++)
            {
                var wheel=truck.wheels[wheelIndex];if(wheel==null)continue;
                Transform visual=null;string authority="PUBLIC_NWH_WHEEL_VISUAL";
                if(truck.appearanceHost!=null&&truck.appearanceHost.AppearanceRoot!=null)
                {
                    var matches=truck.appearanceHost.AppearanceRoot.GetComponentsInChildren<SportsTruckAppearanceMountTag>(true).Where(value=>value.slotId==SportsTruckAppearanceSlots.Wheels&&value.wheelIndex==wheelIndex).ToArray();
                    if(matches.Length>1)throw new InvalidOperationException("Actual composed appearance has duplicate semantic wheel anchors; do not guess which mesh to measure.");
                    if(matches.Length==1){visual=matches[0].transform;authority="EXISTING_COMPOSED_APPEARANCE_WHEEL_LINKED_TO_SAME_NWH_INDEX";}
                }
                var vendorVisual=wheel.wheel.rotatingContainer!=null?wheel.WheelVisual:null;if(visual==null&&vendorVisual!=null)visual=vendorVisual.transform;
                var record=new DeckWheel{name=wheel.name,controllerEnabled=wheel.enabled,radius=wheel.wheel.radius,springLength=wheel.spring.length,maximumSpringLength=wheel.spring.maxLength,visual=visual!=null?AnimationUtility.CalculateTransformPath(visual,truck.transform):"MISSING_PUBLIC_NWH_OR_COMPOSED_WHEEL_VISUAL",visualAuthority=authority,vendorVisual=vendorVisual!=null?AnimationUtility.CalculateTransformPath(vendorVisual.transform,truck.transform):"MISSING_VENDOR_VISUAL"};
                record.theoreticalFullDroopBottom=wheel.transform.position-up*(record.radius+record.maximumSpringLength);
                if(visual!=null)
                {
                    var points=ReadVisibleModelVertices(visual);record.sampledVisibleVertices=points.Count;
                    if(points.Count>0)
                    {
                        float bottom=points.Min(point=>Vector3.Dot(point,up));var contact=points.Where(point=>Vector3.Dot(point,up)<=bottom+.002f).ToArray();record.actualRenderedBottom=contact.Aggregate(Vector3.zero,(sum,point)=>sum+point)/contact.Length;record.actualVisualFound=true;
                        record.theoreticalDroopToActualWheelBottom=Vector3.Dot(record.actualRenderedBottom-record.theoreticalFullDroopBottom,up);
                        Vector3 origin=record.actualRenderedBottom+up*10;float best=float.PositiveInfinity;
                        foreach(var face in triangles)
                        {
                            var normal=Vector3.Cross(face.b-face.a,face.c-face.a).normalized;if(Vector3.Dot(normal,up)<.25f)continue;
                            if(!ActualTriangleRay(origin,-up,face,out float distance)||distance>=best||distance>30)continue;
                            best=distance;record.deckFaceFound=true;record.actualDeckFace=origin-up*distance;record.deckFaceNormal=normal;record.deckFaceSource=face.source;
                        }
                        if(record.deckFaceFound)record.actualGap=Vector3.Dot(record.actualRenderedBottom-record.actualDeckFace,up);
                    }
                }
                wheels.Add(record);
            }
            report.wheels=wheels.ToArray();return report;
        }
        static bool VisibleWithin(Transform value,Transform root)
        {for(var node=value;node!=null;node=node.parent){if(!node.gameObject.activeSelf)return false;if(node==root)return true;}return false;}
        static int ReadActualIndex(Mesh.MeshData mesh,IndexFormat format,int index)=>format==IndexFormat.UInt16?mesh.GetIndexData<ushort>()[index]:(int)mesh.GetIndexData<uint>()[index];
        static List<Vector3> ReadVisibleModelVertices(Transform root)
        {
            var result=new List<Vector3>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(filter.sharedMesh==null||renderer==null||!renderer.enabled||!VisibleWithin(filter.transform,root))continue;
                using(var data=MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh))using(var values=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp)){data[0].GetVertices(values);foreach(var point in values)result.Add(filter.transform.TransformPoint(point));}
            }
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(value=>value.sharedMesh!=null&&value.enabled&&VisibleWithin(value.transform,root)))
            {var mesh=new Mesh();try{skin.BakeMesh(mesh,true);foreach(var point in mesh.vertices)result.Add(skin.transform.TransformPoint(point));}finally{UnityEngine.Object.DestroyImmediate(mesh);}}
            return result;
        }
        static bool ActualTriangleRay(Vector3 origin,Vector3 direction,ActualDeckTriangle triangle,out float distance)
        {
            distance=0;var edge1=triangle.b-triangle.a;var edge2=triangle.c-triangle.a;var cross=Vector3.Cross(direction,edge2);float determinant=Vector3.Dot(edge1,cross);if(Mathf.Abs(determinant)<.00000001f)return false;
            float inverse=1/determinant;var offset=origin-triangle.a;float u=Vector3.Dot(offset,cross)*inverse;if(u<-.00001f||u>1.00001f)return false;var q=Vector3.Cross(offset,edge1);float v=Vector3.Dot(direction,q)*inverse;if(v<-.00001f||u+v>1.00001f)return false;distance=Vector3.Dot(edge2,q)*inverse;return distance>=0;
        }
    }
}
#endif
