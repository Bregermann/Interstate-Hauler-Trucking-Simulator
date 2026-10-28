using System;
using UnityEngine;
namespace LWS.TruckSnowmanBuilding
{
    // Physical workshop catch only. Contacts are observations; original hoist/growth/score rules remain owners.
    public sealed class SnowmanCollectionRack:MonoBehaviour
    {
        public SnowmanMatch match;public BoxCollider[] planks=Array.Empty<BoxCollider>();
        public readonly int[] RealContacts=new int[3];public readonly float[] FirstContactClock={-1,-1,-1},LastContactClock={-1,-1,-1};
        public void ResetContacts(){Array.Clear(RealContacts,0,3);for(int i=0;i<3;i++)FirstContactClock[i]=LastContactClock[i]=-1;}
        public bool ContainsLane(SnowmanBall ball)
        {
            if(ball==null||ball.role<0||ball.role>=3||planks.Length!=19)return false;
            // Original authored ordering: each rail pair then its two posts.
            var left=planks[ball.role*4];var right=planks[(ball.role+1)*4];
            float slop=ball.sphere.contactOffset+Mathf.Max(left.contactOffset,right.contactOffset),x=ball.body.position.x;
            return x-ball.radius>=left.bounds.max.x-slop&&x+ball.radius<=right.bounds.min.x+slop;
        }
        public void Observe(SnowmanBall ball,Collision collision)
        {
            if(match==null||!match.Live||ball.match!=match||ball.Packed||ball.role<0||ball.role>=3||collision.contactCount<1)return;
            int role=ball.role;if(FirstContactClock[role]<0)FirstContactClock[role]=match.Clock;LastContactClock[role]=match.Clock;RealContacts[role]++;
        }
    }
}
