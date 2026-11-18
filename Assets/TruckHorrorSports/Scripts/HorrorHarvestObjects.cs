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
        BoxCollider gateShape;
        bool initialized, unlocked;
        public bool Unlocked => unlocked;
        public void ResetObjects()
        {
            if(!initialized){closed=gateHinge.localRotation;gateShape=gateHinge.GetComponent<BoxCollider>();initialized=true;}
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
            if(unlocked)
            {
                Quaternion current = gateHinge.localRotation;
                Quaternion next = Quaternion.RotateTowards(current, closed * Quaternion.Euler(0, -90, 0), dt * 45);
                if (next == current) return;
                // A real truck can enter the leaf's sweep after the key is used,
                // including at its ends outside the original center-distance check.
                // Pause the solid leaf rather than driving its collider through the rig.
                for(int i=0;i<match.setup.count;i++)
                {
                    var truck=match.trucks[i];
                    if(truck!=null&&WouldGateHitParticipant(gateShape,next,truck.shape))return;
                }
                gateHinge.localRotation=next;
            }
        }
        static bool WouldGateHitParticipant(BoxCollider leaf,Quaternion localRotation,BoxCollider participant)
        {
            if(leaf==null||participant==null||!participant.enabled||!participant.gameObject.activeInHierarchy)return false;
            Quaternion worldRotation=leaf.transform.parent!=null?leaf.transform.parent.rotation*localRotation:localRotation;
            return Physics.ComputePenetration(leaf,leaf.transform.position,worldRotation,
                participant,participant.transform.position,participant.transform.rotation,out _,out float depth)&&depth>.002f;
        }
    }
}
