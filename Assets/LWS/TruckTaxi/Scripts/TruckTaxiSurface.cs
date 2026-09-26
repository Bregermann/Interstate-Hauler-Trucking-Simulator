using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiSurface : MonoBehaviour
    {
        public bool isRoad;
        private static readonly RaycastHit[] hits=new RaycastHit[32];
        public static bool TrySample(Vector3 position,Transform tractor,out bool onRoad)
        {
            float closest=float.MaxValue; bool found=false; onRoad=true;
            int count=Physics.RaycastNonAlloc(position+Vector3.up*2,Vector3.down,hits,12,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];
                if(hit.transform.IsChildOf(tractor) || hit.distance>=closest) continue;
                var surface=hit.collider.GetComponentInParent<TruckTaxiSurface>();
                if(surface==null) continue;
                closest=hit.distance; onRoad=surface.isRoad; found=true;
            }
            return found;
        }
    }
}
