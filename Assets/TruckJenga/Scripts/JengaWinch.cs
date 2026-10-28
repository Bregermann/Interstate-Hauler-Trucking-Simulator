using UnityEngine;
using LWS.TruckSports;
using LWS.TruckTugOfWar;
namespace LWS.TruckJenga
{
    // Existing Tug SpringJoint supplies both-sided PhysX forces. This adapter only reels its actual rest length.
    public sealed class JengaWinch : MonoBehaviour
    {
        public LineRenderer cable;public float reelSpeed=.42f,spring=26000,damper=1800,breakingLoad=42000,maximumConnectDistance=60;
        public SpringJoint Joint { get; private set; }
        public Rigidbody Piece { get; private set; }
        public SportsEventTruck Truck { get; private set; }
        public float Tension=>Joint!=null?Joint.currentForce.magnitude:0;
        public float Length=>Joint!=null?Joint.maxDistance:0;
        public bool Attached=>Joint!=null;
        public int Connections { get; private set; }
        Vector3 pieceAnchor,truckAnchor;float originalLength;
        public bool Attach(Rigidbody piece,SportsEventTruck truck,Vector3 localPieceAnchor)
        {
            if(Attached||piece==null||truck==null)return false;pieceAnchor=localPieceAnchor;truckAnchor=new Vector3(0,1,-2.3f);float distance=Vector3.Distance(piece.transform.TransformPoint(pieceAnchor),truck.transform.TransformPoint(truckAnchor));if(distance<.2f||distance>maximumConnectDistance)return false;
            Piece=piece;Truck=truck;originalLength=distance;Joint=TugMatch.Rope(piece,truck.body,pieceAnchor,truckAnchor,distance,spring,damper);Joint.breakForce=breakingLoad;Connections++;if(cable!=null)cable.enabled=true;return true;
        }
        public void Step(bool reel,float dt){if(Joint==null)return;if(reel)Joint.maxDistance=Mathf.Max(.5f,Joint.maxDistance-reelSpeed*dt);}
        public void Draw(){if(cable==null)return;cable.enabled=Attached;if(!Attached)return;Vector3 a=Piece.transform.TransformPoint(pieceAnchor),b=Truck.transform.TransformPoint(truckAnchor);float sag=Mathf.Clamp((Joint.maxDistance-Vector3.Distance(a,b))*.3f,.02f,.7f);cable.positionCount=17;for(int i=0;i<17;i++){float t=i/16f;cable.SetPosition(i,Vector3.Lerp(a,b,t)-Vector3.up*Mathf.Sin(t*Mathf.PI)*sag);}}
        public void Disconnect(){if(Joint!=null){Joint.spring=Joint.damper=0;Destroy(Joint);}Joint=null;Piece=null;Truck=null;if(cable!=null)cable.enabled=false;}
        public void ResetWinch(){Disconnect();Connections=0;}
        void OnDisable(){Disconnect();}
    }
}
