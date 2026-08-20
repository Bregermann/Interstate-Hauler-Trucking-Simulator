# Prompt 015A - 50-Mile Test Matrix

| Test Area | Validation | Method | Expected Result | Status |
| --- | --- | --- | --- | --- |
| Exact distance | 50 miles equals 80,467.2 m | EditMode test and model constants | Total length and Mile 50 conversion match exactly | Automated |
| Chunk count | 25 streamed chunks | EditMode test and validator | `IH_50MI_CHUNK_000..024` exist and are unique | Automated |
| Chunk length | 2 miles each | EditMode test and validator | Every chunk is 3,218.688 m long and contiguous | Automated |
| Whole-mile markers | Mile 0 through Mile 50 | EditMode test and chunk builder | 51 unique whole-mile markers are assigned to chunks | Automated |
| Global/local math | Chunk local placement after origin shift | EditMode test | Local chunk position equals global minus origin offset | Automated |
| Origin validation API | Teleport-style offset set | PlayMode test | Global mile remains stable after changing origin offset | Automated |
| Streaming scope | Mid-corridor neighborhood load decision | PlayMode test | Current chunk is 012 at Mile 25 and desired set is less than all 25 chunks | Automated |
| Road graph | Mile 0 to Mile 50 route | EditMode test | Eastbound route solves to 80,467.2 m | Automated |
| Road graph provider | Runtime provider accepts graph | PlayMode test | Provider stores `IH_50_MILE_GLOBAL_ROAD_GRAPH` | Automated |
| Build settings | Master and 25 chunks included | Validator | Editor Build Settings contains all 26 scenes | Automated |
| GPS runtime | Route starts on scene launch | Manual Editor play | GPS route is active and remaining distance decreases while driving | Manual required |
| Floating origin shifts | Long drive crosses threshold repeatedly | Manual Editor play | Origin offset increments while local truck position stays bounded | Manual required |
| NWH truck drive | Automatic W/S/A/D driving | Manual Editor play | Truck starts, accelerates, shifts, brakes, and remains stable | Manual required |
| Trailer stability | Long straight drive with trailer | Manual Editor play | Trailer remains coupled and stable under ordinary driving | Manual required |
| Traffic streaming | UTS active vehicles around player | Manual Editor play | Around 12-20 vehicles appear when spawn points are valid; distant traffic despawns | Manual required |
| Weather cycle | Weather changes by global mile | Manual Editor play and quick teleports | Weather Maker receives global weather requests at mile bands | Manual required |
| Road conditions | Weatherade/NWH condition handoff | Manual Editor play | Wet/snow/ice condition state follows global weather and remains origin safe | Manual required |
| Quick teleports | Mile 0/10/25/40/49 buttons | Manual Editor play | Teleport moves truck safely, aligns origin, updates streaming, and refreshes GPS/weather | Manual required |
| End report | Mile 50 completion report | Manual Editor play or Mile 49 plus final drive | Completion report logs distance, shifts, loads, traffic, weather, GPS, and last error | Manual required |
| Console health | No repeating LWS errors | Manual Editor play | Console remains free of new LWS error spam during long-drive test | Manual required |

Physical driving and visual/performance checks must be verified in the normal Unity Editor. The older batchmode vendor validation path remains non-authoritative for this project.
