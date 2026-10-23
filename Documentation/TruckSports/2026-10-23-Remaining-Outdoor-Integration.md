# Curling / Skee Ball environment integration and grounded fixtures — October 23

Q22-SKY remains **PARTIAL**. This milestone extends shared weather/time presentation to Curling and Skee Ball and corrects floating sport-owned night fixtures in Bungee Demolition and Fishing. Existing gameplay and the historical backlog are preserved.

## Implementation and root causes

Curling and Skee Ball had fixed daylight and no weather lifecycle. Their saved scenes now opt into the already delivered `SportsEventSession.presentationWeather` hook, Heat weather/time selectors, installed Weather Maker profile registry, frozen match clock, sky/cloud rendering and night vehicle/venue lighting. Scene-owned renderer clones preserve original features; no second rendering or weather authority is added. All authored driving/replay/spectator rigs are registered. This presentation pass adds no new grip, projectile force or scoring rules.

Curling intentionally has circular MeshColliders and no truck BoxCollider. Headlight authoring now calculates visual bounds when the box is absent, preserving the circular footprint. Six side fixtures cover the extended sheet. Skee Ball retains its four equivalent courses and existing hot-seat/simultaneous controls. Its four fixtures per lane are placed and aimed separately for the approach and inclined target, keeping rings readable at night. Fixture geometry uses the tracked Toon Gas Station streetlight model without new route colliders or shadow-casting lights.

Bungee and Fishing fixtures had been placed at their aggregate renderer bounds' top, which includes towers or trees. The explicit correction moves all 16 owned pole bases in each scene to the audited venue-root ground elevation. Only those 32 original Transform positions and the 16 owned Fishing light intensities change; no original gameplay object, material, collider or Rigidbody is changed in these two scenes. Fishing floods are softened to 1200. Future fixture authoring uses the root elevation for these assemblies and Skee Ball. Normal builds/Play Mode never run these authoring methods or regenerate designer settings.

The first Windows candidate passed 81 checks but visual review found the Skee ramp bright while the target was dark, and Fishing's floods too strong. The second also passed 81 checks, but inspection revealed Skee fixtures above/outside the lowered return platform and an overbright target. Final Skee poses use the actual floor's top surface and inner footprint, with 800-intensity target floods; the native test now asserts a real floor beneath every fixture. Those candidates' captures/receipts remain in local `RemainingOutdoorsWindows` and `RemainingOutdoorsFinalWindows`; final acceptance uses the rebuilt payload below. The final four-scene native test follows the fixture refinements; the 45-scene regression preceded them.

## Actual validation

| Check | Observed result |
|---|---|
| remaining-outdoors-grounded-native | 1 PlayMode test passed; 11.53s test time |
| outdoor-45-regression-native | 1 PlayMode test passed; 104.51s test time |
| RemainingOutdoorsAcceptedWindows | 81 checks / 0 errors; 35.56s; PID 21420 |

The focused native test exercises the four actual saved scenes, maximum supported rosters, Day/Night starts, single lifecycle/render owner, camera bindings, Heat layout and pause/resume. It explicitly checks the 32 fixture bases, all 16 circular Curling stones and six sheet lights. The regression exercises all 45 compatible shared outdoor sessions, separately from indoor, protected Plowing and legacy-owner scenes.

The Windows run uses the exact executable and actual backbuffers at 1280×720 and 1920×1080: four setup views and eight Day/Night starts, 24 captured frames. The setup and night views were inspected before promotion. These short starts are not complete matches or human driving. Eight blocks record 480 whole-player frame intervals: medians 6.25–6.25ms; p95 6.27–12.47ms. The application cap is included; this does not isolate CPU/GPU cost or establish sustained performance.

The scene review preserves all original documents and physics components. Original world poses remain unchanged except the 32 explicitly reviewed fixture bases. 925 unrelated pending files were rehashed and preserved. Exact added/changed asset scope and native/player receipts are in `Evidence/2026-10-23-Remaining-Outdoors/`.

## Build

`Builds/TruckSports/TruckSports.exe` and all companion files. Full payload SHA-256 `8062514a6e016e33d2a481bbd0a137f3e299ea2ca15bb177be17f8fec54e5e8f`; managed SHA-256 `76bae8fe8c6a64152be3f683e8c3c6ab30efb12be82a2bba994504aa188e9707`; 547 payload files, 60 saved build scenes including the menu. Exact compiled source inputs are archived with local build evidence; the previous accepted payload is retained in Recovery.

## Remaining acceptance

Hockey/Wallball's legacy lifecycle integration, expanded legacy Cheese/Base forecasts, additional venue-specific artistic lighting/reflections, snow accumulation/tyre tracks, recorded-replay headlights, collection-wide score publishers and new results presentation remain open. Human, physical-controller/wheel, listening, complete-match and sustained CPU/GPU/memory acceptance are not claimed by this milestone. Existing score and earlier sky evidence retain their individual limits. Carnival remains queued after Q22-SKY.

## VENDOR ASSETS AUDITED

Installed Weather Maker sky/day-night/weather profiles and URP feature; existing NWH lighting interfaces; Heat dropdowns; Toon Gas Station streetlight prefab; existing scene-scoped renderer and Weatherade integration boundaries.

## VENDOR ASSETS USED

Existing Weather Maker prefab/registry/sky/cloud/sun/moon/precipitation/URP feature, Heat widgets, NWH lighting through the existing adapter, Toon Gas Station fixture. No new vendor dependencies published.

## RELEVANT ASSETS NOT USED

Weatherade accumulation is not newly authored here; sport-specific surface masks and track acceptance remain open. Original NWH/WheelController, camera and match systems are retained.

## CUSTOM SYSTEMS CREATED

None. Existing LWS explicit authoring, vendor adapters and validation were extended.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
