# Next repair audit — source evidence, not acceptance

Read during the current Windows acceptance run on October 18. These findings guide subsequent reproductions. They do not mark the queued features complete, and no speculative handling changes have been applied.

## Plowing AI follow-up from full-match evidence

The arena Normal eight-AI run completed on its round timer, but two competitors recorded zero standard, bonus or penalty impacts. That is an observed lack of contribution, not proof that every AI made progress. A source candidate is concrete: the obstacle-avoidance branch sets throttle to exactly `.3f`, while `Recover` accumulates stationary time only when `Abs(command.y) > .3f`. Thus a stationary upright bot using obstacle throttle cannot arm that reverse recovery. A head-on surface normal also gives zero lateral steering in the avoidance expression. Reproduce an actual head-on wall blockage, then verify physical escape and individual full-game progress. Keep scoring contact-driven and preserve human controls. This is a pre-existing path; the current presentation milestone does not claim to resolve it.

## Target Launch

`TargetRacer.Lock` immediately computes `match.ScoreAt(center)`, sets Resolved and calls `match.Lock` from a qualifying top contact. Both `OnCollisionEnter` and the swept landing observer can invoke it. There is no resting-position observation interval in that path. Existing `SportsLimitedAftertouch` supplies bounded horizontal correction only; it does not apply pitch/yaw/roll torque. The saved-scene camera review shows the original elevated launch/target structures, not the requested completed volcanic environment. Reproduce sliding across rings, late falls, inverted resting and persistent motion before implementing the Party resting-score/environment requirement and later wing correction. Preserve launch tuning and existing per-participant score authority.

## Terrier

`TerrierCourse.HalfWidth` currently lerps from 34m to the finishing width across the entire course. `UpdateGeometry` has only start/end rows for that taper, with corresponding straight side rails. This confirms the requested final-only funnel is absent from these authoring/runtime defaults. `TerrierRacer.Step` delegates powered driving to the existing NWH adapter and only applies righting torque while grounded and significantly tilted. The shared adapter limits motor torque rather than clearing world velocity; no unconditional airborne world-velocity clamp was found there. Measure ordinary ramp departures and contact impulses before blaming a speed clamp or adding stabilization. Keep the verified ordered finish/wall-crash policy and winner-only dessert landing.

## Mattress

`MattressMatch.Present` anchors a finisher at FinishOrigin; only DNF/Inactive views explicitly follow the current leader. Finished-player spectator behavior needs integration. `Arrived` plays feedback and does not stop weather. `SportsChaosWeather` is match-owned and the normal race loop continues ticking it until all racers resolve. No first-finisher weather shutdown was established by this source audit. Reproduce an early valid finisher with other racers still active and inspect actual precipitation/allowed cameras, rather than changing the lifecycle based on an assumption.

## Shared identities and weather

Names currently persist in independent serialized setups: Bowling/Cornhole/Cheese store BowlingSetup; SportsEventSession stores a wrapper containing it; Billiards, Hockey, Air Hockey and Wallball use their own slot DTOs. No stable shared participant-ID registry was found. Read the full master identity requirements before migration; retain every conflicting legacy name deterministically, keep sources/teams/slots separate, permit duplicate display names and do not invent career statistics.

The existing host `LwsWeatherPresetCatalog` contains ten actual presets: Clear, Partly Cloudy, Cloudy, Overcast, Light Rain, Heavy Rain, Thunderstorm, Light Snow, Heavy Snow and Fog, with explicit Weather Maker asset paths. Sports also has existing working Blizzard/Hurricane presentations. Universal weather must coordinate those installed authorities and actual compatible profiles rather than merely relabeling the current five-value Sports enum. Preserve explicit selection, sport-specific random weights and the earlier Fare Fight equal-weight default. Do not modify shared Taxi/vendor source to implement Sports preferences.

## Preservation and workflow

The current accepted/pending milestone receipts identify each tested build. New repair source must begin only after the active Windows batch stops and its completed scope is committed/pushed. Continue the ordered ledgers, reuse NWH, Weather Maker/Weatherade, CNP, Heat, DOTween and existing scene-owned adapters. No new sport, input framework, physics framework, weather framework or save framework was created by this read-only audit.
