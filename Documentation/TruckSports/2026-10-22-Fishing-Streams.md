# October22 — Fishing real-net and preserved-feature acceptance

HPR-2-1 through HPR-2-5. The already implemented river/speed/net repair is now checked in the saved scene and actual Windows player. This milestone preserves its gameplay and adds a saved-scene regression plus safer explicit development-probe setup. The two newly supplied master directives are archived and queued after the existing HPR/GBC work, in the requested order; neither has been implemented by this milestone.

## Implemented behavior retained

- Visible fish motion and scoring use the same arrival time and real net-plane crossing. The earlier defect used the upstream spawn lead as the flight origin, separating visual movement from the analytic catch. The accepted correction uses arrival minus travel, retains the 1.1 m hoop radius, interpolates moving-net position within the crossing step and resolves each fish once.
- Real model movement starts at 2x the original 2.5 m/s baseline, adds 0.5x per 30 seconds, uses bounded per-fish tempo variation and a configurable 6x limit. The schedule is not a substitute for actual translation.
- The saved 240 m winding river, prominent bend, half-width fish corridor, 2.6 m net travel, terrain clearance, banks, rocks, grass and installed trees remain. All three sport-owned fish prefabs retain 130% scale. Original vendor models/materials/rigs remain unchanged.
- Existing offscreen spawning, debris, four-stage alternating release/input challenge, six-second automatic recovery, independent local inputs, weight rules, poison, survival, draws, cameras, pause, results and rematches remain.

## Actual tests and observations

Native **12 EditMode and 1 PlayMode tests passed**, 0 failures. These cover timing/tempo, deterministic schedules/rules, and the actual saved four-stream scene. Each real pooled crossing scores once; all three prefab scales, original hoop size, clear terrain under the net, paused clock and pooled rematch identities are asserted. Pool capacity is 156 models per active stream, 624 at four participants; no rematch reallocation was observed.

Exact final Windows **385 checks / 0 errors**: FishingReviewedQTEWindows 21; FishingReviewedStreamsWindows 364. Ten explicit fast real-net crossings award 1 lb / 5 lb in the same simulation step without lead-time teleporting or duplicate awards. Actual Easy, Normal and Hard four-AI matches finish through the ordinary result flow with measured model movement, bounded pools and repeated rematches. Recorded observations:

```
NATURAL MATCH difficulty=0 clock=175.999930500984 caught=60,78,113,112 missed=21,20,22,23 winner=-1 measuredSamples=101471 peakDownstreamMetersPerSecond=22.50498 acceleratedSimulation=4x; not a GPU benchmark
NATURAL MATCH difficulty=1 clock=164.97993485257 caught=80,28,93,92 missed=20,22,21,23 winner=-1 measuredSamples=101532 peakDownstreamMetersPerSecond=21.97286 acceleratedSimulation=4x; not a GPU benchmark
NATURAL MATCH difficulty=2 clock=620.119755126536 caught=396,389,376,372 missed=17,24,20,24 winner=0 measuredSamples=381944 peakDownstreamMetersPerSecond=29.33984 acceleratedSimulation=4x; not a GPU benchmark
```

The mixed-source run owns one shared gamepad's separate left/right sticks plus two separate keyboard clusters. Actual debris snags a net while the other player continues scoring; holding a direction cannot consume repeated stages; pause preserves clock/stage; fresh alternating input clears exactly once; an unanswered keyboard challenge recovers without elimination. Sixteen explicit offscreen-birth fixtures cover720p,1080p,4:3and ultrawide, followed by naturally scheduled fish/debris entries during a complete four-AI survival match. AI cameras OFF still supplies a live spectator; rematch clears QTE/pools; menu exit leaves one listener.

Agent inspected the actual Windows720p/1080p river/net/natural-match screenshots and the controlled rendered QTE views. The QTE appears only inside the owning player's viewport with L STICK LEFT and its progress bar. Controlled captures are identified separately from natural screen captures. Instrumented matches use4x or8x simulation; recorded model speeds and completion are not CPU/GPU benchmarks or human driving acceptance.

The original mixed-source development probe rejected setup, then attempted spawning in uninitialized streams and threw. Its incomplete result, log and failure receipt are retained. The probe now owns explicit keyboard clusters and a temporary focus-independent InputSettings clone, records source-validation/readiness diagnostics, and fails before spawning if start/countdown fails. Production input/focus policy is unchanged. The corrected run reports its prior saved fixture was already valid, so this evidence does not isolate whether the original rejection was caused by focus or historical bindings. No production game defect is claimed from that failed fixture.

The legacy probe footer describes a native simulation and says no Windows claim; the independently retained wrapper receipt identifies the actual Windows executable/PID and full payload. No native-only result is represented as Windows evidence.

All 925 unrelated pending files remain byte-identical. All 157 existing Fishing assets/gameplay files besides the explicit probe remain identical to the accepted Git revision after documented checkout CRLF normalization. No scene, prefab, material, gameplay, vendor or production-input change was made in this validation milestone. Future water-level/wildlife requirements belong to queued Carnival and are not claimed complete here.

## Delivery and limits

Combined Windows candidate: 60 scenes / 546 files. Full payload `12f9edb42642e5cefe1b2e1194d841cc5376819086c2d7a5aa8f0f6f4cab5d47`; managed `ec7acc429a5abc3fcbf6e7d8dbc5a6f7b6267304ac5376ca2e2e52c6534ec989`. 265 exact scoped source inputs are archived locally. Promotion and Git delivery receipts record the final copy and commit separately. The previous accepted payload remains recoverable.

No developer playtest, physical controller/wheel check, subjective listening, exhaustive weather matrix, or isolated CPU/GPU/memory profiling was performed. Human difficulty/taste acceptance remains open. This report closes only the exercised automated/visual Fishing criteria, not the entire current backlog or queued directives.

VENDOR ASSETS AUDITED: existing Layer Lab3DCharacters-Fish models/rigs/materials, JC_LPMegaCity trees, RiverModeler water, RoadPropsPack debris, Pixel Crushers StandardUIQTEControls, DOTween, Heat/TMP/uGUI and original NWH parked-truck integration.

VENDOR ASSETS USED: all those existing saved-scene foundations are retained; Pixel Crushers owns indicator visibility, DOTween owns net/QTE presentation, original NWH and collection input/camera/UI remain authoritative. Existing fish/trees/water/props remain unchanged.

RELEVANT ASSETS NOT USED: no new navigation, fluid simulation, vehicle or UI framework is required for this targeted regression milestone. The newly requested wildlife and responsive-water pass has not begun.

CUSTOM SYSTEMS CREATED: saved-scene integration regression; safer explicit development-probe configuration, diagnostics and start-failure guard. Existing game-specific fish schedule/net/scoring semantics are reused.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
