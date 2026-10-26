# Eight party venues: readable night lighting — October 24, 2026

Balloon Battle, Hot Potato, Whack-a-Mole, King of the Hill, Tetherball, Magnet Mayhem, Volleyball and Seesaw now have saved venue-specific artificial illumination controlled by their existing Weather Maker match clock. Original clock lamp lists were empty; actual pre-fix Windows night images showed dim play surfaces and isolated truck-headlight patches.

| Venue | Saved fixtures | Placement |
|---|---:|---|
| Balloon Battle | 6 | Existing outer seating tiers; subtle pink-white light |
| Hot Potato | 6 | Existing outer seating tiers; warm light on dark asphalt |
| Whack-a-Mole | 6 | Existing outer seating tiers; warm arena illumination |
| King of the Hill | 16 | Actual paired elevated bridge rails, aiming at adjacent plateaus |
| Tetherball | 6 | Outer seating; cool-white light preserves court/rope contrast |
| Magnet Mayhem | 6 | Floor perimeter outside collection bays, between warehouses |
| Volleyball | 4 | Sand court perimeter; warm poolside-style lighting |
| Seesaw | 6 | Fixed overhead gantry underside; no attachment to the moving deck |

The purchased Union Field seating tilts eight degrees. Mounting poles at the top AABB corner would float them; placement follows the actual top surface instead. Hill fixtures use the real local rail top and bridge endpoints, and Seesaw housings touch the fixed gantry. All new fixtures are render-only with non-shadow spotlights. No original physics, camera, truck, scoring, weather-profile, replay or rule records were changed. Every original scene document is retained; each scene changes only the existing clock's lamp references and one parent child list. Inert newly serialized null/default fields were restored exactly.

Implementation is an explicit additive editor command in SportsEnvironmentAuthoring. It preserves already-populated light arrays and existing fixture roots. Builds, entering Play Mode and rematches do not regenerate fixtures. Shared assets comprise one sport-owned URP adaptation of the installed streetlight material, one original nine-LED detail mesh/material for the purchased wall-lamp housing, and Unity metadata. No new runtime light manager was introduced.

## Actual validation

* Pre-fix Windows:161checks/0errors, actualexit0. Eight720pNightframes individually reviewed; other setup/day/1080p captures retained without claiming inspection.
* Initial native8PASS, then visual review found some dark surfaces still weak. Forty new-light intensities were tuned; original scene records were untouched. Initial source/scene ZIP, XML and16images remain retained locally; only eight initial720pimages were inspected.
* Final native8PlayModePASS,79.449499s test time, actualEditor38468exit0. Checks cover all eight saved venues, actual supported mounts, zero added colliders/bodies/joints/gameplay scripts, Night/Evening on, Day/Morning off, same-match clock latching, pause and rematch lamp identities. Same frozen original camera with lamps on/off brightened35471–57319of57600pixels. All sixteen final controlled720p/1080p active-gameplay frames inspected. Ordinary countdown timing is retained.
* Existing time/authoritative-score EditMode contracts8PASS, actualEditor32916exit0. Only the editor authoring intensity expression was reconciled after native rendering to reproduce the saved tuning; no runtime/test/scene behavior changed afterward.
* Exact final Windows:161checks/0errors, actualPID35976exit0/133.82s. Original environment probe exercised Day/Night setup/start, frozen clocks, forecast state and menu return across eight venues. All32actual Day/Night720p/1080p gameplay frames individually inspected; setup frames are not credited as reviewed. These focused starts do not establish full matches, human driving, physical controllers, subjective audio or isolated performance acceptance.
* Combined Windows x64 development build:60saved scenes; actual build summary retained.426owned C# sources frozen and archived. The managed gameplay assembly is byte-identical to the preceding d03c candidate; this increment changes saved lighting assets and editor/tests only. Full content bed4b2b7bcfbdd8357da3438a35a451fab432481cddac325e078fed324aa8299; managed bcbe8474574d8d411fc0d1722e134c80b290ced6a395317e5c48aff5aba59020.

Candidate: Builds/TruckSportsCamera/TruckSports.exe. No promotion: accepted Builds/TruckSports/TruckSports.exe remains60d4716f. Previous d03c candidate is fully retained at .sports-validation/Recovery/StadiumCabCandidateBeforePartyVenues-20261024. The separate native Dock shutdown blocker remains on the user-retained Editor.925unrelated pending files remain byte-identical. Public branches remain at the already-approved3a337c13; no public push of this increment. Parent Q22-SKY, completeCarnival0.6 andImpossible0.7 remain unfinished.

## VENDOR ASSETS AUDITED

Weather Maker match-time and sky controls; installed Toon Gas Station Streetlight_1C, Modern Restaurant Street_Lamp, Pixel Crushers WallLamp FBX and actual Room-prefab material assignment; original venue geometry and rendering adapters.

## VENDOR ASSETS USED

Toon Gas Station modeled streetlights; Pixel Crushers WallLamp housing with its actual vendor-authored URP material; existing shared material adaptation/copy tooling, original Weather Maker match clock and ordinary URP non-shadow spots.

## RELEVANT ASSETS NOT USED

Full vendor Room prefab would import unrelated room/gameplay geometry. Restaurant freestanding lamps do not fit Seesaw's suspended structure. Existing render-only copies use the appropriate model/material without unrelated components. No alternate weather, physics or camera framework was needed.

## CUSTOM SYSTEMS CREATED

Explicit venue authoring and focused native acceptance. One shared nine-LED mesh/material supplies the missing luminous detail on the installed housing. No new runtime lighting/weather/physics/camera manager.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
