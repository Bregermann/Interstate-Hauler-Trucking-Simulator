# Keepaway and Terrier — current-build regression

This validates existing implemented repairs; no additional gameplay retuning was needed. Exact full Windows payload `5b12d4c0bab3bd7f2fe17a68e80304078fd1dfcefc257dc4bade9f4a8888e727` (the accepted Dock milestone) passed 245 checks with zero failures across four separate owned processes.

- Keepaway: 111/0, both complete default60-second matches, actual RAIN/SNOW selection, ball pickup/theft, recovery, real cab ownership, results, rematches and menu return. The original legacy probe uses camera-only renders; these are not presented as full-window UI evidence.
- Separate composed Keepaway run: 54/0, both saved variants, actual720p/1080p screen captures, projected truck size, action targets and retired-AI spectator checks. Images were inspected separately from the collision/match assertions.
- Terrier: 21/0, actual twelve-truck first-to-five competition over32 heats/32finish locks, randomized courses, complete-body finishes, replay, rematch, pause and collection return.
- Terrier views: 59/0, clear/blizzard presentation and actual compact Windows1–4human/optionalAI layouts at720p/1080p, owner/bounds/unique-camera checks. Controlled camera fixtures are identified in the original results; no physical-device or human acceptance is inferred.
- Four native PlayMode tests: both saved Keepaway variants have2.999591-times actual physical/rendered surface area, retained22potential/18Lively props with one-third density, original model fragments, reachable starts; actual NWH/PhysX contact breaks a prop and rematch restores it; shared pooled damage reset remains functional; actual airborne Terrier wind response is0.70 of the old response and does not rewrite authoritative weather.

Keepaway's expansion/destruction is already implemented in78b0ff5a. Terrier's11651d08 change adds only a mode-owned wind-force multiplier to the racer/course; acceleration, impulses, ramps, steering, geometry and other airborne behavior are retained. This continuation adds evidence and reconciles stale ledger entries, rather than repeating completed implementation.

These are automated and agent-visual observations. Human playtesting, physical controller/wheel checks, audio listening and isolated CPU/GPU profiling were not performed. Other active HPR/GBC requirements remain open.

VENDOR ASSETS AUDITED: NWH, existing Core-Collapse/EzySlice fracture adapter, Heat/TMP/DOTween, Compass Navigator Pro and shared installed weather integrations.

VENDOR ASSETS USED: The existing NWH gameplay, existing owned-model pooled fragments, Heat/TMP/DOTween cards, CNP ball guidance and authoritative weather adapters.

RELEVANT ASSETS NOT USED: No replacement physics, destruction, UI, camera or weather package was needed for this regression continuation.

CUSTOM SYSTEMS CREATED: No new runtime system; evidence/status reconciliation only.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
