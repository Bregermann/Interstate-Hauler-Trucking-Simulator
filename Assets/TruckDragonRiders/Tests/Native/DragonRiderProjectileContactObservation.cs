#if UNITY_EDITOR
using UnityEngine;

// Test-only observer of real PhysX contacts. It never calls the rider, applies
// an impulse to the target, changes a pose, or substitutes a collision event.
public sealed class DragonRiderProjectileContactObservation : MonoBehaviour
{
    [System.Serializable]public sealed class Contact{public int frame,count;public float fixedTime,realtime,maximumNormalSpeed;public string collider,root;public bool isRequestedOriginalTarget;public Vector3 relativeVelocity,point,normal;}
    readonly System.Collections.Generic.List<Contact> contacts=new System.Collections.Generic.List<Contact>();
    public Contact[] ObservedContacts=>contacts.ToArray();
    public Collider target;
    public bool ActualTargetContact {get;private set;}
    public int TargetContactCount {get;private set;}
    public float MaximumTargetNormalSpeed {get;private set;}
    public string ActualCollider {get;private set;}
    void OnCollisionEnter(Collision collision)
    {
        var record=new Contact{frame=Time.frameCount,fixedTime=Time.fixedTime,realtime=Time.realtimeSinceStartup,collider=collision.collider.name,root=collision.collider.transform.root.name,isRequestedOriginalTarget=collision.collider==target,count=collision.contactCount,relativeVelocity=collision.relativeVelocity};
        for(int index=0;index<collision.contactCount;index++){var point=collision.GetContact(index);record.maximumNormalSpeed=Mathf.Max(record.maximumNormalSpeed,Mathf.Abs(Vector3.Dot(collision.relativeVelocity,point.normal)));if(index==0){record.point=point.point;record.normal=point.normal;}}contacts.Add(record);
        if(collision.collider!=target)return;ActualTargetContact=true;TargetContactCount++;ActualCollider=collision.collider.name;
        for(int index=0;index<collision.contactCount;index++)MaximumTargetNormalSpeed=Mathf.Max(MaximumTargetNormalSpeed,Mathf.Abs(Vector3.Dot(collision.relativeVelocity,collision.GetContact(index).normal)));
    }
}
#endif
