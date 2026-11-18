using UnityEngine;
using UnityEngine.AI;
namespace LWS.TruckHorrorSports
{
    /// <summary>Visible carried ritual objects and the existing barn key's physical route lock.</summary>
    public sealed class HorrorHarvestObjects : MonoBehaviour
    {
        public HorrorMatch match;
        public int keyObjective=2;
        public Transform gateHinge;
        public NavMeshObstacle routeObstacle;
        public GameObject lockEffigy;
        public GameObject[] carriedEffigies;
        Quaternion closed;
        bool initialized, unlocked;
        public bool Unlocked => unlocked;
        public void ResetObjects()
        {
            if(!initialized){closed=gateHinge.localRotation;initialized=true;}
            unlocked=false;gateHinge.localRotation=closed;if(routeObstacle!=null)routeObstacle.enabled=true;if(lockEffigy!=null)lockEffigy.SetActive(true);
            foreach(var visual in carriedEffigies)if(visual!=null)visual.SetActive(false);
        }
        public void SetCarried(int slot,int objective)
        {if(slot>=0&&slot<carriedEffigies.Length&&carriedEffigies[slot]!=null)carriedEffigies[slot].SetActive(objective>=0);}
        public void TickObjects(float dt)
        {
            if(match==null||!match.Live||!initialized)return;
            if(!unlocked)
            {
                bool hasKey=false,occupied=false;
                for(int i=0;i<match.setup.count;i++)
                {hasKey|=match.rules.Done(i,keyObjective);occupied|=Vector3.Distance(match.PlayerPosition(i),gateHinge.position+gateHinge.right*10)<8;}
                if(hasKey&&!occupied){unlocked=true;if(routeObstacle!=null)routeObstacle.enabled=false;if(lockEffigy!=null)lockEffigy.SetActive(false);}
            }
            if(unlocked)gateHinge.localRotation=Quaternion.RotateTowards(gateHinge.localRotation,closed*Quaternion.Euler(0,-90,0),dt*45);
        }
    }
}
