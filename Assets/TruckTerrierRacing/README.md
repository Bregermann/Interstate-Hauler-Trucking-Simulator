# Truck Terrier Racing

Twelve physical NWH trucks race seeded hurdles into one finish tunnel. Up to four offline humans use the collection input assignments, shared controller sticks, and per-source jump bindings. AI fills the other slots. The first racer to five heat wins takes the match; every starting stall rotates across twelve heats.

Movement drives. Opposite throttle brakes before reverse. Action jumps while grounded, with a 0.85-second cooldown and the same 6.2m/s hop for humans and AI. The default is a stable driver-seat viewport per human; Shared Broadcast is a setup option. AI-only uses one broadcast camera. A wrecked player's view follows a live racer.

Only ordered front entry followed by full rear exit through the 2.7m-wide, 3.2m-high, 2.8m-deep tunnel counts. Going above/around the finish or entering from the side earns nothing. Finishes are resolved by interpolated crossing time rather than callback order. Equal timestamps rerun without a point. Wall impacts use incoming wall-normal speed (10.5m/s threshold); ordinary grazes and truck bumps do not explode racers. Explosions are bounded. All-wreck or 28-second no-finish heats rerun; repeated failures select the gentler validated layout. No score injection or catch-up teleports.

Each heat records actual body and visual-part poses into a bounded replay buffer. Replay ghosts contain no rigidbodies/colliders, replay cannot score, and recorded crashes are visual events. The shared camera presents the start and decisive segment, slows near the finish, and offers angle/speed/rewind/skip controls. Skip unlocks after one second. AI-only automatically advances. Pause and reconnect preserve progression and replay state.

Authoring: `LWS.TruckTerrierRacing.Editor.TerrierAuthoring.Create`. Saved scene: `Assets/TruckTerrierRacing/Scenes/TruckTerrierRacing.unity`. Combined build uses `LWS.TruckSports.Editor.SportsBuilder.Build`, with an explicit Sports-only scene list and restored host settings.

Validation is in progress. Rule fixtures and saved references have passed in the native Editor. Full-match physical, layout, build and hardware acceptance are not yet complete; see the active Truck Sports checkpoint. The supplied YouTube reference was inaccessible and no footage was viewed.
