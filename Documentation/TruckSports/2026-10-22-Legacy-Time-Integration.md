# Legacy match time integration — October 22–23, 2026

Q22-SKY remains **PARTIAL**. Cornhole, Cheese Rolling and Base Jumping now use the same saved Random/Morning/Day/Evening/Night preference and Weather Maker sky as the 35 previously integrated compatible outdoor scenes. Remaining outdoor venues, venue-specific lighting, complete forecast integration, authoritative scoring celebrations and results remain open. Carnival remains queued after Q22-SKY.

## Implemented

All three saved setups expose a real installed Heat time selector. Cornhole keeps its installed weather selector beside the clock. Cheese and Base retain their original weather choices, gust forces, traction, aircraft/terrain and scoring; this milestone does not claim that they now expose all 33 weather presets. Runtime loads and builds do not regenerate these controls or replace designer selections.

Cornhole and Cheese expose their existing match identity to the same clock coordinator. Cornhole allocates that identity before applying its forecast. Reconfiguring a heat/attempt with a new seed keeps the active match's time, while starting a new match creates a new identity. Source day/night profiles are copied, never edited by gameplay.

Cheese's existing look owner previously cloned its procedural sky at startup and kept assigning the fixed noon sun. It now receives the owned vendor-backed sky template, uses the vendor sun and applies procedural-only tint only to materials that support it. Mushroom grading and wave effects remain in place. Cheese and Base's sport-owned URP renderers previously contained only the mushroom feature or no feature. They now include the same installed Weather Maker render feature and documented forward/intermediate configuration used by the other outdoor sports. No vendor/shared renderer was edited. The first Windows payload passed logical checks but exposed stars below the horizon around the elevated courses. It was rejected; the owned elevated-horizon profile now uses Weather Maker's public ground-color gradient to mask the lower hemisphere, with no vendor shader changes.

The masked lower hemisphere initially remained too bright because Weather Maker's inactive night sun reports a gradient lookup of 1. The existing match-time adapter now freezes the vendor's public ground tint from the selected clock in these three venues, with a runtime-owned sky-profile copy and source restoration on cleanup. The final native test checks the actual dark nighttime gradient as well as opaque ground coverage.

## Actual verification

Six targeted native PlayMode tests passed, covering actual saved legacy starts across four explicit times plus Random, fresh match identities, same-match forecast refreshes, source profile preservation, actual renderer feature presence and runtime sky material, Cheese's retained mushroom feature/HUD lifecycle, Base flare pooling and the existing 35-scene shared time/orthographic regressions. After the horizon correction, three focused legacy/HUD/flare tests passed again, including vendor ground-gradient coverage. An earlier five-test pass preceded the renderer-feature assertions and is retained separately.

The exact accepted Windows payload passed **122 checks / 0 errors** across two fresh processes: 121 legacy scene checks and one saved-preference restart check. Three four-AI setups, all four explicit times, two software-keyboard human views with AI cameras off, and return to the actual main menu were exercised. The run captured 36 actual backbuffers at 720p/1080p and 12 blocks of 60 whole-player frame intervals. These are short starts and rematches, not complete-match, sustained-performance, listening, physical-controller or human-playtest acceptance.

Build: `Builds/TruckSports/TruckSports.exe`, 60 scenes / 546 files. Full payload SHA256: `c8034e0aecd032a835e4ee0f4f92f6ac4fdb03b703f27ea2f01145a0e6432efd`. Managed SHA256: `48418096bcd5e12f5f2f04d1736d0ed49e8ecbad461ac64038e09a127a22e179`. Exact 292 owned C# inputs are archived with the payload identity. The prior accepted build is preserved by the promotion receipt.

## Assets and preservation

Scenes: Assets/TruckCornhole/Scenes/TruckCornhole.unity; Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity; Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity.

Owned assets: Cheese and Base `Materials/Weather Maker viewport sky.mat`, their existing sport-owned renderer assets and `Sports elevated horizon sky.asset`, derived from the shared atmospheric profile with an opaque vendor ground gradient. No duplicate weather world or original vendor character/vehicle asset was created. The new LegacyMatchTimeTests and development-only Windows probe provide targeted acceptance.

The scene review retains all original document IDs, collider/rigidbody data and original world poses. Existing renderer feature IDs remain. All 925 unrelated pending files are preserved. No Truck Taxi/vendor source was changed. Protected Plowing rosters/moods, Poker and concert lighting were untouched.

## Remaining work

Remaining applicable outdoor venues still need integration. Cheese and Base retain their three original forecasts; broader registry selection is not completed here. All scene-specific lamps/headlights/reflections/exposure and snow/tyre tracks still require their own acceptance. The elevated venues still need artistic horizon/surrounding-environment acceptance; using the correct dark vendor ground does not establish finished scenery. Stable-ID score events, tiered celebrations and results/statistics remain open under their existing ledger IDs. Concurrent source-only scoring work is outside this frozen legacy payload and commit. No full prompt completion or hardware acceptance is claimed.

VENDOR ASSETS AUDITED: Installed Weather Maker sky/day-night/cloud/URP APIs and profiles; existing Heat dropdown; Cheese's existing mushroom-wave renderer; saved Cheese/Base/Cornhole vehicle, weather and replay lifecycles.

VENDOR ASSETS USED: Weather Maker remains the sole sky, celestial light and weather renderer; Heat provides the selectors. Existing vehicle/physics owners, Cheese mushroom presentation and Base flare pools remain in use.

RELEVANT ASSETS NOT USED: No new Weatherade accumulation or road/navigation tool is needed for this clock/render integration; broader snow acceptance remains open. DOTween's existing menu animation remains in place; no replacement animation framework was added.

CUSTOM SYSTEMS CREATED: Narrow existing-owner time hooks, additive saved-scene/renderer integration and focused validation.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None.
