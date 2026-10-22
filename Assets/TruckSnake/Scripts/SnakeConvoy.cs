using System.Collections.Generic;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckSnake {
 // Kinematic, collider-backed followers of the actual driven 3D path. NWH
 // remains the tractor authority; collision sweeps prevent ghosting through
 // major obstacles without introducing a numerically unstable joint chain.
 public sealed class SnakeConvoy:MonoBehaviour {
  public SportsEventTruck tractor;public GameObject trailerPrefab;public SnakeMatch match;public int slot;
  public float spacing=15,frontOffset=7,minimumSample=.3f;public Vector3 tractorHitch=new Vector3(0,.9f,-3.3f);
  public readonly SnakePathHistory history=new SnakePathHistory();
  public int ActiveCount=>active;public int Pending=>pending;public float Length=>active*spacing;
  public int SweepCount{get;private set;}
  public string LastSpawnBlocker{get;private set;}
  sealed class Segment {public Rigidbody body;public BoxCollider shape;public Transform visual;public LineRenderer coupling;public float protectedUntil;public Vector3 previousPosition;public Quaternion previousRotation;}
  readonly Dictionary<Material,Material> materials=new Dictionary<Material,Material>();
  readonly List<Segment> pool=new List<Segment>(64);readonly RaycastHit[] hits=new RaycastHit[64];readonly Collider[] overlaps=new Collider[64];int active,pending;Vector3 previousHitch;Collider[] tractorSolids;Collider[][] competitorSolids;bool collisionCacheReady;
  public void ResetConvoy(){collisionCacheReady=false;ApplyColor();tractorSolids=System.Array.FindAll(tractor.GetComponentsInChildren<Collider>(true),c=>!c.isTrigger);for(int i=0;i<pool.Count;i++){pool[i].body.gameObject.SetActive(false);pool[i].coupling.gameObject.SetActive(false);}active=pending=SweepCount=0;Vector3 hitch=tractor.transform.TransformPoint(tractorHitch);history.Reset(hitch,tractor.transform.rotation);previousHitch=hitch;}
  void ApplyColor(){foreach(var pair in materials)if(pair.Key.name.Contains("TRLR-trailer")){var color=Color.Lerp(Color.white,match.setup.players[slot].color,.55f);if(pair.Value.HasProperty("_BaseColor"))pair.Value.SetColor("_BaseColor",color);if(pair.Value.HasProperty("_Color"))pair.Value.SetColor("_Color",color);}}
  void OnDestroy(){foreach(var copy in materials.Values)if(copy!=null)Destroy(copy);materials.Clear();}
  public int SharedMaterialCount=>materials.Count;
  public void StartWithTrailer(){
   // Only the short, checked starting bay is pre-seeded. Later growth still
   // follows the real route driven by the tractor, including delayed jumps.
   var hitch=tractor.transform.TransformPoint(tractorHitch);
   history.Reset(hitch,tractor.transform.rotation,frontOffset+7);
   if(!Pose(0,out var p,out var q)||!SpawnClear(p,q))throw new System.InvalidOperationException("Truck Snake starter trailer bay is obstructed for participant "+(slot+1)+" · "+LastSpawnBlocker);
   // Require support beneath front and rear, rather than inventing a path over a gap.
   for(int end=-1;end<=1;end+=2){var foot=p+q*(Vector3.forward*end*5);bool supported=false;foreach(var hit in Physics.RaycastAll(foot+Vector3.up*2,Vector3.down,4,~0,QueryTriggerInteraction.Ignore))if(IsRoadSupport(hit.collider)&&Mathf.Abs(hit.point.y-p.y)<.65f){supported=true;break;}if(!supported)throw new System.InvalidOperationException("Truck Snake starter trailer lacks a level road bay for participant "+(slot+1));}
   pending=SnakeRules.StartingTrailers;AttachPending();
   if(active!=SnakeRules.StartingTrailers)throw new System.InvalidOperationException("Truck Snake could not attach its starter trailer.");
  }
  bool SpawnClear(Vector3 p,Quaternion q){
   LastSpawnBlocker=null;
   int count=Physics.OverlapBoxNonAlloc(p+q*Vector3.up*1.6f,new Vector3(1.3f,1.55f,6.7f),overlaps,q,~0,QueryTriggerInteraction.Ignore);
   var candidates=count==overlaps.Length?Physics.OverlapBox(p+q*Vector3.up*1.6f,new Vector3(1.3f,1.55f,6.7f),q,~0,QueryTriggerInteraction.Ignore):overlaps;if(count==overlaps.Length)count=candidates.Length;
   for(int i=0;i<count;i++)if(candidates[i]!=null&&candidates[i].bounds.max.y>p.y+.45f){LastSpawnBlocker=candidates[i].name+" at "+candidates[i].bounds.center;return false;}return true;
  }
  void AttachPending(){while(pending>0&&Pose(active,out var p,out var q)&&SpawnClear(p,q)){
   var segment=Ensure(active);segment.body.transform.SetPositionAndRotation(p,q);segment.body.gameObject.SetActive(true);IgnoreCompetitors(segment.shape);segment.body.position=p;segment.body.rotation=q;segment.previousPosition=p;segment.previousRotation=q;Physics.SyncTransforms();segment.coupling.gameObject.SetActive(true);segment.coupling.SetPosition(0,p+q*new Vector3(0,1.15f,6.75f));segment.coupling.SetPosition(1,active==0?tractor.transform.TransformPoint(tractorHitch):pool[active-1].body.position+pool[active-1].body.rotation*new Vector3(0,1.15f,-6.75f));segment.protectedUntil=Time.time+1;active++;pending--;
   }match.rules.SetAttached(slot,active);
  }
  public void Grow(int count){if(count>0)pending+=count;}
  void CacheCompetitors(){if(collisionCacheReady)return;competitorSolids=new Collider[match.trucks.Length][];for(int owner=0;owner<match.trucks.Length;owner++)competitorSolids[owner]=System.Array.FindAll(match.trucks[owner].GetComponentsInChildren<Collider>(true),c=>!c.isTrigger);tractorSolids=competitorSolids[slot];foreach(var segment in pool)IgnoreCompetitors(segment.shape);collisionCacheReady=true;}
  public void RebindCollisionFilters(){collisionCacheReady=false;CacheCompetitors();}
  void IgnoreCompetitors(Collider shape){foreach(var solids in competitorSolids)foreach(var solid in solids)if(solid!=null)Physics.IgnoreCollision(shape,solid,true);}
  public int UnsafeCompetitorPairs(){int count=0;for(int i=0;i<active;i++)foreach(var truck in match.trucks)foreach(var solid in truck.GetComponentsInChildren<Collider>(true))if(!solid.isTrigger&&solid.enabled&&!Physics.GetIgnoreCollision(pool[i].shape,solid))count++;return count;}
  Segment Ensure(int index){CacheCompetitors();if(index<pool.Count)return pool[index];var root=new GameObject("Pooled path-backed cargo trailer "+index);root.layer=2;root.transform.SetParent(transform,false);var body=root.AddComponent<Rigidbody>();body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.mass=2500;var shape=root.AddComponent<BoxCollider>();shape.size=new Vector3(2.6f,3.2f,13.5f);shape.center=Vector3.up*1.6f;var model=Instantiate(trailerPrefab,root.transform);model.transform.localPosition=Vector3.up*1.6f;model.transform.localRotation=Quaternion.identity;model.transform.localScale=new Vector3(2.6f/6,3.2f/4.6f,13.5f/16);foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=2;foreach(var renderer in model.GetComponentsInChildren<Renderer>()){var assigned=renderer.sharedMaterials;for(int n=0;n<assigned.Length;n++){var source=assigned[n];if(source==null)continue;if(!materials.TryGetValue(source,out var copy)){copy=new Material(source){name="Snake pooled instanced "+source.name,enableInstancing=true};materials.Add(source,copy);}assigned[n]=copy;}renderer.sharedMaterials=assigned;}ApplyColor();
   var coupling=new GameObject("Visible articulated coupling "+index).AddComponent<LineRenderer>();coupling.transform.SetParent(transform,false);coupling.positionCount=2;coupling.widthMultiplier=.16f;coupling.sharedMaterial=match.couplerMaterial;var segment=new Segment{body=body,shape=shape,visual=model.transform,coupling=coupling};pool.Add(segment);if(tractorSolids==null)tractorSolids=System.Array.FindAll(tractor.GetComponentsInChildren<Collider>(true),c=>!c.isTrigger);IgnoreCompetitors(shape);var owner=root.AddComponent<SnakeTrailerContact>();owner.convoy=this;owner.index=index;root.SetActive(false);return segment;}
  public void Tick(float dt){
   if(!match.rules.Playing(slot))return;var hitch=tractor.transform.TransformPoint(tractorHitch);history.Append(hitch,tractor.transform.rotation,minimumSample);previousHitch=hitch;
   // Pending rewards never inflate the attached-length score multiplier.
   AttachPending();
   for(int i=0;i<active;i++){
    if(!Pose(i,out var position,out var rotation))continue;var segment=pool[i];Vector3 movement=position-segment.previousPosition;float length=movement.magnitude;
    if(length>.001f){
     int count=Physics.BoxCastNonAlloc(segment.previousPosition+segment.previousRotation*Vector3.up*1.6f,new Vector3(1.24f,1.5f,6.65f),movement/length,hits,segment.previousRotation,length,1,QueryTriggerInteraction.Ignore);SweepCount++;
     var measured=count==hits.Length?Physics.BoxCastAll(segment.previousPosition+segment.previousRotation*Vector3.up*1.6f,new Vector3(1.24f,1.5f,6.65f),movement/length,segment.previousRotation,length,1,QueryTriggerInteraction.Ignore):hits;if(count==hits.Length)count=measured.Length;
     for(int k=0;k<count;k++){var hit=measured[k];if(IsRoadSupport(hit.collider)||hit.normal.y>.55f||hit.collider.transform.IsChildOf(tractor.transform)||hit.rigidbody!=null)continue;float normalSpeed=Mathf.Max(0,-Vector3.Dot(movement/dt,hit.normal));if(normalSpeed>6){match.Fail(slot,"TRAILER HIT "+hit.collider.name);return;}}
    }
    int overlapCount=Physics.OverlapBoxNonAlloc(position+rotation*Vector3.up*1.6f,new Vector3(1.24f,1.5f,6.65f),overlaps,rotation,1,QueryTriggerInteraction.Ignore);
    var candidate=overlapCount==overlaps.Length?Physics.OverlapBox(position+rotation*Vector3.up*1.6f,new Vector3(1.24f,1.5f,6.65f),rotation,1,QueryTriggerInteraction.Ignore):overlaps;if(overlapCount==overlaps.Length)overlapCount=candidate.Length;
    for(int k=0;k<overlapCount;k++){var c=candidate[k];if(IsRoadSupport(c)||c.attachedRigidbody!=null||c.transform.IsChildOf(tractor.transform))continue;if(Physics.ComputePenetration(segment.shape,position,rotation,c,c.transform.position,c.transform.rotation,out var direction,out float depth)&&depth>.25f&&Mathf.Abs(direction.y)<.6f){match.Fail(slot,"TRAILER BLOCKED · "+c.name);return;}}
    // Own-tractor contacts are measured against the actual compound hull,
    // without allowing an infinite-mass kinematic follower to fling its tractor.
    // Meaningful mature-tail overlap still fails through one authoritative rule.
    if(Time.time>=segment.protectedUntil)for(int owner=0;owner<match.trucks.Length;owner++){if(!match.rules.Playing(owner)||owner==slot&&i<2)continue;var truck=match.trucks[owner];var delta=truck.body.worldCenterOfMass-(position+rotation*Vector3.up*1.6f);if(delta.sqrMagnitude>=14*14)continue;foreach(var solid in competitorSolids[owner]){if(solid==null||!solid.enabled)continue;if(Physics.ComputePenetration(solid,solid.transform.position,solid.transform.rotation,segment.shape,position,rotation,out var away,out float depth)&&depth>.06f){float closing=Mathf.Max(0,-Vector3.Dot(truck.body.linearVelocity-movement/Mathf.Max(.001f,dt),away));if(closing>=5){match.Fail(owner,owner==slot?"HIT YOUR OWN TRAILER "+(i+1):"HIT "+match.setup.players[slot].name+"'S TRAILER "+(i+1));if(owner==slot)return;break;}}}}
    segment.body.MovePosition(position);segment.body.MoveRotation(rotation);segment.previousPosition=position;segment.previousRotation=rotation;
    Vector3 front=position+rotation*new Vector3(0,1.15f,6.75f);Vector3 back=i==0?hitch:pool[i-1].body.position+pool[i-1].body.rotation*new Vector3(0,1.15f,-6.75f);segment.coupling.SetPosition(0,front);segment.coupling.SetPosition(1,back);
   }
   history.Trim((active+pending+4)*spacing+80);
  }
  static bool IsRoadSupport(Collider c)=>c is TerrainCollider||c.GetComponent<LWS.InterstateHauler.LwsRoadSurface>()!=null||c.GetComponent<LWS.TruckTaxi.TruckTaxiSurface>() is LWS.TruckTaxi.TruckTaxiSurface surface&&surface.isRoad;
  public bool SegmentPose(int index,out Vector3 position,out Quaternion rotation){position=default;rotation=Quaternion.identity;if(index<0||index>=active)return false;position=pool[index].body.position;rotation=pool[index].body.rotation;return true;}
  bool Pose(int index,out Vector3 position,out Quaternion rotation){float center=frontOffset+index*spacing;position=default;rotation=Quaternion.identity;if(!history.TryBehind(center,out var sample)||!history.TryBehind(Mathf.Max(0,center-5),out var front)||!history.TryBehind(center+5,out var back))return false;position=sample.position-Vector3.up*.9f;Vector3 direction=front.position-back.position;if(direction.sqrMagnitude<.1f)return false;Vector3 up=sample.rotation*Vector3.up;if(Vector3.Dot(up,Vector3.up)<.4f)up=Vector3.up;rotation=Quaternion.LookRotation(direction,up);return true;}
  public void TractorContact(int index,float normalSpeed){if(index<2||index>=active||normalSpeed<5||Time.time<pool[index].protectedUntil)return;match.Fail(slot,"HIT YOUR OWN TRAILER "+(index+1));}
  public bool IsOwn(Collider collider,out int index){var hit=collider.GetComponentInParent<SnakeTrailerContact>();index=hit!=null?hit.index:-1;return hit!=null&&hit.convoy==this;}
  public void Freeze(){for(int i=0;i<active;i++)pool[i].body.isKinematic=true;}
 }
}
