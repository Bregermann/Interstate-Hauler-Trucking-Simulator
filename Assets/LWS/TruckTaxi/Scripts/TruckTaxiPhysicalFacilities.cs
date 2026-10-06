using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Validation art around authored service points. Replace the geometry later; retain the interactables and IDs.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiPhysicalFacilities : MonoBehaviour
    {
        private readonly Dictionary<string, GameObject> roots = new Dictionary<string, GameObject>();
        private readonly List<Material> materials = new List<Material>();
        private readonly HashSet<string> toiletIds = new HashSet<string>(System.StringComparer.Ordinal);
        private TruckTaxiBootstrap host;
        private Material wall, floor, accent, fixture, glass;
        public int BuiltCount => roots.Count;
        public int TotalToilets => toiletIds.Count;

        public void Initialize(TruckTaxiBootstrap owner)
        {
            if (host != null) return;
            host = owner;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            wall = MaterialFor(shader, new Color(.55f, .61f, .64f));
            floor = MaterialFor(shader, new Color(.25f, .29f, .31f));
            accent = MaterialFor(shader, new Color(.85f, .24f, .16f));
            fixture = MaterialFor(shader, new Color(.85f, .86f, .81f));
            glass = MaterialFor(shader, new Color(.21f, .47f, .55f));
            if (owner.Player.GetComponent<TruckTaxiCabSleeper>() == null)
            {
                var sleeper = owner.Player.gameObject.AddComponent<TruckTaxiCabSleeper>();
                sleeper.SetStableId("taxi.truck.cab-sleeper"); sleeper.interactionRange = 4f;
            }
            if (owner.Player.GetComponentInChildren<TruckTaxiFuelFiller>(true) == null)
            {
                var filler = Part(owner.Player.transform, "Diesel filler - hold E with nozzle",
                    new Vector3(-1.45f, 1.18f, -.6f), new Vector3(.22f, .28f, .3f), accent);
                filler.GetComponent<Collider>().isTrigger = true;
                var interaction = filler.AddComponent<TruckTaxiFuelFiller>();
                interaction.SetStableId("taxi.truck.diesel-filler"); interaction.interactionRange = 2.5f;
            }
            Refresh();
        }
        private Material MaterialFor(Shader shader, Color color)
        {
            if (shader == null) return null;
            var material = new Material(shader); material.color = color; materials.Add(material); return material;
        }
        public void Refresh()
        {
            if (host == null) return;
            TruckTaxiServicePoint.BindExistingPoints(host.gameObject.scene);
            foreach (var point in TruckTaxiServicePoint.Points)
            {
                if (point == null || point.gameObject.scene != host.gameObject.scene ||
                    string.IsNullOrWhiteSpace(point.stableId) || !point.generateValidationFacility) continue;
                if (roots.TryGetValue(point.stableId, out var existing) && existing != null) continue;
                bool store = point.Supports(TruckTaxiServiceCapability.Store);
                bool diner = point.Supports(TruckTaxiServiceCapability.Food) && point.location != null &&
                    point.location.locationType == TaxiLocationType.Restaurant;
                bool motel = point.Supports(TruckTaxiServiceCapability.Sleep);
                bool gas = point.Supports(TruckTaxiServiceCapability.Refuel);
                bool restroom = point.Supports(TruckTaxiServiceCapability.Restroom);
                if (!store && !diner && !motel && !gas && !restroom) continue;
                var root = new GameObject("TT Validation Facility - " + point.displayName);
                root.transform.SetParent(point.transform, true);
                roots[point.stableId] = root;
                if (gas) BuildPump(root.transform, point);
                if (store || diner || motel || restroom) BuildInterior(root.transform, point, store, diner, motel, restroom);
                if (gas || store) BuildPortaPotty(root.transform, point);
            }
        }
        private void BuildPump(Transform root, TruckTaxiServicePoint point)
        {
            var station = point.GetComponent<TruckTaxiGasStationPoint>();
            if (station == null) return;
            Vector3 at = point.Position + point.transform.right * 4f;
            var go = Block(root, "Diesel pump - hold E", at + Vector3.up * .9f, new Vector3(.9f, 1.8f, .65f), accent);
            var interaction = go.AddComponent<TruckTaxiDieselPump>(); interaction.station = station;
            interaction.SetStableId(point.stableId + ".pump"); interaction.interactionRange = 3f;
            Block(root, "Pump screen", at + point.transform.forward * .34f + Vector3.up * 1.35f,
                new Vector3(.64f, .35f, .04f), glass);
        }
        private void BuildInterior(Transform root, TruckTaxiServicePoint point, bool store, bool diner, bool motel, bool restroom)
        {
            Vector3 center = point.Position + point.transform.right * 17f;
            center.y = GroundHeight(center, point.Position.y);
            var room = new GameObject("Enterable " + (motel ? "motel" : diner ? "diner" : store ? "store" : "restroom"));
            room.transform.SetParent(root, false);
            room.transform.SetPositionAndRotation(center, Quaternion.LookRotation(-point.transform.right, Vector3.up));
            Part(room.transform, "Floor", new Vector3(0, .08f, 0), new Vector3(11, .16f, 9), floor);
            Part(room.transform, "Back wall", new Vector3(0, 1.65f, -4.45f), new Vector3(11, 3.3f, .24f), wall);
            Part(room.transform, "Left wall", new Vector3(-5.4f, 1.65f, 0), new Vector3(.24f, 3.3f, 9), wall);
            Part(room.transform, "Right wall", new Vector3(5.4f, 1.65f, 0), new Vector3(.24f, 3.3f, 9), wall);
            Part(room.transform, "Front left", new Vector3(-3.75f, 1.65f, 4.45f), new Vector3(3.5f, 3.3f, .24f), wall);
            Part(room.transform, "Front right", new Vector3(3.75f, 1.65f, 4.45f), new Vector3(3.5f, 3.3f, .24f), wall);
            Part(room.transform, "Roof", new Vector3(0, 3.32f, 0), new Vector3(11, .2f, 9), wall);
            var light = new GameObject("Interior light").AddComponent<Light>();
            light.transform.SetParent(room.transform, false); light.transform.localPosition = new Vector3(0, 2.9f, 0);
            light.type = LightType.Point; light.range = 12; light.intensity = 2.5f;
            if (store) BuildStore(room.transform, point);
            if (diner)
            {
                var counter = Part(room.transform, "Diner service counter", new Vector3(0, .65f, -1f), new Vector3(3.2f, 1.2f, .9f), accent);
                counter.AddComponent<TruckTaxiDinerSeat>().SetStableId(point.stableId + ".diner");
                Part(room.transform, "Diner booth", new Vector3(3f, .4f, 1f), new Vector3(2, .8f, 1.6f), fixture);
            }
            if (motel)
            {
                var desk = Part(room.transform, "Motel desk", new Vector3(-2.8f, .6f, 1.3f), new Vector3(1.8f, 1.2f, .8f), accent);
                var checkIn = desk.AddComponent<TruckTaxiMotelDesk>(); checkIn.service = point;
                checkIn.SetStableId(point.stableId + ".desk");
                var bed = Part(room.transform, "Motel bed", new Vector3(1.4f, .42f, -1.3f), new Vector3(2.3f, .7f, 3.1f), fixture);
                var sleep = bed.AddComponent<TruckTaxiMotelBed>(); sleep.service = point;
                sleep.SetStableId(point.stableId + ".bed");
            }
            if (restroom) BuildRestroom(room.transform, point);
        }
        private void BuildStore(Transform room, TruckTaxiServicePoint point)
        {
            var store = point.GetComponent<TruckTaxiStorePoint>();
            if (store == null) return;
            TruckTaxiNeedsItem[] products = { TruckTaxiNeedsItem.WaterBottle, TruckTaxiNeedsItem.Snack,
                TruckTaxiNeedsItem.Soda, TruckTaxiNeedsItem.Sandwich, TruckTaxiNeedsItem.EmptyPissJug, TruckTaxiNeedsItem.EnergyDrink };
            for (int i = 0; i < products.Length; i++)
            {
                var definition = TruckTaxiNeedsItems.Find(products[i]);
                if (definition == null || !store.Sells(products[i])) continue;
                int column = i % 3, row = i / 3;
                var product = Part(room, "Shelf - " + definition.Name, new Vector3(-3.2f + column * 2.9f, 1f + row * .65f, -2.2f),
                    new Vector3(.8f, .55f, .7f), row == 0 ? fixture : accent);
                var shelf = product.AddComponent<TruckTaxiProductShelf>(); shelf.store = store; shelf.item = products[i];
                shelf.SetStableId(point.stableId + ".shelf." + definition.StableId);
            }
            var register = Part(room, "Checkout register", new Vector3(3.1f, .8f, 2.2f), new Vector3(1.5f, 1.4f, .9f), accent);
            register.AddComponent<TruckTaxiCheckoutRegister>().SetStableId(point.stableId + ".checkout");
        }
        private void BuildRestroom(Transform room, TruckTaxiServicePoint point)
        {
            var toilet = Part(room, "Toilet", new Vector3(3.6f, .48f, -3f), new Vector3(.8f, .9f, 1), fixture);
            var bowel = toilet.AddComponent<TruckTaxiRestroomFixture>(); bowel.bowel = true;
            bowel.SetStableId(point.stableId + ".toilet");
            toiletIds.Add(point.stableId + ".toilet");
            var urinal = Part(room, "Urinal", new Vector3(4.85f, 1.15f, -1.6f), new Vector3(.4f, .75f, .6f), fixture);
            var bladder = urinal.AddComponent<TruckTaxiRestroomFixture>(); bladder.bowel = false;
            bladder.SetStableId(point.stableId + ".urinal");
        }
        private void BuildPortaPotty(Transform root, TruckTaxiServicePoint point)
        {
            Vector3 center = point.Position + point.transform.right * 8f + point.transform.forward * 5f;
            center.y = GroundHeight(center, point.Position.y);
            var stall = new GameObject("Portable restroom - validation art");
            stall.transform.SetParent(root, false);
            stall.transform.SetPositionAndRotation(center, Quaternion.LookRotation(-point.transform.right, Vector3.up));
            Part(stall.transform, "Porta floor", new Vector3(0, .08f, 0), new Vector3(1.8f, .16f, 2.1f), floor);
            Part(stall.transform, "Porta back", new Vector3(0, 1.2f, -1f), new Vector3(1.8f, 2.4f, .12f), glass);
            Part(stall.transform, "Porta left", new Vector3(-.85f, 1.2f, 0), new Vector3(.12f, 2.4f, 2.1f), glass);
            Part(stall.transform, "Porta right", new Vector3(.85f, 1.2f, 0), new Vector3(.12f, 2.4f, 2.1f), glass);
            Part(stall.transform, "Porta roof", new Vector3(0, 2.4f, 0), new Vector3(1.8f, .12f, 2.1f), glass);
            var toilet = Part(stall.transform, "Portable toilet", new Vector3(0, .46f, -.52f), new Vector3(.7f, .75f, .7f), fixture);
            var interaction = toilet.AddComponent<TruckTaxiRestroomFixture>(); interaction.bowel = true;
            interaction.SetStableId(point.stableId + ".porta");
            toiletIds.Add(point.stableId + ".porta");
        }
        private GameObject Part(Transform parent, string label, Vector3 localPosition, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = label;
            go.transform.SetParent(parent, false); go.transform.localPosition = localPosition; go.transform.localScale = scale;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
        private GameObject Block(Transform parent, string label, Vector3 position, Vector3 scale, Material material)
        {
            var go = Part(parent, label, Vector3.zero, scale, material); go.transform.position = position; return go;
        }
        private static float GroundHeight(Vector3 position, float serviceHeight)
        {
            float selected = float.NegativeInfinity;
            foreach (var hit in Physics.RaycastAll(position + Vector3.up * 10f, Vector3.down, 35f,
                         ~0, QueryTriggerInteraction.Ignore))
                if (hit.normal.y >= .7f && hit.point.y <= serviceHeight + .75f &&
                    hit.point.y >= serviceHeight - 3f && hit.point.y > selected)
                    selected = hit.point.y;
            return float.IsNegativeInfinity(selected) ? serviceHeight : selected;
        }
        private void OnDestroy()
        { foreach (var material in materials) if (material != null) Destroy(material); }
    }
}
