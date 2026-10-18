# Truck Sports queued-release checkpoint — October 18

## Active build state

The canonical output is currently an in-progress Plowing candidate, not the previously accepted binary below. Do not infer final acceptance from the executable's presence. Follow PlowingPresentationRepair.md and version-bound evidence under Logs/TruckSports/Plowing. The earlier gameplay/layout candidate completed six eight-AI matches with every competitor contributing. Its 37–39ms four-view cost was traced to wardrobe tint blocks; shared runtime fabric materials restored chase24.30ms / cab26.57ms with p95about37.5ms and unchanged visuals. Exact-color and palette-cleanup checks pass. Candidate `28cd5346` / data `997688c9` is running the remaining full regression suite. This pass is not pushed yet.

## Last accepted published milestone (historical binary)

Windows x64 Development: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Keep the entire folder. 35 explicit Sports-only scenes; TruckSports startup. Managed SHA256 `ef8a78590f6119af62340482d3b2c16fcc49be010f1e87e0cfa435fb19ea7b28`. Build succeeded with zero errors; all 21 version-bound Windows suites passed. Human/hardware checks remain manual.

Grounded Cheese starts/terrain, optional AI cameras, swept Cornhole hole credit, Bowling cab/model alignment and bounded Plowing impact/retained-pose presentation are implemented and validated. Read PartyCheeseImpactValidation.md for causes/tests/manual limits. Existing points remain 10/100/−100, independent of character appearance; no demographic mapping was introduced.

## Queue remains active

1. Continue PartyMasterLedger.md: audit historical implementation before changing working mechanics. Global stable-ID identity/lossless migration/result contract, remaining mode repairs, Kickball and Dance-Off remain outstanding.
   First reproduce and repair the Plowing AI obstacle-throttle/stall threshold issue observed in full-match results; preserve contact scoring and human handling.
2. Show AI Cameras is accepted for 20 applicable current modes; integrate future Parking cab/new modes when implemented.
3. Implement FareFightLedger.md as a complete Taxi-derived sport, not a menu stub.
4. Continue GoddessPlaytestLedger.md by priority. Cornhole clean-hole repair is accepted; broader Bowling wall/environment/gutters/replays, Terrier jump/momentum, Mattress spectator/weather, Wife AI, Target air control and Parking cab still require investigation/integration/runtime acceptance.
5. Complete the safe Plowing prop-interaction audit and remaining release/human acceptance. Never claim unperformed tests.

## Later queued additions — user intake, October 18

After the existing queue, implement [Sky Tow Rings Arctic expansion](QueuedSkyTowRings.md), then [Truck Limbo and Trailer Plug](QueuedLimboTrailerPlug.md). These are pending, not delivered menu entries. Sky Tow needs a real 110–130 second spline-driven speedboat course through six authored Arctic sections, constrained tow physics, reachable rings, owned scenery, AI, rematches and uninterrupted runtime acceptance. Limbo needs physically matching NWH suspension/body poses and complete competitive flow. Trailer Plug needs a real hitch/release preserving momentum, bungee controls, settled coverage/sealing, finite distinct crowd impacts, full venue, turns/AI/cameras/replays and resets.

Trailer Plug addendum: reuse the existing Plowing character assets, animation/navigation, Taxi ragdolls, impact reactions/audio, pooling and cleanup. Adapt mode-owned emergence, gradual density and panic from the sinkhole; stop spawning on an authoritative settled seal. Keep each sport's scoring independent, deduplicate actual trailer impacts, preserve Plowing's roster/balance and both variants. Test shared identities/assets, traversal, physical strikes, scoring, seal/spawn stop, resets and Plowing regression. Do not create a second pedestrian framework or generic replacement crowd.

Active Plowing pass: label-free role costumes, full-moon Weather Maker night, pitchfork props, AI wall recovery, bounded bonus dodges/perches and Heat/DOTween UI polish are in progress. Native wall baseline reached the isolated target around 18.6s after setup; the repaired same-wall test reached it around 7.6s. Windows acceptance for this new pass is pending. Prior accepted implementation `3a9c64a2` and receipt `fc290f21` are pushed to both public branches; later work is not yet committed/pushed.

## Workspace and delivery safety

F:/Unity Projects/Interstate-Hauler-Trucking-Simulator is the only active checkout/output root. Root main includes private unrelated Taxi ancestry; never push it wholesale. Publish reviewed Sports changes as clean descendants of the existing public Sports branch/main. Preserve unrelated dirty files and licensed original imports.

Full queued-release exclusive lease remains with Sports. Taxi task 019fefac-6230-7731-8406-588128ebbbfe is waiting until explicitly coordinated release. Do not start a competing editing agent or Editor. Keep the owned Editor idle between tests; earlier restart licensing trouble was resolved without license/account changes.

Canonical build only: Builds/TruckSports/TruckSports.exe. No additional validation-build folders. Do not close user-tested players. Ignored evidence is under Logs/TruckSports/Party/Cheese, with hash/PID/exit/completion receipts. Exclude builds/logs/caches/credentials/vendor originals from commits. Review scope and native meta files, preserve outside hashes, commit and normal atomic push to the existing Sports branch and GitHub main; verify remote tips. Never force-push or amend unrelated commits.
