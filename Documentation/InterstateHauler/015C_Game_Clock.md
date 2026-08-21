# Prompt 015C - Game Clock

## Summary

Prompt 015C adds a project-owned gameplay clock for Interstate: Hauler.

Runtime authority:

- `ILwsGameClockService`
- `LwsGameClockService`
- `LwsGameClockCoordinator`
- `LwsGameClockSnapshot`
- `LwsGameDateTime`
- `LwsGameClockTuning`

The default validation start time is 8:00 AM on 2026-06-01.

## Authority

LWS game clock owns:

- game date
- day of week
- time of day
- game-time scale
- paused game-time state
- future job deadlines
- future economy schedules
- future save/load time

Weather Maker owns:

- visual sun/moon/sky
- atmospheric day/night presentation
- lighting presentation

Weather Maker does not own semantic game time.

## Weather Maker Sync

`LwsWeatherMakerAdapter` now resolves `ILwsGameClockService`.

When a clock exists:

1. `LwsGameClockCoordinator` ticks game time with unscaled real delta.
2. `LwsWeatherMakerAdapter` copies `CurrentSnapshot.timeOfDayHours` to `ILwsWeatherService`.
3. Weather Maker `TimeOfDay` is updated through the weather adapter.
4. Weather Maker `Speed` and `NightSpeed` are held at `0f`.

This prevents two independent day/night clocks from drifting apart.

## Development Controls

The Development Control Center Weather tab now exposes:

- `-1 HOUR`
- `+1 HOUR`
- `6 AM`
- `8 AM`
- `NOON`
- `5 PM`
- `8 PM`
- `MIDNIGHT`
- `PAUSE` / `RESUME`
- `1x`
- `6x`
- `20x`
- `60x`

These controls change game time only. They do not change Unity physics `Time.timeScale`.

## HUD

`LwsGameClockHud` creates a small Screen Space Overlay clock such as:

`MON 8:42 AM`

The HUD is deliberately simple development presentation. Prompt 016 can persist the snapshot, and future production UI can replace the presentation without changing clock authority.

## Save Readiness

Future save data should store LWS game date/time from `LwsGameClockSnapshot`, not Weather Maker internal time.
