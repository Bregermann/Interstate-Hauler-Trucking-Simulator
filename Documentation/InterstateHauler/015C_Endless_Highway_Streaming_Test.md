# Prompt 015C - Endless Highway Streaming Test

## Road-End Root Cause

The observed `StreamingHighwayValidation` road end was expected exhaustion of the original Prompt 014 finite validation corridor.

The previous scene used a five-chunk manifest and a finite graph whose centerline ended around 3350 m. The load queue was not the root cause.

## Scene Distinction

`IH_50MileFloatingOriginValidation` remains finite and exact.

`StreamingHighwayValidation` is now the effectively endless development torture test.

## Endless Model

The endless validation highway uses:

- `LwsEndlessHighwayModel`
- `LwsEndlessStreamingHighwayController`

Logical segment length:

`3218.688 m` / `2.0 mi`

Physical chunk pool:

`7` slots

Default neighborhood:

- 2 segments behind
- current segment
- 4 segments ahead

Road ahead target:

`6400 m`

Emergency threshold:

`1600 m`

## Logical Vs Physical Identity

Logical global segment IDs are generated as:

`IH_ENDLESS_TEST_SEG_000000`

`IH_ENDLESS_TEST_SEG_000001`

`IH_ENDLESS_TEST_SEG_000002`

Physical slots are recycled:

`POOL_CHUNK_00`

`POOL_CHUNK_01`

`POOL_CHUNK_02`

A physical slot may represent different logical segments over time.

## Floating Origin

Logical segment placement is global and double precision.

Unity presentation position is calculated from:

`GlobalSegmentPosition - CurrentOriginOffset`

The controller refreshes slot local placement after origin-version changes.

## Road Graph / GPS

The active road graph is a moving finite window around the player.

GPS should not route to infinity. The Development Control Center can request finite routes:

- 10 miles ahead
- 25 miles ahead
- 50 miles ahead

## Traffic Recycling

When the logical window advances, the UTS adapter rebuilds its project-owned path presentation from the new LWS graph and seeds the minimum local traffic presence for the new time-of-day demand.

Vendor UTS source is not modified.

## Diagnostics

The Streaming tab shows:

- logical segment
- physical pool size
- road ahead available
- highest segment generated
- lowest segment retained
- chunks recycled
- chunk load failures
- per-slot logical segment and local position

Mileage milestone logs are emitted at:

10, 25, 50, 100, 250, and 500 miles.
