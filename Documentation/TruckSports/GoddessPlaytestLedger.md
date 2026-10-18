# Goddess playtest — queued implementation checklist

Status: **active queue; individual accepted rows link evidence, remaining rows are pending**. Append after all existing work: Pedestrian Plowing → Party master/Cheese addendum → Show AI Cameras → Fare Fight → this pass. Human supplied the complete numbered 1–50 directive in conversation. This ledger is a continuation contract, not a substitute implementation. Newer requirements override conflicting earlier behavior. No additional game modes: extend existing modes/courses only.

Preserve shared global human/AI identities, multiplayer, NWH vehicle authority, controls/camera options, established scoring, environmental upgrades, physical interactions, shared weather, enlarged Keepaway and optional AI cameras. Inspect actual scenes/prefabs/code and vendor capabilities first; diagnose reported defects before editing. Each row needs changed scene/scripts/assets, root cause where relevant, actual tests/results/evidence, limitations and final status.

## Shared features

- [ ] **1 Universal horn:** dedicated consistent keyboard/controller action in every sport, nonconflicting even in Poker/Dance-Off/rhythm. Reuse installed horn and alternate sounds, spatially audible to others while driving/stationary/airborne where appropriate; bounded overlapping audio, actual prompts/help.
- [ ] **2 Menu art/cards:** every registered playable sport gets verified representative actual gameplay screenshot, consistent aspect, updated after camera/environment changes; original colorful coherent small PNG icon, transparent where suitable, recognizable sports motifs. Clean icon+screenshot+title+supported short description cards through existing menu architecture, preserving mouse/controller navigation/configuration. No emoji/stock substitutes for actual gameplay.
- [ ] **3 Universal weather:** persisted shared pre-match Random plus EACH actual supported registry preset; explicit selection overrides random. Match-owned consistent visuals and existing physics/snow; no active-play reset, rematch preserves selected configuration or rerolls only intended Random. Configurable weighted selection with per-sport overrides, not incompatible mode managers. Minefield snow reduction below; Fare Fight earlier equal-weight requirement must remain its default override unless explicit configuration changes it.
- [ ] **4 Race countdown:** audit actual registry, share polished Cheese 3–2–1–GO visuals/audio across all racing modes (Cheese/Terrier/Mattress/Wife/etc), synchronize humans/AI, stationary safe initialization/no unlock impulse, timing/scoring starts correctly, reset/rematch. Do not impose racing countdown on stationary/turn-based modes.

## Cornhole

- [ ] **5 Critical clean-hole miss:** reproduce centered full downward passage with NO board contact repeatedly, including fast flight. Inspect actual truck-as-bag colliders, hole trigger size/placement/layers/timestep/CCD, scoring/miss/OOB order and board-contact prerequisite. Reliable geometric swept full-passage detection or scoring volume, once only; valid hole locks before later ground/miss, rim uses real physics, near-hole misses remain misses. Do not enlarge hole arbitrarily or replace thrown truck with separate projectile.
- [ ] **6 Physical wind/variance:** shared selectable/random actual weather, head/tail/crosswinds/gusts/intensity with continuous relative-air/drag flight influence. Preserve core throwing controls/aim/power/skill, no arbitrary random failure.
- [ ] **7 Wind HUD:** compact attractive bottom-right arcade-golf arrow relative to actual player's aiming direction, MPH, optional subtle gust animation. Same authoritative physics wind, viewport-safe/readable.

## Cheese

- [ ] **8 Indicator contrast:** outline/shadow/subtle backing/high-contrast cheese marker through clear/snow/heavy/blizzard/bright/dark, same tracking/function, no oversized obstruction. Preserve repaired mountain and clean grounded starts.

## Bowling

- [ ] **9 Duplicate interior:** reproduce old placeholder coexisting with intended new alley; inspect saved/additive/runtime/rematch generation/renderers/colliders, remove only obsolete geometry and prevent respawn. Preserve needed new environment.
- [ ] **10 Back wall:** strong physical PIN rebound/tumble into deck, truck receives ordinary safe collision without pin restitution/extra impulses/launch/freeze. Correct layer/material/per-object response, not globally intangible wall. Test both actual pins and truck.
- [ ] **11 Gutters:** whole-truck-width geometry/colliders, continuous seams and lane alignment, genuine truck entry/fall, correct gutter scoring and existing pin logic.
- [ ] **12 Automatic spectator:** after actual pin impact/control authority ends, smooth useful free-camera pin framing, manual options where supported, unusual truck stop handled, driving camera restored next attempt. No premature switch while controllable.
- [ ] **13 Replay:** record actual approach/impact/pins, cinematic useful angles and notable slowmo, skip/continue/configurable ordinary shots; sealed real outcome, no duplicate score or physics mutation.
- [ ] **14 Original cheesy animations:** colorful humorous original truck/pin/ball retro celebrations/reactions for strikes/spares/gutters/splits/near misses/unusual combinations/exceptional shots; temporary overlays, never scoring interruption.
- [ ] **15 Acceptance:** actual new-only interior, pin-vs-truck wall comparison, entire truck gutter, spectator+reset, actual replay, strike/spare/gutter animation, accurate scoring/multiplayer/rematches.

