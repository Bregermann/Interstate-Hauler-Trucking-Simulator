# Prompt 006 - Transmission Test Matrix

| Area | Automated Coverage | Manual Editor Coverage | Current Result | Notes |
|---|---|---|---|---|
| 18 forward ratios | EditMode definition test | Validate Project | Implemented | Ensures exactly 18 valid forward mappings and unique NWH gear indexes. |
| Low/crawler split | EditMode mapping test | TruckValidation drive test | Implemented | Gate 2 low range maps to LO-L and LO-H. |
| Low range 1-4 | EditMode mapping test | TruckValidation drive test | Implemented | Gate 3-6 low range maps 1L through 4H. |
| High range 5-8 | EditMode mapping test | TruckValidation drive test | Implemented | Gate 3-6 high range maps 5L through 8H. |
| Invalid high crawler | EditMode mapping test | Validate Project | Implemented | Gate 2 high range is rejected. |
| Neutral | EditMode input test | TruckValidation drive test | Implemented | Neutral remains semantic and maps to NWH gear 0. |
| Reverse | EditMode input test | TruckValidation drive test | Implemented | Reverse remains semantic and maps to NWH gear -1. |
| Range preselection | Definition/controller logic | Manual required | Implemented with manual verification required | Engaged range changes only after neutral. |
| Splitter preselection | Controller logic | Manual required | Implemented with manual verification required | Assisted mode engages immediately; hardcore mode requires clutch/RPM sync. |
| Clutch threshold | EditMode shift evaluation test | Manual G29 clutch test | Implemented | LWS convention is 0 released, 1 fully depressed. |
| Float shift tolerance | EditMode shift evaluation test | Manual road test | Implemented logic | Hardcore behavior requires manual validation under NWH load. |
| Abuse detection | EditMode overspeed test | Manual road test | Implemented logic | Emits events; damage remains deferred. |
| Money-shift prevention | EditMode overspeed test | Manual road test | Implemented logic | Assisted mode blocks severe/catastrophic overspeed downshifts. |
| NWH 18-gear configuration | Validator source/asset check | TruckValidation Play Mode | Implemented with manual verification required | Runtime adapter reconfigures spawned NWH instance only. |
| Prompt 005 direct mapping disabled | EditMode/PlayMode G29 tests | Validate Project | Implemented | Validation-only Gate1-6 to NWH 1-6 mapping is off by default. |
| Save payload | EditMode serialization test | Manual save/load later | Implemented shape | Stores logical state and restore synchronization flag. |
| Scene reload | Play Mode manual required | TruckValidation reload | Manual required | Requires normal Unity Editor validation. |
| Physical G29 shifting | Not automatable | Actual G29 hardware | Manual required | Prompt 006 consumes LWS shifter state; physical discovery remains Prompt 005. |
| Cab feedback | Debug panel only | Manual required | Deferred | Prompt 008 should connect physical cab feedback/dashboard. |

## Manual Test Sequence

Use the normal Unity Editor, not the known faulty batchmode route.

1. Open `Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity`.
2. Enter Play Mode.
3. Confirm the 18-speed debug panel appears when validation debug UI is enabled.
4. With the shifter in neutral, toggle range and confirm requested/engaged range can synchronize in neutral.
5. Select Gate 2 low range and test splitter low/high.
6. Drive through Gate 3-6 low range, both splitter positions.
7. Return to neutral, select high range, then drive through Gate 3-6 high range.
8. Confirm Gate 2 high range is rejected.
9. Test reverse only at safe low/no forward speed.
10. Check Console for genuine LWS errors.

## Status

Automated source-level coverage has been added. Full driving validation remains manual because it requires the normal Unity Editor, NWH runtime physics, and ideally the physical G29/shifter stack.
