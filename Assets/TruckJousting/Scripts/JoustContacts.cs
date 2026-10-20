using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckJousting
{
    public sealed class JoustContacts:MonoBehaviour
    {
        public JoustMatch match;public int owner;readonly ContactPoint[] contacts=new ContactPoint[24];
        void OnCollisionEnter(Collision collision)
        {
            if(!match.Live||match.Countdown>0)return;int count=collision.GetContacts(contacts);
            for(int i=0;i<count;i++)
            {
                var p=contacts[i];if(p.thisCollider!=match.lances[owner].tip)continue;
                var shield=p.otherCollider.GetComponent<JoustShield>();
                if(shield==null||shield.match!=match||shield.owner==owner)continue;
                float normalSpeed=Mathf.Abs(Vector3.Dot(collision.relativeVelocity,p.normal));
                match.Strike(owner,shield,p.point,normalSpeed);return;
            }
        }
    }
}
