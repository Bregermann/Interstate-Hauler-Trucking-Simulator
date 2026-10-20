# Truck Sports: Party!

The current authorized sprint is Dock Diving, Poker, original PS1/PS2-inspired sport logos and the menu overhaul. Use the [permanent acceptance ledger](../../TRUCK_SPORTS_ACCEPTANCE_LEDGER.md) for exact requirement status, implementation evidence, automated verification and remaining work. Historical requests are retained in the [dated archive](../../Documentation/TruckSports/Archive/2026-10-20-LegacyBacklog/), rather than automatically reopened.

[Presentation changes and validation](../../Documentation/TruckSports/PresentationSprint.md) · [Build information](BUILD-INFO.md) · [Original logo prompts and authoring](../../Tools/TruckSports/ArtworkSources/README.md)

Choose a sport from the paginated logo catalogue. Hover or focus a sport to see its larger artwork, representative gameplay and participant information. Use Your Crew to edit shared names/colors; Back returns to the page you opened it from. Each sport retains its existing local input, AI and mode configuration.

The combined Windows executable is `Builds/TruckSports/TruckSports.exe`; keep all companion files. Build with `LWS.TruckSports.Editor.SportsBuilder.Build`, using the explicit Sports-only scene list. Do not use CreateValidateBuild for delivery: it reauthors scenes. Normal builds must preserve saved courses, designer settings, unrelated Taxi/host changes and vendor source.

Scene artwork is saved under `Assets/TruckSports/Art/Logos`. Retain each PNG's `.meta` GUID when replacing art. Explicit additive menu/branding upgrades are available in the project-owned Editor tools. They do not generate images or recapture previews during normal builds.

Previous milestones and their historical hashes/test claims are preserved in `Documentation/TruckSports/Archive/2026-10-20-LegacyBacklog/Sports-README.md` and `Sports-BUILD-INFO.md`. Those records do not establish acceptance of the current executable.
