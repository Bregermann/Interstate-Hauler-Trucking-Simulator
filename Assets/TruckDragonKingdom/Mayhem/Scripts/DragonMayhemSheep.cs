using LWS.TruckCheeseRolling;
using LWS.TruckKeepaway;
using MalbersAnimations.Controller;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Mayhem
{
    // This is interaction semantics only; vendor Pickable owns all attachment
    // and Rigidbody/collider restoration. Existing sheep own every explosion.
    public sealed class DragonMayhemSheep : MonoBehaviour
    {
        public Pickable item;
        public CheeseSheep cheese;
        public KeepawaySheep keepaway;
        public bool temporary;
        DragonMayhemDirector director;
        Vector3 home, wander;
        float wanderAt;
        public bool Consumed { get; private set; }
        public bool Explosive => cheese != null || keepaway != null;
        public bool Available => !Consumed && gameObject.activeInHierarchy && item != null && !item.IsPicked && (cheese == null || !cheese.Queued && !cheese.Detonated) && (keepaway == null || keepaway.Active && !keepaway.Queued);
        public bool Detonated => cheese != null && cheese.Detonated || keepaway != null && !keepaway.Active;
        public void Consume()
        {
            if (Consumed) return;
            Consumed = true;
            if (cheese != null) cheese.Queue();
            else if (keepaway != null) keepaway.Queue();
            else gameObject.SetActive(false);
        }
        public void AcquireTemporary(DragonMayhemDirector owner, Vector3 point, int index)
        {
            director=owner; Consumed=false; home=wander=point;
            transform.SetParent(owner.RuntimeRoot,true);transform.position=point;
            wanderAt=Time.time+index+1;gameObject.SetActive(true);
        }
        void FixedUpdate()
        {
            if(!temporary || director==null || !director.Active || !Available || item.RigidBody==null)return;
            if(Time.time>=wanderAt)
            {
                wanderAt=Time.time+3;
                var offset=new Vector3(Mathf.Sin(wanderAt*1.37f),0,Mathf.Cos(wanderAt*.91f))*4;
                if(director.adapter.TryGround(home+offset,out var ground))wander=ground+Vector3.up*.65f;
            }
            var delta=Vector3.ProjectOnPlane(wander-transform.position,Vector3.up);
            if(delta.sqrMagnitude<.2f)return;
            foreach(var hit in Physics.SphereCastAll(transform.position+Vector3.up*.5f,.35f,delta.normalized,1.2f,~0,QueryTriggerInteraction.Ignore))
                if(!hit.collider.transform.IsChildOf(transform) && hit.normal.y<.5f)return;
            var next=transform.position+delta.normalized*Mathf.Min(delta.magnitude,Time.fixedDeltaTime*.8f);
            if(!director.adapter.TryGround(next,out var support))return;
            item.RigidBody.MovePosition(support+Vector3.up*.65f);
            item.RigidBody.MoveRotation(Quaternion.RotateTowards(item.RigidBody.rotation,Quaternion.LookRotation(delta),Time.fixedDeltaTime*70));
        }
    }
}
