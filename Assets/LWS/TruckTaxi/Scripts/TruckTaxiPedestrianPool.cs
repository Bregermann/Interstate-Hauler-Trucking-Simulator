using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // UTS supplies each prefab and its walking components; the pool only owns inactive instances.
    public sealed class TruckTaxiPedestrianPool
    {
        private readonly Stack<TruckTaxiPedestrian> inactive = new Stack<TruckTaxiPedestrian>();
        public int Count => inactive.Count;

        public void Release(TruckTaxiPedestrian pedestrian)
        {
            if (pedestrian == null) return;
            if (!pedestrian.CanReuse) { Object.Destroy(pedestrian.gameObject); return; }
            pedestrian.PrepareForPool();
            inactive.Push(pedestrian);
        }

        public TruckTaxiPedestrian Acquire(Vector3 position, Quaternion rotation, string id, bool fullPhysics)
        {
            while (inactive.Count > 0)
            {
                var pedestrian = inactive.Pop();
                if (pedestrian == null) continue;
                pedestrian.Reactivate(position, rotation, id, fullPhysics);
                return pedestrian;
            }
            return null;
        }

        public void Clear()
        {
            while (inactive.Count > 0)
            {
                var pedestrian = inactive.Pop();
                if (pedestrian != null) Object.Destroy(pedestrian.gameObject);
            }
        }
    }
}
