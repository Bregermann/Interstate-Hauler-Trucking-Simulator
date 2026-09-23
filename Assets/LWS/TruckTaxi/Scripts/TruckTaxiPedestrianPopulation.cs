using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // UTS owns walking, path following, avoidance and animation; LWS adds scoring and respawn.
    public sealed class TruckTaxiPedestrianPopulation : MonoBehaviour
    {
        public Component[] peoplePaths;
        public int maximumPeople = 24;
        public float respawnSeconds = 12;
        private readonly List<TruckTaxiPedestrian> people = new List<TruckTaxiPedestrian>();
        private int serial;
        public int ActiveCount => people.Count;
        public void Initialize()
        {
            if (people.Count > 0) return;
            foreach (var path in peoplePaths)
            {
                path.GetType().GetMethod("SpawnPeople").Invoke(path,null);
                Bind(path);
            }
        }
        private void Bind(Component path)
        {
            var type=path.GetType();
            var parent=type.GetField("par").GetValue(path) as GameObject;
            if(parent==null) return;
            foreach(Transform child in parent.transform)
            {
                if(child.GetComponent<TruckTaxiPedestrian>()!=null) continue;
                child.gameObject.layer=0;
                var body=child.GetComponent<Rigidbody>() ?? child.gameObject.AddComponent<Rigidbody>();
                var ped=child.gameObject.AddComponent<TruckTaxiPedestrian>();
                ped.respawnDelay=respawnSeconds;
                var target=child.gameObject.AddComponent<TruckTaxiImpactTarget>();
                target.kind=TaxiImpactKind.Pedestrian; target.targetId="taxi.pedestrian."+serial++;
                people.Add(ped);
            }
        }
        public void SpawnOne()
        {
            if(people.Count>=maximumPeople || peoplePaths.Length==0) return;
            var path=peoplePaths[serial%peoplePaths.Length];
            path.GetType().GetMethod("SpawnOnePeople").Invoke(path,new object[]{0,true});
            Bind(path);
        }
        public void ResetPopulation()
        { foreach(var ped in people) if(ped!=null) ped.ResetPedestrian(); }
    }
}
