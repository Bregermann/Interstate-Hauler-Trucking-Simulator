#if UNITY_EDITOR
using UnityEngine;
// Read-only test observer. Retains actual Enter/Stay pair until actual Exit;
// sleeping-body callback expiry can be distinguished from real separation.
public sealed class SnowmanPackingContactProbe:MonoBehaviour
{
    public Collider target;public int enters,stays,exits;public bool paired;public float last=-100,maxUp;public Vector3 point;
    void Observe(Collision c){if(c.collider!=target)return;last=Time.fixedTime;paired=true;for(int i=0;i<c.contactCount;i++){var p=c.GetContact(i);maxUp=Mathf.Max(maxUp,p.normal.y);point=p.point;}}
    void OnCollisionEnter(Collision c){if(c.collider==target)enters++;Observe(c);}
    void OnCollisionStay(Collision c){if(c.collider==target)stays++;Observe(c);}
    void OnCollisionExit(Collision c){if(c.collider!=target)return;exits++;paired=false;}
}
#endif
