using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Modules.Trailer;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckHitchSports.Editor
{
    public static class HitchAuthoring
    {
        public const string TractorSource = "Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab";
        public const string TrailerSource = "Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTrailer Variant.prefab";
        const string Root = "Assets/TruckHitchSports";
        public static SportsEventTruck CreateTractor(SportsEventSession match, int slot, Vector3 position)
        {
            var native = Native(TractorSource, "Competition tractor " + (slot + 1), position, Quaternion.identity);
            var truck = native.AddComponent<SportsEventTruck>(); truck.session = match; truck.slot = slot;
            truck.body = native.GetComponent<Rigidbody>(); truck.visuals = native.transform;
            truck.paint = native.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == "Cab") ?? native.GetComponentInChildren<Renderer>();
            truck.wheels = native.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);
            truck.shape = native.AddComponent<BoxCollider>(); truck.shape.isTrigger = true;
            var bounds = Bounds(native); truck.shape.center = bounds.center; truck.shape.size = bounds.size;
            truck.tractorMotor = native.AddComponent<SportsNwhTractorMotor>();
            truck.tractorMotor.vehicle = native.GetComponent<VehicleController>(); truck.tractorMotor.settings = match.settings;
            truck.tractorMotor.forceSpatialAudio = true;
            var coupler = native.AddComponent<HitchCoupler>(); coupler.Body = truck.body; coupler.Participant = slot;
            coupler.Hitch = native.GetComponentInChildren<TrailerHitchModuleWrapper>(true);
            if (coupler.Hitch == null) throw new InvalidOperationException("Matched NWH tractor requires its original fifth wheel.");
            coupler.Hitch.module.attachOnEnable = false; coupler.Hitch.module.detachable = true;
            return truck;
        }
        public static TrailerBody CreateTrailer(Vector3 position, Quaternion rotation, string name = "Cargo trailer")
            => CreateNativeTrailer(TrailerSource, position, rotation, name);
        public static TrailerBody CreateFlatbed(Vector3 position, Quaternion rotation, string name = "Native suspended-axle flatbed")
            => CreateNativeTrailer("Assets/NWH/Vehicle Physics 2/Vehicles/RocketCarrier/RocketCarrierTrailer.prefab", position, rotation, name);
        static TrailerBody CreateNativeTrailer(string source, Vector3 position, Quaternion rotation, string name)
        {
            var native = Native(source, name, position, rotation);
            var trailer = native.AddComponent<TrailerBody>(); trailer.Body = native.GetComponent<Rigidbody>();
            trailer.Module = native.GetComponentInChildren<TrailerModuleWrapper>(true); trailer.Vehicle = native.GetComponent<VehicleController>();
            if (trailer.Module == null) throw new InvalidOperationException("Matched NWH trailer requires its original kingpin and axle configuration.");
            return trailer;
        }
        public static void PlaceCoupledAtStart(HitchCoupler tractor, TrailerBody trailer)
        {
            trailer.transform.rotation = tractor.transform.rotation;
            trailer.transform.position += tractor.Anchor.position - trailer.Kingpin.position;
        }
        static GameObject Native(string path, string name, Vector3 position, Quaternion rotation)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Required licensed NWH source missing: " + path);
            var native = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(native, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            native.name = name; native.transform.SetPositionAndRotation(position, rotation);
            foreach (var listener in native.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(listener);
            foreach (var camera in native.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (var behaviour in native.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var ns = behaviour.GetType().Namespace ?? "";
                if (ns.StartsWith("NWH.Common.Cameras") || ns.StartsWith("NWH.VehiclePhysics2.Input")) behaviour.enabled = false;
            }
            foreach (var collider in native.GetComponentsInChildren<Collider>(true)) collider.gameObject.layer = 2;
            native.layer = 2;
            var vc = native.GetComponent<VehicleController>(); vc.input.autoSetInput = false; vc.enabled = false;
            foreach (var body in native.GetComponentsInChildren<Rigidbody>(true))
            { body.isKinematic = true; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.solverIterations = 20; body.solverVelocityIterations = 8; }
            foreach (var renderer in native.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var original = materials[i]; if (original == null || original.shader.name.StartsWith("Universal Render Pipeline/")) continue;
                    Directory.CreateDirectory(Root + "/Materials");
                    var asset = Root + "/Materials/Native " + original.name.Replace('/', '_') + ".mat";
                    var owned = AssetDatabase.LoadAssetAtPath<Material>(asset);
                    if (owned == null)
                    {
                        owned = new Material(Shader.Find("Universal Render Pipeline/Lit")); owned.mainTexture = original.mainTexture;
                        if (original.HasProperty("_Color")) owned.color = original.color;
                        AssetDatabase.CreateAsset(owned, asset);
                    }
                    materials[i] = owned;
                }
                renderer.sharedMaterials = materials;
            }
            return native;
        }
        public static Bounds Bounds(GameObject root)
        {
            Bounds result = default; bool first = true;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            { if (filter.sharedMesh == null) continue; var bounds = SportsRallyAuthoring.BoundsIn(filter, root.transform); if (first) { result = bounds; first = false; } else result.Encapsulate(bounds); }
            return result;
        }
    }
}
