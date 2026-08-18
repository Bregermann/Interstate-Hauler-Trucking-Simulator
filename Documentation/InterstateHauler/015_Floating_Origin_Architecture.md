# Prompt 015 - Floating-Origin Architecture

## 1. Authority

LWS owns global world coordinates, accumulated origin offset, local/global conversion, origin-shift scheduling, participant registration, and origin-shift lifecycle events.

Scene Streamer keeps its existing chunk load/unload role. NWH keeps tractor/trailer physics. UTS keeps NPC driving. Weather Maker keeps atmospheric weather. Weatherade remains visual road accumulation.

No vendor package owns Interstate: Hauler global world coordinates.

## 2. Global/Local Coordinate Model

The project now uses a double-precision global coordinate model:

`GlobalPosition = LocalUnityPosition + CurrentOriginOffset`

`LocalUnityPosition = GlobalPosition - CurrentOriginOffset`

Unity scene objects still use float `Vector3` local positions near the origin. Stable semantic systems use global positions through `ILwsWorldOriginService`.

## 3. Double-Precision Position Type

`LwsWorldPositionD` is the project-owned double-precision position payload. It stores `double x`, `double y`, and `double z`, supports equality, distance, approximate equality, vector conversion, and local presentation conversion from a double origin offset.

This is the save-facing world-position shape for future persistence.

## 4. World Origin Service

`ILwsWorldOriginService` and `LwsWorldOriginService` expose:

- `CurrentOriginOffset`
- `PlayerLocalPosition`
- `PlayerGlobalPosition`
- `OriginVersion`
- `ShiftCount`
- `LocalToGlobal(Vector3)`
- `GlobalToLocal(LwsWorldPositionD)`
- `QueueShift(...)`
- `TryExecuteQueuedShift(...)`
- `ForceShiftNow(...)`
- participant registration
- `OriginShiftStarting`
- `OriginShiftCompleted`

The service is registered by `LwsApplicationBootstrap` before world streaming and road graph services.

## 5. Shift Trigger

`LwsFloatingOriginCoordinator` monitors the active player truck's local Unity position. When the local horizontal distance exceeds the validation threshold, it queues a grid-aligned shift.

The shift is not executed directly from arbitrary `Update` code.

## 6. Shift Grid

Shifts are grid aligned through `LwsWorldCoordinateUtility.CalculateGridAlignedShift`. Validation uses a 500 m grid, so a player at 1325 m on a shifted axis produces a 1000 m global origin shift and moves the local player back near 325 m.

## 7. Atomic Shift Lifecycle

The lifecycle is:

1. Determine global shift delta.
2. Fire `OriginShiftStarting`.
3. Translate all registered spatial participants by the opposite local delta.
4. Update `CurrentOriginOffset`.
5. Preserve physics velocities through registered Rigidbody participants.
6. Call `Physics.SyncTransforms()` when tuning enables it.
7. Fire `OriginShiftCompleted`.
8. Increment `OriginVersion` and `ShiftCount`.

Systems should not observe a half-shifted world.

## 8. Participant Registry

Origin shifts use registered `ILwsFloatingOriginParticipant` instances. The shift path does not scan every `Transform` in the scene.

Current participant types:

- loaded chunk roots
- player tractor root
- player trailer root
- UTS traffic lane root
- active UTS traffic vehicle roots
- general transform participants
- Rigidbody participants with velocity preservation

## 9. NWH Tractor Behavior

The player tractor receives `LwsFloatingOriginRigidbodyParticipant` through `LwsPlayerTruckSpawner`. This shifts the truck root while preserving Rigidbody linear and angular velocity.

NWH source is not modified. Engine, drivetrain, transmission mode, controls, FFB, and road-condition response stay inside the existing LWS/NWH authority boundaries.

## 10. Trailer Behavior

The validation dry van receives its own `LwsFloatingOriginRigidbodyParticipant`. Attached and active detached player trailers shift in the same origin operation as the tractor, preserving relative position and physics velocity.

## 11. Scene Streamer Integration

Scene Streamer remains behind the `LwsSceneStreamerAdapter`. Prompt 014 found Scene Streamer 1.26.1 and an installed `Load(string sceneName)` path whose explicit argument is not reliable for LWS policy loads, so LWS continues to use Unity additive loading for selected chunk scenes.

Prompt 015 adds the origin service beside streaming rather than embedding origin math into Scene Streamer.

## 12. Chunk Global/Local Bounds

`LwsWorldChunkDefinition` keeps canonical global bounds. New helpers derive local presentation bounds from the current origin offset:

- `GlobalBoundsCenter`
- `GetLocalBounds(...)`
- `GetLocalBoundsCenter(...)`

Canonical manifest chunk data is not permanently mutated during origin shifts.

## 13. Road Graph Integration

Road IDs remain stable. Road graph queries that begin from a truck transform convert local player position to global coordinates before semantic lookup.

Updated systems include road graph debug lookup and road-condition runtime lookup.

## 14. GPS Integration

Navigation and the physical cab GPS now use origin-aware player positions. The cab GPS remains presentation under the truck, while route/map calculations consume global position from `ILwsWorldOriginService`.

Routes and maneuvers should not be invalidated merely because the local player position rebased.

## 15. UTS Integration

UTS traffic keeps ownership of NPC driving. LWS converts global lane/path positions to local presentation positions when creating UTS paths and spawning vehicles.

Active traffic vehicles and the traffic lanes root register as floating-origin participants. After an origin shift, LWS refreshes UTS path point caches through the existing adapter boundary.

## 16. Weather Maker Behavior

Weather Maker atmospheric state is not restarted or rebuilt by origin shifts. The active camera and player presentation move through normal participant relocation.

## 17. Weatherade Behavior

Weatherade remains visual accumulation only. `LwsWeatheradeAdapter` subscribes to `OriginShiftCompleted`, reconfigures its coverage follow target, and refreshes coverage materials after a rebase.

## 18. Road-Condition Behavior

Semantic road-condition state is global and does not change solely because an origin shift occurs. NWH road-condition response remains owned by the existing road-condition/NWH adapter path.

## 19. Global Position/Save Seam

Future persistence must save global coordinates, not raw `Transform.position`.

Prompt 016 should save at least:

- player truck ID
- truck definition ID
- player global world position
- current origin offset if useful for restoration
- trailer identity and global state
- stable chunk/road IDs where relevant

## 20. Performance

Origin shifts are rare, grid aligned, and participant based. Shift diagnostics report participant counts, chunk roots shifted, dynamic bodies shifted, traffic participants shifted, and last shift duration.

The debug panel is cached/throttled and does not scan the scene in `OnGUI`.

## 21. Limitations

Normal Unity Editor manual validation is still required for visible NWH physics behavior, UTS path stability, GPS continuity, Weather Maker continuity, Weatherade recentering, and streaming while driving.

The current validation asset intentionally uses a low threshold. Production tuning can be larger after route/streaming scale testing.

## 22. Prompt 016 Recommendations

Prompt 016 should make `LwsWorldPositionD` the authoritative save position payload and avoid saving raw local Unity coordinates as stable world location. Loading should establish a suitable origin offset, load required chunks by stable global IDs, then spawn the player at `GlobalToLocal(savedGlobalPosition)`.
