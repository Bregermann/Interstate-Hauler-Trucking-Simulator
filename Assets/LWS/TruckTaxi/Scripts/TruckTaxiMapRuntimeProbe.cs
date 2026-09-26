#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NWH.Common.Cameras;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Explicit opt-in fixture. Uses actual Compass per-instance UI state, not just configured sprite fields.
    public static class TruckTaxiMapRuntimeProbe
    {
        private sealed class DiscoverySnapshot
        {
            public string id,key;
            public bool existed,discovered;
            public int value;
        }
        public static IEnumerator Run(TruckTaxiBootstrap host,Action<bool,string> check,Action<string> capture)
        {
            bool ready=host!=null && host.Ready && host.GPS?.MapMarkers?.Registry!=null;
            check(ready,"Map probe has initialized existing GPS and typed registry"); if(!ready) yield break;
            var gps=host.GPS; var map=gps.MapMarkers; var session=host.Session;
            var originalRegistry=map.Registry; var registry=UnityEngine.Object.Instantiate(originalRegistry);
            var body=host.Player.GetComponent<Rigidbody>(); var position=body.position; var rotation=body.rotation;
            var velocity=body.linearVelocity; var angular=body.angularVelocity; bool wasKinematic=body.isKinematic,wasPaused=host.Paused;
            string settings=JsonUtility.ToJson(gps.DisplaySettings);
            var changer=host.Player.GetComponentInChildren<CameraChanger>(true); int cameraIndex=changer!=null ? changer.currentCameraIndex : -1;
            var fixture=new GameObject("Map runtime probe ONLY");
            var forced=new List<TruckTaxiMapMarker>(); var definitions=new List<PassengerRequestDefinition>();
            var discovery=map.Markers.Where(m=>m!=null && (m.markerType==TruckTaxiMapMarkerType.Shortcut || m.markerType==TruckTaxiMapMarkerType.DiscoveredShortcut))
                .Select(m=>new DiscoverySnapshot { id=m.stableId,key=TruckTaxiMapMarkers.ShortcutPreferenceKey(m.stableId),
                    existed=PlayerPrefs.HasKey(TruckTaxiMapMarkers.ShortcutPreferenceKey(m.stableId)),
                    value=PlayerPrefs.GetInt(TruckTaxiMapMarkers.ShortcutPreferenceKey(m.stableId)),discovered=map.IsShortcutDiscovered(m.stableId) }).ToArray();
            var source=host.configuration.passengerDatabase!=null ? host.configuration.passengerDatabase.passengers.FirstOrDefault(p=>p!=null) : host.configuration.passengers.FirstOrDefault(p=>p!=null);
            check(source!=null,"Map probe has an authored passenger fixture");
            PassengerProfile passenger=source!=null ? UnityEngine.Object.Instantiate(source) : null;
            GameObject legend=null;
            try
            {
                if(passenger==null) yield break;
                passenger.possibleRequests=Array.Empty<PassengerRequestDefinition>(); passenger.uniqueMechanics=Array.Empty<TruckTaxiMechanic>();
                passenger.specialAppreciationEligible=false; passenger.requestFrequency=10000; passenger.basePatience=10000; passenger.requestDifficultyRange=Vector2.one;
                map.registry=registry;
                gps.DisplaySettings.showHud=true; gps.DisplaySettings.showPois=true;
                gps.DisplaySettings.hudRangeMeters=gps.DisplaySettings.cabRangeMeters=700; gps.ApplyDisplaySettings(false);
                body.isKinematic=true;
                session.EndShift(); host.StartShift();
                int compassCount=CountCompasses(host); int markerCount=map.Markers.Count;
                map.Initialize(host,gps);
                check(CountCompasses(host)==compassCount && map.Markers.Count==markerCount,"Repeated map initialization creates no second Compass or marker set");
                bool offered=session.OfferRide(passenger); check(offered,"Map probe authored ride offered"); if(!offered) yield break;
                session.OfferTimerPaused=true; host.hud.SetOfferMapVisible(true);
                yield return Wait(.8f); Canvas.ForceUpdateCanvases();
                var offerMap=host.hud.GetComponentInChildren<TruckTaxiOfferMap>(true);
                CheckOfferBase(offerMap,registry,check); capture?.Invoke("Map_Offer_ActualPickupDestination");
                check(session.AcceptRide(),"Map probe ride accepted");
                host.SetPaused(false);
                Place(host,session.Pickup.StopPosition+Vector3.right*35);
                map.Refresh(); yield return Wait(.9f);
                var pickup=map.Markers.FirstOrDefault(m=>m!=null && m.stableId==session.Pickup.locationId);
                check(pickup!=null && pickup.markerType==TruckTaxiMapMarkerType.PassengerPickup && pickup.state==TruckTaxiMapMarkerState.Active,"Actual pickup uses active passenger silhouette");
                ValidateCompassPair(map,pickup,gps,check,"Actual pickup");
                ValidateEndpointUniqueness(map,pickup,gps,check);
                ValidatePlayerAndRoute(gps,registry,check);
                yield return CaptureViews(host,"Pickup",changer,check,capture);
                host.TeleportNear(session.Pickup);
                yield return Until(()=>session.HasPassenger,12);
                check(session.HasPassenger,"Map probe boards through existing passenger runtime"); if(!session.HasPassenger) yield break;
                Place(host,session.Destination.StopPosition+Vector3.right*35); map.Refresh(); yield return Wait(.8f);
                var destination=map.Markers.FirstOrDefault(m=>m!=null && m.stableId==session.Destination.locationId);
                ValidateCompassPair(map,destination,gps,check,"Actual destination flag");
                yield return CaptureViews(host,"Destination",changer,check,capture);

                foreach(var type in new[]{TaxiRequestType.ScenicRoute,TaxiRequestType.IllicitStop})
                {
                    var authored=host.configuration.requests.FirstOrDefault(d=>d!=null && d.requestType==type);
                    check(authored!=null,"Authored stop definition exists: "+type); if(authored==null) continue;
                    var definition=UnityEngine.Object.Instantiate(authored); definitions.Add(definition); definition.timer=600;
                    bool assigned=session.GenerateRequest(definition); check(assigned,"Map probe assigns existing "+type); if(!assigned) continue;
                    var request=session.ActiveStop; if(request?.StopPoint==null) { check(false,"Stop request has runtime point"); continue; }
                    Place(host,request.StopPoint.Position+Vector3.right*(request.StopPoint.radius+22));
                    map.Refresh(); yield return Wait(.9f);
                    var marker=map.Markers.FirstOrDefault(m=>m!=null && m.stableId==request.StopPoint.stableId);
                    check(marker!=null && marker.state==TruckTaxiMapMarkerState.Active && marker.markerType==TruckTaxiMapIconRegistry.ForStop(request.StopPoint.category),"Actual stop retains category while active: "+type);
                    ValidateCompassPair(map,marker,gps,check,"Actual "+type);
                    check(destination!=null && destination.Presented && destination.state==TruckTaxiMapMarkerState.Known,"Final destination remains known during "+type+" detour");
                    ValidateEndpointUniqueness(map,marker,gps,check);
                    yield return CaptureViews(host,type.ToString(),changer,check,capture);
                    session.DebugResolveRequest(true); map.Refresh(); yield return Wait(.7f);
                    check(marker!=null && marker.state==TruckTaxiMapMarkerState.Completed,"Forced stop completion changes map state: "+type);
                    check(gps.TargetId==session.Destination.locationId && destination.state==TruckTaxiMapMarkerState.Active,"Destination becomes primary again after "+type);
                    ValidateCompassPair(map,marker,gps,check,"Completed "+type);
                }
                if(discovery.Length>0)
                {
                    var saved=discovery[0]; var shortcut=map.Markers.First(m=>m!=null && m.stableId==saved.id);
                    registry.showUndiscoveredShortcuts=true;
                    map.RestoreShortcutDiscoveryForProbe(saved.id,false);
                    Place(host,shortcut.Position+Vector3.right*30); map.Refresh(); yield return Wait(.7f);
                    ValidateCompassPair(map,shortcut,gps,check,"Undiscovered shortcut");
                    capture?.Invoke("Map_Shortcut_Undiscovered");
                    check(map.DiscoverShortcut(saved.id) && !map.DiscoverShortcut(saved.id),"Forced shortcut discovery is idempotent");
                    map.Refresh(); yield return Wait(.7f);
                    check(shortcut.markerType==TruckTaxiMapMarkerType.DiscoveredShortcut && PlayerPrefs.GetInt(saved.key)==1,"Discovered shortcut type and preference set");
                    ValidateCompassPair(map,shortcut,gps,check,"Discovered shortcut checked arrow");
                    yield return CaptureViews(host,"ShortcutDiscovered",changer,check,capture);
                }
                else check(false,"Existing shortcut source present for discovery probe");

                var ram=host.configuration.requests.FirstOrDefault(d=>d!=null && d.requestType==TaxiRequestType.RamTraffic);
                if(ram!=null)
                {
                    var definition=UnityEngine.Object.Instantiate(ram); definitions.Add(definition); definition.target=1; definition.timer=600;
                    bool assigned=session.GenerateRequest(definition); check(assigned,"Existing traffic target request assignable");
                    map.Refresh();
                    var traffic=map.Markers.FirstOrDefault(m=>m!=null && m.markerType==TruckTaxiMapMarkerType.TargetVehicle && m.source!=null);
                    check(traffic!=null,"Actual UTS target receives car-reticle marker");
                    if(traffic!=null)
                    {
                        Place(host,traffic.Position+Vector3.right*25); map.Refresh(); yield return Wait(.7f);
                        ValidateCompassPair(map,traffic,gps,check,"Actual moving traffic target");
                        yield return CaptureViews(host,"TrafficTarget",changer,check,capture);
                        session.RecordEvent(TaxiEventType.TrafficRam,traffic.stableId,8);
                    }
                }
                session.EndShift(); host.StartShift(); Place(host,position);
                // Clone-only showcase capacity: do not let normal nearby-world points evict the explicit fixture.
                registry.maximumNearbyPoints=64; registry.showDebugPoints=true;
                gps.DisplaySettings.northUp=true;
                gps.DisplaySettings.hudRangeMeters=gps.DisplaySettings.cabRangeMeters=350; gps.ApplyDisplaySettings(false);
                // Let the existing map cameras consume the new range before converting display UVs.
                yield return Wait(.7f);
                foreach(TruckTaxiMapMarkerType type in Enum.GetValues(typeof(TruckTaxiMapMarkerType)))
                {
                    if(type==TruckTaxiMapMarkerType.Player || type==TruckTaxiMapMarkerType.ActiveRoute) continue;
                    var go=new GameObject("Forced marker fixture "+type); go.transform.SetParent(fixture.transform,false);
                    go.transform.position=position;
                    var marker=go.AddComponent<TruckTaxiMapMarker>(); marker.stableId="probe.map."+type;
                    marker.label="FIXTURE "+type; marker.markerType=type; marker.state=TruckTaxiMapMarkerState.Optional;
                    check(map.Register(marker) && map.Register(marker),"Fixture registration idempotent: "+type); forced.Add(marker);
                }
                var fromUv=gps.CabCompass?.GetType().GetMethod("GetMiniMapWorldPositionFromUV",new[]{typeof(Vector2)});
                check(fromUv!=null,"Fixture placement uses actual Compass minimap UV-to-world API");
                if(fromUv==null) yield break;
                var shift=gps.CabCompass.GetType().GetProperty("miniMapIconPositionShift")?.GetValue(gps.CabCompass) is Vector2 s ? s : Vector2.zero;
                // A fixed metre grid ignored vendor zoom=0.5 and the cab's wide aspect ratio.
                // Fit inside the narrower cab viewport; equal-range, north-up HUD also contains it.
                for(int i=0;i<forced.Count;i++)
                {
                    var uv=new Vector2(.15f+i%5*.175f,.18f+i/5*.19f)-shift;
                    var point=(Vector3)fromUv.Invoke(gps.CabCompass,new object[]{uv}); point.y=body.position.y;
                    forced[i].transform.position=point;
                }
                map.Refresh(); yield return Wait(1);
                foreach(var marker in forced) ValidateCompassPair(map,marker,gps,check,"Forced fixture "+marker.markerType);
                // Being eligible for presentation is not a promise that an optional POI is on-screen.
                var clipped=forced[forced.Count-1]; var insidePosition=clipped.transform.position;
                var outside=(Vector3)fromUv.Invoke(gps.CabCompass,new object[]{new Vector2(1.25f,.5f)-shift}); outside.y=body.position.y;
                clipped.transform.position=outside; map.Refresh(); yield return Wait(.6f);
                foreach(var compass in new[]{gps.CabCompass,gps.HudCompass})
                {
                    RenderedImage(clipped.CompassPoi,compass,out bool visible);
                    var configured=clipped.CompassPoi?.GetType().GetField("iconNonVisited")?.GetValue(clipped.CompassPoi) as Sprite;
                    check(!visible && configured==registry.Find(clipped.markerType)?.icon,
                        (compass==gps.CabCompass ? "CAB" : "HUD")+" clips an offscreen optional fixture without losing its configured sprite");
                }
                clipped.transform.position=insidePosition; map.Refresh(); yield return Wait(.6f);
                ValidateCompassPair(map,clipped,gps,check,"Offscreen fixture returned to visible map");
                yield return CaptureViews(host,"ForcedIconFamilies",changer,check,capture);
                legend=BuildLegend(host,registry); yield return Wait(.2f); capture?.Invoke("Map_IconLegend_ForcedReference");
                UnityEngine.Object.Destroy(legend); legend=null;

                offered=session.OfferRide(passenger); check(offered,"Offer preview fixture offered");
                if(offered)
                {
                    session.OfferTimerPaused=true; host.hud.SetOfferMapVisible(true); yield return Wait(.7f);
                    var camera=gps.PreviewCamera; check(camera!=null,"Offer uses existing Compass map camera");
                    if(camera!=null)
                    {
                        for(int i=0;i<forced.Count;i++)
                        {
                            var p=camera.ViewportToWorldPoint(new Vector3(.15f+i%5*.175f,.18f+i/5*.19f,100)); p.y=position.y;
                            forced[i].transform.position=p;
                            forced[i].state=TruckTaxiMapMarkerState.Active;
                        }
                        map.Refresh(); yield return Wait(.7f); CheckOfferBase(offerMap,registry,check);
                        foreach(var marker in forced)
                        {
                            var icon=offerMap!=null ? offerMap.GetComponentsInChildren<UnityEngine.UI.Image>(true).FirstOrDefault(i=>i.name=="Map context: "+marker.stableId) : null;
                            check(Usable(icon) && icon.sprite==registry.Find(marker.markerType)?.icon,"Offer actual context sprite: "+marker.markerType);
                        }
                        capture?.Invoke("Map_Offer_ForcedIconFamilies");
                    }
                    session.DeclineRide(); host.SetPaused(false);
                }
                var expired=forced[0]; string expiredId=expired.stableId; var oldPoi=expired.CompassPoi;
                UnityEngine.Object.Destroy(expired.gameObject); yield return Wait(.4f); map.Refresh();
                check(oldPoi==null && !map.Markers.Any(m=>m!=null && m.stableId==expiredId),"Destroying dynamic marker cleans up vendor POI and stable ID");
                var replacement=new GameObject("Replacement marker fixture").AddComponent<TruckTaxiMapMarker>(); replacement.transform.SetParent(fixture.transform,false);
                replacement.stableId=expiredId; replacement.markerType=TruckTaxiMapMarkerType.PassengerPickup;
                check(map.Register(replacement),"Destroyed marker stable ID can be registered again"); forced.Add(replacement);
            }
            finally
            {
                foreach(var marker in forced) if(marker!=null) map.Unregister(marker);
                if(legend!=null) UnityEngine.Object.Destroy(legend); UnityEngine.Object.Destroy(fixture);
                foreach(var saved in discovery)
                {
                    if(saved.existed) PlayerPrefs.SetInt(saved.key,saved.value); else PlayerPrefs.DeleteKey(saved.key);
                    map.RestoreShortcutDiscoveryForProbe(saved.id,saved.discovered);
                }
                PlayerPrefs.Save();
                session.OfferTimerPaused=false; session.EndShift(); gps.FrameOffer(null);
                map.registry=originalRegistry; JsonUtility.FromJsonOverwrite(settings,gps.DisplaySettings); gps.ApplyDisplaySettings(false);
                body.position=position; body.rotation=rotation; host.Player.transform.SetPositionAndRotation(position,rotation);
                body.isKinematic=wasKinematic; if(!wasKinematic) { body.linearVelocity=velocity; body.angularVelocity=angular; }
                Physics.SyncTransforms(); session.DiscardTeleportDistance(); host.SetPaused(wasPaused);
                if(changer!=null) for(int n=0;n<changer.cameras.Count && changer.currentCameraIndex!=cameraIndex;n++) changer.NextCamera();
                map.Refresh(); foreach(var definition in definitions) UnityEngine.Object.Destroy(definition);
                if(passenger!=null) UnityEngine.Object.Destroy(passenger); UnityEngine.Object.Destroy(registry);
                foreach(var saved in discovery) check(PlayerPrefs.HasKey(saved.key)==saved.existed && (!saved.existed || PlayerPrefs.GetInt(saved.key)==saved.value) &&
                    map.IsShortcutDiscovered(saved.id)==saved.discovered,"Shortcut preferences and discovery restored exactly: "+saved.id);
            }
        }
        private static void CheckOfferBase(TruckTaxiOfferMap offer,TruckTaxiMapIconRegistry registry,Action<bool,string> check)
        {
            check(offer!=null && offer.isActiveAndEnabled,"Existing offer preview visible"); if(offer==null) return;
            var raw=offer.GetComponent<UnityEngine.UI.RawImage>(); check(raw!=null && raw.texture!=null,"Offer map retains Compass render texture");
            var images=offer.GetComponentsInChildren<UnityEngine.UI.Image>();
            foreach(var type in new[]{TruckTaxiMapMarkerType.Player,TruckTaxiMapMarkerType.PassengerPickup,TruckTaxiMapMarkerType.Destination})
                check(images.Any(i=>Usable(i) && i.sprite==registry.Find(type)?.icon),"Offer actual main sprite: "+type);
        }
        private static void ValidateCompassPair(TruckTaxiMapMarkers map,TruckTaxiMapMarker marker,TruckTaxiGPSAdapter gps,Action<bool,string> check,string label)
        {
            check(marker!=null && marker.Presented,label+" semantic marker presented");
            foreach(var compass in new[]{gps.CabCompass,gps.HudCompass})
            {
                string view=compass==gps.CabCompass ? "CAB" : "HUD";
                var poi=map.RenderedPoi(marker); var image=RenderedImage(poi,compass,out bool visible);
                bool valid=marker!=null && visible && Usable(image) && image.sprite==map.Registry.Find(marker.markerType)?.icon;
                var configured=poi?.GetType().GetField("iconNonVisited")?.GetValue(poi) as Sprite;
                check(valid,label+" actual "+view+" Compass Image sprite/visibility/rect visible="+visible+" configured="+(configured!=null ? configured.name : "<null>")+
                    (image!=null ? " rect="+image.rectTransform.rect.size+" sprite="+(image.sprite!=null ? image.sprite.name : "<null>")+" enabled="+image.isActiveAndEnabled : " (no rendered image)"));
            }
        }
        private static void ValidatePlayerAndRoute(TruckTaxiGPSAdapter gps,TruckTaxiMapIconRegistry registry,Action<bool,string> check)
        {
            foreach(var compass in new[]{gps.CabCompass,gps.HudCompass})
            {
                if(compass==null) { check(false,"Existing Compass instance present"); continue; }
                string view=compass==gps.CabCompass ? "CAB" : "HUD";
                var images=compass.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                check(images.Any(i=>Usable(i) && i.sprite==registry.Find(TruckTaxiMapMarkerType.Player)?.icon),view+" renders actual player arrow Image");
                var cue=compass.GetType().GetProperty("routeWaypointPOI")?.GetValue(compass) as Component;
                var icon=RenderedImage(cue,compass,out bool visible);
                check(visible && Usable(icon) && icon.sprite==registry.Find(TruckTaxiMapMarkerType.ActiveRoute)?.icon,view+" renders distinct existing route-cue arrow");
                var camera=compass.GetType().GetField("miniMapCamera")?.GetValue(compass) as Camera;
                check(camera!=null && camera.targetTexture!=null && camera.targetTexture.IsCreated(),view+" existing map camera retains a live render texture");
            }
        }
        private static UnityEngine.UI.Image RenderedImage(Component poi,Component compass,out bool visible)
        {
            visible=false; if(poi==null || compass==null) return null;
            object group=compass.GetType().GetProperty("compassGroup")?.GetValue(compass);
            if(!(group is int index)) return null;
            object state=poi.GetType().GetMethod("GetState",new[]{typeof(int)})?.Invoke(poi,new object[]{index});
            if(state==null) return null;
            visible=state.GetType().GetField("miniMapIsVisible")?.GetValue(state) is bool shown && shown;
            return state.GetType().GetField("miniMapIconImage")?.GetValue(state) as UnityEngine.UI.Image;
        }
        private static void ValidateEndpointUniqueness(TruckTaxiMapMarkers map,TruckTaxiMapMarker marker,TruckTaxiGPSAdapter gps,Action<bool,string> check)
        {
            if(marker==null) return;
            var rendered=map.RenderedPoi(marker);
            if(rendered!=null && marker.CompassPoi!=null && rendered!=marker.CompassPoi)
            {
                RenderedImage(marker.CompassPoi,gps.CabCompass,out bool cab); RenderedImage(marker.CompassPoi,gps.HudCompass,out bool hud);
                check(!cab && !hud,"Fallback endpoint is hidden while legacy route POI is reused");
            }
            else check(rendered!=null,"Exactly one available endpoint POI for "+marker.stableId);
        }
        private static bool Usable(UnityEngine.UI.Image image) => image!=null && image.isActiveAndEnabled && image.sprite!=null &&
            image.color.a>.01f && image.canvas!=null && image.canvas.isActiveAndEnabled && image.rectTransform.rect.width>1 && image.rectTransform.rect.height>1 &&
            image.rectTransform.lossyScale.sqrMagnitude>1e-12f;
        private static IEnumerator CaptureViews(TruckTaxiBootstrap host,string name,CameraChanger changer,Action<bool,string> check,Action<string> capture)
        {
            if(changer!=null)
            {
                for(int n=0;n<changer.cameras.Count && changer.cameras[changer.currentCameraIndex].name.Contains("Driver");n++) changer.NextCamera();
            }
            yield return Wait(.5f); Canvas.ForceUpdateCanvases(); capture?.Invoke("Map_HUD_"+name);
            if(changer==null) { check(false,"Cab capture requires existing CameraChanger"); yield break; }
            for(int n=0;n<changer.cameras.Count && !changer.cameras[changer.currentCameraIndex].name.Contains("Driver");n++) changer.NextCamera();
            yield return Wait(.5f); Canvas.ForceUpdateCanvases();
            check(changer.cameras[changer.currentCameraIndex].name.Contains("Driver"),"Existing cockpit camera selected for "+name);
            capture?.Invoke("Map_Cab_"+name);
        }
        private static GameObject BuildLegend(TruckTaxiBootstrap host,TruckTaxiMapIconRegistry registry)
        {
            var panel=host.hud.Panel(host.hud.Root,"Map probe legend (temporary)",new Vector2(.3f,.18f),new Vector2(.97f,.95f));
            host.hud.Text(panel,"Legend header",new Vector2(.03f,.91f),new Vector2(.97f,.98f),24).text="FORCED MAP ICON REFERENCE / NOT GAMEPLAY SOURCES";
            int index=0;
            foreach(TruckTaxiMapMarkerType type in Enum.GetValues(typeof(TruckTaxiMapMarkerType)))
            {
                float x=.025f+(index%2)*.5f,y=.85f-(index/2)*.084f;
                var rect=TruckTaxiHud.Rect(panel,type+" icon",new Vector2(x,y),new Vector2(x+.045f,y+.052f));
                var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite=registry.Find(type)?.icon;
                image.color=registry.Tint(type,TruckTaxiMapMarkerState.Active); image.preserveAspect=true; image.raycastTarget=false;
                host.hud.Text(panel,type+" label",new Vector2(x+.06f,y),new Vector2(x+.47f,y+.056f),22).text=type.ToString(); index++;
            }
            return panel.gameObject;
        }
        private static int CountCompasses(TruckTaxiBootstrap host) => host.Player.GetComponentsInChildren<MonoBehaviour>(true).Count(c=>c!=null && c.GetType().FullName=="CompassNavigatorPro.CompassPro");
        private static void Place(TruckTaxiBootstrap host,Vector3 position)
        { var body=host.Player.GetComponent<Rigidbody>(); body.position=position+Vector3.up*1.6f; host.Player.transform.position=body.position; Physics.SyncTransforms(); host.Session.DiscardTeleportDistance(); }
        private static WaitForSecondsRealtime Wait(float seconds) => new WaitForSecondsRealtime(seconds);
        private static IEnumerator Until(Func<bool> predicate,float seconds)
        { float deadline=Time.realtimeSinceStartup+seconds; while(!predicate() && Time.realtimeSinceStartup<deadline) yield return null; }
    }
}
#endif
