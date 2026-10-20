using UnityEngine;
using Assets.CoreCollapse.Scripts.Structural;
namespace LWS.TruckJousting
{
    // Core Collapse owns damage; this adapter restores pooled sport-owned splinters per pass.
    public sealed class JoustLance:Collapser
    {
        public Rigidbody owner; public GameObject model;public SphereCollider tip;
        public Rigidbody[] splinters;public Transform[] originals;
        Vector3[] positions;Quaternion[] rotations;Transform[] parents;bool broken;
        public bool Broken=>broken;
        void Awake(){startingHp=1;positions=new Vector3[splinters.Length];rotations=new Quaternion[splinters.Length];parents=new Transform[splinters.Length];for(int i=0;i<splinters.Length;i++){var t=splinters[i].transform;positions[i]=t.localPosition;rotations[i]=t.localRotation;parents[i]=t.parent;}}
        public void ResetLance(){currentHp=startingHp;broken=false;model.SetActive(true);tip.enabled=true;for(int i=0;i<splinters.Length;i++){var b=splinters[i];b.isKinematic=true;b.gameObject.SetActive(false);b.transform.SetParent(parents[i],false);b.transform.SetLocalPositionAndRotation(positions[i],rotations[i]);}}
        public void Break(){if(!broken)ReceiveAllDamage();}
        protected override void Collapse()
        {
            if(broken)return;broken=true;base.Collapse();model.SetActive(false);tip.enabled=false;
            for(int i=0;i<splinters.Length;i++)
            {
                var body=splinters[i];body.transform.SetPositionAndRotation(originals[i].position,originals[i].rotation);body.transform.SetParent(parents[i],true);body.gameObject.SetActive(true);body.isKinematic=false;
                body.linearVelocity=Vector3.ClampMagnitude(owner.GetPointVelocity(body.position),28);body.angularVelocity=Vector3.ClampMagnitude(owner.angularVelocity+transform.right*(i%2==0?4:-4),12);
            }
        }
        public void Freeze(){foreach(var piece in splinters){if(!piece.isKinematic)piece.linearVelocity=piece.angularVelocity=Vector3.zero;piece.isKinematic=true;}}
    }
}