## Dock Diving

- [ ] **16 Canadian bubbles:** occasional correctly-owned airborne comic speech (Eh/Bud/Oh bud/Sorry/Beauty/Give’r/Holy smokes/Oh fer sure), smooth in/out, varied cooldown/no spam, readable gameplay/broadcast, no score/camera obstruction, unchanged dive mechanics.

## Minefield

- [ ] **17 Flag labels:** name directly on/attached to player flag, remove ugly rectangle, player color with outline/shadow, camera-readable and weather contrast, compact.
- [ ] **18 Random snow weights:** configurable lower COMBINED snow/heavy/blizzard chance; clear/non-snow more common, snow remains possible, explicit selection always honored.
- [ ] **19 Strict preservation:** no changes to mine power/explosions/scoring/handling/extended world/terrain boundaries/recovery or other gameplay; this pass only labels/weather.

## Terrier

- [ ] **20 Cherry:** giant glossy recognizable red cherry/stem/detail on existing winner ice cream, collision-safe nonblocking landing, preserve working finish/catch.
- [ ] **21 Volcanic track:** owned volcanic rock texture/rough dark terrain/ridges/bumps/cracks/geological variation matching volcano; navigable matching visual/collision surface.
- [ ] **22 Jump spin:** reproduce unintended ramp/landing rotational impulses; substantially reduce wild ordinary airborne spin via bounded angular/stability tuning, preserve general upright and limited deliberate corrections, natural rotation and genuine spectacular collision crashes.
- [ ] **23 Scaffolding:** remove misplaced/repetitive stairs/framework under track, preserve sensible supports/track collisions/volcano/lava.
- [ ] **24 Funnel:** maintain broad track across majority/earlier jumps. Progressive narrowing ONLY between last wall and THROUGH HERE. Continuous safe boundaries, competitive collisions, unchanged correct finish opening and aligned ice cream.
- [ ] **25 Momentum:** diagnose airborne braking/drag/orientation speed clamps/ramp impulses/contact friction/controller transitions. Preserve ballistic forward momentum, successive successful jumps generally build speed through geometry/gravity/bounded boosts, configurable speed cap and stable landings; no unlimited acceleration.
- [ ] **26 Acceptance:** full12-truck randomized races, broad→final funnel, true finish/icecream/cherry, volcanic surfaces/no junk supports, ordinary stable jumps and successive speed, lava/loser explosions.

## Mattress

- [ ] **27 Finished-player spectator:** free/shared spectator for finishers, locked results; unfinished humans/AI continue with their physics/race states, no premature global end.
- [ ] **28 Weather lifecycle:** reproduce rain stopping on first finish, move ownership to match lifecycle. One finisher never stops precipitation/accumulation/wind/particles/environment. Test early finish while others race until legitimate round end.

## Beer Pong

- [ ] **29 AI pacing:** selectable Normal/Fast-forward/Simulate-to-result, obvious controls/configuration. All use actual established shot logic/physics and preserved difficulty/accuracy/selection/outcome/cups/turns/completion. No free predetermined points, double resolve or stale objects.

## Musical Parking

- [ ] **30 First person:** selectable real cockpit alongside existing overhead/chase, cab look/source ownership/split1–4humans+AI, clear bay/nav/HUD, switch where supported, shared optional AI cameras. Preserve AI occupied-bay retarget/recovery and competition.

## Wife Carrying

- [ ] **31 Floating rocks:** diagnose authored/runtime terrain sampling/offsets, bases naturally grounded/embedded with matching colliders, no ground burial or floating.
- [ ] **32 Finish:** checkered physical line/flag or structure/signage, aligned authoritative trigger, unmistakable high-speed recognition/victory.
- [ ] **33 Competitive AI:** improve acceleration/commitment/route/cornering/obstacle recovery/overtake/carried-object decisions, appropriate difficulties, not merely max-speed increase or suicidal driving. Full races/human checks distinguished.

## Curling

- [ ] **34 Wall markings:** alternating red/white sections at actual10ft (3.048m) intervals along travel, legible, no z-fight or extra colliders.
- [ ] **35 Target closer:** reduce CURRENT start-to-target distance20%, not world coordinate or board length. Update volumes/markings/AI/distance/camera/dependencies. Preserve threefold longer board and launch sensitivity.

