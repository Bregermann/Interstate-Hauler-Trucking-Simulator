using UnityEngine;
namespace LWS.TruckCheeseRolling {
// Physics callbacks gather a mask; resolution on the NEXT fixed step removes callback/index bias.
public static class CheeseRules {
 public const int Target=5,BackgroundCount=12;
 public static int Resolve(uint mask,int count){int winner=-1;for(int i=0;i<count;i++)if((mask&(1u<<i))!=0){if(winner>=0)return -2;winner=i;}return winner;}
 public static int[] StarterGrid(int seed,int round){var slots=new int[16];for(int i=0;i<16;i++)slots[i]=i;var rng=new System.Random(seed+round*977);for(int i=15;i>0;i--){int j=rng.Next(i+1),swap=slots[i];slots[i]=slots[j];slots[j]=swap;}return slots;}
 public static float Height(float x,float z){float hill=z<=14?330:z<34?330-(z-14)*(z-14)/20:z<164?310-(z-34)*2:z<214?(214-z)*(214-z)/50:0;float bank=Mathf.Max(0,Mathf.Abs(x)-49);return 5+Mathf.Max(0,hill)+bank*bank*.15f+((z>40&&z<155)?(Mathf.Sin(z*.22f+x*.13f)*.12f+Mathf.Sin(x*.5f-z*.12f)*.08f)*Mathf.SmoothStep(0,1,(z-40)/12)*Mathf.SmoothStep(0,1,(155-z)/12):0);}
 public static float RunoutControl(Vector3 p){float slope=Mathf.Abs(Height(p.x,p.z+1)-Height(p.x,p.z-1))*.5f;return Mathf.SmoothStep(0,1,Mathf.InverseLerp(214,244,p.z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.65f,slope)));}
 public static Quaternion SlopeRotation(Vector3 p)=>Quaternion.Euler(Mathf.Atan2(Height(p.x,p.z-.5f)-Height(p.x,p.z+.5f),1)*Mathf.Rad2Deg,0,0);
 public static Vector3 Surface(float x,float z,float lift=0)=>new Vector3(x,Height(x,z)+lift,z);
 public static bool Safe(Vector3 p)=>Mathf.Abs(p.x)<65&&p.z>-15&&p.z<325&&p.y>Height(p.x,p.z)-12&&p.y<Height(p.x,p.z)+500;
}
}
