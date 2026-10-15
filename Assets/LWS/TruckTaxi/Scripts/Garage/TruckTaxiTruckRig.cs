using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Effects;
using NWH.VehiclePhysics2.VehicleGUI;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Applies authored NWH-compatible presentation/configuration to the existing
    // canonical rig. Used by prefab authoring and by the runtime factory alike.
    public static class TruckTaxiTruckRig
    {
        public const string Prefix = "TT_Authored_";
        public static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        private static Transform Mount(Transform parent, string name, Vector3 worldPosition, Quaternion worldRotation)
        {
            var t = new GameObject(Prefix + name).transform;
            t.SetParent(parent, false); t.SetPositionAndRotation(worldPosition, worldRotation);
            return t;
        }
        public static void RemoveAuthored(Transform root)
        {
            foreach(var mount in root.GetComponentsInChildren<Transform>(true))
                if(mount.name==Prefix+"CockpitMount")
                    for(int i=mount.childCount-1;i>=0;i--)
                    {
                        var camera=mount.GetChild(i);
                        if(camera.GetComponent<Camera>()!=null) camera.SetParent(mount.parent,false);
                    }
            var vehicle=root.GetComponent<VehicleController>();
            if(vehicle!=null)
                foreach(var group in LightGroups(vehicle))
                    group?.lightSources?.RemoveAll(source=>source!=null &&
                        (source.meshRenderer!=null && IsAuthored(source.meshRenderer.transform,root) ||
                         source.light!=null && IsAuthored(source.light.transform,root)));
            foreach(var resources in root.GetComponentsInChildren<TruckTaxiRigLampMaterials>(true)) resources.Release();
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t == null || !t.name.StartsWith(Prefix, StringComparison.Ordinal) ||
                    t.parent != null && t.parent.name.StartsWith(Prefix, StringComparison.Ordinal)) continue;
                t.gameObject.SetActive(false);
                // Deferred destruction must not leave old bindings discoverable during same-frame rollback.
                t.SetParent(null,true);
                if (Application.isPlaying) UnityEngine.Object.Destroy(t.gameObject);
                else UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }
        private static bool IsAuthored(Transform node,Transform root)
        {
            for(var t=node;t!=null && t!=root;t=t.parent)
                if(t.name.StartsWith(Prefix,StringComparison.Ordinal)) return true;
            return false;
        }
        internal static IEnumerable<VehicleLight> LightGroups(VehicleController vehicle)
        {
            var lights=vehicle.effectsManager?.lightsManager;
            if(lights==null) yield break;
            yield return lights.lowBeamLights; yield return lights.highBeamLights;
            yield return lights.tailLights; yield return lights.brakeLights;
            yield return lights.leftBlinkers; yield return lights.rightBlinkers;
            yield return lights.reverseLights; yield return lights.extraLights;
        }
        internal static bool AuthoredActive(Transform node,Transform root)
        {
            for(var t=node;t!=null;t=t.parent)
            {
                if(!t.gameObject.activeSelf) return false;
                if(t==root) return true;
            }
            return false;
        }
        public static void Apply(GameObject root, TruckTaxiTruckModel model)
        {
            if (root == null || model == null) throw new ArgumentException("Missing rig/model.");
            if (!model.Validate(out string reason)) throw new ArgumentException(reason);
            var vehicle = root.GetComponent<VehicleController>();
            var cab = root.transform.Find("Cab");
            if (vehicle == null || cab == null) throw new ArgumentException("Model needs the original NWH tractor and Cab.");
            if(model.retainsOriginalBody) return;
            RemoveAuthored(root.transform);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                if (Array.IndexOf(model.replacedSourceMeshes, filter.sharedMesh) >= 0 && filter.TryGetComponent(out MeshRenderer renderer))
                    renderer.enabled = false;
            var placeholder = Find(root.transform, LwsTruckCabInteriorRecovery.SleeperPlaceholderRootName);
            if (placeholder != null) placeholder.gameObject.SetActive(false);

            root.GetComponent<Rigidbody>().mass = model.tractorMassKg;
            for (int i = 0; i < vehicle.powertrain.wheels.Count; i++)
            {
                var wheel = vehicle.powertrain.wheels[i];
                if (wheel.wheelGroupSelector.index != 1) continue;
                var position = wheel.wheelUAPI.transform.localPosition;
                position.z += model.rearAxleOffset; wheel.wheelUAPI.transform.localPosition = position;
            }
            var hitch = Find(root.transform, "TrailerAttachmentPoint");
            if (hitch != null) hitch.localPosition += Vector3.forward * model.fifthWheelOffset;
            var cabCollider = cab.GetComponent<BoxCollider>();
            if (cabCollider != null)
            {
                cabCollider.center = cab.InverseTransformPoint(root.transform.TransformPoint(model.cabinCollision.center));
                cabCollider.size = model.cabinCollision.size;
            }
            var chassis = Find(root.transform, "Chassis");
            if (chassis != null && chassis.TryGetComponent(out BoxCollider chassisBox))
            { var size=chassisBox.size; size.z-=model.rearAxleOffset; chassisBox.size=size; chassisBox.center+=Vector3.forward*model.rearAxleOffset*.5f; }
            var hood = Mount(root.transform, "HoodCollision", root.transform.position, root.transform.rotation).gameObject.AddComponent<BoxCollider>();
            hood.center = model.hoodCollision.center; hood.size = model.hoodCollision.size;

            var cockpit = Find(root.transform, "InsideCamera");
            if(cockpit==null) cockpit=Find(root.transform,"Vehicle Camera Driver");
            if (cockpit == null)
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                    if (camera.transform.IsChildOf(cab) && !camera.name.Contains("Mirror")) { cockpit=camera.transform; break; }
            if (cockpit != null)
            {
                // NWH caches the camera's local pose in Start. Move its mount,
                // not that cached pose, and retain the original camera instance.
                var mount=new GameObject(Prefix+"CockpitMount").transform;
                mount.SetParent(cockpit.parent,false);
                cockpit.SetParent(mount,false);
                mount.position+=root.transform.TransformPoint(model.driverEye)-cockpit.position;
            }
            var dashboard=root.GetComponent<LwsTruckDashboardController>();
            var steering=dashboard!=null ? dashboard.SteeringWheelVisual : null;
            if(steering==null) steering=vehicle.steering.steeringWheel;
            if (steering != null) steering.position = root.transform.TransformPoint(new Vector3(-.56f,1.99f,1.06f));
            var gps = Find(root.transform,"IH_CabAnchor_GpsMount");
            if (gps != null) gps.position = root.transform.TransformPoint(model.gpsMount);

            var levels = new List<Renderer[]>();
            var lamps = new Dictionary<string,List<MeshRenderer>>();
            var mirrors = root.GetComponent<LwsTruckMirrorController>();
            Renderer leftSurface=null,rightSurface=null;
            for (int level=0; level<3; level++)
            {
                var source = level==0 ? model.bodyVisualPrefab : model.distanceLodPrefabs[level-1];
                var instance=UnityEngine.Object.Instantiate(source,root.transform,false);
                instance.name=Prefix+"Import"+level;
                var rendered=new List<Renderer>();
                try
                {
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    bool active=AuthoredActive(renderer.transform,instance.transform);
                    string group=renderer.name.Split(new[]{"__"},StringSplitOptions.None)[0];
                    Transform target=group.StartsWith("Cab",StringComparison.Ordinal) || group.StartsWith("Mirror",StringComparison.Ordinal) ? cab : root.transform;
                    Vector3 pivot=Vector3.zero;
                    Quaternion orientation=root.transform.rotation;
                    if (group.StartsWith("Wheel_",StringComparison.Ordinal))
                    {
                        string tag=group.Substring(6);
                        var controller=Find(root.transform,"Whl HD "+tag+"_WheelController");
                        var visual=Find(root.transform,"Whl HD "+tag);
                        if (controller==null || visual==null) throw new InvalidOperationException("Missing NWH wheel binding "+tag);
                        target=visual;
                        pivot=root.transform.InverseTransformPoint(controller.position);
                        // Mesh vertices were authored around suspension anchor, while NWH
                        // supplies the actual contact-dependent rotating visual centre.
                        var wheelMount=Mount(target,"Wheel"+tag+"_LOD"+level,target.position,root.transform.rotation);
                        renderer.transform.SetParent(wheelMount,true);
                        renderer.transform.position+=target.position-root.transform.TransformPoint(pivot);
                    }
                    else if (group=="SteeringWheel" && steering!=null)
                    {
                        pivot=new Vector3(-.56f,1.99f,1.06f);
                        var parent=Mount(steering,"Steering",root.transform.TransformPoint(pivot),root.transform.rotation);
                        renderer.transform.SetParent(parent,true);
                    }
                    else if (group=="GaugeSpeed" || group=="GaugeRpm")
                    {
                        var gauge=FindGauge(root,group=="GaugeSpeed");
                        if (gauge==null) throw new InvalidOperationException("Missing original NWH dashboard gauge "+group);
                        pivot=new Vector3(group=="GaugeSpeed"?-.72f:-.44f,1.99f,1.144f);
                        gauge.transform.position=root.transform.TransformPoint(pivot);
                        gauge.transform.rotation=root.transform.rotation;
                        var needle=gauge.transform.Find("Needle");
                        var parent=Mount(needle,group,root.transform.TransformPoint(pivot),root.transform.rotation);
                        renderer.transform.SetParent(parent,true);
                    }
                    else
                    {
                        var parent=Mount(target,group+"_LOD"+level,root.transform.position,orientation);
                        renderer.transform.SetParent(parent,true);
                    }
                    renderer.gameObject.name=Prefix+renderer.gameObject.name;
                    renderer.gameObject.SetActive(active);
                    rendered.Add(renderer);
                    if (level==0 && group.StartsWith("Lamp",StringComparison.Ordinal))
                    {
                        if (!lamps.TryGetValue(group,out var values)) lamps[group]=values=new List<MeshRenderer>();
                        values.Add(renderer);
                    }
                    if (level==0 && (group=="MirrorLeft" || group=="MirrorRight") && mirrors!=null)
                    {
                        var old=Find(root.transform,group=="MirrorLeft"?"MirrorGlassL":"MirrorGlassR");
                        if (old != null && old.TryGetComponent(out Renderer glass)) renderer.sharedMaterials=glass.sharedMaterials;
                        if(group=="MirrorLeft") leftSurface=renderer; else rightSurface=renderer;
                    }
                }
                levels.Add(rendered.ToArray());
                }
                finally
                {
                    instance.SetActive(false); instance.transform.SetParent(null,true);
                    if (Application.isPlaying) UnityEngine.Object.Destroy(instance); else UnityEngine.Object.DestroyImmediate(instance);
                }
            }
            var lod=root.GetComponent<LODGroup>();
            if(lod==null) lod=root.AddComponent<LODGroup>();
            lod.enabled=true;
            lod.SetLODs(new[]{new UnityEngine.LOD(.13f,levels[0]),new UnityEngine.LOD(.045f,levels[1]),new UnityEngine.LOD(.009f,levels[2])});
            lod.RecalculateBounds();
            var lightGroups=vehicle.effectsManager.lightsManager;
            foreach(var group in new[]{lightGroups.lowBeamLights,lightGroups.highBeamLights})
                if(group?.lightSources!=null) foreach(var source in group.lightSources)
                    if(source?.light!=null)
                    {
                        var point=root.transform.InverseTransformPoint(source.light.transform.position);
                        source.light.transform.position=root.transform.TransformPoint(new Vector3(Mathf.Sign(point.x)*model.headlampOffset.x,model.headlampOffset.y,model.headlampOffset.z));
                    }
            Bind(lamps,"LampHeadlamp",lightGroups.lowBeamLights,Color.white*2);
            Bind(lamps,"LampHighBeam",lightGroups.highBeamLights,Color.white*3);
            Bind(lamps,"LampMarker",lightGroups.tailLights,new Color(1,.4f,.02f)*2);
            Bind(lamps,"LampBrake",lightGroups.brakeLights,Color.red*3);
            Bind(lamps,"LampTurnLeft",lightGroups.leftBlinkers,new Color(1,.3f,0)*3);
            Bind(lamps,"LampTurnRight",lightGroups.rightBlinkers,new Color(1,.3f,0)*3);
            BindBeams(root,model,lightGroups.lowBeamLights,false);
            BindBeams(root,model,lightGroups.highBeamLights,true);
            if(mirrors!=null && leftSurface!=null && rightSurface!=null)
            {
                mirrors.RebindSurfaces(leftSurface,rightSurface);
                if(mirrors.LeftMirrorCamera!=null) mirrors.LeftMirrorCamera.transform.position=root.transform.TransformPoint(new Vector3(-1.49f,2.58f,1.30f));
                if(mirrors.RightMirrorCamera!=null) mirrors.RightMirrorCamera.transform.position=root.transform.TransformPoint(new Vector3(1.49f,2.58f,1.30f));
            }
            if (model.hasSleeper)
            {
                var lamp=Mount(cab,"SleeperLight",root.transform.TransformPoint(model.sleeperLamp),root.transform.rotation).gameObject.AddComponent<Light>();
                lamp.type=LightType.Point; lamp.color=new Color(1,.80f,.60f); lamp.intensity=.9f; lamp.range=3; lamp.shadows=LightShadows.None;
            }
            if (Application.isPlaying && vehicle.IsInitialized) vehicle.wheelbase=vehicle.steering.CalculateWheelbase();
            Physics.SyncTransforms();
        }
        private static AnalogGauge FindGauge(GameObject root,bool speed)
        {
            foreach(var gauge in root.GetComponentsInChildren<AnalogGauge>(true))
                if (gauge.name.IndexOf(speed?"Speed":"RPM",StringComparison.OrdinalIgnoreCase)>=0 ||
                    !speed && gauge.name.IndexOf("Tach",StringComparison.OrdinalIgnoreCase)>=0) return gauge;
            var gauges=root.GetComponentsInChildren<AnalogGauge>(true);
            return gauges.Length>=2 ? gauges[speed?0:1] : null;
        }
        private static void BindBeams(GameObject root,TruckTaxiTruckModel model,VehicleLight group,bool high)
        {
            if(group==null) return;
            for(int side=-1;side<=1;side+=2)
            {
                var point=model.headlampOffset;point.x*=side;
                var light=Mount(root.transform,(high?"High Beam ":"Low Beam ")+side,
                    root.transform.TransformPoint(point),root.transform.rotation*Quaternion.Euler(high?2:5,side*2,0)).gameObject.AddComponent<Light>();
                light.type=LightType.Spot;light.color=new Color(1,.95f,.85f);light.intensity=high?5:3.5f;
                light.range=high?110:65;light.spotAngle=high?38:65;light.innerSpotAngle=high?22:40;
                light.shadows=LightShadows.None;light.enabled=false;
                var source=new LightSource{type=LightSource.LightType.Light,light=light};group.lightSources.Add(source);
                if(Application.isPlaying && group.On) source.TurnOn();
            }
        }
        private static void Bind(Dictionary<string,List<MeshRenderer>> all,string key,VehicleLight group,Color color)
        {
            if (group==null || !all.TryGetValue(key,out var values)) return;
            foreach(var renderer in values)
            {
                if(renderer==null || !renderer.enabled || !renderer.gameObject.activeSelf) continue;
                if(Application.isPlaying)
                {
                    var resources=renderer.gameObject.AddComponent<TruckTaxiRigLampMaterials>();
                    resources.Capture(renderer);
                }
                var source=new LightSource{type=LightSource.LightType.Mesh,meshRenderer=renderer,rendererMaterialIndex=0,emissionColor=color};
                group.lightSources.Add(source);
                if (Application.isPlaying && group.On) source.TurnOn();
            }
        }
    }

    // Runtime-only ownership of the material instances requested by NWH's mesh-light API.
    internal sealed class TruckTaxiRigLampMaterials : MonoBehaviour
    {
        private Material[] owned=Array.Empty<Material>();
        public void Capture(MeshRenderer renderer)
        {
            var shared=renderer.sharedMaterials;
            var instances=renderer.materials;
            var created=new List<Material>();
            foreach(var material in instances)
                if(material!=null && Array.IndexOf(shared,material)<0 && !created.Contains(material)) created.Add(material);
            owned=created.ToArray();
        }
        public void Release()
        {
            foreach(var material in owned)
                if(material!=null)
                { if(Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
            owned=Array.Empty<Material>();
        }
        private void OnDestroy() => Release();
    }
}