## Fishing

- [ ] **36 Offscreen spawn:** actual camera-layout-aware outside-view valid water, no terrain/fish overlap, naturally swim reachable active area; preserve species distribution and doubled/rising speed, no inaccessible spawns/popping.
- [ ] **37 Unwanted catches:** occasional modeled rocks/trash/debris/comedic swimmer rescue/appropriate surprises, environment integrated, not ordinary fish treatment for people.
- [ ] **38 Net-clear QTE:** per-owner brief keyboard/controller sequence/timing interruption with clear actual-device prompts, fair multiplayer/time/scoring, success resumes fish catching, retry/recovery on failure never permanent trap. Tests offscreen entry/speed progression/QTE/rescue/normal resume.

## Bog Snorkelling

- [ ] **39 Hard course:** additional course option within existing mode, long meaningful submerged route/terrain/obstacles/narrow passages/clear goal, preserve easy courses.
- [ ] **40 Oxygen:** existing submerged depletion/failure plus visible air, optimizable consistently skilled-beatable Hard route, inefficient run risks/depletes air, no unrealistic perfect-only tuning. Compare actual inefficient/skilled runs.

## Sumo

- [ ] **41 Costume:** front-mounted diaper/thong geometry per actual truck dimensions, no bumper/wheel/ground clipping/collision effects, attached in motion, player colors retained. Preserve credited ringout+1 and survivor+3.

## Target Launch

- [x] **42 Limited wings:** appropriate existing inputs modest pitch/yaw/roll/orientation/landing corrections, weaker than ground control, bounded smooth torque respecting angular state, gravity/momentum, no aircraft/hover/unlimited acceleration. Preserve volcano/lava/target/final-rest scoring/settling/launch controls and power; physical comparison and score still waits for rest.

## Field Goal

- [ ] **43 Fans:** densely populated optimized stadium models/animations/LOD/billboards/banners/signs, meaningful sporting crowd.
- [ ] **44 Reactions:** actual score→cheer/stand/wave; miss→boo/disappointment; close miss→gasp; powerful/unusual→excitement. Event-driven visuals/audio, not constant cheering.
- [ ] **45 Rare runner:** configurable rare entrance/run/animated exit, funny audience/announcer reaction, no active scoring/ball/holder collision interference in normal mode. Preserve truck foot/holder/upright ball/endzone/branding/wind/weather.

## Poker

- [ ] **46 Mobile female dealer:** recognizable existing dealer on owned suitable small cart, no teleport. Before hand shuffle/prepare, physically travel safe route to each participant, stop/turn/reach/visible short card handoff synced to correct player, continue and return waiting spot. Operate/steer animations, collision-safe presentation. Preserve actual private card values/privacy, betting/evaluator/52 ranks+suits/reveal/full showdown/hand labels/expressive trucks/awards. Fast-forward/skip never duplicate cards/deals; reset every hand. Observe repeated real hands and physical travel/handoff.

## Execution and acceptance

- [ ] **47 Preservation:** explicitly regress Minefield physics, Target rest scoring, Bungee controls, Sumo attribution, Parking retarget, Keepaway cameras, Terrier landing, Poker evaluation, Fishing speeds, identities and multiplayer.
- [ ] **48 Runtime:** each affected scene: reproduce, root cause, correction/integration, EditMode+PlayMode, actual gameplay/Windows where available, visual cameras/UI, reset/rematch, human/AI, performance. Compilation/scripts alone never acceptance; distinguish injected versus real hardware/human testing.
- [ ] **49 Priority:** first broken Cornhole, Bowling environment/wall, Terrier spin/momentum, Mattress weather, Wife AI, Target aftertouch, Parking cab. Next shared weather/countdown, Fishing/QTE, SnorkelHard, Pong pacing, Poker mobile dealer. Then presentations/replays/crowds/bubbles/labels/scenery/Curling/Sumo/menu art/horn/wind/cheese marker. Adjust only for dependencies.
- [ ] **50 Report:** every feature implementation/root cause/files/assets/tests/actual observed result/limitations, completion matrix throughout, screenshots/video particularly Cornhole/Bowling/Terrier/Fishing/SnorkelHard/FieldGoal/Poker/menu. Never invent tests. If interrupted preserve exact checkpoint. Combined canonical build/docs/scoped Git delivery follows existing project policy.

Target Launch42 accepted in the October18 milestone; see [TargetLaunchRepair.md](TargetLaunchRepair.md) and its exact-build evidence. Other unchecked rows remain outstanding.
