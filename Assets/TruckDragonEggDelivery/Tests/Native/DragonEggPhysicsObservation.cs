#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

// Read-only native witness. No state/transform/joint/physics settings are written.
public static class DragonEggPhysicsObservation
{
    static object Get(object item,string name)
    {
        for(var type=item.GetType();type!=null;type=type.BaseType)
        {
            var field=type.GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(item);
            var property=type.GetProperty(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(property!=null)return property.GetValue(item);
        }
        throw new MissingMemberException(item.GetType().FullName,name);
    }
    static string Path(Transform item)=>item==null?"none":item.parent==null?item.name:Path(item.parent)+"/"+item.name;
    static string ColliderFact(Collider shape)=>shape==null?"none":Path(shape.transform)+"#"+shape.GetEntityId()+" type="+shape.GetType().Name+" trigger="+shape.isTrigger+" active="+shape.gameObject.activeInHierarchy+" enabled="+shape.enabled+" rb="+(shape.attachedRigidbody==null?"static":Path(shape.attachedRigidbody.transform));
    public static void Append(object match,int slot,StringBuilder text,bool includeSupport=true)
    {
        var truck=((Array)Get(match,"trucks")).GetValue(slot);var truckBody=(Rigidbody)Get(truck,"body");var truckTransform=((Component)truck).transform;
        var carrier=((Array)Get(match,"carriers")).GetValue(slot);var cargo=Get(carrier,"cargo");var cargoBody=(Rigidbody)Get(cargo,"body");var shape=(BoxCollider)Get(truck,"shape");var active=(Collider)Get(cargo,"ActiveShape");var socket=(Transform)Get(carrier,"socket");var crane=Get(carrier,"crane");var head=(Rigidbody)Get(crane,"spreader");
        text.AppendLine(" PHYSICS slot="+slot+" truck="+truckBody.position.ToString("F5")+" rotation="+truckBody.rotation+" shape="+ColliderFact(shape)+" shapeBounds="+shape.bounds+" cargo="+cargoBody.position.ToString("F5")+" cargoVel="+cargoBody.linearVelocity.ToString("F5")+" cargoAng="+cargoBody.angularVelocity.ToString("F5")+" cargoMass="+cargoBody.mass+" cargoShape="+ColliderFact(active)+" cargoBounds="+active.bounds+" socket="+socket.position.ToString("F5")+" head="+head.position.ToString("F5")+" headVel="+head.linearVelocity.ToString("F5")+" headKinematic="+head.isKinematic+" headMass="+head.mass+" headParent="+Path(head.transform.parent)+" craneStage="+Get(crane,"Stage")+" craneLoad="+Get(crane,"Load")+" frozenDestination="+Get(crane,"destination")+" obstacleTop="+Get(carrier,"obstacleTop"));
        foreach(var joint in cargoBody.GetComponents<Joint>().Take(8))
        {
            var connected=joint.connectedBody;Vector3 ownAnchor=cargoBody.transform.TransformPoint(joint.anchor),otherAnchor=connected==null?joint.connectedAnchor:connected.transform.TransformPoint(joint.connectedAnchor);
            text.AppendLine(" JOINT "+joint.GetType().Name+" id="+joint.GetEntityId()+" auto="+joint.autoConfigureConnectedAnchor+" localAnchor="+joint.anchor.ToString("F5")+" connectedAnchor="+joint.connectedAnchor.ToString("F5")+" worldOwn="+ownAnchor.ToString("F5")+" worldConnected="+otherAnchor.ToString("F5")+" actualError="+Vector3.Distance(ownAnchor,otherAnchor)+" connected="+(connected==null?"world":Path(connected.transform))+" breakForce="+joint.breakForce+" breakTorque="+joint.breakTorque+" collision="+joint.enableCollision);
            if(joint is ConfigurableJoint configurable)text.AppendLine(" CONFIGURABLE axes="+configurable.xMotion+","+configurable.yMotion+","+configurable.zMotion+" target="+configurable.targetPosition+" xDrive="+configurable.xDrive.positionSpring+","+configurable.xDrive.positionDamper+","+configurable.xDrive.maximumForce+" yDrive="+configurable.yDrive.positionSpring+","+configurable.yDrive.positionDamper+","+configurable.yDrive.maximumForce+" zDrive="+configurable.zDrive.positionSpring+","+configurable.zDrive.positionDamper+","+configurable.zDrive.maximumForce);
        }
        if(!includeSupport)return;
        var overlaps=truckTransform.GetComponentsInChildren<Collider>(true);text.AppendLine(" ACTUAL_TRUCK_COLLIDER_COUNT="+overlaps.Length);
        foreach(var other in overlaps.Take(64))
            if(other.enabled&&!other.isTrigger&&other.gameObject.activeInHierarchy&&Physics.ComputePenetration(active,active.transform.position,active.transform.rotation,other,other.transform.position,other.transform.rotation,out var direction,out float distance))
                text.AppendLine(" CARGO_TRUCK_PENETRATION "+ColliderFact(other)+" separateCargo="+direction+" depth="+distance);
        Vector3 goal=((Transform)Get(((Array)Get(match,"nests")).GetValue(slot),"approach")).position;Vector3 toward=Vector3.ProjectOnPlane(goal-truckBody.position,Vector3.up).normalized;
        SupportAndBody(match,slot,truck,truckBody.position,truckBody.rotation,"CURRENT_PHYSICAL_ROTATION",text);
        if(toward.sqrMagnitude>.1f)SupportAndBody(match,slot,truck,truckBody.position,Quaternion.LookRotation(toward),"CORRIDOR_ZERO_TOWARD_BAY",text);
    }
    static void SupportAndBody(object match,int slot,object truck,Vector3 road,Quaternion rotation,string phase,StringBuilder text)
    {
        var own=((Component)truck).transform;var shape=(BoxCollider)Get(truck,"shape");float lift=((float[])Get(match,"lift"))[slot];var hits=Physics.RaycastAll(road+Vector3.up*6,Vector3.down,30,~0,QueryTriggerInteraction.Ignore);float best=float.PositiveInfinity;RaycastHit selected=default;bool found=false;
        text.AppendLine(" CENTER_QUERY phase="+phase+" road="+road.ToString("F5")+" rayCount="+hits.Length+" nativeBufferLimit=32 lift="+lift);
        foreach(var hit in hits.OrderBy(hit=>hit.distance).Take(40))
        {
            bool truckSurface=hit.collider.GetComponentInParent(Type.GetType("LWS.TruckSports.SportsEventTruck, Assembly-CSharp",true))!=null;float error=Mathf.Abs(hit.point.y-road.y);bool eligible=hit.normal.y>=.65f&&!truckSurface&&error<4;
            text.AppendLine(" RAY "+ColliderFact(hit.collider)+" point="+hit.point.ToString("F5")+" normal="+hit.normal.ToString("F5")+" heightError="+error+" baseEligible="+eligible);
            if(eligible&&error<best){best=error;selected=hit;found=true;}
        }
        if(!found){text.AppendLine(" SELECTED_SUPPORT none");return;}
        var normal=selected.normal;rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(rotation*Vector3.forward,normal).normalized,normal);Vector3 p=selected.point+normal*lift,center=p+rotation*Vector3.Scale(shape.center,own.lossyScale),half=Vector3.Scale(shape.size*.5f,own.lossyScale)+new Vector3(.2f,.05f,.3f);var bodies=Physics.OverlapBox(center,half,rotation,~0,QueryTriggerInteraction.Ignore);
        text.AppendLine(" SELECTED_SUPPORT "+ColliderFact(selected.collider)+" origin="+p.ToString("F5")+" boxCenter="+center.ToString("F5")+" half="+half.ToString("F5")+" overlaps="+bodies.Length);
        foreach(var other in bodies.Take(64))
        {
            bool isTruck=other.GetComponentInParent(Type.GetType("LWS.TruckSports.SportsEventTruck, Assembly-CSharp",true))!=null;
            bool ignored=other==selected.collider||other.transform.IsChildOf(own)||isTruck;
            text.AppendLine(" BOX "+ColliderFact(other)+" planningIgnored="+ignored);
        }
    }
}
#endif
