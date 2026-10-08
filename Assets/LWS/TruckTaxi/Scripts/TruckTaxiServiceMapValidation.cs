using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public static class TruckTaxiServiceMapValidation
    {
        public static List<string> Validate(IEnumerable<TruckTaxiServicePoint> services,IEnumerable<TruckTaxiMapMarker> markers,
            IReadOnlyDictionary<string,TruckTaxiServiceRings.Ring> rings=null,Vector3? player=null,float ringRange=350,
            bool includeInactiveServices=false)
        {
            var findings=new List<string>();
            var byId=new Dictionary<string,TruckTaxiMapMarker>();
            foreach(var marker in markers)
            {
                if(marker==null) continue;
                if(string.IsNullOrWhiteSpace(marker.stableId))
                { findings.Add("Map marker has no stable ID: "+TruckTaxiWorldColliderReview.Identity(marker.transform)); continue; }
                if(byId.TryGetValue(marker.stableId,out var previousMarker)) findings.Add("Duplicate map marker: "+marker.stableId+
                    " :: "+TruckTaxiWorldColliderReview.Identity(previousMarker.transform)+" <-> "+TruckTaxiWorldColliderReview.Identity(marker.transform));
                else byId.Add(marker.stableId,marker);
                var service=marker.service!=null ? marker.service : TruckTaxiServicePresentation.Find(marker.stableId);
                if(!TruckTaxiServicePresentation.Matches(service,marker.markerType))
                    findings.Add(marker.markerType+" icon has no matching usable capability: "+marker.stableId);
            }
            var serviceIds=new Dictionary<string,TruckTaxiServicePoint>();
            foreach(var service in services)
            {
                if(service==null || !includeInactiveServices && !service.isActiveAndEnabled) continue;
                string id=service.stableId ?? "";
                if(string.IsNullOrWhiteSpace(id)) { findings.Add("Service has no stable ID: "+TruckTaxiWorldColliderReview.Identity(service.transform)); continue; }
                if(serviceIds.TryGetValue(id,out var previousService)) findings.Add("Duplicate service ID: "+id+
                    " :: "+TruckTaxiWorldColliderReview.Identity(previousService.transform)+" <-> "+TruckTaxiWorldColliderReview.Identity(service.transform));
                else serviceIds.Add(id,service);
                if(!service.isActiveAndEnabled) continue;
                if((service.capabilities & TruckTaxiServicePresentation.Advertised)==0) continue;
                if(!float.IsFinite(service.InteractionRadius) || service.InteractionRadius<=0) findings.Add("Invalid service radius: "+id);
                if(!byId.TryGetValue(id,out var marker) || !marker.isActiveAndEnabled || marker.state==TruckTaxiMapMarkerState.Hidden)
                    findings.Add("Real service has no visible map marker: "+id);
                else
                {
                    if(marker.markerType!=TruckTaxiServicePresentation.MarkerType(service.capabilities))
                        findings.Add("Map marker omits/misrepresents capabilities: "+id);
                    if(Vector3.Distance(marker.Position,service.Position)>.25f) findings.Add("Map marker is not at interaction bay: "+id);
                }
                var store=service.GetComponent<TruckTaxiStorePoint>();
                if(service.Supports(TruckTaxiServiceCapability.Store) && store==null)
                    findings.Add("Store capability has no actual Store point: "+id);
                var gas=service.GetComponent<TruckTaxiGasStationPoint>();
                if(service.Supports(TruckTaxiServiceCapability.Refuel) && gas==null)
                    findings.Add("Gas capability has no refuel interaction: "+id);
                if(gas!=null && (Vector3.Distance(gas.Position,service.Position)>.25f ||
                    Mathf.Abs(gas.stoppingRadius-service.InteractionRadius)>.1f))
                    findings.Add("Refuel interaction and service bay/radius differ: "+id);
                if(rings==null || player.HasValue && (player.Value-service.Position).sqrMagnitude>ringRange*ringRange) continue;
                if(!rings.TryGetValue(id,out var ring) || ring.Line==null || !ring.Line.enabled || !ring.Line.gameObject.activeInHierarchy)
                { findings.Add("Real nearby service has no world ring: "+id); continue; }
                if(Vector3.Distance(ring.Center,service.Position)>.25f) findings.Add("Ring center not aligned: "+id);
                if(Mathf.Abs(ring.Radius-service.InteractionRadius)>.1f) findings.Add("Ring radius inconsistent: "+id);
                for(int i=0;i<ring.Line.positionCount;i++)
                {
                    var vertex=ring.Line.useWorldSpace ? ring.Line.GetPosition(i) : ring.Line.transform.TransformPoint(ring.Line.GetPosition(i));
                    if(Mathf.Abs(Vector3.ProjectOnPlane(vertex-service.Position,Vector3.up).magnitude-service.InteractionRadius)>.15f)
                    { findings.Add("Rendered ring geometry differs from interaction radius: "+id); break; }
                }
            }
            return findings;
        }

        public static List<string> ValidateAuthoringData(IEnumerable<TruckTaxiServicePoint> services,
            IEnumerable<TruckTaxiWorldInteractable> interactions)
        {
            var findings = new List<string>();
            foreach (var service in services)
            {
                if (service == null) continue;
                string identity = TruckTaxiWorldColliderReview.Identity(service.transform);
                if (service.capabilities == TruckTaxiServiceCapability.None)
                    findings.Add("Service has no capabilities: " + identity);
                if (!float.IsFinite(service.radius) || service.radius <= 0 ||
                    !float.IsFinite(service.verticalTolerance) || service.verticalTolerance <= 0)
                    findings.Add("Invalid authored service radius/vertical tolerance: " + identity);
                if (service.location != null && service.location.truckStopPoint == null)
                    findings.Add("Service location has no explicit parking bay: " + identity);
                if (!Finite(service.Position)) findings.Add("Service bay position is not finite: " + identity);
                if ((service.capabilities & TruckTaxiServiceCapability.Refuel) != 0 && service.GetComponent<TruckTaxiGasStationPoint>() == null)
                    findings.Add("Authored refuel capability lacks gas station data: " + identity);
                if ((service.capabilities & TruckTaxiServiceCapability.Store) != 0 && service.GetComponent<TruckTaxiStorePoint>() == null)
                    findings.Add("Authored store capability lacks store data: " + identity);
            }
            var ids = new Dictionary<string, TruckTaxiWorldInteractable>(System.StringComparer.Ordinal);
            foreach (var interaction in interactions)
            {
                if (interaction == null) continue;
                string identity = TruckTaxiWorldColliderReview.Identity(interaction.transform);
                string kind = interaction is TruckTaxiRestroomFixture ? "Toilet/restroom" : "Interactable";
                if (string.IsNullOrWhiteSpace(interaction.StableId))
                { findings.Add(kind + " has no stable ID: " + identity); continue; }
                if (ids.TryGetValue(interaction.StableId, out var previous))
                    findings.Add("Duplicate interaction ID: " + interaction.StableId + " :: " +
                        TruckTaxiWorldColliderReview.Identity(previous.transform) + " <-> " + identity);
                else ids.Add(interaction.StableId, interaction);
            }
            return findings;
        }
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
