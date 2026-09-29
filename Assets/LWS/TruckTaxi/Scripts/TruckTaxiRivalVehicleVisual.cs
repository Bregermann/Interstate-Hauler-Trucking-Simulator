using UnityEngine;

namespace LWS.TruckTaxi
{
    // A reversible overlay on a pooled UTS vehicle. No vendor renderer or physics asset is modified.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRivalVehicleVisual : MonoBehaviour
    {
        private Renderer[] bodyRenderers;
        private MaterialPropertyBlock[] originalBlocks;
        private GameObject sign;
        private GameObject driver;
        private Material signMaterial;
        private Material driverBody;
        private Material driverClothes;
        private Material driverSkin;
        private MaterialPropertyBlock paint;
        private TextMesh roofLabel, leftDoorLabel, rightDoorLabel;
        private bool active;

        public void Apply(int variant) => Apply(variant, Mathf.Abs(variant % 1000));

        public void Apply(int variant, int fleetNumber)
        {
            paint ??= new MaterialPropertyBlock();
            if (active) ResetForPool();
            if (bodyRenderers == null)
            {
                bodyRenderers = GetComponentsInChildren<Renderer>(true);
                originalBlocks = new MaterialPropertyBlock[bodyRenderers.Length];
                for (int i = 0; i < bodyRenderers.Length; i++)
                {
                    originalBlocks[i] = new MaterialPropertyBlock();
                    bodyRenderers[i].GetPropertyBlock(originalBlocks[i]);
                }
            }
            Color[] colors = {
                new Color(.96f, .72f, .12f), new Color(.13f, .74f, .69f),
                new Color(.91f, .31f, .22f), new Color(.87f, .88f, .91f)
            };
            Color color = colors[Mathf.Abs(variant % colors.Length)];
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                var renderer = bodyRenderers[i];
                if (renderer == null || renderer.name.ToLowerInvariant().Contains("wheel")) continue;
                renderer.GetPropertyBlock(originalBlocks[i]);
                renderer.GetPropertyBlock(paint);
                paint.SetColor("_BaseColor", color);
                paint.SetColor("_Color", color);
                renderer.SetPropertyBlock(paint);
            }
            if (sign == null)
            {
                Bounds bounds = new Bounds(transform.position, Vector3.one * 2);
                bool found = false;
                foreach (var renderer in bodyRenderers)
                    if (renderer != null) { if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds); }
                Vector3 roof = transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y + .12f, bounds.center.z));
                sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sign.name = "Rival Taxi roof sign";
                sign.transform.SetParent(transform, false);
                sign.transform.localPosition = roof;
                sign.transform.localScale = new Vector3(.75f, .25f, .3f);
                var signCollider = sign.GetComponent<Collider>();
                signCollider.enabled = false;
                Destroy(signCollider);
                signMaterial = NewMaterial(new Color(1f, .95f, .72f));
                sign.GetComponent<Renderer>().sharedMaterial = signMaterial;
                roofLabel = AddLabel(sign.transform, "Taxi roof lettering",
                    new Vector3(0, .51f, -.52f), Quaternion.identity, .32f);
                leftDoorLabel = AddLabel(transform, "Taxi left door",
                    new Vector3(-1.3f, 1.9f, .4f),
                    Quaternion.Euler(0, -90, 0), .24f);
                rightDoorLabel = AddLabel(transform, "Taxi right door",
                    new Vector3(1.3f, 1.9f, .4f),
                    Quaternion.Euler(0, 90, 0), .24f);
                driver = new GameObject("Rival Taxi Wobble driver");
                driver.transform.SetParent(transform, false);
                driver.transform.localPosition = roof + new Vector3(-.36f, -.82f, .08f);
                driver.transform.localScale = Vector3.one * .55f;
                driverBody = NewMaterial(new Color(.2f, .38f, .43f));
                driverClothes = NewMaterial(new Color(.17f, .17f, .2f));
                driverSkin = NewMaterial(new Color(.75f, .52f, .37f));
                TruckTaxiWobbleVisual.CreateThemed(driver.transform, new TruckTaxiWobbleVisual.Design {
                    silhouette = TruckTaxiWobbleVisual.Silhouette.Human,
                    headwear = TruckTaxiWobbleVisual.Headwear.Cap,
                    accessory = TruckTaxiWobbleVisual.Accessory.None,
                    height = 1.5f, width = 1, headScale = 1, upperBodyScale = 1,
                    clothedAdult = true, body = driverBody, clothes = driverClothes,
                    accent = signMaterial, skin = driverSkin
                });
                foreach (var collider in driver.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            }
            string[] companies = { "CITY HAUL TAXI", "BIG RIG CAB CO.", "TRUCK TAXI", "NIGHT HAUL", "RAPID RIGS" };
            string company = companies[Mathf.Abs(variant % companies.Length)];
            roofLabel.text = "TAXI";
            leftDoorLabel.text = company + "\n#" + fleetNumber.ToString("000");
            rightDoorLabel.text = leftDoorLabel.text;
            sign.SetActive(true);
            leftDoorLabel.gameObject.SetActive(true);
            rightDoorLabel.gameObject.SetActive(true);
            driver.SetActive(true);
            active = true;
        }

        public void ResetForPool()
        {
            if (!active) return;
            for (int i = 0; i < bodyRenderers.Length; i++)
                if (bodyRenderers[i] != null) bodyRenderers[i].SetPropertyBlock(originalBlocks[i]);
            if (sign != null) sign.SetActive(false);
            if (leftDoorLabel != null) leftDoorLabel.gameObject.SetActive(false);
            if (rightDoorLabel != null) rightDoorLabel.gameObject.SetActive(false);
            if (driver != null) driver.SetActive(false);
            active = false;
        }

        private static Material NewMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader) { color = color, hideFlags = HideFlags.DontSave };
        }

        private static TextMesh AddLabel(Transform parent, string name, Vector3 position, Quaternion rotation, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            var label = go.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = size;
            label.fontSize = 48;
            label.color = Color.black;
            return label;
        }

        private void OnDestroy()
        {
            if (signMaterial != null) Destroy(signMaterial);
            if (driverBody != null) Destroy(driverBody);
            if (driverClothes != null) Destroy(driverClothes);
            if (driverSkin != null) Destroy(driverSkin);
        }
    }
}
