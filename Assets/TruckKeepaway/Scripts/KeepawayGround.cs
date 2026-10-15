using UnityEngine;
namespace LWS.TruckKeepaway {
// Presentation selection has its own seed stream; it never changes obstacle/AI randomness.
public sealed class KeepawayGround:MonoBehaviour {
 public MeshRenderer surface;public Material[] themes;public GameObject[] dressing;public int Theme{get;private set;}
 public string Description=>new[]{"ROCKY MUD","SNOWFIELD","BEACH SAND","MEADOW"}[Theme];
 public void Select(int seed){Theme=new System.Random(unchecked(seed*397^0x61a7)).Next(4);surface.sharedMaterial=themes[Theme];for(int i=0;i<dressing.Length;i++)dressing[i].SetActive(i==Theme);}
 public static float Height(float x,float z){float edge=Mathf.Min(32-Mathf.Abs(x),18-Mathf.Abs(z));return .012f+Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/1.4f))*(.025f+.065f*Mathf.PerlinNoise(x*.18f+17,z*.18f+31));}
}
}
