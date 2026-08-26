# Cab GPS Mount

## Summary

The cockpit GPS uses the real Compass Navigator Pro presentation inside the truck cab. The exterior HUD minimap remains the normal bottom-right Compass HUD minimap and is not changed by this repair.

There is still one navigation authority:

`ILwsRoadGraphService + ILwsNavigationService + ILwsWorldOriginService`

Compass Navigator Pro is presentation only.

## Runtime Cab GPS Hierarchy

Current runtime hierarchy:

`IH_PlayerTruck_NWH_Runtime/Cab/IH_CabAccessoryAnchors/IH_CabAnchor_GpsMount/IH Cab GPS Compass Navigator Pro`

The stable cab GPS anchor is:

`IH_CabAnchor_GpsMount`

`LwsCabGpsController` first creates the physical world-space GPS screen at the cab anchor. `LwsCompassNavigatorProAdapter` then uses that physical screen transform as the placement reference and creates the vendor Compass cab instance as a sibling under the same mount. The fallback semantic screen is hidden once the Compass cab presentation is active.

## Canvas And Placement

Cab GPS requirements:

- Canvas render mode: `World Space`
- physical placement inherited from the cab/truck hierarchy
- follows floating-origin movement naturally through parentage
- readable dashboard-scale UI instead of a far-away or screen-space overlay

Placement values currently come from `LwsCabGpsController`:

- local position: `localScreenPosition`
- local rotation: `localScreenEulerAngles`
- screen size: `screenSize`
- world scale: `screenScale`

The new `PhysicalScreenTransform` property exposes the runtime physical screen so the Compass adapter can match that placement precisely.

## Compass Minimap Fit

For the cab instance only, `LwsCompassNavigatorProAdapter` sets:

`miniMapPositionAndSize = UserDefined`

Then it fits the vendor prefab's `MiniMap Root` and `MiniMap` RectTransforms to the physical GPS screen. This prevents Compass' HUD-oriented placement code from pushing the cab display far from the dashboard.

The HUD instance still uses:

`miniMapPositionAndSize = ControlledByCompassNavigatorPro`

with `miniMapLocation = BottomRight`.

## Camera Policy

- Cockpit camera: cab world GPS active, HUD minimap hidden.
- Exterior/chase/other camera: cab world GPS may remain active, HUD minimap visible.
- Full map: preserved through the existing Compass full-map path.

The policy is managed by `ILwsCameraPresentationService` and does not create separate routes, destinations, player markers, or route progress state.

## Vendor Assets Audited

- Compass Navigator Pro 4 runtime prefab: `Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Prefabs/CompassNavigatorPro.prefab`
- Compass minimap placement API: `miniMapPositionAndSize`, `miniMapLocation`, `miniMapOrientation`, `miniMapSize`
- Vendor `MiniMap Root` layout behavior in `CompassProPrivate.MiniMap.cs`

## Vendor Assets Used

- Compass Navigator Pro 4 prefab and public runtime properties
- Existing LWS Compass adapter
- Existing LWS cab GPS anchor/controller

## Vendor Source Modified

None.

## Validation Notes

Automated source checks verify that cab GPS placement uses the physical screen transform and Compass `UserDefined` minimap placement. Final visual proof requires normal Unity Editor play mode from the cockpit camera.