# Truck Sports original logo sources

The 41 `Truck*.json` files record the selected PNG's title, built-in image-generation prompt, revision and provenance. Final artwork lives in `Assets/TruckSports/Art/Logos/<scene>.png`. The earlier aggregate `LogoGeneration.json` is historical draft provenance; the per-scene records describe the selected revisions.

Current direction, requested October 20: expressive 1997–2002 console mascot/party-game lettering, chunky asymmetric proportions, thick dark outlines, cream keylines, broad painted color planes and faceted toy semi-trucks. Keep each sport's own imagery and silhouette. Avoid photographic materials, excessive chrome, fine noise and modern glossy rendering. These are crisp HD images, not pixel art.

The final Dock Diving PNG is the shared style reference. Use the existing final image as the edit target for further revisions. Preserve alpha, accurate titles and transparent margins. Do not substitute icons or a runtime font for the finished logos.

`SportsRetroMenuAuthoring` imports these as single sprites, clamps wrapping, disables mipmaps, preserves aspect and limits them to 1024 pixels with high-quality compression. Its explicit upgrade preserves existing launch actions, crew configuration and descriptions. `SportsSportLogoAuthoring` adds setup/help branding and replaces suitable existing world-title signs; it never regenerates courses or gameplay.

Normal builds consume the saved scene references. They do not generate artwork, replace PNGs, recapture screenshots or reset designer settings. Edit the PNG through an image-art workflow, retain its `.meta` GUID, and run the explicit menu upgrade only when its layout/references need updating.

Built-in image generation initially saves outside the repository; selected files have been copied into the project. Provenance locations in JSON are not runtime dependencies or build output directories. No new vendor artwork or vendor source was modified for these logos.
