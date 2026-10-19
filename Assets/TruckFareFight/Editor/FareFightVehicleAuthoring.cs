using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using LWS.TruckSports;
using LWS.TruckTaxi;
using NWH.VehiclePhysics2;
namespace LWS.TruckFareFight.Editor
{
    public static class FareFightVehicleAuthoring
    {
        public const string PrefabPath = "Assets/TruckFareFight/Prefabs/Fare Fight Taxi Tractor.prefab";
        public static GameObject EnsurePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); if (existing != null) { DisableLegacyCameras(existing);return existing; }
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab");
            if (source == null) throw new InvalidOperationException("Existing Truck Taxi full NWH tractor prefab is required.");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var root = UnityEngine.Object.Instantiate(source); root.name = "Fare Fight Taxi Tractor";
            try
            {
                // The sport routes local inputs. Keep NWH's complete vehicle,
                // powertrain, suspension and visual components intact.
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    string space = behaviour.GetType().Namespace ?? "";
                    if (space.StartsWith("LWS.InterstateHauler") || space.StartsWith("NWH.VehiclePhysics2.Input")) behaviour.enabled = false;
                }
                var vehicle = root.GetComponent<VehicleController>();
                if (vehicle == null) throw new InvalidOperationException("Taxi tractor has no full NWH VehicleController.");
                vehicle.input.autoSetInput = false;
                vehicle.powertrain.transmission.transmissionType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.TransmissionShiftType.Automatic;
                vehicle.powertrain.transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.Auto;
                vehicle.powertrain.engine.autoStartOnThrottle = true;
                var motor = root.AddComponent<SportsNwhTractorMotor>(); motor.vehicle = vehicle;
                motor.handling = root.GetComponent<TruckTaxiVehicleHandlingOverride>() ?? root.AddComponent<TruckTaxiVehicleHandlingOverride>();
                var facade = root.AddComponent<SportsEventTruck>(); facade.tractorMotor = motor;
                facade.body = root.GetComponent<Rigidbody>(); facade.visuals = root.transform;
                facade.wheels = root.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);
                // Taxi uses its authored compound/convex colliders. Preserve
                // them; shared camera/contact metadata needs a box envelope only.
                var physical = root.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger && c.enabled).ToArray();
                if (physical.Length == 0) throw new InvalidOperationException("Taxi's authored physical body colliders are missing.");
                bool first=true;Bounds envelope=default;
                foreach(var collider in physical)for(int i=0;i<8;i++){var b=collider.bounds;var point=root.transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){envelope=new Bounds(point,Vector3.zero);first=false;}else envelope.Encapsulate(point);}
                facade.shape = root.AddComponent<BoxCollider>(); facade.shape.center=envelope.center;facade.shape.size=envelope.size;facade.shape.isTrigger=true;
                facade.paint = root.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == "Cab");
                if (facade.paint == null) throw new InvalidOperationException("Taxi's authored cab paint renderer is missing.");
                facade.steerAngle = motor.handling.lowSpeedSteeringLock; facade.engine = null;
                DisableLegacyCameras(root);motor.Power(false); facade.body.isKinematic = true;
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static void DisableLegacyCameras(GameObject root){
            foreach(var camera in root.GetComponentsInChildren<Camera>(true)){camera.enabled=false;EditorUtility.SetDirty(camera);}
            foreach(var listener in root.GetComponentsInChildren<AudioListener>(true))UnityEngine.Object.DestroyImmediate(listener,true);
            foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))if((behaviour.GetType().Namespace??"").StartsWith("NWH.Common.Cameras")){behaviour.enabled=false;EditorUtility.SetDirty(behaviour);}
        }
        public static SportsEventTruck Spawn(SportsEventSession match, int slot, Vector3 position, Quaternion rotation)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(EnsurePrefab()); root.transform.SetPositionAndRotation(position, rotation);
            var truck = root.GetComponent<SportsEventTruck>(); truck.session = match; truck.slot = slot;
            truck.tractorMotor.settings = match.settings;
            EditorUtility.SetDirty(truck); EditorUtility.SetDirty(truck.tractorMotor); return truck;
        }
    }
}
