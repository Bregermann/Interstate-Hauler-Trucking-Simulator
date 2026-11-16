using System;
using System.Collections.Generic;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonRiders
{
    // Authored visual fittings only. The existing saddle, original NWH assembly
    // and real collision/rescue binding remain the sole attachment authorities.
    [DefaultExecutionOrder(110)]
    public sealed class DragonRiderSaddleRigging : MonoBehaviour
    {
        [Serializable]public sealed class Leg{public Transform timber;public int patch;public Vector3 deckEnd;}
        [Serializable]public sealed class Girth{public LineRenderer line;public int[] skinVertices;public Vector3 leftDeckEnd,rightDeckEnd;}
        [Serializable]public sealed class Tie{public LineRenderer line;public Vector3 leftDeckEnd,rightDeckEnd,leftTruckTop,rightTruckTop;}
        public int presentationVersion=1;
        public DragonRiderSaddle frame;
        public SportsEventTruck truck;
        public Transform[] wheelCradles=Array.Empty<Transform>();
        public Leg[] legs=Array.Empty<Leg>();
        public Girth[] girths=Array.Empty<Girth>();
        public Tie[] upperTies=Array.Empty<Tie>();
        bool attached;
        Mesh detachedSkin;
        readonly List<Vector3> detachedVertices=new List<Vector3>();
        public bool UpperTiesVisible=>attached;
        public void SetAttached(bool value)
        {
            attached=value;foreach(var tie in upperTies)if(tie?.line!=null)tie.line.enabled=value;
        }
        public void RefreshAuthoredPresentation(bool mounted)
        {
            if(frame==null||truck==null||frame.surfaceRenderer==null||frame.supportPresentation==null)return;
            SetAttached(mounted);
            if(!mounted)
            {
                if(detachedSkin==null){detachedSkin=new Mesh{name="Riders detached visual harness skin"};detachedSkin.MarkDynamic();}
                frame.surfaceRenderer.BakeMesh(detachedSkin,true);detachedVertices.Clear();detachedSkin.GetVertices(detachedVertices);
                // A detached deck is a visible fitting on the moving animal.
                // This does not evaluate or replace the physical saddle pose.
                var anchor=frame.surfacePatchAnchors[0];Vector3 skin=ReadVertex(anchor.a)*anchor.barycentric.x+ReadVertex(anchor.b)*anchor.barycentric.y+ReadVertex(anchor.c)*anchor.barycentric.z;
                var orientation=frame.actor.motor.transform.rotation;frame.supportPresentation.SetPositionAndRotation(skin+orientation*Vector3.up*frame.dorsalClearance,orientation);
            }
            var support=frame.supportPresentation;
            foreach(var leg in legs)
            {
                var anchor=frame.surfacePatchAnchors[leg.patch];Vector3 start=ReadVertex(anchor.a)*anchor.barycentric.x+ReadVertex(anchor.b)*anchor.barycentric.y+ReadVertex(anchor.c)*anchor.barycentric.z+support.up*.012f,end=support.TransformPoint(leg.deckEnd),delta=end-start;
                if(leg.timber==null||delta.magnitude<.01f)continue;leg.timber.SetPositionAndRotation((start+end)*.5f,Quaternion.FromToRotation(Vector3.up,delta));leg.timber.localScale=new Vector3(1,delta.magnitude,1);
            }
            foreach(var girth in girths)
            {
                if(girth.line==null)continue;girth.line.positionCount=girth.skinVertices.Length+2;girth.line.SetPosition(0,support.TransformPoint(girth.leftDeckEnd));girth.line.SetPosition(girth.skinVertices.Length+1,support.TransformPoint(girth.rightDeckEnd));
                Vector3 center=Vector3.zero;foreach(int vertex in girth.skinVertices)center+=ReadVertex(vertex);center/=Mathf.Max(1,girth.skinVertices.Length);
                for(int index=0;index<girth.skinVertices.Length;index++){var point=ReadVertex(girth.skinVertices[index]);girth.line.SetPosition(index+1,point+(point-center).normalized*.025f);}
            }
            foreach(var tie in upperTies)
            {
                if(tie.line==null||!mounted)continue;tie.line.positionCount=4;tie.line.SetPosition(0,support.TransformPoint(tie.leftDeckEnd));tie.line.SetPosition(1,truck.transform.TransformPoint(tie.leftTruckTop));tie.line.SetPosition(2,truck.transform.TransformPoint(tie.rightTruckTop));tie.line.SetPosition(3,support.TransformPoint(tie.rightDeckEnd));
            }
        }
        Vector3 ReadVertex(int index)
        {
            if(attached)return frame.ReadActualSkinVertexWorld(index);
            if(index<0||index>=detachedVertices.Count)throw new InvalidOperationException("Authored visual girth no longer references the actual imported skin vertex.");return frame.surfaceRenderer.transform.TransformPoint(detachedVertices[index]);
        }
        void LateUpdate(){if(frame!=null&&frame.actor!=null&&frame.actor.Prepared)RefreshAuthoredPresentation(attached);}
        void OnDisable(){SetAttached(false);if(detachedSkin!=null){if(Application.isPlaying)Destroy(detachedSkin);else DestroyImmediate(detachedSkin);}detachedSkin=null;detachedVertices.Clear();}
    }
}
