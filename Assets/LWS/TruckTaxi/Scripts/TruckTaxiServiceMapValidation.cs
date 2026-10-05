using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public static class TruckTaxiServiceMapValidation
    {
        public static List<string> Validate(IEnumerable<TruckTaxiServicePoint> services,IEnumerable<TruckTaxiMapMarker> markers,
            IReadOnlyDictionary<string,TruckTaxiServiceRings.Ring> rings=null,Vector3? player=null,float ringRange=350)
        {
            var findings=new List<string>();
            var byId=new Dictionary<string,TruckTaxiMapMarker>();
            foreach(var marker in markers)
            {
                if(marker==null || string.IsNullOrWhiteSpace(marker.stableId)) continue;
                if(byId.ContainsKey(marker.stableId)) findings.Add("Duplicate map marker: "+marker.stableId);
                else byId.Add(marker.stableId,marker);
                var service=marker.service!=null ? marker.service : TruckTaxiServicePresentation.Find(marker.stableId);
                if(!TruckTaxiServicePresentation.Matches(service,marker.markerType))
                    findings.Add(marker.markerType+" icon has no matching usable capability: "+marker.stableId);
            }
            var serviceIds=new HashSet<string>();
            foreach(var service in services)
            {
                if(service==null || !service.isActiveAndEnabled) continue;
                string id=service.stableId ?? "";
                if(string.IsNullOrWhiteSpace(id)) { findings.Add("Service has no stable ID: "+service.name); continue; }
                if(!serviceIds.Add(id)) findings.Add("Duplicate service ID: "+id);
                if((service.capabilities & TruckTaxiServicePresentation.Advertised)==0) continue;
                if(!float.IsFinite(service.InteractionRadius) || service.InteractionRadius<=0) findings.Add("Invalid service radius: "+id);
                if(!byId.TryGetValue(id,out var marker) || marker.state==TruckTaxiMapMarkerState.Hidden)
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
    }
}
