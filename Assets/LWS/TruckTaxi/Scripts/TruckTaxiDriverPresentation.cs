using System.Collections.Generic;
using NWH.Common.Cameras;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiDriverPresentation : MonoBehaviour
    {
        private const int HeadOnlyLayer = 29;
        private readonly Dictionary<Camera,int> cameraMasks = new Dictionary<Camera,int>();
        private readonly List<Material> materials = new List<Material>();
        private GameObject driver;

        public bool Ready => driver != null;

        public void Initialize(Transform truck)
        {
            if(Ready || truck == null) return;
            var mount = truck.Find("Cab") ?? truck;
            driver = new GameObject("Truck Taxi visible driver (Wobble)");
            driver.transform.SetParent(mount,false);
            driver.transform.localPosition = new Vector3(-.48f,.05f,.05f);
            driver.transform.localRotation = Quaternion.identity;
            var scale = mount.lossyScale;
            driver.transform.localScale = new Vector3(.88f/Mathf.Max(Mathf.Abs(scale.x),.0001f),
                .88f/Mathf.Max(Mathf.Abs(scale.y),.0001f),.88f/Mathf.Max(Mathf.Abs(scale.z),.0001f));
            var body = Material(new Color(.2f,.43f,.55f));
            var clothes = Material(new Color(.12f,.15f,.18f));
            var accent = Material(new Color(.87f,.26f,.18f));
            var skin = Material(new Color(.76f,.54f,.38f));
            var visual = TruckTaxiWobbleVisual.CreateThemed(driver.transform,new TruckTaxiWobbleVisual.Design {
                silhouette=TruckTaxiWobbleVisual.Silhouette.Human,
                headwear=TruckTaxiWobbleVisual.Headwear.Cap,
                accessory=TruckTaxiWobbleVisual.Accessory.None,
                height=1.5f,width=1f,headScale=1f,upperBodyScale=1f,clothedAdult=true,
                body=body,clothes=clothes,accent=accent,skin=skin
            });
            // Pose the existing editable Wobble parts; no animation or physics authority is added.
            Pose(visual.transform,"Pants",new Vector3(0,.36f,.16f),new Vector3(80,0,0));
            Pose(visual.transform,"Left foot",new Vector3(-.13f,.13f,.34f),Vector3.zero);
            Pose(visual.transform,"Right foot",new Vector3(.13f,.13f,.34f),Vector3.zero);
            Pose(visual.transform,"Left sleeve",new Vector3(-.28f,.56f,.12f),new Vector3(-50,0,15));
            Pose(visual.transform,"Right sleeve",new Vector3(.28f,.56f,.12f),new Vector3(-50,0,-15));
            Pose(visual.transform,"Left hand",new Vector3(-.25f,.48f,.3f),Vector3.zero);
            Pose(visual.transform,"Right hand",new Vector3(.25f,.48f,.3f),Vector3.zero);
            foreach(var collider in driver.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
            foreach(var part in visual.GetComponentsInChildren<Transform>(true))
                if(part.name.Contains("Head") || part.name.Contains("eye") || part.name.Contains("Cap") ||
                    part.name.Contains("Brim") || part.name.Contains("Hair")) part.gameObject.layer=HeadOnlyLayer;
            foreach(var inside in truck.GetComponentsInChildren<CameraInsideVehicle>(true))
            {
                var camera=inside.GetComponent<Camera>();
                if(camera==null) continue;
                cameraMasks[camera]=camera.cullingMask;
                camera.cullingMask &= ~(1<<HeadOnlyLayer);
            }
        }

        private static void Pose(Transform visual,string name,Vector3 position,Vector3 euler)
        {
            var part=visual.Find(name);
            if(part==null) return;
            part.localPosition=position*1.5f;
            part.localRotation=Quaternion.Euler(euler);
        }

        private Material Material(Color color)
        {
            var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material=new Material(shader) { color=color, hideFlags=HideFlags.DontSave };
            materials.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            foreach(var entry in cameraMasks) if(entry.Key!=null) entry.Key.cullingMask=entry.Value;
            if(driver!=null) Destroy(driver);
            foreach(var material in materials) if(material!=null) Destroy(material);
        }
    }
}
