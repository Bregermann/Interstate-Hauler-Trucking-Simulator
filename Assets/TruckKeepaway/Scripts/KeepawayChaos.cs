using System;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckKeepaway {
public sealed class KeepawayChaos:MonoBehaviour {
 public KeepawayGround ground;public KeepawayMatch match;public SportsChaosWeather weather;public Transform[] solidProps,ramps,patches;public Rigidbody[] movable,sweepers;public KeepawaySheep[] sheep;public int seed=8191,chaos=1,weatherChoice;
 public int LayoutHash{get;private set;}public int Chains{get;private set;}public int ConnectedCells{get;private set;}
 const int Width=33,Height=19,Count=Width*Height;readonly bool[] blocked=new bool[Count],connected=new bool[Count];readonly int[] queue=new int[Count],parents=new int[Count],visited=new int[Count];int searchStamp,effectBudget;float age;readonly KeepawaySheep[] pending=new KeepawaySheep[32];int head,tail;System.Random random;
 int Cell(Vector3 p)=>Mathf.Clamp(Mathf.RoundToInt((p.z+18)*.5f),0,Height-1)*Width+Mathf.Clamp(Mathf.RoundToInt((p.x+32)*.5f),0,Width-1);
 Vector3 Point(int cell)=>new Vector3(cell%Width*2-32,.48f,cell/Width*2-18);
 public void NewLayout(int value){seed=value;random=new System.Random(seed);age=0;head=tail=Chains=0;LayoutHash=17;Array.Clear(blocked,0,Count);Array.Clear(connected,0,Count);
  for(int i=0;i<solidProps.Length;i++){var p=new Vector3((i%4-1.5f)*11,.6f,i<4?-7:7);p.x+=(float)(random.NextDouble()-.5)*2;p.z+=(float)(random.NextDouble()-.5)*2;solidProps[i].position=p;solidProps[i].rotation=Quaternion.Euler(0,random.Next(4)*90,0);solidProps[i].gameObject.SetActive(i<4+chaos*2);LayoutHash=unchecked(LayoutHash*31+Mathf.RoundToInt(p.x*100)+Mathf.RoundToInt(p.z*100));}
  for(int i=0;i<ramps.Length;i++){ramps[i].position=new Vector3(i%2==0?-19:19,0,i<2?0:13);ramps[i].rotation=Quaternion.Euler(0,(i<2?0:90)+(i%2==0?0:180),0);}
  for(int i=0;i<patches.Length;i++)patches[i].position=new Vector3(i%3*18-18,.018f,i<3?-12:12);
  for(int i=0;i<movable.Length;i++){var b=movable[i];b.gameObject.SetActive(i<4+chaos*2);b.isKinematic=false;b.position=new Vector3((i%4-1.5f)*8,.8f,i<4?-11:11);b.rotation=Quaternion.Euler(0,random.Next(360),0);b.linearVelocity=b.angularVelocity=Vector3.zero;}
  BuildConnectivity();if(ConnectedCells<Count*.6f){foreach(var p in solidProps)p.gameObject.SetActive(false);BuildConnectivity();}
  for(int i=0;i<sheep.Length;i++){sheep[i].index=i;sheep[i].arena=this;if(i<6+chaos*5&&SafeSheepSpawn(i,out var p))sheep[i].ResetSheep(p);else sheep[i].Dormant();}
  ground.Select(seed);weather.Configure(seed,weatherChoice,chaos);Debug.Log("KEEP_LAYOUT seed="+seed+" hash="+LayoutHash+" connected="+ConnectedCells);
 }
 void BuildConnectivity(){for(int cell=0;cell<Count;cell++){var p=Point(cell);blocked[cell]=!KeepawayBoundary.Contains(p,2);foreach(var prop in solidProps)if(prop.gameObject.activeSelf){var shape=prop.GetComponent<Collider>();if(shape!=null){var b=shape.bounds;b.Expand(new Vector3(2.8f,0,2.8f));if(p.x>=b.min.x&&p.x<=b.max.x&&p.z>=b.min.z&&p.z<=b.max.z)blocked[cell]=true;}}}Array.Clear(connected,0,Count);int first=Cell(Vector3.zero),read=0,write=0;queue[write++]=first;connected[first]=true;while(read<write){int c=queue[read++];Visit(c-1);Visit(c+1);Visit(c-Width);Visit(c+Width);}ConnectedCells=write;
  void Visit(int n){if(n<0||n>=Count||blocked[n]||connected[n])return;connected[n]=true;queue[write++]=n;}
 }
 public bool Reachable(Vector3 p)=>KeepawayBoundary.Contains(p,2)&&connected[Cell(p)];
 public Vector3 Route(Vector3 from,Vector3 to){int begin=Cell(from),end=Cell(to);if(!connected[end]){float best=9999;for(int i=0;i<Count;i++)if(connected[i]){float d=(Point(i)-to).sqrMagnitude;if(d<best){best=d;end=i;}}}if(begin==end)return to;searchStamp++;int read=0,write=0;queue[write++]=begin;visited[begin]=searchStamp;parents[begin]=-1;bool found=false;while(read<write&&!found){int c=queue[read++];Visit(c-1);Visit(c+1);Visit(c-Width);Visit(c+Width);}if(!found)return Point(begin);int step=end;while(parents[step]>=0&&parents[step]!=begin)step=parents[step];return Point(step);
  void Visit(int n){if(n<0||n>=Count||blocked[n]||visited[n]==searchStamp)return;visited[n]=searchStamp;parents[n]=queue[read-1];queue[write++]=n;if(n==end)found=true;}
 }
 public Vector3 Wander(int index,float time){for(int k=0;k<Count;k++){int cell=(int)((uint)unchecked(index*1777+(int)(time*.2f)*199+k*97+seed)%Count);if(connected[cell]){var p=Point(cell);p.y=.65f;return p;}}return new Vector3(0,.65f,0);}
 public bool SafeSheepSpawn(int index,out Vector3 p){for(int k=0;k<50;k++){p=Wander(index+k,age+k*2);bool safe=Mathf.Abs(p.x)<23||Mathf.Abs(p.z)<8;for(int i=0;i<match.setup.count;i++)if(match.alive[i]&&Vector3.Distance(p,match.trucks[i].body.position)<6)safe=false;if(safe)return true;}p=Vector3.zero;return false;}
 public float SurfaceGrip(Vector3 p){float grip=weather.Grip;for(int i=0;i<patches.Length;i++){var d=p-patches[i].position;if(Mathf.Abs(d.x)<3&&Mathf.Abs(d.z)<2.5f)grip*=i%2==0?.72f:.56f;}return Mathf.Max(.42f,grip);}
 public void Queue(KeepawaySheep item){if(tail-head>=pending.Length)return;pending[tail++%pending.Length]=item;}
 public void Blast(Vector3 p){Chains++;foreach(var item in sheep)if(item.Active&&!item.Queued&&Vector3.Distance(item.body.position,p)<10)item.Queue();}
 public void Step(float dt){age+=dt;weather.Tick(age);effectBudget=2;while(head<tail&&effectBudget-->0){var item=pending[head++%pending.Length];if(item==null||!item.Active)continue;var p=item.body.position;item.Detonated(age);match.HazardBlast(p);Blast(p);}if(head==tail)head=tail=0;for(int i=0;i<sheep.Length;i++)if(i<6+chaos*5)sheep[i].Step(age,dt);
  for(int i=0;i<sweepers.Length;i++){var b=sweepers[i];b.MoveRotation(Quaternion.Euler(0,Mathf.Sin(age*.65f+i*2)*100,0));}
  foreach(var b in movable)if(b.gameObject.activeSelf){if(b.position.y< -4||!KeepawayBoundary.Contains(b.position,.7f)){b.position=Wander(31,age)+Vector3.up;b.linearVelocity=b.angularVelocity=Vector3.zero;}b.AddForce(weather.Flow*.3f,ForceMode.Acceleration);}
 }
 public void Pause(bool value)=>weather.Pause(value);
}
}
