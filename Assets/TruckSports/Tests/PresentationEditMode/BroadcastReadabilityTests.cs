using System;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckSports.Tests
{
    public sealed class BroadcastReadabilityTests
    {
        static Type Framing=>Type.GetType("LWS.TruckSports.SportsBroadcastFraming, Assembly-CSharp",true);
        [Test]public void LongSportAttachmentCannotInflateAuthoritativeTruckReadability()
        {
            var truck=GameObject.CreatePrimitive(PrimitiveType.Cube);var lance=GameObject.CreatePrimitive(PrimitiveType.Cube);var go=new GameObject("Actual tractor measurement");
            try
            {
                truck.transform.localScale=new Vector3(3,4,10);lance.transform.SetParent(truck.transform,false);lance.transform.localPosition=new Vector3(0,0,3);lance.transform.localScale=new Vector3(.1f,.1f,4);
                var body=truck.AddComponent<Rigidbody>();var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=40;camera.transform.SetPositionAndRotation(new Vector3(0,100,0),Quaternion.Euler(90,0,0));
                var framing=go.AddComponent(Framing);Framing.GetField("bodies").SetValue(framing,new[]{body});Framing.GetField("modelRoots").SetValue(framing,new[]{truck.transform});Framing.GetField("bodyShapes").SetValue(framing,new[]{truck.GetComponent<Collider>()});Framing.GetField("excludedSubjectRoots").SetValue(framing,new[]{lance.transform});
                var actual=(Rect)Framing.GetMethod("ProjectedTruckRect").Invoke(framing,new object[]{camera,0});
                Assert.That(actual.height,Is.EqualTo(10f/80).Within(.0001f),"The tractor, not its weapon, sets minimum rendered size.");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(truck);}
        }
        [TestCase(40f,.08f)] [TestCase(58f,.11f)] [TestCase(70f,.05f)]
        public void PerspectiveDistanceActuallyProjectsAuthoredTruckBoundsAtRequestedSize(float elevation,float height)
        {
            var go=new GameObject("Actual projection regression");
            try
            {
                var camera=go.AddComponent<Camera>();camera.fieldOfView=58;camera.aspect=16f/9;
                var bounds=new Bounds(new Vector3(75,9,-30),new Vector3(3.4f,4.5f,11));
                var rotation=Quaternion.Euler(elevation,-40,0);
                float distance=(float)Framing.GetMethod("MaximumReadableDistance").Invoke(null,new object[]{bounds,bounds.center,rotation,58f,height});
                camera.transform.SetPositionAndRotation(bounds.center-rotation*Vector3.forward*distance,rotation);
                float low=1,high=0;
                for(int i=0;i<8;i++){var p=camera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));low=Mathf.Min(low,p.y);high=Mathf.Max(high,p.y);}
                Assert.That(high-low,Is.EqualTo(height).Within(.001f),"World-space distance must correspond to actual eight-corner renderer projection.");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [TestCase(1.777778f)] [TestCase(.888889f)] [TestCase(2.37037f)]
        public void OrthographicProjectedHeightRemainsReadableAcrossActualViewportAspects(float aspect)
        {
            var go=new GameObject("Ortho actual projection");
            try
            {
                var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.aspect=aspect;
                var bounds=new Bounds(Vector3.zero,new Vector3(4,5,12));var rotation=Quaternion.Euler(58,0,0);
                float span=(float)Framing.GetMethod("ProjectedSpan").Invoke(null,new object[]{bounds,rotation});camera.orthographicSize=span/(2*.08f);
                camera.transform.SetPositionAndRotation(-(rotation*Vector3.forward)*100,rotation);
                var type=Type.GetType("LWS.TruckSports.SportsCameraFit, Assembly-CSharp",true);
                var rect=(Rect)type.GetMethod("Projected").Invoke(null,new object[]{camera,bounds});Assert.That(rect.height,Is.EqualTo(.08f).Within(.001f));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [TestCase(false)] [TestCase(true)]
        public void RotatedTruckReadabilityMatchesRenderedMeshInsteadOfInflatedWorldAabb(bool orthographic)
        {
            var model=GameObject.CreatePrimitive(PrimitiveType.Cube);var cameraObject=new GameObject("Oriented bounds camera");
            try
            {
                model.transform.SetPositionAndRotation(new Vector3(14,7,-50),Quaternion.Euler(0,60,0));model.transform.localScale=new Vector3(3,4,12);
                var body=model.AddComponent<Rigidbody>();var camera=cameraObject.AddComponent<Camera>();camera.orthographic=orthographic;camera.orthographicSize=30;camera.fieldOfView=58;
                var rotation=Quaternion.Euler(42,-90,0);camera.transform.SetPositionAndRotation(model.transform.position-rotation*Vector3.forward*45,rotation);
                var framing=cameraObject.AddComponent(Framing);Framing.GetField("bodies").SetValue(framing,new[]{body});Framing.GetField("modelRoots").SetValue(framing,new[]{model.transform});Framing.GetField("bodyShapes").SetValue(framing,new[]{model.GetComponent<Collider>()});
                var actual=(Rect)Framing.GetMethod("ProjectedTruckRect").Invoke(framing,new object[]{camera,0});
                Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
                foreach(var vertex in model.GetComponent<MeshFilter>().sharedMesh.vertices){var p=camera.WorldToViewportPoint(model.transform.TransformPoint(vertex));min=Vector2.Min(min,new Vector2(p.x,p.y));max=Vector2.Max(max,new Vector2(p.x,p.y));}
                Assert.That(actual.height,Is.EqualTo(max.y-min.y).Within(.00001f));Assert.That(actual.width,Is.EqualTo(max.x-min.x).Within(.00001f));
            }
            finally{UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(model);}
        }
    }
}
