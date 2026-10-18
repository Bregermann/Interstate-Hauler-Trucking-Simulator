# Truck Sports: Party! — original wordmark refresh

October 18, 2026. This is an artwork and front-end source milestone. The canonical Windows executable still contains the previously accepted Target Launch milestone; this logo has **not** yet been delivered in a new player build. Field Goal and the broader queued repairs remain in progress.

The first glossy italic treatment was rejected by the user. Its replacement uses custom, uneven, wedge-ended lettering, painted yellow faces, blue dimensional sides and a red-orange PARTY tier. Truck exhaust stacks frame the top. The O in SPORTS is an 8-ball, and the exclamation combines a bowling pin with a cheese-wheel dot. The logo is original; no reference-game characters or logo artwork ship with it.

## Reference study

Actual reference images were downloaded and visually inspected in ignored project-local research logs. The useful qualities were the original Crash title's chunky uneven letters, Jersey Devil's flared angular strokes, Spyro's sculpted contours, and Sly's contrast and dimensional hierarchy. These are design observations, not claims about the original designers' intentions.

- [Crash original title-screen reference](https://www.standard.co.uk/culture/gaming/crash-bandicoot-iconic-moments-for-fans-a4476256.html)
- [Jersey Devil original packaging reference](https://www.thevideogamecompany.com/products/jersey-devil-sony-playstation-1-ps1)
- [Spyro logo reference archive](https://gamesdb.launchbox-app.com/games/images/4730-spyro-the-dragon)
- [Sly Cooper original title presentation](https://store.playstation.com/en-us/product/UP9000-PPSA21300_00-SCUS971980000000)
- [Sucker Punch's retrospective on its graphic approach](https://blog.playstation.com/2022/09/23/sly-cooper-celebrates-20-years-today/)

## Assets and integration

`Assets/TruckSports/Art/Brand/Truck Sports Party wordmark.png` is the selected 1536×1024 RGBA image, generated with the built-in image-generation tool. Transparent background pixels were inspected; Unity imports it as a single Sprite with alpha transparency, clamped edges, no mipmaps and uncompressed color. The original generation and cleanup prompts are in [BrandEvidence/Prompts.md](BrandEvidence/Prompts.md).

`SportsBrandAuthoring` imports the sprite and upgrades the existing title page without replacing its buttons or action bindings. The saved `Assets/TruckSports/Scenes/TruckSports.unity` uses the mark on the left and its four existing main actions on the right. Your Crew remains available. The old text title is disabled. Existing Heat controls, rounded button presentation and DOTween page transitions remain in use. `SportsBuilder.CreateFront` and `SportsExpansionFront.Upgrade` retain the new mark during explicit authoring. Normal builds do not regenerate the front end.

## Validation

The explicit Play Mode integration fixture passed **12 checks, zero runtime errors**: one visible title page, wired noninteractive image, retired text heading, the five existing title actions retained exactly once, catalogue access with all 32 entries, Back, Settings and clean return with one AudioListener. These are automated menu-method/lifecycle checks, not a physical-controller or human playtest.

Initialized uGUI was captured with the existing controlled-render helper at **1280×720 and 1920×1080**; both images were visually inspected. They confirm legibility, transparent compositing, no clipping and separation from controls. They are explicitly controlled Editor renders, not Windows-player screenshots. The earlier uninitialized edit-time render showed an unrelated active Scrap Heap overlay and was rejected as acceptance evidence.

See [result.txt](BrandEvidence/result.txt), [720p](BrandEvidence/title-1280-controlled.png), and [1080p](BrandEvidence/title-1920-controlled.png). Build and actual hardware checks remain for the combined delivery. Scoped commit/publication outcome is recorded in ignored `Logs/TruckSports/BrandStudy/commit.json`; never infer a new executable from a source commit.

## Vendor accounting

- **VENDOR ASSETS AUDITED:** Heat production UI, existing DOTween menu motion, installed placeholder branding and existing Sports front-end authoring.
- **VENDOR ASSETS USED:** Existing Heat/uGUI controls and existing DOTween page fades; their behavior is preserved.
- **RELEVANT ASSETS NOT USED:** Vendor placeholder logos are unrelated brands and do not provide the requested original game identity. Historical reference artwork is research only.
- **CUSTOM SYSTEMS CREATED:** One project-owned Editor import/title integration adapter and original raster wordmark. No replacement UI or animation framework.
- **VENDOR SOURCE MODIFIED:** None.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** None.
