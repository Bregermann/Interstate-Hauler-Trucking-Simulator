using UnityEngine;
using LWS.TruckSports;
using LWS.TruckJenga;
namespace LWS.TruckArchaeology
{
    public sealed partial class ArchaeologyEquipment:MonoBehaviour
    {
        public ArchaeologyMatch match;public SportsEventTruck truck;public Rigidbody head;public Collider headShape;
        public Transform firstBoom,secondBoom,toolVisual;public GameObject[] toolHeads;public JengaCrane hoist;public BoxCollider rack;
        public ParticleSystem dust;public Vector3 Aim {get;private set;}
        public bool AtWork=>Vector3.Distance(head.position,Aim)<.28f;
        public void ResetEquipment()
        {
            ResetWithdrawal();hoist.ResetCrane();head.position=truck.transform.TransformPoint(new Vector3(0,6,5));head.rotation=Quaternion.identity;Aim=head.position;
            foreach(var own in truck.GetComponentsInChildren<Collider>(true))Physics.IgnoreCollision(headShape,own,true);
            if(dust!=null)dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void Reach(Vector3 target,float dt)
        {
            Aim=truck.body.position+Vector3.ClampMagnitude(target-truck.body.position,28);
            if(hoist.Stage==JengaCrane.MotorStage.Idle)head.MovePosition(Vector3.MoveTowards(head.position,Aim,dt*9));
        }
        public void Present(ArchaeologyTool tool,bool live,float clock)
        {
            for(int i=0;i<toolHeads.Length;i++)toolHeads[i].SetActive(i==(int)tool);
            Vector3 start=truck.transform.TransformPoint(new Vector3(0,3.2f,1)),end=head.position,elbow=(start+end)*.5f+Vector3.up*5;
            Beam(firstBoom,start,elbow);Beam(secondBoom,elbow,end);
            if(toolVisual!=null)toolVisual.localRotation=tool==ArchaeologyTool.Drill&&live?Quaternion.Euler(0,clock*720,0):Quaternion.identity;
        }
        static void Beam(Transform beam,Vector3 a,Vector3 b){beam.position=(a+b)*.5f;beam.rotation=Quaternion.FromToRotation(Vector3.up,b-a);beam.localScale=new Vector3(.45f,Vector3.Distance(a,b)*.5f,.45f);}
        public void StopEquipment(){ResetWithdrawal();hoist.StopCrane();if(dust!=null)dust.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
    }
}
