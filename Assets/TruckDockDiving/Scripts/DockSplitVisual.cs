using UnityEngine;
namespace LWS.TruckDockDiving {
// Cab/upper model pieces separate from the chassis. One authoritative physics body is untouched.
public sealed class DockSplitVisual:MonoBehaviour {
 public Transform[] upper;Vector3[] rest;public float Separation{get;private set;}
 void Awake(){Cache();}void Cache(){if(rest!=null)return;rest=new Vector3[upper.Length];for(int i=0;i<upper.Length;i++)rest[i]=upper[i].localPosition;}
 public void Apply(float amount){Cache();Separation=amount;for(int i=0;i<upper.Length;i++)upper[i].localPosition=rest[i]+Vector3.up*amount;}
 void OnDisable(){Apply(0);}
}
}
