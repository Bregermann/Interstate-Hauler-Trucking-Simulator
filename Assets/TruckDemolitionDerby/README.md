# Current combined Windows delivery — October16,2026

Derby is included in the final31-sport Windows build. Full multi-difficulty matches, physical damage/rollover/inactivity/survivor lock, actual rear mirror pixels, source/reconnect/reset checks and a sustained60second four-cab rendered workload passed. The Windows-only repeated stationary reset failure was traced to NWH cancelling its limiter coroutine while preserving revLimiterActive; the fresh Derby reset now clears that public transient latch, with injected-state and real per-heat departure regressions. Timings and before/after evidence are in the collection VALIDATION.md; hardware/audio/human handling remain manual. See [collection validation](../TruckSports/VALIDATION.md) for version-labelled evidence, performance limits and remaining human checks. Earlier native-stage statements below are historical.

---

# Truck Demolition Derby

Two to eight trucks, up to four local human drivers, named Easy/Normal/Hard AI and autonomous play. Last running truck wins a heat; first to three heat wins takes the match. A same-step final wipe draws the heat. Choose split-screen cab views or the shared stadium broadcast.

Drive with the assigned movement source. Reverse brakes first, then backs up. Rear rams reduce engine exposure but do not grant immunity. Right-stick or the assigned mouse looks independently; shared-controller sticks keep useful fixed cameras. Cab mirrors show the owning truck's actual rearward world. Pause, replacement devices, results and rematch use the collection's existing ownership and neutral-confirmation policies.

**KEEP MOVING.** Five seconds without meaningful chassis movement causes a fiery explosion. Wheelspin and minor vibration do not count. The timer only runs in active play. Rollovers permit a short physical rocking recovery using movement, then eliminate after four seconds unrecoverably overturned. A locked heat protects its survivors from later stop-rule explosions.

Genuine energetic impacts damage front, rear, sides, cab, roof, engine, wheels and structure. Engine damage reduces the actual NWH powertrain; wheel damage changes the actual tire grip and steering response. Visible NWH mesh deformation, smoke, sparks, leaks, escalating fire and pooled physical fragments follow that damage. Sustained compound contact is deduplicated; no second truck collision impulse is added. Props include movable vendor barrels and resettable Core-Collapse barriers.

The arena uses installed stadium, barrier, road-bank and character models. Replay is recorded presentation of truck/prop motion, NWH dents, breakup and effects; it has no live scoring or collision authority. The bounded rolling record retains the final two minutes of a long heat. Rematches restore original meshes, wheel handling, engine state, fragments, props and mirror resources.

## Vendor foundations

- `Assets/NWH/Vehicle Physics 2/Vehicles/EuroTruckbyGR3D/SemiTruck.prefab`: full `VehicleController`, suspension, wheel/powertrain, original steering wheel and mirror placements; public `DamageHandler.Enqueue`, deformation and repair. LWS qualifies impact energy/location and applies explicit opt-in damage consequences through public APIs.
- `Assets/CoreCollapse`: existing licensed `Collapser`/support lifecycle and prefractured Bungee meshes for reusable stadium barriers. Original licensed package remains a local prerequisite.
- Existing EasyRoads bank geometry, Field Goal stadium assets, RoadPropsPack/ConstructionPropsBundle, UTS/Taxi character models, Heat fonts/glyphs, DOTween animation, Cinemachine owned look and shared Sports input/audio/replay components.
- Rope Toolkit was not found in the audited owned collection; it is irrelevant to Derby. Mobile mesh damage uses NWH rather than the static structural collapse solver. Networking, traffic routing, GPS, dialogue and a second vehicle/fluid solver are unnecessary here.

Vendor source modified: **NO**. Duplicate vendor functionality created: **NO**. LWS additions own Derby rules, collision qualification, localized damage semantics, the movement timer, AI decisions, bounded effects, replay coordination and UI.
