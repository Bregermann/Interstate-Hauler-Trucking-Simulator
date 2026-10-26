# Final volcanic, launch and aerial venue lighting

Q22-SKY-LIGHT-FINAL-01 now has saved, editable fixtures in its seven remaining venues. This is an additive lighting integration, not a rebuild of the courses. The original clock owns daytime zero contribution, evening/night lighting and dawn gain. Existing accumulation adapters supply snow gain where the scene has one; Terrier's intentional absence of that adapter does not create a second snow system.

| Venue | New lights | Actual support |
|---|---:|---|
| Floor Is Lava |10|Original terrain outside the collapsing deck, with modeled towers high enough to illuminate its top|
| Tug of War |4|Original solid beveled ledges; artistic lava glow retained|
| Cornhole |32|Existing isolation-wall tops across all four original courses|
| Target Launch |5|Actual modeled gantry/platform vertices; the first gantry aims toward the starting trucks|
| Terrier Racing |8|Broad original deck before the earliest randomized funnel; independent of scaled rails|
| Base Jumping |8|Four cargo-rail fixtures and four terrain-supported landing lights|
| Sky Tow |62|Actual dry terrain along the existing fjord, outside the four tow lanes|

Strict native scene review retains all51,712 original serialized records. Only render-only fixture-root children and the existing clock bindings change. No original collider, terrain, road, vehicle, camera, artistic light or scoring record changes. Seven local Forward+ renderer copies retain every original renderer feature and the existing secondary Weatherade renderer; original renderer source assets are unchanged. Original native saves are retained, and inert null fields Unity added to unrelated components are excluded from export.

The first authoring process45104 saved four venues, then exited1 because Terrier has no accumulation surface. The correction reused its intended null snow reference;33352 completed the remaining three venues and exited0/71.45s. Initial native32520 ran seven cases:5passed/2failed. Lava fixtures illuminated below the raised platform, and the Base initial exterior-helicopter shot did not exercise landing illumination. Those results and all14 initial images are retained.

The reviewed correction14288 exited0/145.16s and changed only32 newly authored fixture transform/intensity records. The second native47568 exited2/110.45s:6passed/1failed,61.841872s. Base's actual descent now exercises landing lighting, with1,803 changed light pixels in a320x180 sample; the Lava field remained too dark. Target's first light was re-aimed after visual inspection despite its passing numerical threshold. A final explicit power correction14724 exited0/36.99s and changed only13 new light-intensity records. Final native29612 exited0/55.59s:2passed/0failed,15.3621574s, covering Lava and Target.

Seven distinct venue cases therefore pass across the retained runs: the five unchanged Base/Cornhole/SkyTow/Terrier/Tug cases from47568, plus corrected Lava/Target from29612. This is not described as a single seven-test passing run. Final actual-light sample counts range1,704–37,888 of57,600pixels; Lava improves to30,405 and Target to2,528. These are controlled rendering samples, not calibrated exposure or performance benchmarks.

All14 final720p/1080p native camera/HUD captures were individually inspected and retained with hashes. Base captures active descent; other captures show the initial gameplay phase. They are not Windows screenshots or human playtests. The combined thirteen-venue candidate and focused Windows rendering pass are complete as recorded below; full-course traversal, maximum-view performance, human driving, physical controllers and subjective listening remain separate acceptance work. No release promotion or public push is claimed here.

Related six-course integration and the canonical-output incident are recorded in `2026-10-25-Mountain-Course-Lighting.md`. The canonical path currently contains the preserveda59candidate, not the old accepted60d runtime. The interrupted440source build47236 has no completion receipt. The current combined build uses442exact owned C# inputs and52reviewed scene/pipeline/renderer assets, writes explicitly to `Builds/TruckSportsCamera/TruckSports.exe`, and checks canonical full-content preservation.

## Combined Windows validation

Combined candidate248fd02f (548files,60registered scenes,442exact owned C# inputs) built in Editor42376, exit0/303.4s. Actual Windows Day/Night18600 exit0/90.62s:206checks/0errors; weather20756 exit0/88.93s:157/0; legacy52880 exit0/53.94s:124/0. Total487checks/0errors and three clean exits. All108gameplay frames reviewed through20overview sheets plus individually viewed two-human images;30setup captures excluded. Initial Minefield720p camera sky wedges remain tracked under Q22-SKY-CAMERA-MINEFIELD-START-01. Prior failed runs remain retained. Canonical full content before/after unchangeda59c4c8a9975748c3b0986703429748238087af50d51a0b9a84c8d28e4156495; this is the preserved candidate, not the unrecovered old60d accepted runtime. No canonical promotion/public push. Human/device/listening/full-course/performance acceptance remains unperformed.

## VENDOR ASSETS AUDITED

Toon Gas Station streetlights; Pixel Crushers modeled WallLamp and material; actual EasyRoads/NWH course content; URP pipeline ownership; Weather Maker time/sky; Weatherade accumulation/depth; existing authored ledges, deck, helicopter and fjord terrain.

## VENDOR ASSETS USED

Installed `Streetlight_1C.prefab`, installed Pixel Crushers WallLamp geometry/material, existing original Sports LED panel and existing shared light/clock adapters. URP Forward+ retains original vendor renderer features. Existing vehicles, roads, terrain and artistic effects remain authoritative.

## RELEVANT ASSETS NOT USED

No replacement vehicle, road, terrain, weather, traffic, UI or time system was required. No external model acquisition or new vendor dependency was introduced.

## CUSTOM SYSTEMS CREATED

No new runtime system. Existing owned authoring is split into a partial class with explicit support-aware placement and guarded reviewed corrections. Focused scene tests, source/artifact preservation and evidence tooling were extended.

## VENDOR SOURCE MODIFIED

None. Truck Taxi source and original vendor assets remain unchanged.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

None. Native URP lights and existing Weather Maker/Weatherade/LWS ownership remain in place.
