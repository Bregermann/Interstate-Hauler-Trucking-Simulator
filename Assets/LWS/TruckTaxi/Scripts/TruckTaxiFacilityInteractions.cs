using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiProductShelf : TruckTaxiWorldInteractable
    {
        public TruckTaxiStorePoint store;
        public TruckTaxiNeedsItem item;
        public override string Prompt => "ADD " + (TruckTaxiNeedsItems.Find(item)?.Name ?? "ITEM") + " TO BASKET";
        public override bool CanUse(TruckTaxiWorldLifeController actor) => base.CanUse(actor) && store != null && store.Sells(item);
        public override void Use(TruckTaxiWorldLifeController actor) => actor.AddToBasket(store, item);
    }

    public sealed class TruckTaxiCheckoutRegister : TruckTaxiWorldInteractable
    {
        public override string Prompt => "CHECK OUT SHOPPING BASKET";
        public override void Use(TruckTaxiWorldLifeController actor) => actor.Checkout();
    }

    public sealed class TruckTaxiDieselPump : TruckTaxiWorldInteractable
    {
        public TruckTaxiGasStationPoint station;
        public override string Prompt => station == null ? "DIESEL PUMP UNAVAILABLE" :
            "TAKE / RETURN DIESEL NOZZLE  " + TruckTaxiHud.Money(TruckTaxiUsUnits.CentsPerGallon(station.centsPerLiter)) + "/GAL";
        public override bool CanUse(TruckTaxiWorldLifeController actor) => base.CanUse(actor) && station != null;
        public override void Use(TruckTaxiWorldLifeController actor) => actor.ToggleNozzle(station);
    }

    public sealed class TruckTaxiFuelFiller : TruckTaxiWorldInteractable
    {
        private Renderer[] renderers;
        private Material[][] originals, highlights;
        public bool Highlighted { get; private set; }
        public void SetHighlighted(bool value)
        {
            if (Highlighted == value) return;
            Highlighted = value;
            if (value)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
                originals = new Material[renderers.Length][]; highlights = new Material[renderers.Length][];
                for (int i = 0; i < renderers.Length; i++)
                {
                    originals[i] = renderers[i].sharedMaterials;
                    highlights[i] = new Material[originals[i].Length];
                    for (int m = 0; m < originals[i].Length; m++)
                    {
                        if (originals[i][m] == null) continue;
                        var material = new Material(originals[i][m]) { name = "Carried nozzle filler highlight", hideFlags = HideFlags.DontSave };
                        material.color = new Color(.15f, 1f, .55f);
                        if (material.HasProperty("_EmissionColor"))
                        { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(.15f, 1f, .55f) * 3f); }
                        highlights[i][m] = material;
                    }
                    renderers[i].sharedMaterials = highlights[i];
                }
            }
            else if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null) renderers[i].sharedMaterials = originals[i];
                    foreach (var material in highlights[i])
                        if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
                }
                renderers = null; originals = highlights = null;
            }
        }
        protected override void OnDisable() { SetHighlighted(false); base.OnDisable(); }
        public override bool HoldToUse => true;
        public override string Prompt => "HOLD TO FUEL TRUCK";
        public override bool CanUse(TruckTaxiWorldLifeController actor) => base.CanUse(actor) && actor.ActiveNozzleStation != null;
        public override void Use(TruckTaxiWorldLifeController actor) { }
        public override void Hold(TruckTaxiWorldLifeController actor, float seconds) => actor.PumpFromNozzle(seconds);
    }

    public sealed class TruckTaxiDinerSeat : TruckTaxiWorldInteractable
    {
        public override string Prompt => "SIT AT DINER COUNTER";
        public override void Use(TruckTaxiWorldLifeController actor) => actor.OpenDiner(transform);
    }

    public sealed class TruckTaxiMotelDesk : TruckTaxiWorldInteractable
    {
        public TruckTaxiServicePoint service;
        public override string Prompt => "CHECK IN - RENT ROOM";
        public override void Use(TruckTaxiWorldLifeController actor) => actor.CheckInMotel(service);
    }

    public sealed class TruckTaxiMotelBed : TruckTaxiWorldInteractable
    {
        public TruckTaxiServicePoint service;
        public override string Prompt => "SLEEP 8 HOURS (MOTEL RATE)";
        public override void Use(TruckTaxiWorldLifeController actor) => actor.SleepAtMotel(service);
    }

    public sealed class TruckTaxiCabSleeper : TruckTaxiWorldInteractable
    {
        public override string Prompt => "REST IN THE CAB";
        public override void Use(TruckTaxiWorldLifeController actor) => actor.OpenCabSleep();
    }

    public sealed class TruckTaxiRestroomFixture : TruckTaxiWorldInteractable
    {
        public bool bowel;
        public override string Prompt => bowel ? "USE TOILET (BOWEL TIMING)" : "USE URINAL (BLADDER TIMING)";
        public override void Use(TruckTaxiWorldLifeController actor) => actor.StartRestroom(StableId, bowel);
    }
}
