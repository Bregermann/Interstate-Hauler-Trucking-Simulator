using System;
using UnityEngine;

namespace LWS.TruckSports
{
    // Pure display/resolution data. Evaluating a detached draft never creates
    // a model, modifies a profile or initializes a gameplay actor.
    public sealed class SportsTruckAppearanceCompatibilityReport
    {
        public bool canApply;
        public string requestedBodyId="",effectiveBodyId="",notice="";
        public string[] unavailableSlots=Array.Empty<string>();
        internal SportsTruckAppearanceCatalog.Body body;
        internal SportsTruckAppearanceCompatibility fit;
    }
}
