using System.Collections.Generic;
using NWH.Common.Cameras;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiDriverPresentationState { Driving, MovingToSleeper, SleeperOccupied, ReturningToDriver }

    [DisallowMultipleComponent]
    public sealed class TruckTaxiDriverPresentation : MonoBehaviour
    {
        private const int InteriorHiddenLayer = 29;
        private readonly Dictionary<Camera,int> cameraMasks = new Dictionary<Camera,int>();
        private readonly List<Material> materials = new List<Material>();
        private GameObject driver;
        private GameObject footVisual;
        private TruckTaxiDriverAppearanceData currentAppearance = new TruckTaxiDriverAppearanceData();
        private TruckTaxiCabOccupantRig eventRig;

        public bool Ready => driver != null;
        public Transform Occupant => driver != null ? driver.transform : null;
        public Transform OnFootVisual => footVisual != null ? footVisual.transform : null;
        public bool SupportsChestShape => Ready && SupportsAdultShape(driver.transform.Find(TruckTaxiWobbleVisual.VisualName));
        public TruckTaxiDriverPresentationState State { get; private set; }
        public bool SleeperTransitionValid => eventRig != null && eventRig.isActiveAndEnabled && eventRig.PrivateTransitionValid;

        public bool TryApplyAppearance(TruckTaxiDriverAppearanceData appearance, out string reason)
        {
            reason = "The existing driver visual is not ready.";
            if (!Ready || materials.Count != 4 || materials.Exists(m => m == null)) return false;
            if (appearance == null) { reason = "Missing driver appearance."; return false; }
            if (!appearance.Validate(out reason)) return false;
            if (!SupportsChestShape) { reason = "This driver has no editable clothed adult chest parts."; return false; }
            var visual = driver.transform.Find(TruckTaxiWobbleVisual.VisualName);
            var cap = visual != null ? visual.Find("Cap") : null;
            var brim = visual != null ? visual.Find("Brim") : null;
            if (cap == null || brim == null) { reason = "Existing cap parts are unavailable; no replacement art was generated."; return false; }
            // These four materials belong exclusively to this presentation and are already disposed below.
            materials[0].color = appearance.shirt;
            materials[1].color = appearance.pantsAndCap;
            materials[2].color = appearance.trim;
            materials[3].color = appearance.skin;
            cap.gameObject.SetActive(appearance.capVisible); brim.gameObject.SetActive(appearance.capVisible);
            ApplyVisualAppearance(visual, appearance);
            if (footVisual != null) ApplyVisualAppearance(footVisual.transform, appearance);
            currentAppearance = appearance.Copy();
            reason = null; return true;
        }
        private static bool SupportsAdultShape(Transform visual) => visual != null &&
            visual.Find("Left Clothed chest") != null && visual.Find("Right Clothed chest") != null;
        public static bool ApplyVisualAppearance(Transform visual, TruckTaxiDriverAppearanceData appearance)
        {
            if (!SupportsAdultShape(visual) || appearance == null || !appearance.Validate(out _)) return false;
            float shape = appearance.chestShape;
            foreach (string side in new[] { "Left", "Right" })
            {
                var part = visual.Find(side + " Clothed chest");
                part.localScale = new Vector3(.3f * (1 + .2f * shape), .27f * (1 + .15f * shape), .27f * (1 + .5f * shape));
                part.localPosition = new Vector3(side == "Left" ? -.195f : .195f, .915f, .24f + .035f * shape);
            }
            visual.Find("Cap")?.gameObject.SetActive(appearance.capVisible);
            visual.Find("Brim")?.gameObject.SetActive(appearance.capVisible);
            return true;
        }
        public GameObject CreateAppearanceVisual(Transform parent)
        {
            if (!Ready || parent == null || materials.Count != 4) return null;
            var visual = TruckTaxiWobbleVisual.CreateThemed(parent, new TruckTaxiWobbleVisual.Design {
                silhouette = TruckTaxiWobbleVisual.Silhouette.Human, headwear = TruckTaxiWobbleVisual.Headwear.Cap,
                height = 1.5f, width = 1, headScale = 1, upperBodyScale = 1, clothedAdult = true,
                body = materials[0], clothes = materials[1], accent = materials[2], skin = materials[3]
            });
            ApplyVisualAppearance(visual.transform, currentAppearance);
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            return visual;
        }
        public void SetOnFoot(Transform parent, bool visible)
        {
            if (visible && footVisual == null)
            {
                footVisual = CreateAppearanceVisual(parent);
                if (footVisual != null)
                    foreach (Transform part in footVisual.transform)
                        if (part.name == "Head" || part.name.Contains("eye") || part.name == "Cap" || part.name == "Brim")
                            part.gameObject.layer = InteriorHiddenLayer;
            }
            SetFootVisualVisible(visible);
        }
        public void SetFootVisualVisible(bool visible) { if (footVisual != null) footVisual.SetActive(visible); }

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
            foreach(var part in driver.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer=InteriorHiddenLayer;
            var rig = truck.GetComponent<TruckTaxiCabOccupantRig>();
            if (rig != null && rig.HasSleeperCapability && rig.TrySeat(driver.transform,
                TruckTaxiCabOccupantKind.Driver, TruckTaxiCabAnchorId.DriverSeat,
                TruckTaxiSeatType.StandardPassenger, driver.transform.lossyScale)) return;
            foreach(var inside in truck.GetComponentsInChildren<CameraInsideVehicle>(true))
            {
                if(!inside.isInsideVehicle) continue;
                var camera=inside.GetComponent<Camera>() ?? inside.GetComponentInChildren<Camera>(true);
                if(camera==null || cameraMasks.ContainsKey(camera)) continue;
                cameraMasks[camera]=camera.cullingMask;
                camera.cullingMask &= ~(1<<InteriorHiddenLayer);
            }
        }

        public bool TryBeginSleeperTransition(TruckTaxiCabOccupantRig rig, Transform companion, TruckTaxiSeatType companionType)
        {
            if (!Ready || State != TruckTaxiDriverPresentationState.Driving || rig == null ||
                !rig.TryBeginPrivateTransition(driver.transform, companion, companionType)) return false;
            eventRig = rig;
            State = TruckTaxiDriverPresentationState.MovingToSleeper;
            return true;
        }

        public bool StepSleeperTransition(float seconds)
        {
            if (!SleeperTransitionValid || !eventRig.StepPrivateTransition(seconds)) return false;
            State = TruckTaxiDriverPresentationState.SleeperOccupied;
            return true;
        }

        public void RestoreDriving()
        {
            if (eventRig == null) { State = TruckTaxiDriverPresentationState.Driving; return; }
            State = TruckTaxiDriverPresentationState.ReturningToDriver;
            eventRig.EndPrivateTransition();
            eventRig = null;
            State = TruckTaxiDriverPresentationState.Driving;
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
            RestoreDriving();
            foreach(var entry in cameraMasks) if(entry.Key!=null) entry.Key.cullingMask=entry.Value;
            if(driver!=null) Destroy(driver);
            if(footVisual!=null) Destroy(footVisual);
            foreach(var material in materials) if(material!=null) Destroy(material);
        }

        private void OnDisable() => RestoreDriving();
    }
}
