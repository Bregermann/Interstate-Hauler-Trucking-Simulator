| Area | Prompt 010 Guard | Current Setting | Validation Status | Notes |
|---|---|---|---|---|
| Spawn count | Active NPC cap | 8 | Implemented | Prevents runaway traffic. |
| Spawn cadence | Interval gate | 4 seconds | Implemented | `_nextSpawnTime` gates spawning. |
| Player proximity | Minimum spawn distance | 140 m | Implemented | Avoids spawning on top of player. |
| Despawn | Distance from player | 850 m | Implemented | Removes distant NPCs when player exists. |
| Lane-end cleanup | End proximity despawn | 90 m | Implemented | Prevents UTS loop/end behavior from becoming the main lifecycle. |
| Debug UI | Cached stats | 0.25 s refresh | Implemented | No per-frame scene scans in panel. |
| Player lookup | Throttled fallback | 2 s retry | Implemented | Only used until player transform is resolved. |
| Scene searches | Broad searches in traffic loop | None | Implemented | No `FindObjectsByType` or `Resources.FindObjectsOfTypeAll` in traffic update loop. |
| Traffic density | Sparse default | Sparse | Implemented | Dense traffic deferred to later performance pass. |
| Unity Editor profiler | Normal Editor measurement | Manual required | Pending | Codex cannot profile the live Editor scene here. |
| Steam Deck | 30 FPS with traffic | Manual required | Pending | Requires hardware/profile testing later. |
