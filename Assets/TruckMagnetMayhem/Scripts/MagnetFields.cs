using UnityEngine;
namespace LWS.TruckMagnetMayhem
{
 // Mode-local field semantics over ordinary PhysX bodies. NWH owns truck dynamics.
 public sealed class MagnetFields:MonoBehaviour
 {
  [Min(1)]public float range=32,maximumNewtons=120000,maximumAcceleration=10;
  public int ForceApplications{get;private set;}public float MaximumApplied{get;private set;}
  MagnetMetal[] objects;int players,truckCount;readonly RaycastHit[] hits=new RaycastHit[64];
  public void ResetFields(MagnetMetal[] registry,int activePlayers,int authoredTrucks){objects=registry;players=activePlayers;truckCount=authoredTrucks;ForceApplications=0;MaximumApplied=0;}
  public void Tick()
  {
   for(int i=0;i<players;i++)for(int j=i+1;j<objects.Length;j++)
   {
    var source=objects[i];var target=objects[j];
    if(!source.gameObject.activeInHierarchy||!target.gameObject.activeInHierarchy||source.body.isKinematic||target.body.isKinematic)continue;
    var delta=source.Point-target.Point;float distance=delta.magnitude;
    float scalar=MagnetForce.Newtons(source.polarity,target.polarity,distance,range,source.body.mass,target.body.mass,maximumNewtons,maximumAcceleration);
    if(j<truckCount)scalar+=MagnetForce.Newtons(target.polarity,source.polarity,distance,range,target.body.mass,source.body.mass,maximumNewtons,maximumAcceleration);
    float cap=Mathf.Min(maximumNewtons,Mathf.Min(source.body.mass,target.body.mass)*maximumAcceleration);scalar=Mathf.Clamp(scalar,-cap,cap);
    if(Mathf.Abs(scalar)<.01f||Blocked(source,target,delta,distance))continue;
    var force=delta/distance*scalar;target.body.AddForceAtPosition(force,target.Point,ForceMode.Force);source.body.AddForceAtPosition(-force,source.Point,ForceMode.Force);
    ForceApplications++;MaximumApplied=Mathf.Max(MaximumApplied,Mathf.Abs(scalar));
   }
  }
  bool Blocked(MagnetMetal source,MagnetMetal target,Vector3 delta,float distance)
  {
   int count=Physics.RaycastNonAlloc(target.Point,delta/distance,hits,distance,~0,QueryTriggerInteraction.Ignore);
   for(int i=0;i<count;i++){var hit=hits[i];if(hit.distance>=distance-.05f||hit.collider.transform.IsChildOf(source.transform)||hit.collider.transform.IsChildOf(target.transform))continue;return true;}return false;
  }
 }
}
