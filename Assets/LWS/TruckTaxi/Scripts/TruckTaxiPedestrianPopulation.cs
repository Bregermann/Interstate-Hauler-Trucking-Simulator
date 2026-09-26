using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // UTS owns walking, path following, avoidance and animation; LWS adds scoring and respawn.
    public sealed class TruckTaxiPedestrianPopulation : MonoBehaviour
    {
        public Component[] peoplePaths;
        public int maximumPeople = 24;
        private TruckTaxiPedestrianImpactSettings settings=new TruckTaxiPedestrianImpactSettings();
        private readonly List<TruckTaxiPedestrian> people = new List<TruckTaxiPedestrian>();
        private readonly Dictionary<TruckTaxiPedestrian,Component> origins=new Dictionary<TruckTaxiPedestrian,Component>();
        private int serial;
        public int ActiveCount => people.Count;
        public IReadOnlyList<TruckTaxiPedestrian> People => people;
        public bool ShowColliders { get; set; }
        public void Initialize(TruckTaxiPedestrianImpactSettings configuration=null)
        {
            settings=configuration ?? settings;
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
                if(child.GetComponent<Rigidbody>()==null) child.gameObject.AddComponent<Rigidbody>();
                var ped=child.gameObject.AddComponent<TruckTaxiPedestrian>();
                ped.settings=settings;
                var target=child.gameObject.AddComponent<TruckTaxiImpactTarget>();
                target.kind=TaxiImpactKind.Pedestrian; target.targetId="taxi.pedestrian."+serial++;
                people.Add(ped);
                origins.Add(ped,path); ped.Expired+=Retire;
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
        {
            foreach(var ped in people)
                if(ped!=null) { ped.Expired-=Retire; ped.gameObject.SetActive(false); Destroy(ped.gameObject); }
            people.Clear(); origins.Clear(); Initialize();
        }
        private void Retire(TruckTaxiPedestrian ped)
        {
            if(!origins.TryGetValue(ped,out var path)) return;
            ped.Expired-=Retire; people.Remove(ped); origins.Remove(ped);
            ped.gameObject.SetActive(false); Destroy(ped.gameObject);
            if(!isActiveAndEnabled || path==null || people.Count>=maximumPeople) return;
            path.GetType().GetMethod("SpawnOnePeople").Invoke(path,new object[]{0,true});
            Bind(path);
        }
        public void DebugRagdoll(Vector3 origin,Camera camera,bool allVisible)
        {
            TruckTaxiPedestrian nearest=null; float distance=float.PositiveInfinity;
            foreach(var ped in people)
            {
                if(ped==null || ped.IsRagdoll) continue;
                float d=(ped.transform.position-origin).sqrMagnitude;
                if(d<distance) { distance=d; nearest=ped; }
                if(!allVisible || camera==null) continue;
                var point=camera.WorldToViewportPoint(ped.transform.position+Vector3.up);
                if(point.z>0 && point.x>=0 && point.x<=1 && point.y>=0 && point.y<=1)
                    ped.TryStrike((ped.transform.position-origin).normalized*8,ped.transform.position+Vector3.up);
            }
            if(!allVisible && nearest!=null)
                nearest.TryStrike((nearest.transform.position-origin).normalized*8,nearest.transform.position+Vector3.up);
        }
        private void OnDrawGizmos()
        {
            if(!ShowColliders) return;
            Gizmos.color=Color.cyan;
            foreach(var ped in people) if(ped!=null)
                foreach(var collider in ped.GetComponentsInChildren<Collider>()) if(collider.enabled)
                    Gizmos.DrawWireCube(collider.bounds.center,collider.bounds.size);
        }
    }
}
