using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Presentation-only lease: WorldLife retains control mode, player identity and walking.
    public sealed class TruckTaxiDriverVisualVisibility : IDisposable
    {
        private Transform visual;
        private readonly Dictionary<Renderer, bool> previous = new Dictionary<Renderer, bool>();
        public bool Hidden => visual != null;

        public void SetOutsideCab(Transform driverVisual, bool outside)
        {
            if (!outside || visual != driverVisual) Dispose();
            if (!outside || driverVisual == null || visual == driverVisual) return;
            visual = driverVisual;
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                previous[renderer] = renderer.forceRenderingOff;
                renderer.forceRenderingOff = true;
            }
        }

        public void Dispose()
        {
            foreach (var entry in previous)
                if (entry.Key != null) entry.Key.forceRenderingOff = entry.Value;
            previous.Clear();
            visual = null;
        }
    }
}
