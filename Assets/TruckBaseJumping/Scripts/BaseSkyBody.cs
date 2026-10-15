using UnityEngine;
namespace LWS.TruckBaseJumping {
public sealed class BaseSkyBody:MonoBehaviour {
 public BaseSkyField field;public Rigidbody body;public int kind,index;public bool queued;public float recycleAt;public TrailRenderer trail;
 void OnCollisionEnter(Collision c){var jumper=c.rigidbody!=null?c.rigidbody.GetComponent<BaseJumper>():null;if(jumper!=null&&!jumper.Landed&&jumper.Released&&kind!=0)field.Queue(this);}
}
}
