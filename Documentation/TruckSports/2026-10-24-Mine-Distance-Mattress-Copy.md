# Minefield distance readability and Mattress result wording

Minefield’s existing eight-card row now gives its numeric distance enough width, with FT and actual heat points in the adjacent detail region. The numeric value retains the exact original one-decimal foot conversion and rounding. It no longer shares a72pxbadge with the unit suffix. Only the Minefield layout branch changes; other compact-card layouts remain unchanged. Original distance state, precision, final-rest/tie rules, explosions, physics, terrain, cameras and input are untouched.

Mattress result statistics use RACE WIN at exactly one victory and RACE WINS otherwise. Original placement totals, finish history, rankings, shared unit formatter and game rules are unchanged. Three production files and two development probes changed; one targeted native fixture was added. No saved scene rebuild or vendor source change was needed.

Native acceptance: two explicitly synthetic UI tests pass, plus fifteen existing result-unit EditMode cases. Eight supplied distance/point model values exercise0through36089.2feet in the real saved cards at720p/1080p. Original formatted precision matches exactly; numeric/unit/point labels fit without TMP overflow or overlap, and rendering does not change supplied data or create score receipts. The original series bookkeeping receives supplied finish timestamps to produce0/1/2wins; the saved result board displays correct copy and preserves totals/placings/history. These are labeled controlled presentation tests, not physical distances or actual race wins. All four controlled native images were individually inspected.

Exact Windows original full-match acceptance:
- MinefieldMetricCardsWindows: 44checks, zero errors; PID32468, 282.27seconds.
- MattressMetricCopyWindows: 36checks, zero errors; PID33068, 84.43seconds.

Total 80checks, zero errors. Original Minefield/Mattress physical workloads, supported-rest/recovery/series assertions, score-receipt accounting, results and rematches remain. Added checks read actual current per-slot card values/units/heat points with no TMP overflow, and original per-participant victory counts in result details. All8actual Windows backbuffers were individually inspected. Native controlled UI evidence remains separate from these ordinary actual-match images.

Installed combined Windows x64 build: Builds/TruckSports/TruckSports.exe,60scenes, 548byte-verified files; exact413scoped owned-source archive retained. Full payload 4218c7cd1704041179fed209012dd154982a2257127212c5c45905974d573e66; managed 89b0c627f914a2bccb656605d9caeb3480198ae225c69054461be92074e8b0c4. Prior complete accepted build retained at F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\.sports-validation\Recovery\AcceptedBuildBeforeCamera-20261024-124250. The public Snake first-hitch fix and earlier accepted scoring increments remain included.

Remaining limits: original Minefield replay tree occlusion and broader environment/camera work remain. No human playtest, physical-device/reconnect, listening, isolated profiling or extended-soak acceptance. The earlier751-file candidate3a337c13 remains a separate immutable local snapshot, preserved privately in1d23ca11; automatic review requires the pending direct human approval before its public push. These newer HUD changes are not part of that751-file approval request. Local Git preservation and any future public publication are recorded separately. Q22-SKY, completeCarnival0.6 and Impossible0.7 remain unfinished.

- VENDOR ASSETS AUDITED: existing Heat/TMP/DOTween card/result presentation and original per-sport read-only bindings.
- VENDOR ASSETS USED: unchanged Heat/TMP/DOTween UI and existing original physics, race and measurement systems.
- RELEVANT ASSETS NOT USED: no extra UI, measurement, scoring, vehicle or replay framework is required.
- CUSTOM SYSTEMS CREATED: only two small presentation corrections, a Minefield-specific layout allocation and targeted native/development assertions.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.

Evidence: [native/Windows captures, results and exact source review](Evidence/2026-10-24-Mine-Distance-Mattress-Copy/).
