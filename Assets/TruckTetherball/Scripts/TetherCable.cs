using UnityEngine;
namespace LWS.TruckTetherball
{
    // PhysX's spherical linear limit owns the tether force. These pooled,
    // render-only segments show the actual accumulated winding and free rope;
    // no chain of unstable dynamic joints or contact-based scoring is added.
    public sealed class TetherCable : MonoBehaviour
    {
        public Rigidbody ball;
        public ConfigurableJoint constraint;
        public Transform poleTop;
        public Transform[] segments;
        public float poleRadius=.8f,ballRadius=3,ballHeight=4,terminalOrbitRadius=8;
        public float InitialLength {get;private set;}
        public float FreeLength {get;private set;}
        public Vector3 StartPosition {get;private set;}
        public void ResetCable(int turns)
        {
            float height=poleTop.position.y-ballHeight;
            InitialLength=Mathf.Sqrt(height*height+terminalOrbitRadius*terminalOrbitRadius)+turns*2*Mathf.PI*poleRadius;
            FreeLength=InitialLength;StartPosition=new Vector3(poleTop.position.x+Mathf.Sqrt(InitialLength*InitialLength-height*height),ballHeight,poleTop.position.z);
            constraint.autoConfigureConnectedAnchor=false;constraint.connectedAnchor=poleTop.position;constraint.anchor=Vector3.zero;
            var limit=constraint.linearLimit;limit.limit=FreeLength;constraint.linearLimit=limit;Draw(0);
        }
        public void Tick(float netDegrees)
        {
            FreeLength=Mathf.Max(2,InitialLength-Mathf.Abs(netDegrees)*Mathf.Deg2Rad*poleRadius);
            var limit=constraint.linearLimit;limit.limit=FreeLength;constraint.linearLimit=limit;Draw(netDegrees);
        }
        public void Draw(float netDegrees)
        {
            const int coil=48;float radians=netDegrees*Mathf.Deg2Rad;var centre=poleTop.position;float drop=Mathf.Abs(netDegrees)/360*.2f;
            Vector3 Coil(float t){float angle=radians*t;return centre+new Vector3(Mathf.Cos(angle)*(poleRadius+.1f),-drop*t,Mathf.Sin(angle)*(poleRadius+.1f));}
            for(int i=0;i<coil;i++)Pose(segments[i],Coil(i/(float)coil),Coil((i+1)/(float)coil));
            var from=Coil(1);var to=ball.worldCenterOfMass;to+=(from-to).normalized*ballRadius;
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
