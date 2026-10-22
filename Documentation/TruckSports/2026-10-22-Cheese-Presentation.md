# Cheese Rolling presentation and regression delivery — October 22

The existing mountain, speed, steering, NWH suspension, scoring, sheep damage, recovery and earned starting boost remain authoritative and unchanged. HPR-17 adds saved presentation rather than rebuilding the course.

## Implemented

- Eight original explosion-pool entries now play a six-layer owned GAPH Particle Ingredient Pack effect: smoke, flame, flash/sparks and trails. A local URP material conversion and stripped visual-only prefab supply the artwork. Original root simulation, blast damage/forces, sounds, physical debris and recovery safety stay unchanged. No cosmetic startup blast; effects clear and reuse their original instances. Visual review caught a legacy demo point light whose fade script had been stripped; the runtime adapter now disables/removes it before activation, and future authoring strips it. No permanent pooled demo lights compete with the original weather.
- Four original cats now use the owned Milo/MalberS generic rig with actual idle, look-around, scratch and yawn clips, smooth Animator transitions, varied cadence and truck-nearby looks. Original seeded placement, orange material, scale/grounding and capsule obstacles stay intact. Ambient animation does not move collision geometry. Walking/running or roaming behavior is not claimed.
- Saved uGUI racer panels show original portraits/crew colors/names, catches/progress, explicitly labelled distance-based CHASE order, actual MPH, round/time and three compact opponent totals. Existing countdown/earned-boost ring and feedback remain. The existing outlined cheese projection now has a dark rounded backing. Graphical result cards read original catches and winner. DOTween catch feedback respects reduced motion.

## Validation performed

- Five EditMode tests passed, including repeat-authoring preservation and the original 3,520 terrain, input, starter-grid, boost and scoring assertions.
- Three PlayMode tests passed: real cat skin animation (6,202/7,557 baked vertices change), pause/root/collider stability; two pool reuses with 665 actual particles each and no startup/damage effects; HUD read-only scores/poses, pause and rematch lifecycle.
- Windows checks: 341/0 presentation and 86/0 grounded-start regression, 427/0 total, on exactly the same executable/assets. Forty human/AI-camera configurations across 720p, 1080p, 4:3 and ultrawide; rendered glyphs/owner bounds; actual autonomous first-to-five physics match; real sheep explosions; results, rematch, pause/resume. The existing start probe repeats all 1–4 participant grids three times, checks all real wheel contacts and neutral release, preserves earned-boost timing and runs a physical AI match.
- All 5,574 original scene documents retained; 581 owned UI/configuration documents added. No original physical transform or collider removed/modified. Original core race/vehicle/camera/weather/scoring sources remain byte-equivalent. All 925 unrelated pending files retain their original SHA hashes.
- Close-up cat/VFX images are explicitly labelled visual fixtures; natural-race images and score results come from actual simulation. Human playtesting, physical controllers, listening and isolated CPU/GPU benchmarking have not been performed.
- The first Windows candidate passed 337 automated checks but was not promoted: actual image review required results-header spacing, development-watermark clearance, permanent-demo-light removal, and a dedicated seeded-blizzard image/menu cleanup check. Its exact receipt and images remain in local `CheeseInitialPresentationWindows-ac1bb7dc`; automated success was not substituted for visual review.

## Delivery and local licensed dependencies

Windows executable: `Builds/TruckSports/TruckSports.exe` with all companion files and 60 scenes. Full payload SHA256 `3769c3a1f5e081a611c37b55ece12d808bacb5a70b351d3f9884d9ca7777a794`; managed SHA256 `98f54b470a7b678fa91b878a676700e38e21ec3ac4e8e1317716137fb5b7200a`. Prior accepted build is preserved in `.sports-validation/Recovery` and the exact compiled source archive remains in local validation logs.

Original licensed vendor inputs and generated VFX derivatives are intentionally local/ignored. `Tools/TruckSports/ImportCheesePresentationAssets.py --cat-source <owned Milo Cat folder>` and `ImportCheeseVfxAssets.py --package <owned Particle Ingredient Pack.unitypackage>` reproduce the local intake without modifying source installations or replacing differing customizations. No new raw vendor files are published. Public sport-owned prefab/controller configuration references the preserved private asset GUIDs. The packaged Windows build contains its required runtime content.

In Unity use **Truck Sports → Cheese Rolling → Restore owned animated cats**, **Upgrade pooled owned explosion visuals**, and **Upgrade saved race HUD** only for initial installation. Repeating these tools preserves the saved authored assets. Edit `Animated owned cat.prefab`, `Scenery cat gestures.controller`, the saved HUD hierarchy/`CheeseHudPresentation`, and the ignored `CheeseOwnedExplosion.prefab`/URP materials for owner customization. Normal builds do not rebuild the scene or regenerate these selections.

## Vendor accounting

VENDOR ASSETS AUDITED: Milo the Cat/MalberS owned installation and actual rig/clips; GAPH Particle Ingredient Pack examples/materials/textures; Core-Collapse example layers; existing Heat/TMP/DOTween, NWH, Cheese physics/Weather Maker/accumulation integration.

VENDOR ASSETS USED: Owned Milo avatar/four clips; GAPH Explosion3 artwork with project-owned URP conversion; existing Heat fonts, original truck portrait, uGUI/TMP, DOTween and Unity Animator/ParticleSystem APIs; original NWH race and existing weather.

RELEVANT ASSETS NOT USED: Core-Collapse's inspected example emphasizes shockwave/dust; the owned GAPH example supplies the requested distinct fire/smoke/spark layers. No replacement vehicle, damage, weather, navigation or UI framework is needed. Cat walking/running does not replace stable scenery colliders.

CUSTOM SYSTEMS CREATED: Small visual adapters, explicit idempotent authoring, owner-only licensed intake scripts, saved HUD configuration and targeted tests/probes. No custom animation rig, particle engine or vehicle physics.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
