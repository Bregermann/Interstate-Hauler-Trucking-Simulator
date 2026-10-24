# Result metric wording and actual Bog forecast caption

The first Snake trailer hitch remains fixed in this build; later trailers retain their path following. This separate Q22 pass fixes two observed display bugs without changing score values, placings, physics, weather selection or original match rules.

Result-count cause: the immutable result row appended the raw plural unit, and its DOTween value animation independently did the same. The display-only DisplayUnit maps six actual count-unit phrases at exactly one: WIN, TEAM WIN, TEAM DELIVERY, CUP HIT, TEAM CUP HIT and FARE. Original Unit/Measurement/identity/placing data remain unchanged. Unknown units and physical measurement abbreviations remain untouched. Both initial text and completed animation use the same display unit.

Bog caption cause: Header read an older local weather index even when the shared registry resolved a different installed preset. Header now reads course.weather.Description, matching the actual configured registry entry. No preset, fog, wind, time or road-grip behavior is changed.

Before-fix evidence: the first native run reproduced CLEAR selected versus RAIN displayed; its second case failed because the test reflection helper matched two Show overloads. That fixture ambiguity was corrected, then a separate unchanged-production run reproduced 1 WINS versus expected 1 WIN. These failed results are retained rather than counted as successful verification. Final saved-scene native2/2 and targeted EditMode27/27 passed. All8controlled native images were individually inspected: an explicitly labelled synthetic eight-row result UI fixture and actual installed CLEAR, ACID RAIN and HEAVY SNOW caption previews at720p/1080p. These are not complete matches or weather-art acceptance.

Exact Windows validation:
- BogForecastCaptionWindows: 33 checks, zero errors; actual PID21940, 61.23 seconds. Original physical workload retained.
- LogMetricLabelsWindows: 72 checks, zero errors; actual PID34344, 125.68 seconds. Original physical workload retained.

Total 105 Windows checks, zero errors. All 12 actual Windows backbuffers and 5 controlled Windows RenderTextures were individually inspected. Bog runs the original four-AI three-race physical series and checks its header against the actual shared forecast throughout all races. Log runs the original vendor-equilibrium fixture and Easy/Normal/Hard first-to-five physical matches. Existing results acceptance additionally compares every completed animated metric label with its immutable row display value at both capture resolutions. No scores, contacts or finishes were injected into these physical matches.

Builds/TruckSports/TruckSports.exe is the byte-verified combined Windows x64 build:60scenes, 548 payload files,405scoped project-source inputs. Payload SHA-256: 7cb671af7596b0c0a3695e6358dcc0224b23ad151fe1b1e0529effbfd06157f8. Managed SHA-256: a5e213a3078281a5881ae9709ba433d786a48290003faae0f629a4c506ffd72f. Previous full accepted build retained at F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\.sports-validation\Recovery\AcceptedBuildBeforeCamera-20261024-080842. The source archive is the scoped owned inputs, not all vendor/Unity build inputs.

Unverified/remaining: no human playtest, physical-controller, listening, isolated performance or extended-soak acceptance. Forecast previews visibly retain dark angular foliage in the original Bog venue; this caption-only pass does not claim to fix it or establish weather-art quality. Parent Q22, Carnival0.6 and Impossible0.7 remain incomplete. Public Git publication of the separate142-file Parking payload remains pending the exact approval already requested after automatic review rejected the helper/scope change. This local milestone has not been committed or pushed;925unrelated files,142pending files and private Dead Air history remain preserved.

- VENDOR ASSETS AUDITED: existing Heat saved results board, TMP text, DOTween number animation, Weather Maker preset adapter and original NWH/Floating Entities match probes.
- VENDOR ASSETS USED: existing Heat/TMP/DOTween display and original Weather Maker description authority; unchanged NWH/Floating Entities physical matches.
- RELEVANT ASSETS NOT USED: no replacement UI, weather, physics or localization framework is needed for the observed English display-copy corrections.
- CUSTOM SYSTEMS CREATED: one small display-unit property, one header binding correction and targeted regression/acceptance checks.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.

Evidence: [native XML, source review, exact build receipt and inspected images](Evidence/2026-10-24-Result-Metrics-Forecast-Caption/).
