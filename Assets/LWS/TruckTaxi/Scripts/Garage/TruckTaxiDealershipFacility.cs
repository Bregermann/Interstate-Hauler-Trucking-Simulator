using System.Collections;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Regional facility binding only. The existing Garage, HUD and Career own
    // interaction, presentation and purchases; there is no second ownership store.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiDealershipFacility : MonoBehaviour
    {
        public TruckTaxiGarage garage;
        public BoxCollider interactionVolume;
        public bool configureGarage=true;
        private TruckTaxiBootstrap host;
        private string markerId;
        public static void Ensure(TruckTaxiBootstrap owner)
        {
            foreach(var existing in FindObjectsByType<TruckTaxiDealershipFacility>(FindObjectsSortMode.None))
                if(existing.gameObject.scene==owner.gameObject.scene) {existing.host=owner;return;}
            var eden=FindFirstObjectByType<TruckTaxiEdenboroughSceneBinding>();
            if(eden!=null && eden.gameObject.scene==owner.gameObject.scene)
            {
                var binding=eden.garage.gameObject.AddComponent<TruckTaxiDealershipFacility>();
                binding.garage=eden.garage;binding.interactionVolume=eden.garageVolume;binding.configureGarage=false;binding.host=owner;
                return;
            }
            var prefab=Resources.Load<GameObject>("TruckTaxi/Trucks/DealershipFacility");
            if(prefab==null){Debug.LogError("Truck Taxi dealership facility prefab is missing. Run Vehicle Factory authoring.",owner);return;}
            var root=Instantiate(prefab);
            root.name="Truck Taxi Dealership and Customization";
            var at=owner.playerSpawn.position+owner.playerSpawn.right*36-owner.playerSpawn.forward*20;
            if(Physics.Raycast(at+Vector3.up*150,Vector3.down,out var ground,300,~0,QueryTriggerInteraction.Ignore)) at.y=ground.point.y;
            root.transform.SetPositionAndRotation(at,owner.playerSpawn.rotation);
            root.GetComponent<TruckTaxiDealershipFacility>().host=owner;
        }
        private IEnumerator Start()
        {
            if(host==null) host=TruckTaxiBootstrap.Instance;
            while(host!=null && !host.StartupComplete) yield return null;
            if(host==null || !host.Ready || garage==null || interactionVolume==null) yield break;
            if(configureGarage) garage.Configure(host,host.hud,interactionVolume,host.TruckFactory.CurrentPaintMaterials);
            markerId="taxi.dealership."+host.gameObject.scene.name;
            host.GPS.MapMarkers.RegisterRegionalPoint(markerId,"Truck Dealership & Customization","Truck dealership",interactionVolume.bounds.center,
                TruckTaxiMapMarkerType.Repair,true);
            host.GPS.RequestPresentationRefresh();
        }
        private void OnDestroy(){if(markerId!=null && host?.GPS?.MapMarkers!=null) host.GPS.MapMarkers.UnregisterRegionalPoint(markerId);}
    }
}
