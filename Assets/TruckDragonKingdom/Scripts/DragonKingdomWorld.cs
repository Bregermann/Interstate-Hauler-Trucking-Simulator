using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    [Serializable]
    public sealed class DragonWorldRegion
    {
        public string id;
        public Transform root;
        public Transform[] truckStarts = Array.Empty<Transform>();
        public Transform[] perches = Array.Empty<Transform>();
        public Transform[] nests = Array.Empty<Transform>();
        public Transform[] treasureCaches = Array.Empty<Transform>();
        public Transform[] ambientRoutes = Array.Empty<Transform>();
    }
    [Serializable]
    public sealed class DragonExtractionSite
    {
        public string id;
        public Collider secureZone;
        public Transform approach;
        public DragonEvacuationVessel vessel;
    }
    public interface IDragonWorldResettable { void ResetDragonMatch(int seed); }

    // One shared scene composition supports all four games. Per-match props,
    // dragons and vessel retain explicit reset ownership; no scene rebuild.
    public sealed class DragonKingdomWorld : MonoBehaviour
    {
        public DragonKingdomDefinition definition;
        public DragonWorldRegion[] regions = Array.Empty<DragonWorldRegion>();
        public DragonExtractionSite[] extractionSites = Array.Empty<DragonExtractionSite>();
        public Transform[] aerialCheckpoints = Array.Empty<Transform>();
        public Transform[] riderStarts = Array.Empty<Transform>();
        public MonoBehaviour[] resettableComponents = Array.Empty<MonoBehaviour>();

        public DragonWorldRegion Region(string id)
        {
            foreach (var region in regions) if (region != null && region.id == id) return region;
            throw new InvalidOperationException("Unbound kingdom region: " + id);
        }
        public DragonExtractionSite Extraction(string id)
        {
            foreach (var site in extractionSites) if (site != null && site.id == id) return site;
            throw new InvalidOperationException("Unbound kingdom extraction: " + id);
        }
        public void ValidateReferences()
        {
            if (definition == null) throw new InvalidOperationException("The shared kingdom definition is required.");
            var issues = new List<string>(); definition.CollectValidationIssues(issues);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var region in regions)
                if (region == null || !ids.Add(region.id) || definition.FindRegion(region.id) == null || region.root == null || region.root.GetComponentInChildren<Renderer>(true) == null)
                    issues.Add("Every kingdom region needs a unique binding and actual environment renderers.");
            if (ids.Count != definition.regions.Length) issues.Add("Every defined region must exist in the shared world.");
            ids.Clear();
            foreach (var site in extractionSites)
                if (site == null || !ids.Add(site.id) || site.secureZone == null || site.approach == null)
                    issues.Add("Each extraction needs a unique ID, physical inner zone and real approach.");
            foreach (var extraction in definition.extractions)
            {
                if (extraction == null || !ids.Contains(extraction.id)) { issues.Add("Missing physical extraction binding."); continue; }
                if (extraction.kind == DragonExtractionKind.Boat && Extraction(extraction.id).vessel == null)
                    issues.Add("Boat extraction requires a real moving evacuation vessel.");
            }
            if (aerialCheckpoints.Length < 8 || riderStarts.Length < 4) issues.Add("The kingdom requires an authored aerial loop and four comparable mounted starts.");
            foreach (var component in resettableComponents)
                if (component == null || !(component is IDragonWorldResettable)) issues.Add("All authored reset bindings need a valid mode-local reset owner.");
            if (issues.Count > 0) throw new InvalidOperationException(string.Join("\n", issues));
        }
        public void ResetWorld(int seed)
        {
            ValidateReferences();
            foreach (var component in resettableComponents) ((IDragonWorldResettable)component).ResetDragonMatch(seed);
        }
    }
}
