# Truck Taxi session continuity and demand

## Integration

- Before each session `Tick`, Bootstrap calls `SetWorldConditions(double elapsedGameMinutes, float localHour, TruckTaxiDemandWeather weather)`. The first argument is monotonic game-clock minutes, not wall-clock time or hour-of-day. The setter ignores backwards clock values. Without a provider, the session remains at neutral noon demand and advances a fallback minute counter from `Tick` delta seconds so isolated tests remain deterministic.
- Map the authoritative weather semantics into `Neutral`, `Rain`, `Storm`, `Snow`, `HeavySnow`, or `Blizzard`. Weather Maker/Weatherade remain visual authorities; the session does not drive either vendor.
- The HUD reads `OfferRemaining`, `PickupRemaining`, `PickupDuration`, `IsRepeatPassenger`, and `DemandEstimatedFareCents`. An expired post-acceptance pickup timer records cancellation and transitions through existing `RideFailed` automatically back to `Available`; Bootstrap clears GPS and pickup markers. `PickupCancelled` and `RepeatPickup` dialogue categories are wired without changing voice assets.
- Stores use `TrySpend(long cents)`. Required rescue/refill service uses `ChargeService(long cents)`. Both reject negative charges. `ShiftEarnings` is gross fare earnings; `WalletBalanceCents` is spendable cash, net of purchases and service charges. A mandatory service may make the balance negative; subsequent fare earnings pay this debt automatically. `ServiceDebtCents` exposes the outstanding deficit. Do not maintain a second wallet.
- Needs integration calls `RecordNeedsEvent(TruckTaxiDriverNeedEvent)` once per gameplay event. `JugsSucceeded`, `JugsSpilled`, and `ContainersThrown` are aggregate session totals; accepted rides snapshot their own counts. The needs state owns inventory, while the session owns stats/history.
- `RideHistory` is an in-memory, append-only view of immutable accepted-ride outcomes. It is not a save provider. `TryGetPassengerContinuity` exposes last completed dropoff ID, monotonic game minute, and district without exposing mutable state.
- External objective coordinators call `RecordObjectiveProgress(TaxiRequestProgress request, float amount, bool complete = false)`. It rejects foreign, inactive, invalid, or no-passenger progress, clamps to the request target, and resolves through the session's normal request event path. For authored non-stop `targetId` requirements, set `CanResolveSpecificTarget` to a predicate that checks an actual runtime target binding before assignment; leaving it unset preserves the existing rejection.

## Selection and continuity

The old session selected a stop pair first, then rolled a passenger from the database's district-filtered candidates. There was no recent-offer memory. At the initial audit, the database had 75 unique registered profiles; Captain Applause (`space-hero`) appeared once with common rarity, weight 1, and no district restriction. Of those 75, 74 had common rarity and weight 1; the remaining legendary profile had weight 0.08. No Captain-specific duplicate, reseed, or special fallback was found. Independent rolls without recent memory explain repeat clustering; this is not a claim to reproduce the designer's historical sequence. The expansion roster now contains 114 profiles. Selection deduplicates stable IDs, rolls a weighted eligible passenger, then selects a valid pair. Weight is `spawnWeight * configured rarity multiplier * time/tag multiplier * recent-offer multiplier`.

Recent history holds the last ten offered IDs by default and applies a configurable 0.2 soft factor. Repeats remain possible. A completed ride records the passenger's last dropoff. For the first 60 game minutes, a repeat starts there. Until 360 minutes, the same location or district is preferred; afterward normal authored pickup districts apply. Explicit `specialTraits` tags (`commuter`, `business`, `social`, `scenic`, `glamorous`) control time-of-day type boosts; legacy freeform archetype text is a fallback. Existing authored `TimeObsessed` tags shorten pickup patience, including Grant Deadline. Profiles without applicable tags receive neutral type weighting.

Continuity snapshots the dropoff position as well as its ID. If that stop permits only
dropoffs (for example a World Builder destination over an existing bay), a recent
repeat uses the nearest enabled legal pickup within `repeatPickupAccessRadiusMeters`
(40 m default). Ties use stable ID order. With no nearby bay the passenger remains
ineligible rather than teleporting elsewhere. Recorded ride history retains the
original destination ID. The final continuity suite has 11 passing tests, including
the nearby-bay and no-distant-relocation cases.

Demand controls offer cadence, fare base/distance/time components, rarity/type weighting, and appreciation probability. Fare demand is locked at offer creation. Heavy snow and blizzard have separate configured multipliers. Pickup patience measures the route from the truck's position at acceptance, then uses a configurable reasonable speed, grace, passenger/personality multiplier, and weather allowance; route service's documented straight-line fallback applies when no graph route exists. The offer timer and pickup timer are independent.

Special Appreciation remains gated by explicit 21+ adult authorship, human casting, presentation flags, excellent satisfaction and five stars. The configured base chance is 35 percent after eligibility, with a moderate evening/late-night multiplier. Player acceptance remains explicit.

## Verification ownership

`TruckTaxiContinuityTests` and `TruckTaxiLateDemandTests` passed in Unity EditMode. The integrated Play Mode run completed a ride, re-offered that passenger at the recorded dropoff, expired a subsequent pickup, and checked automatic Available/GPS cleanup. No asset/prefab/scene YAML was manually created or edited.

Late night is 22:00-03:00 and pre-dawn 03:00-06:00. Defaults increase offer intervals by 1.5x / 2x and fare quality by 1.25x / 1.35x respectively. Explicit rare/weird/nightlife/chaotic/flirtatious weights are exposed in the existing configuration asset. Evening is separate; it no longer captures every hour after 18:00.
