using UnityEngine;
namespace LWS.TruckCheeseRolling {
public sealed class CheeseScenery:MonoBehaviour {
 public CheeseTree[] trees;public Transform[] props;public float[] radii;public Terrain terrain;public int TreeCount=14,RockCount=18,AnimalCount=8;public int LayoutHash{get;private set;}public int TallCells{get;private set;}
 TerrainData original,owned;readonly Vector3[] placed=new Vector3[40];
 void Awake(){original=terrain.terrainData;owned=Instantiate(original);terrain.terrainData=owned;terrain.GetComponent<TerrainCollider>().terrainData=owned;}
 public static bool Corridor(float x,float z){float bend=Mathf.Sin(z*.035f)*2.5f;return Mathf.Abs(x-bend)<4||Mathf.Abs(x+29-bend)<4||Mathf.Abs(x-29-bend)<4;}
 public void Randomize(int seed){var rng=new System.Random(seed);LayoutHash=17;for(int i=0;i<props.Length;i++){Vector3 p=Vector3.zero;bool found=false;for(int attempt=0;attempt<100;attempt++){float x=(float)(rng.NextDouble()*88-44),z=(float)(rng.NextDouble()*210+35);if(Corridor(x,z))continue;p=CheeseRules.Surface(x,z);bool clear=true;for(int j=0;j<i;j++)if((p-placed[j]).sqrMagnitude<49){clear=false;break;}if(!clear)continue;found=true;break;}props[i].gameObject.SetActive(found);placed[i]=p;if(!found)continue;props[i].position=p;float yaw=(float)rng.NextDouble()*360;props[i].rotation=Quaternion.Euler(0,yaw,0);if(i>=TreeCount+RockCount){var normal=new Vector3(CheeseRules.Height(p.x-.5f,p.z)-CheeseRules.Height(p.x+.5f,p.z),1,CheeseRules.Height(p.x,p.z-.5f)-CheeseRules.Height(p.x,p.z+.5f)).normalized;props[i].rotation=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,yaw,0);}if(i<TreeCount)trees[i].ResetTree(p,props[i].rotation);LayoutHash=unchecked(LayoutHash*31+Mathf.RoundToInt(p.x*10)+Mathf.RoundToInt(p.z*100));}
  owned.SetDetailLayer(0,0,0,GrassMap(owned.detailResolution,rng,out int cells));TallCells=cells;terrain.Flush();
 }
 public static int[,] GrassMap(int n,System.Random rng,out int cells){var map=new int[n,n];cells=0;for(int patch=0;patch<30;patch++){float x=(float)(rng.NextDouble()*96-48),z=(float)(rng.NextDouble()*185+35),radius=(float)(rng.NextDouble()*3+3);for(int row=0;row<n;row++)for(int col=0;col<n;col++){float px=col/(float)n*140-70,pz=row/(float)n*360-20;if(Corridor(px,pz)||new Vector2(px-x,pz-z).sqrMagnitude>radius*radius)continue;if(map[row,col]==0)cells++;map[row,col]=6;}}return map;}

 public bool ClearAt(Vector3 p,float margin){for(int i=0;i<props.Length;i++)if(props[i].gameObject.activeSelf&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(props[i].position.x,props[i].position.z))<(i<TreeCount&&trees[i].KnockedDown?5:radii[i])+margin)return false;return true;}
 public float Avoid(Vector3 p){float steer=0;for(int i=0;i<props.Length;i++){if(!props[i].gameObject.activeSelf)continue;var d=props[i].position-p;if(d.z>0&&d.z<12&&Mathf.Abs(d.x)<(i<TreeCount&&trees[i].KnockedDown?5:radii[i])+3)steer-=Mathf.Sign(d.x)*5;}return Mathf.Clamp(steer,-8,8);}
 void OnDestroy(){if(terrain!=null&&original!=null){terrain.terrainData=original;var c=terrain.GetComponent<TerrainCollider>();if(c!=null)c.terrainData=original;}if(owned!=null)Destroy(owned);}
}
}
