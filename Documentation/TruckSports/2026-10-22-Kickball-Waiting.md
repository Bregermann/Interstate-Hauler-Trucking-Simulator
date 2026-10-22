# October22 — Kickball waiting cameras, AI pacing and home-run acceptance

Scope: HPR-5-1 through HPR-5-5 in the current Human Playtest Repair directive. Dock’s accepted pool/crowd update remains included. This does not close the whole HPR/GBC backlog.

## Implemented and revalidated

- Waiting players can look around in their own existing Chase or Taxi Cab camera. Whole controllers retain right-stick look; benched shared-stick and keyboard participants can temporarily use only their own idle driving cluster. The first keyboard participant owns mouse look even when a controller occupies viewport one. Role changes reset look inertia and require neutral input before normal driving. Shared broadcast retains one camera authority.
- Select **AI PLAY · NORMAL / FAST-FORWARD 2×** in setup or Pause. Normal is the initial default; the choice is saved. The existing match, NWH and PhysX perform every pitch, contact, out and run. Acceleration is permitted only when every human is waiting; any human kicker, runner or fielder prevents it. It releases for pause, replay, home-run celebration, setup/results/rematch transitions and scene cleanup. Nothing skips another human's active role.
- Setup/pause camera selection is retained. Actual Windows review rejected the initial otherwise-passing candidate because its weather/roster and guidance overlapped. The corrected saved layout separates those rows and widens the existing Heat weather caption. Split screens reuse the existing compact branded viewport cards with correct names, team scores, roles and owned contextual prompts.
- The first-base-side existing Cinemachine broadcast rig remains. Pitching frames pitcher/kicker; loose-ball fielding follows actual ball/action. When defense secures the ball and an unsafe runner is active, the action adapter follows that runner and their next relevant base. Readability limits and offscreen guidance remain with the existing global director.
- Revalidated the previously implemented sealed home run: actual fair airborne fence clearance immediately credits entitled runs, ends defensive tagging, displays the animated banner/truck/crowd celebration and returns to exactly one next play. No scoring or vehicle-rule replacement was introduced in this milestone.

## Actual validation

Final native **35 EditMode / 0 failures** and **5 PlayMode / 0 failures**: existing rules/home-run regressions, saved references/idempotence, explicit runner/base camera fixture, actual NWH/PhysX pitches/kicks/base progress, pause in flight, four team sizes, four owned waiting inputs, mouse ownership, compact cards, natural AI-to-human turn handoff, reconnect and rematch.

Final exact-payload Windows **87 checks / 0 errors**. Per-run counts: {"KickballReviewedWaitingWindows": 27, "KickballPacingRestartWindows": 7, "KickballReviewedHomeRunWindows": 16, "KickballPhysicalMatchWindows": 21, "KickballBroadcastWindows": 16}. These include four software human input configurations, a separate application restart restoring the saved pacing choice, two explicit physical fence-clearance fixtures, a complete actual three-inning twelve-AI match and targeted broadcast readability/layout checks. The fence fixtures explicitly seed a legally kicked ball flight after a real pitch; they do not claim human bumper-kick acceptance. Full-match scoring is produced by actual contacts and rules, without injected results. Recorded captures distinguish those fixtures from natural match frames.

The first native keyboard tests failed because the Editor runner had no focused Game View and reset the synthetic keyboard state. A trace recorded key held=false and zero drive. Only the temporary cloned test InputSettings now use the same all-device Game View routing as the existing cab-look probe; production focus/input settings remain unchanged. Earlier failed XML and the rejected initial Windows receipt/image are preserved in the evidence.

The complete Windows match finished8–6 in three innings:34pitches,33physical kicks,156catches,125throws,90ordered base touches and33completed plays. It entered an actual recorded highlight, restored broadcast rendering, completed the ordinary results/rematch flow and returned to the front end with one listener. Captures were reviewed at720p,1080p,1440×900,4:3 and ultrawide; setup and four owned cabs were separately inspected at720p/1080p.

All 16181 original scene documents are retained; all 92 original physical components remain unchanged. Physics transforms, drive/pitch/home-run-celebration implementations and all925 unrelated pending files are preserved. Added UI and a small pacing adapter do not rebuild the stadium. Agent inspects final actual720p/1080p setup, pause, split cab and home-run images plus the full-match aspect-ratio captures before promotion.

Human playtesting, physical controller/wheel acceptance, listening and isolated CPU/GPU profiling were not performed. Automated input fixtures are not human match acceptance. The safe fast-forward deliberately stays at normal speed if another human is fielding; no consent-based override is provided.

## Editing and delivery

Open Assets/TruckKickball/Scenes/TruckKickball.unity. KickballMatch references the original views, new aiPlayback adapter and setup/pause labels. KickballWaitingAuthoring.Upgrade is an explicit version2 scene installer; rerunning it preserves the saved scene. Normal builds do not regenerate customization.

Combined Windows: Builds/TruckSports/TruckSports.exe;60scenes/546 verified files. Full payload `fdbf35106a6bfc7be4ee33348e0be18cc2d55d6563a8fb7d70ba340afee48e3f`; managed `980cd6a149d830875ef6a64073650ef16fb79730b680bc0b07178fec37b916d1`;263 exact scoped source inputs retained locally with the build. The previous accepted Dock payload remains recoverable. Git delivery is recorded separately after the exact scope is committed and pushed.

VENDOR ASSETS AUDITED: installed Cinemachine AxisState/Recentering and existing rig adapters; Heat ButtonManager/Dropdown public APIs and prefab layout; TMP/uGUI; NWH and existing Kickball contact/vehicle authority; existing PongAIPlayback time ownership; DOTween and shared compact viewport presentation.

VENDOR ASSETS USED: existing Cinemachine rigs/axis acceleration and limits, Heat buttons/dropdown, TMP/collection fonts, shared branded plates/portraits and DOTween score/context animations, original NWH/PhysX trucks and match systems. Existing home-run crowd/audio/celebration assets remain intact.

RELEVANT ASSETS NOT USED: Pong's manual flight simulation is for a single projectile and is unsuitable for a twelve-truck match. No replacement camera, vehicle, weather, animation, navigation or UI framework is needed. No new artwork/vendor asset intake was required.

CUSTOM SYSTEMS CREATED: small Kickball-specific pacing/role-selection glue, an opt-in waiting-input argument to the existing cab-look adapter, explicit idempotent scene integration and targeted acceptance probes/tests.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
