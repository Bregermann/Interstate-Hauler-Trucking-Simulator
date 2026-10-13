using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiGaragePaint : MonoBehaviour
    {
        private sealed class Slot
        {
            public Renderer renderer;
            public int index, property;
            public Color defaultColor;
        }
        private readonly List<Slot> slots = new List<Slot>();
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private TruckTaxiSession session;
        private TruckTaxiCareerCatalog catalog;
        private LwsPlayerTruck truck;
        private bool subscribed;
        public bool Ready => isActiveAndEnabled && slots.Count > 0;
        public Color DefaultColor => slots.Count > 0 ? slots[0].defaultColor : Color.white;

        // Explicit source material identity prevents tinting glass, rubber, occupants, or vendor assets.
        public void Configure(LwsPlayerTruck player, Material[] bodyMaterials, TruckTaxiSession career, TruckTaxiCareerCatalog content)
        {
            if (player == null || player.gameObject != gameObject || bodyMaterials == null || career == null)
                throw new ArgumentException("Paint requires the canonical truck, explicit body materials and Career session.");
            Unsubscribe(); RestoreDefault(); slots.Clear(); truck = player; session = career; catalog = content;
            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || Array.IndexOf(bodyMaterials, material) < 0) continue;
                    string property = material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
                    if (property == null) continue;
                    var original = new MaterialPropertyBlock(); renderer.GetPropertyBlock(original, i);
                    if (original.isEmpty) renderer.GetPropertyBlock(original);
                    int propertyId = Shader.PropertyToID(property);
                    slots.Add(new Slot { renderer = renderer, index = i, property = Shader.PropertyToID(property),
                        defaultColor = original.HasColor(propertyId) ? original.GetColor(propertyId) : material.GetColor(property) });
                }
            }
            Subscribe(); Refresh();
        }
        public Color CareerColor(TruckTaxiOwnedTruck owned)
        {
            if (owned?.hasCustomPaint == true && owned.paint != null && owned.paint.Validate()) return owned.paint.color;
            if (owned?.cosmetics != null)
                foreach (var item in owned.cosmetics)
                {
                    var paint = catalog?.FindCosmetic(item.itemId);
                    if (item.slotId == "paint" && paint?.category == TruckTaxiCosmeticCategory.Paint && paint.HasContent &&
                        TruckTaxiCareerCatalog.Compatible(paint.compatibleTruckIds, owned.truckId)) return paint.paintColor;
                }
            return DefaultColor;
        }
        public void Preview(TruckTaxiGaragePaintData value)
        {
            if (!Ready || value != null && !value.Validate()) return;
            if (value == null) { RestoreDefault(); return; }
            Tint(value.color);
        }
        private void Tint(Color color)
        {
            foreach (var slot in slots)
            {
                if (slot.renderer == null) continue;
                // Preserve other adapters' per-material properties (damage/weather etc.).
                slot.renderer.GetPropertyBlock(block, slot.index);
                if (block.isEmpty) slot.renderer.GetPropertyBlock(block);
                block.SetColor(slot.property, color); slot.renderer.SetPropertyBlock(block, slot.index);
            }
        }
        public void RestoreDefault()
        {
            foreach (var slot in slots)
                if (slot.renderer != null)
                {
                    slot.renderer.GetPropertyBlock(block, slot.index);
                    if (block.isEmpty) slot.renderer.GetPropertyBlock(block);
                    block.SetColor(slot.property, slot.defaultColor);
                    slot.renderer.SetPropertyBlock(block, slot.index);
                }
        }
        public void ApplyCareer(TruckTaxiCareerProfileData profile, TruckTaxiCareerCatalog content)
        {
            if (!Ready) return;
            catalog = content;
            var owned = profile?.trucks?.Find(t => t.truckId == truck.DefinitionId);
            if (profile == null || !profile.Validate(out _) || owned == null || profile.equippedTruckId != truck.DefinitionId)
            { RestoreDefault(); return; }
            if (!owned.hasCustomPaint && !owned.cosmetics.Exists(s => s.slotId == "paint")) RestoreDefault();
            else Tint(CareerColor(owned));
        }
        public void Refresh() => ApplyCareer(session?.CaptureCareerProfile(), catalog);
        private void Subscribe()
        {
            if (!subscribed && isActiveAndEnabled && session != null) { session.CareerEquipmentChanged += Refresh; subscribed = true; }
        }
        private void Unsubscribe()
        { if (subscribed && session != null) session.CareerEquipmentChanged -= Refresh; subscribed = false; }
        private void OnEnable() { Subscribe(); Refresh(); }
        private void OnDisable() { Unsubscribe(); RestoreDefault(); }
        private void OnDestroy() { Unsubscribe(); RestoreDefault(); }
    }
}
