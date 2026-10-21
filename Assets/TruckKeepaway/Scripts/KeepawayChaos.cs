using System;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckKeepaway {
public sealed class KeepawayChaos:MonoBehaviour {
 public KeepawayGround ground;public KeepawayMatch match;public SportsChaosWeather weather;public Transform[] solidProps,ramps,patches;public Rigidbody[] movable,sweepers;public KeepawaySheep[] sheep;public int seed=8191,chaos=1,weatherChoice;
 public int arenaRepairVersion;public float baselinePlayableArea;public int baselineObstacleCount;public KeepawayArenaProp[] breakableProps=Array.Empty<KeepawayArenaProp>();
 public int LayoutHash{get;private set;}public int Chains{get;private set;}public int ConnectedCells{get;private set;}
 const int Width=57,Height=33,Count=Width*Height;readonly bool[] blocked=new bool[Count],connected=new bool[Count];readonly int[] queue=new int[Count],parents=new int[Count],path=new int[Count],visited=new int[Count];int searchStamp,effectBudget;float age;readonly KeepawaySheep[] pending=new KeepawaySheep[32];int head,tail;System.Random random;
 int Cell(Vector3 p)=>Mathf.Clamp(Mathf.RoundToInt((p.z+KeepawayBoundary.HalfZ)*(Height-1)/(2*KeepawayBoundary.HalfZ)),0,Height-1)*Width+Mathf.Clamp(Mathf.RoundToInt((p.x+KeepawayBoundary.HalfX)*(Width-1)/(2*KeepawayBoundary.HalfX)),0,Width-1);
 Vector3 Point(int cell)=>new Vector3(Mathf.Lerp(-KeepawayBoundary.HalfX,KeepawayBoundary.HalfX,cell%Width/(float)(Width-1)),.48f,Mathf.Lerp(-KeepawayBoundary.HalfZ,KeepawayBoundary.HalfZ,cell/Width/(float)(Height-1)));
 static Vector3 Expanded(Vector3 p)=>new Vector3(p.x*KeepawayBoundary.Expansion,p.y,p.z*KeepawayBoundary.Expansion);
 float nextConnectivity;bool connectivityDirty;
 public void NewLayout(int value){seed=value;random=new System.Random(seed);age=0;head=tail=Chains=0;LayoutHash=17;Array.Clear(blocked,0,Count);Array.Clear(connected,0,Count);
  for(int i=0;i<solidProps.Length;i++){var p=new Vector3((i%4-1.5f)*11,.6f,i<4?-7:7);p.x+=(float)(random.NextDouble()-.5)*2;p.z+=(float)(random.NextDouble()-.5)*2;solidProps[i].position=Expanded(p);solidProps[i].rotation=Quaternion.Euler(0,random.Next(4)*90,0);solidProps[i].gameObject.SetActive(i<4+chaos*2);LayoutHash=unchecked(LayoutHash*31+Mathf.RoundToInt(p.x*100)+Mathf.RoundToInt(p.z*100));}
  for(int i=0;i<ramps.Length;i++){ramps[i].position=Expanded(new Vector3(i%2==0?-19:19,0,i<2?0:13));ramps[i].rotation=Quaternion.Euler(0,(i<2?0:90)+(i%2==0?0:180),0);}
  for(int i=0;i<patches.Length;i++)patches[i].position=Expanded(new Vector3(i%3*18-18,.018f,i<3?-12:12));
  for(int i=0;i<movable.Length;i++){var b=movable[i];b.gameObject.SetActive(i<4+chaos*2);b.isKinematic=false;b.position=Expanded(new Vector3((i%4-1.5f)*8,.8f,i<4?-11:11));b.rotation=Quaternion.Euler(0,random.Next(360),0);b.linearVelocity=b.angularVelocity=Vector3.zero;}
  foreach(var prop in breakableProps)prop.ResetProp();nextConnectivity=0;Physics.SyncTransforms();BuildConnectivity();
  for(int i=0;i<sheep.Length;i++){sheep[i].index=i;sheep[i].arena=this;if(i<6+chaos*5&&SafeSheepSpawn(i,out var p))sheep[i].ResetSheep(p);else sheep[i].Dormant();}
  ground.Select(seed);weather.Configure(seed,weatherChoice,chaos);Debug.Log("KEEP_LAYOUT seed="+seed+" hash="+LayoutHash+" connected="+ConnectedCells);
 }
 void BuildConnectivity(){
  for(int cell=0;cell<Count;cell++)blocked[cell]=!KeepawayBoundary.Contains(Point(cell),2);
  foreach(var prop in solidProps)Mark(prop);foreach(var ramp in ramps)Mark(ramp);
  Array.Clear(connected,0,Count);int first=Cell(Vector3.zero);
  if(blocked[first]){float best=float.PositiveInfinity;for(int i=0;i<Count;i++)if(!blocked[i]){float d=Point(i).sqrMagnitude;if(d<best){best=d;first=i;}}}
  if(blocked[first]){ConnectedCells=0;return;}
  int read=0,write=0;queue[write++]=first;connected[first]=true;
  while(read<write){int c=queue[read++];if(c%Width>0)Visit(c-1);if(c%Width<Width-1)Visit(c+1);Visit(c-Width);Visit(c+Width);}ConnectedCells=write;
  void Mark(Transform prop){
   if(prop==null||!prop.gameObject.activeSelf)return;
   var shape=prop.GetComponent<Collider>();if(shape==null||!shape.enabled||shape.attachedRigidbody!=null&&!shape.attachedRigidbody.detectCollisions)return;
   var bounds=shape.bounds;bounds.Expand(new Vector3(3,0,3));int low=Cell(bounds.min),high=Cell(bounds.max);
   for(int z=low/Width;z<=high/Width;z++)for(int x=low%Width;x<=high%Width;x++){int cell=z*Width+x;var p=Point(cell);if(p.x>=bounds.min.x&&p.x<=bounds.max.x&&p.z>=bounds.min.z&&p.z<=bounds.max.z)blocked[cell]=true;}
  }
  void Visit(int n){if(n<0||n>=Count||blocked[n]||connected[n])return;connected[n]=true;queue[write++]=n;}
 }
 public bool Reachable(Vector3 p)=>KeepawayBoundary.Contains(p,2)&&connected[Cell(p)];
 int NearestConnected(Vector3 point){int cell=Cell(point);if(connected[cell])return cell;float best=float.PositiveInfinity;for(int i=0;i<Count;i++)if(connected[i]){float d=(Point(i)-point).sqrMagnitude;if(d<best){best=d;cell=i;}}return cell;}
 public bool ClearRoute(Vector3 from,Vector3 to){float distance=Vector3.ProjectOnPlane(to-from,Vector3.up).magnitude;int steps=Mathf.Max(1,Mathf.CeilToInt(distance/.65f));int start=Cell(from);for(int k=1;k<=steps;k++){var p=Vector3.Lerp(from,to,k/(float)steps);int cell=Cell(p);if(cell!=start&&(!connected[cell]||!KeepawayBoundary.Contains(p,1.5f)))return false;}return true;}
 public Vector3 Route(Vector3 from,Vector3 to){int begin=NearestConnected(from),end=NearestConnected(to);var destination=connected[Cell(to)]&&KeepawayBoundary.Contains(to,2)?to:Point(end);if(ClearRoute(from,destination))return destination;if(begin==end)return destination;searchStamp++;int read=0,write=0;queue[write++]=begin;visited[begin]=searchStamp;parents[begin]=-1;bool found=false;while(read<write&&!found){int c=queue[read++];if(c%Width>0)Visit(c-1);if(c%Width<Width-1)Visit(c+1);Visit(c-Width);Visit(c+Width);}if(!found)return Point(begin);int length=0;for(int step=end;step>=0&&length<Count;step=parents[step])path[length++]=step;
  // String-pull the valid grid path. A two-metre next-cell target is inside this truck's turning circle.
  for(int k=0;k<length;k++){var candidate=Point(path[k]);if(ClearRoute(from,candidate))return candidate;}return Point(begin);
  void Visit(int n){if(n<0||n>=Count||blocked[n]||visited[n]==searchStamp)return;visited[n]=searchStamp;parents[n]=queue[read-1];queue[write++]=n;if(n==end)found=true;}
 }
 public Vector3 Wander(int index,float time){for(int k=0;k<Count;k++){int cell=(int)((uint)unchecked(index*1777+(int)(time*.2f)*199+k*97+seed)%Count);if(connected[cell]){var p=Point(cell);p.y=.65f;return p;}}return new Vector3(0,.65f,0);}
 public bool SafeSheepSpawn(int index,out Vector3 p){for(int k=0;k<50;k++){p=Wander(index+k,age+k*2);bool safe=Mathf.Abs(p.x)<KeepawayBoundary.HalfX-9||Mathf.Abs(p.z)<KeepawayBoundary.HalfZ-10;for(int i=0;i<match.setup.count;i++)if(match.alive[i]&&Vector3.Distance(p,match.trucks[i].body.position)<6)safe=false;if(safe)return true;}p=Vector3.zero;return false;}
 public float SurfaceGrip(Vector3 p){float grip=weather.Grip;for(int i=0;i<patches.Length;i++){var d=p-patches[i].position;if(Mathf.Abs(d.x)<3&&Mathf.Abs(d.z)<2.5f)grip*=i%2==0?.72f:.56f;}return Mathf.Max(.42f,grip);}
 public void Queue(KeepawaySheep item){if(tail-head>=pending.Length)return;pending[tail++%pending.Length]=item;}
 public void Blast(Vector3 p){Chains++;foreach(var item in sheep)if(item.Active&&!item.Queued&&Vector3.Distance(item.body.position,p)<10)item.Queue();}
 public void Step(float dt){age+=dt;bool changed=false;foreach(var prop in breakableProps)changed|=prop.TickProp();connectivityDirty|=changed;if(age>=nextConnectivity){nextConnectivity=age+.5f;bool moved=false;foreach(var prop in breakableProps)moved|=prop.NavigationMoved();if(connectivityDirty||moved){BuildConnectivity();connectivityDirty=false;}}weather.Tick(age);effectBudget=2;while(head<tail&&effectBudget-->0){var item=pending[head++%pending.Length];if(item==null||!item.Active)continue;var p=item.body.position;item.Detonated(age);match.HazardBlast(p);Blast(p);}if(head==tail)head=tail=0;for(int i=0;i<sheep.Length;i++)if(i<6+chaos*5)sheep[i].Step(age,dt);
  for(int i=0;i<sweepers.Length;i++){var b=sweepers[i];if(b.detectCollisions)b.MoveRotation(Quaternion.Euler(0,Mathf.Sin(age*.65f+i*2)*100,0));}
  foreach(var b in movable)if(b.gameObject.activeSelf&&b.detectCollisions){if(b.position.y< -4||!KeepawayBoundary.Contains(b.position,.7f)){b.position=Wander(31,age)+Vector3.up;b.linearVelocity=b.angularVelocity=Vector3.zero;}b.AddForce(weather.Flow*.3f,ForceMode.Acceleration);}
 }
 public void Pause(bool value)=>weather.Pause(value);
}
}
