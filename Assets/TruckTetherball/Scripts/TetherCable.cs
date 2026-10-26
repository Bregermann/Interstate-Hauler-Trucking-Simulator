using UnityEngine;
namespace LWS.TruckTetherball
{
    // The existing PhysX joint owns the free tether force. Its real connected
    // anchor is the last visible coil exit, never a separate decorative spiral.
    public sealed class TetherCable : MonoBehaviour
    {
        public Rigidbody ball;
        public ConfigurableJoint constraint;
        public Transform poleTop;
        public Transform[] segments;
        public float poleRadius=.8f,ballRadius=3,ballHeight=4,terminalOrbitRadius=8;
        [Range(.05f,.5f)]public float wrapPitch=.2f;
        [Range(15,90)]public float capHandoffDegrees=45;
        public float InitialLength {get;private set;}
        public float FreeLength {get;private set;}
        public float WrappedLength {get;private set;}
        public Vector3 FreeAnchor {get;private set;}
        public float CoilRadius=>Mathf.Max(.05f,poleRadius)+.1f;
        public Vector3 StartPosition {get;private set;}
        float Pitch=>float.IsFinite(wrapPitch)?Mathf.Clamp(wrapPitch,.05f,.5f):.2f;
        public void ResetCable(int turns)
        {
            float height=poleTop.position.y-ballHeight;
            float radius=CoilRadius,drop=turns*Pitch,pitchPerRadian=Pitch/(2*Mathf.PI);
            float terminal=Mathf.Max(radius+.1f,terminalOrbitRadius);
            float finalSpan=Mathf.Sqrt((height-drop)*(height-drop)+terminal*terminal-radius*radius);
            InitialLength=finalSpan+turns*2*Mathf.PI*Mathf.Sqrt(radius*radius+pitchPerRadian*pitchPerRadian);
            // At zero winding the rope leaves the near-side top rim. The cap
            // handoff below continuously approaches the signed tangent exit.
            StartPosition=new Vector3(poleTop.position.x+radius+Mathf.Sqrt(Mathf.Max(0,InitialLength*InitialLength-height*height)),ballHeight,poleTop.position.z);
            constraint.autoConfigureConnectedAnchor=false;constraint.anchor=Vector3.zero;
            UpdateConstraint(0,StartPosition);Draw(0);
        }
        public void Tick(float netDegrees)
        {
            UpdateConstraint(netDegrees,ball.worldCenterOfMass);Draw(netDegrees);
        }
        void UpdateConstraint(float netDegrees,Vector3 position)
        {
            float radians=Mathf.Abs(netDegrees)*Mathf.Deg2Rad,radius=CoilRadius,pitchPerRadian=Pitch/(2*Mathf.PI);
            WrappedLength=radians*Mathf.Sqrt(radius*radius+pitchPerRadian*pitchPerRadian);
            FreeLength=Mathf.Max(2,InitialLength-WrappedLength);
            var radial=Vector3.ProjectOnPlane(position-poleTop.position,Vector3.up);float distance=Mathf.Max(radius+.001f,radial.magnitude);
            float angle=Mathf.Atan2(radial.z,radial.x),tangent=Mathf.Acos(Mathf.Clamp01(radius/distance));
            float handoff=float.IsFinite(capHandoffDegrees)?Mathf.Clamp(capHandoffDegrees,15,90):45;
            // Through zero, both winding directions share the near-side rim.
            // This stays inside the non-intersecting tangent cone and prevents
            // an opposite-side anchor jump or an inward chord through the pole.
            float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Abs(netDegrees)/handoff));
            angle-=Mathf.Sign(netDegrees)*tangent*blend;
            FreeAnchor=poleTop.position+new Vector3(Mathf.Cos(angle)*radius,-Mathf.Abs(netDegrees)/360*Pitch,Mathf.Sin(angle)*radius);
            constraint.connectedAnchor=constraint.connectedBody!=null?constraint.connectedBody.transform.InverseTransformPoint(FreeAnchor):FreeAnchor;
            var limit=constraint.linearLimit;limit.limit=FreeLength;constraint.linearLimit=limit;
        }
        public void Draw(float netDegrees)
        {
            int coil=Mathf.Min(48,segments.Length-1);float radians=netDegrees*Mathf.Deg2Rad;var centre=poleTop.position;float drop=Mathf.Abs(netDegrees)/360*Pitch;
            float end=Mathf.Atan2(FreeAnchor.z-centre.z,FreeAnchor.x-centre.x);
            Vector3 Coil(float t){float angle=end-radians+radians*t;return centre+new Vector3(Mathf.Cos(angle)*CoilRadius,-drop*t,Mathf.Sin(angle)*CoilRadius);}
            for(int i=0;i<coil;i++)Pose(segments[i],Coil(i/(float)coil),Coil((i+1)/(float)coil));
            var from=FreeAnchor;var to=ball.worldCenterOfMass;to+=(from-to).normalized*ballRadius;
            float sag=Mathf.Min(2,Mathf.Max(0,FreeLength-Vector3.Distance(from,ball.worldCenterOfMass))*.45f);int free=segments.Length-coil;
            Vector3 Tail(float t)=>Vector3.Lerp(from,to,t)-Vector3.up*(Mathf.Sin(t*Mathf.PI)*sag);
            for(int i=0;i<free;i++)Pose(segments[coil+i],Tail(i/(float)free),Tail((i+1)/(float)free));
        }
        static void Pose(Transform segment,Vector3 a,Vector3 b)
        {
            var delta=b-a;segment.position=(a+b)*.5f;
            if(delta.sqrMagnitude<.000001f){segment.localScale=Vector3.zero;return;}
            segment.rotation=Quaternion.FromToRotation(Vector3.up,delta);segment.localScale=new Vector3(.18f,delta.magnitude*.5f,.18f);
        }
    }
}
