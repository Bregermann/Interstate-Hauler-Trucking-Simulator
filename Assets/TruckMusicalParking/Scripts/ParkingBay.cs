using UnityEngine;
namespace LWS.TruckMusicalParking {
public sealed class ParkingBay:MonoBehaviour {
 public Vector2 size=new Vector2(4.8f,7.5f);public Renderer[] markings;public TextMesh caption;public int Owner{get;private set;}=-1;public bool Open{get;private set;}MaterialPropertyBlock block;
 public void ResetBay(bool active,bool open){gameObject.SetActive(active);Owner=-1;SetOpen(open);}
 public void SetOpen(bool value){Open=value;Paint(value?new Color(.3f,.95f,.58f):new Color(.98f,.25f,.13f));if(caption!=null)caption.text=value?"OPEN":"CLOSED";}
 public void Secure(int owner,Color color,string name){if(Owner>=0)return;Owner=owner;Open=false;Paint(color);if(caption!=null)caption.text=name+"\nSECURED";}
 void Paint(Color color){if(block==null)block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);block.SetColor("_Color",color);foreach(var r in markings)r.SetPropertyBlock(block);}
}
}
