# Prompt 015C - Time-Based Traffic Density

## Summary

Traffic density now consumes LWS game time through:

- `ILwsTrafficDemandService`
- `LwsTrafficDemandService`
- `LwsTrafficDemandProfile`
- `LwsTrafficDemandSample`
- `LwsTrafficDemandSnapshot`

The current validation profile is `Generic Interstate`.

## Current Validation Targets

Generic interstate development tuning:

- overnight nearby target: 2
- daytime nearby target: 4
- peak nearby target: 6
- maximum active vehicles: 24
- nearby radius: 750 m

This replaces the old fixed sparse behavior that capped validation traffic around 8 vehicles with a 4-second spawn cadence.

## Daily Curve

The profile uses hourly samples, not hard-coded daypart if-statements.

General pattern:

- 00:00-05:00: very light
- 05:00-06:30: building
- 06:30-09:00: AM peak
- 09:00-15:30: moderate daytime
- 15:30-19:00: PM peak
- 19:00-22:00: evening decline
- 22:00-00:00: light

`LwsTrafficDemandService` smooths target changes so traffic does not jump instantly from low to peak.

## UTS Adapter Consumption

`LwsUtsHighwayTrafficController` now resolves:

- `ILwsGameClockService`
- `ILwsTrafficDemandService`

It exposes:

- `DemandSnapshot`
- `TargetActiveVehicles`
- `MinimumNearbyTraffic`
- `NearbyTrafficVehicles`
- `EffectiveSpawnIntervalSeconds`

The UTS adapter still owns UTS path creation and vehicle spawning. Vendor UTS source remains untouched.

## No Player-Proximity Swerve

This change does not reintroduce proximity swerving.

Normal player proximity still leaves NPC traffic on its UTS path. Only future horn/air-horn behavior may request a controlled temporary reaction through the established traffic-event seam.

## Future Seams

The demand profile is ready for later multipliers:

- road class
- area density
- city/metro context
- weekday/weekend
- weather
- incidents
- holidays
- jobs/events

Prompt 015C only implements game-clock-driven development demand.
