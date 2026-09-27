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
        private bool active;

        public void Apply(int variant)
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
            sign.SetActive(true);
            driver.SetActive(true);
            active = true;
        }

        public void ResetForPool()
        {
            if (!active) return;
            for (int i = 0; i < bodyRenderers.Length; i++)
                if (bodyRenderers[i] != null) bodyRenderers[i].SetPropertyBlock(originalBlocks[i]);
            if (sign != null) sign.SetActive(false);
            if (driver != null) driver.SetActive(false);
            active = false;
        }

        private static Material NewMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader) { color = color, hideFlags = HideFlags.DontSave };
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
