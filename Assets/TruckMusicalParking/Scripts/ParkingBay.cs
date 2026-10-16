using UnityEngine;
namespace LWS.TruckMusicalParking {
public sealed class ParkingBay:MonoBehaviour {
 public Transform progressFill;public Renderer progressRenderer;public Vector2 size=new Vector2(4.8f,7.5f);public Renderer[] markings;public TextMesh caption;public int Owner{get;private set;}=-1;public bool Open{get;private set;}MaterialPropertyBlock block;
 public void Progress(float value,Color color){if(progressFill==null)return;progressFill.gameObject.SetActive(Open&&Owner<0&&value>0);progressFill.localScale=new Vector3(Mathf.Max(.001f,value)*size.x,.025f,.22f);progressFill.localPosition=new Vector3((value-1)*size.x*.5f,.055f,-size.y*.5f+.25f);if(progressRenderer!=null){if(block==null)block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);block.SetColor("_Color",color);progressRenderer.SetPropertyBlock(block);}}
 public void ResetBay(bool active,bool open){gameObject.SetActive(active);Owner=-1;SetOpen(open);}
 public void SetOpen(bool value){Open=value;Paint(value?new Color(.3f,.95f,.58f):new Color(.98f,.25f,.13f));if(caption!=null)caption.text=value?"OPEN":"CLOSED";}
 public void Secure(int owner,Color color,string name){if(Owner>=0)return;Owner=owner;Open=false;Paint(color);if(caption!=null)caption.text=name+"\nSECURED";}
 void Paint(Color color){if(block==null)block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);block.SetColor("_Color",color);foreach(var r in markings)r.SetPropertyBlock(block);}
}
}
