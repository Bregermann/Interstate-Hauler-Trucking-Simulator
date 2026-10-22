# Demolition Derby — readable health and venue branding

HPR-27-1 through27-4 preserve the existing Derby gameplay and replace the clipped long ENGINE/BODY text with the already-owned NWH check-engine icon and Heat shield icon, each beside its actual remaining-condition percentage. Values use the exact original health calculation; no damage values, rules or contacts are inferred by the UI.

## Cause and implementation

The existing shared card's205-unit detail line contained the heat wins, ENGINE percentage and BODY percentage, causing an ellipsis before the final value. New `DerbyDamageHud` adds two short icon/value groups to the existing shared score cards and compact viewport cards. The shared Heat/TMP/portrait/color/plate/DOTween presentation remains authoritative; six read-only DerbyMatch presentation overrides supply actual wins, participant ownership and small stopped/fire prompts. Shared cards sit in a compact lower4-column group; split-screen cards use the existing inner-corner layout. No duplicated leaderboard inside each viewport.

Explicit `DerbyHudAuthoring.Upgrade` disables only the two exact old world-text renderers and adds two collision-free signs using the existing original Derby PNG. The larger floor logo is on the flat apron outside the original raised roll banks; the far stadium sign provides additional branding. Saved references, material and versioned one-time migration remain editable. Normal builds do not rebuild the scene; repeated authoring preserves the saved file bytes.

## Actual verification

- Native EditMode1/0: existing font/icons, exact two disabled signs, original PNG, zero sign colliders, floor-logo clearance and repeat-authoring byte preservation.
- Native PlayMode1/0: all eight actual engine/body readings begin at100%, a controlled damage-state fixture produces74%, and a real rematch restores100%. This API fixture is not presented as physical collision evidence.
- Windows health presentation57/0: actual composed720p/1080p/ultrawide/4:3 captures across ten human/AI-camera configurations; percentages fit, icons load, sparse human ownership and viewport bounds are correct, shared cards clear the lower status strip, pause/rematch/menu cleanup work. Software gamepads were injected; no physical-device claim.
- Separate original Windows physical-match probe: 39/0, pure existing rule fixtures plus actual NWH/PhysX autonomous competition, real energetic contacts, localized damage/performance scaling, five-second motion rule, outward spawns, recorded wreck replay, complete results and repaired rematches. Its legacy result footer contains old platform boilerplate; the fresh standalone process receipt, executable/full-asset hashes and player log establish actual Windows execution. Receipt handling counts its original PASS/FAIL lines and requires a fresh zero-error completion; it does not invent checks.
- All 9367 original scene documents remain, with12 additions. Every original transform, collider and particle document is unchanged; original damage, motion, AI, rules, replay and contact scripts are unchanged. DerbyMatch changes are restricted to the six read-only presentation API additions. All925 unrelated files retain their prior SHA hashes.
- The first03ee4b61 candidate passed57 assertions but was visually rejected: a generic shared-grid double offset lifted the cards too far, and the original raised banks covered the new floor art. Corrected owned layout/placement and extra bounds/clearance checks passed before promotion. Its receipt/images are retained locally in `DerbyHealthFirstVisual-03ee4b61`.

Human playtesting, physical controls, listening and isolated CPU/GPU profiling were not performed. These automated/agent-visual checks do not constitute developer acceptance. Other active HPR/GBC work remains open.

## Delivery

Combined Windows executable `Builds/TruckSports/TruckSports.exe` with all companion files and60scenes; full payload `2f1059f01175072383a21c491284d526ed5e8772efca8d0264498b3794218fed`, managed `217eaeddde7006cabc96ae34a8dba41c782cbdad72a08ce254a734ebec47901f`,546 SHA-verified files and240 archived exact scoped sources. Previous accepted Dock build remains recoverable.

VENDOR ASSETS AUDITED: Existing NWH dashboard icons/damage adapter, Heat shield and shared production UI, TMP/DOTween, original Derby PNG and existing Cinemachine/broadcast presentation.

VENDOR ASSETS USED: NWH check-engine image and original vehicle/damage physics; Heat shield, existing shared fonts/cards/portraits/plates/DOTween; original owned Derby PNG and URP material.

RELEVANT ASSETS NOT USED: No replacement damage engine, camera framework, UI framework or new logo was needed. Existing gameplay, audience, banks, mirrors, contact and replay remain in place.

CUSTOM SYSTEMS CREATED: Read-only two-value damage binding over existing cards, explicit saved-scene authoring and targeted diagnostics/tests.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
