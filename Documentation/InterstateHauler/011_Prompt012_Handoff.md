# Prompt 012 Handoff - Weather Maker

Use Prompt 011 only as navigation context.

Physical GPS:

- Runtime overlay: `IH Physical Cab GPS Screen`
- Controller: `LwsCabGpsController`
- Map graphic: `LwsGpsMapGraphic`
- Default voice pack: `Assets/LWS/InterstateHauler/Navigation/Data/IH_GpsVoicePack_Default.asset`

Navigation services:

- `ILwsNavigationService`
- `LwsNavigationService`
- `LwsRoutePlanner`
- `ILwsGpsVoiceGuidanceService`
- `ILwsPlayerSettingsService`

Route context available:

- route active/status
- current road/edge
- next maneuver/instruction
- distance to maneuver
- remaining distance/ETA
- off-route/recalculating/arrival

Traffic state:

- Prompt 010 traffic spawning/driving manually passed in the normal Unity Editor.
- GPS routing does not depend on UTS paths or UTS concrete classes.

Settings seam:

- `GpsVoiceGuidanceEnabled`
- default ON
- persisted through PlayerPrefs for the current development baseline

Performance baseline:

- Route solving is event-driven.
- Physical GPS refresh is throttled.
- Route planner does not scan UTS, EasyRoads, or scene objects every frame.

Weather Maker authority reminder:

- Weather Maker owns weather rendering/effects where integrated.
- LWS navigation owns road/route/current-road state.
- Prompt 012 should not make Weather Maker route authority.
