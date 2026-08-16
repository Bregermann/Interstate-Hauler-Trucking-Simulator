# Prompt 014 - Chunk Matrix

| Chunk ID | Scene | Z Range | Neighbors | Road IDs | Content | Persistent Content | Notes |
|---|---|---:|---|---|---|---|---|
| `IH_TEST_CHUNK_000_START` | `IH_Chunk_000_Start` | -180..650 | `IH_TEST_CHUNK_001_HIGHWAY_A` | `IH_TEST_I000_NB`, `IH_TEST_I000_SB`, `IH_TEST_I000_RAMP` | start highway, service area, ramp, lane debug | No | Startup chunk and trailer pickup/service-area validation. |
| `IH_TEST_CHUNK_001_HIGHWAY_A` | `IH_Chunk_001_HighwayA` | 560..1320 | `000_START`, `002_HIGHWAY_B` | `IH_TEST_I000_NB`, `IH_TEST_I000_SB` | highway presentation, colliders, lane debug | No | Overlaps seams with start/B. |
| `IH_TEST_CHUNK_002_HIGHWAY_B` | `IH_Chunk_002_HighwayB` | 1220..2180 | `001_HIGHWAY_A`, `003_HIGHWAY_C` | `IH_TEST_I000_NB`, `IH_TEST_I000_SB` | highway grade/curve presentation | No | Mid-corridor load/unload validation. |
| `IH_TEST_CHUNK_003_HIGHWAY_C` | `IH_Chunk_003_HighwayC` | 2080..3050 | `002_HIGHWAY_B`, `004_TURNAROUND` | `IH_TEST_I000_NB`, `IH_TEST_I000_SB` | highway return curve presentation | No | Late-corridor streaming transition. |
| `IH_TEST_CHUNK_004_TURNAROUND` | `IH_Chunk_004_Turnaround` | 2940..3560 | `003_HIGHWAY_C` | `IH_TEST_I000_NB`, `IH_TEST_I000_SB`, `IH_TEST_I000_TURN` | turnaround/crossover presentation | No | End chunk for reverse route/manual reload validation. |

## Chunk Rules

All chunk scenes:

- contain `LwsStreamedChunkSceneRoot`
- contain `LwsStreamingHighwayChunkBuilder`
- synchronize Scene Streamer neighbor scene names
- build runtime road meshes/colliders
- add `LwsRoadSurface` to streamed road presentation
- omit player, trailer, bootstrap, input, save, weather, traffic, GPS, and road-condition global service roots

Chunk overlaps are intentional. They are validation seams for tractor/trailer safety margins, not final world-generation boundaries.
