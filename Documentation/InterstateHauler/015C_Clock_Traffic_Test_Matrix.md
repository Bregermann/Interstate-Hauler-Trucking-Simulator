# Prompt 015C - Clock / Traffic / Endless Streaming Test Matrix

| Area | Test | Expected Result | Automation |
|---|---|---|---|
| Road end root cause | Inspect Prompt 014 streaming graph/manifest | Finite five-chunk corridor explains road ending | Documented |
| Endless road | Drive beyond original five-chunk distance | Road continues and physical pool recycles | Manual required |
| Segment IDs | Generate segment 42 | `IH_ENDLESS_TEST_SEG_000042` | EditMode |
| Road ahead | Player near highest generated segment end | Unsafe threshold reports true | EditMode |
| Graph window | Build graph from segment 8 for 7 slots | Valid graph with 14 one-way interstate edges | EditMode |
| Floating origin | Recycle after origin offset | Local slot position equals global start minus origin | EditMode |
| Clock advance | 20x for 3 seconds | Game advances 60 seconds | EditMode |
| Clock pause | Paused at 60x | Time does not advance | EditMode |
| Day rollover | 23:59:30 plus 60 seconds | Date rolls to next day | EditMode |
| Time wrap | Set 25:00 | Time becomes 1:00 AM | EditMode |
| Weather sync | Weather Maker adapter reads game clock | Weather presentation time follows LWS clock | Manual/PlayMode |
| Traffic hierarchy | 3 AM, 1 PM, 5 PM | overnight < daytime < peak | EditMode |
| Traffic cap | Maximum active set to 12 | demand target clamps to 12 | EditMode |
| Traffic replenishment | Set 8 AM / 5 PM | target active and nearby target rise | Manual required |
| Night traffic | Set 3 AM / 11 PM | traffic is light but not empty | Manual required |
| Fast clock | Set 60x | traffic demand changes while road streaming remains normal | Manual required |
| GPS | 10/25/50 mile buttons | finite forward routes solve normally | Manual required |
| Long run | 15-30 minute drive | no road end, no repeated LWS console spam | Manual required |

## Manual Long-Run Sequence

1. Open `Assets/LWS/InterstateHauler/World/Streaming/Validation/StreamingHighwayValidation.unity`.
2. Enter Play Mode.
3. Confirm the HUD shows `MON 8:00 AM` or later.
4. Open Development Control Center.
5. Traffic tab: confirm target active and nearby values are nonzero.
6. Streaming tab: confirm road ahead is above the emergency threshold.
7. Drive continuously for at least 15 minutes.
8. Confirm road geometry continues.
9. Confirm chunks recycled increases.
10. Confirm traffic remains populated.
11. Set clock to 60x and continue driving for several minutes.
12. Confirm Weather Maker day/night and traffic demand follow the clock.
