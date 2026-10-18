using UnityEngine;
namespace LWS.TruckCheeseRolling {
// Physics callbacks gather a mask; resolution on the NEXT fixed step removes callback/index bias.
public static class CheeseRules {
 public const int Target=5,BackgroundCount=12;
 public static int Resolve(uint mask,int count){int winner=-1;for(int i=0;i<count;i++)if((mask&(1u<<i))!=0){if(winner>=0)return -2;winner=i;}return winner;}
 public static int[] StarterGrid(int seed,int round){var slots=new int[16];for(int i=0;i<16;i++)slots[i]=i;var rng=new System.Random(seed+round*977);for(int i=15;i>0;i--){int j=rng.Next(i+1),swap=slots[i];slots[i]=slots[j];slots[j]=swap;}return slots;}
 public const float HillGradeScale=.875f;
 public static float Height(float x,float z){
  float hill=z<=14?330:z<34?330-(z-14)*(z-14)/20:z<164?310-(z-34)*2:z<214?(214-z)*(214-z)/50:0;
  float bank=Mathf.Max(0,Mathf.Abs(x)-49);
  // Broad, smoothly blended terrain features. The entire starting platform and flat runout stay level.
  float fade=Mathf.SmoothStep(0,1,(z-36)/18)*Mathf.SmoothStep(0,1,(188-z)/24);
  float rolling=(Mathf.PerlinNoise(x*.045f+43,z*.027f+5)-.5f)*1.8f+(Mathf.PerlinNoise(x*.095f+9,z*.063f+63)-.5f)*.35f;
  float features=2.1f*Mound((x+19)/9,(z-73)/12)-1.8f*Mound((x-16)/10,(z-112)/13)+1.6f*Mound((x-31)/11,(z-144)/14);
  float rut=x-8-Mathf.Sin(z*.035f)*6;
  features-=1.1f*Mound(rut/3.8f,(z-119)/36);
  return 5+Mathf.Max(0,hill)*HillGradeScale+bank*bank*.15f+(rolling+features)*fade;
 }
 static float Mound(float x,float z)=>Mathf.Exp(-x*x-z*z);
 public static float RunoutControl(Vector3 p){float slope=Mathf.Abs(Height(p.x,p.z+1)-Height(p.x,p.z-1))*.5f;return Mathf.SmoothStep(0,1,Mathf.InverseLerp(214,244,p.z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.65f,slope)));}
 public static Quaternion SlopeRotation(Vector3 p)=>Quaternion.Euler(Mathf.Atan2(Height(p.x,p.z-.5f)-Height(p.x,p.z+.5f),1)*Mathf.Rad2Deg,0,0);
 public static Vector3 Surface(float x,float z,float lift=0)=>new Vector3(x,Height(x,z)+lift,z);
 public static bool Safe(Vector3 p)=>Mathf.Abs(p.x)<65&&p.z>-15&&p.z<325&&p.y>Height(p.x,p.z)-12&&p.y<Height(p.x,p.z)+500;
}
}
