# Winter dawn palette — Q22-SKY-SNOW-COLOR-01

This is a focused Q22 weather/time repair, not completion of the entire Q22, Carnival 0.6 or Impossible 0.7 directives. Human playtesting, physical controllers, listening, full-match gameplay, snow-track masks and isolated performance acceptance are not claimed.

## Change and cause

Weather Maker forecast profiles contain precipitation/cloud/sky/fog configuration, but do not replace the separate day/night profile. The existing match-time adapter therefore retained clear dawn's orange sun and ambient gradients during snowfall. Actual Snow Morning rendering and vendor sun color reproduced this in Fare Fight and Truck Snake.

`SportsMatchTime.ApplySnowPalette` now coordinates nine public fields on its existing unsaved runtime-owned day/night profile: sun tint and the four day/four dawn-dusk ambient gradients. Snow colors derive from untouched source keys using a neutral/cool (.82,.91,1) palette and original maximum RGB brightness. Key times, gradient modes, alpha keys and color alpha are retained. Sun/moon movement, intensities, night ambient gradients and authored lamps retain their original authorities. Indoor artistic lighting opts out. Clear/rain restores the original gradients exactly; repeated forecasts do not compound tint.

`SportsChaosWeather.ConfigurePreset` supplies the selected registry entry's actual Snow precipitation property after resolving its metadata. The existing catalog-free shared fallback coordinates the same palette from its selected snow/blizzard kind. No new manager, scene search, serialized setting, scene rebuild, renderer change or vendor edit was introduced. The legacy Cheese/Base scenes already use the shared presentation adapter; their gameplay is unchanged.

## Evidence and validation

- Controlled diagnostic first attempt: Editor26352 exited0; two diagnostic executions passed, but freezing scaled time prevented the vendor interval update. It is retained as **inconclusive**, not palette acceptance.
- Corrected controlled diagnostic: Editor39164 exited0/58.26s; two tests passed. Explicitly refreshed only the unsaved vendor profile and restored its interval. Actual sun changed from(.888,.411,.054) to(.728,.808,.888); warm pixels Fare15622→40 and Snake28729→88 out of57600. All four comparison images were inspected. Historical exact source and results retained separately.
- Production `SnowLightingIntegrationTests`: Editor40396 exited0/115.62s;2 passed/50.2704479s test duration. Saved Fare Fight and Snake scenes exercise Clear→Snow→Heavy Snow→Blizzard→Rain→Snow→Clear, stable match clock, unchanged intensities/night ambient/source profile, unchanged gradient times/alpha/mode, repeated application, indoor exception and snow rematch. Same frozen-pose warm-pixel comparison: Fare9914→85; Snake20157→39. All16 native720p/1080p captures inspected.
- Existing `WeatherTransitionTests`, `MatchTimeIntegrationTests`, `LegacyMatchTimeTests`: Editor23504 exited0/82.41s;3 passed/48.2837739s. Modern and legacy match-clock bindings and Snow→Hurricane precipitation retirement retained.
- Existing `TimeOfDayPreferenceTests`: Editor39364 exited0/33.72s;3 passed/.13636s.
- Combined Windows development candidate: Editor40004 exited0/278.69s; build summary254.4198337s,0errors,90warnings,60scenes,548companion files. Exact430-owned-C# archive retained. Warning record includes obsolete Unity API and vendor/compiler notices; no warning suppression or vendor cleanup was performed.
- Full candidate SHA256: `1f91e8226d59e9a0375075659418cb54418c1cc3c9c957ad1593550325707ba1`; managed assembly: `e76acf99ddce5357a5b90668fd0b45d8cc59f711262ee9e357281e98f996913f`.
- Windows player42880 exited0/93.21s; original nine-case weather/time matrix for Fare Fight and Truck Snake passed157checks/0errors. All36 actual720p/1080p frames individually inspected;4setup frames excluded. Snake terrain snow is cool at dawn and retains detail in Blizzard Night; warm local lamps remain visible. Fare still has green exposed outdoor surfaces, and urban roads remain bare: separate snow-surface requirement remains open. These automated starts/rendering are not complete human matches.

The native Fare dynamic-transition images have limited visible snow coverage and dark roads. They establish palette/lifecycle behavior, not complete snowfall or snow-road acceptance. Initial/rematch countdown frames are not full active-gameplay HUD acceptance. These limits remain recorded rather than inferred from passing state checks.

## Files and preservation

Production: `Assets/TruckSports/Scripts/SportsMatchTime.cs`, `SportsChaosWeather.cs`. Tests: `SnowLightingIntegrationTests.cs` and explicit historical `SnowDawnPaletteDiagnosticsTests.cs`, with metas. Existing diagnostic comparison is now marked explicit so an old orange baseline cannot masquerade as an ordinary post-fix acceptance test. No original character, truck, environment, weather-profile or scene asset was changed.

Candidate: `Builds/TruckSportsCamera/TruckSports.exe` with all companion files. Prior complete40fc candidate retained in `.sports-validation/Recovery/UrbanVenueCandidateBeforeSnowPalette-20261024`. Accepted canonical `Builds/TruckSports/TruckSports.exe` remains60d4716f…; this slice has not been promoted or pushed publicly. The separate confirmed Unity6000.4.10f1 Dock shutdown blocker remains open. The user's instruction to retain the current Editor is honored.

Evidence copied into `Documentation/TruckSports/Evidence/2026-10-24-Winter-Dawn-Palette/`; exact raw logs and process/build/source receipts remain under `Logs/TruckSports/PlaytestRepair/`. Current sprint status is in `TRUCK_SPORTS_ACCEPTANCE_LEDGER.md`.

## VENDOR ASSETS AUDITED

Installed Weather Maker day/night profile, celestial objects and forecast-profile APIs; existing shared Weather Maker catalog and match adapter; existing Weatherade-backed surface coordination. The controlled comparison isolated the public vendor gradients before production editing.

## VENDOR ASSETS USED

Weather Maker remains the sole sun/moon/ambient/cloud/precipitation presentation authority. Existing runtime clone and profile-update path reused. Weatherade surfaces, NWH vehicles and original saved scenes retained.

## RELEVANT ASSETS NOT USED

No new post-processing/color-grading layer: it would obscure the responsible vendor gradients and also recolor UI/indoor art. No replacement weather, exposure or lighting framework was needed.

## CUSTOM SYSTEMS CREATED

No new runtime system. A small weather-aware coordination method extends the existing adapter. Added focused native regression coverage and retained the controlled investigation separately.

## VENDOR SOURCE MODIFIED

None.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

None. Vendor sun movement, gradient evaluation, ambient illumination and profile transitions remain in Weather Maker.
