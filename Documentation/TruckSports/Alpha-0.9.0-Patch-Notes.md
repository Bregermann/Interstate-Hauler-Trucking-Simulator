# Truck Sports: Party! — Alpha 0.9.0: Release Foundation

Release candidate, blocked on native Windows shutdown and a visible full-match failure. Available separately at `Builds/TruckSportsCamera/TruckSports.exe`; the working installation is preserved. These notes describe implemented features, not a certified release.

## General

- Added Game Night: a shared lobby for up to four local human drivers, with AI competitors in sports that support them.
- Player 1 chooses the sport and controls shared match settings. Other drivers keep control of their own profile and ready status. The committee has been replaced with a steering wheel.
- Game Night carries driver identities, colors, assigned controllers and session points between sports.
- Existing sports, rules, artwork, weather options and vehicle systems remain in place. This update adds no new sports.

## Your Crew

- Drivers can create, select, rename and remove local profiles, choose an icon and preferred color, or play as a guest.
- Added a controller-operated on-screen keyboard for essential name entry, including capitals, numbers, spaces, clear, backspace, confirm and cancel.
- Only the controller opening the keyboard owns its controls. Canceling or disconnecting discards the draft rather than creating half a driver.
- Profile identity uses a permanent ID. Two drivers named Dave may now settle their differences on the field without sharing a statistics sheet.
- Existing profile data is migrated through the established save boundary, with backups retained.

## Controllers and Readiness

- Compatible Unity Input System gamepads can join after launch. Specialty peripherals do not automatically claim a player seat.
- Each human readies independently. Joining, changing relevant setup, restarting or rematching requires a fresh ready check.
- Lost controllers keep their driver seat. Replacement controllers can claim a disconnected seat; multiple missing seats require an explicit choice.
- Added explicit host takeover when the host disconnects.
- Shared menus use the host's controller; another driver's inputs cannot steer sport selection.
- Legacy sport input presets cannot silently replace Game Night's controller assignments.

## Results and Winner Points

- A completed competitive sport awards one Game Night point to each actual winning human. Sport scores remain sport scores.
- AI may win a match, but does not earn permanent human-profile points. The robots have been informed.
- Interrupted matches do not award winner points. Duplicate completion notifications cannot award a second point.
- Practice and cooperative results are recorded separately from competitive wins and losses.
- Final standings show the night's human driver points. There is no played-sports checklist or completion grind.
- Integrated the separate Bowling, Cornhole, Cheese Rolling, Hockey, Air Hockey, Wallball and Billiards session owners with the shared lobby flow.
- Result adapters read existing rule authorities, including complete Billiards series results, team winners and completed Bowling bonuses.

## Statistics

- Added permanent profile totals for matches, competitive outcomes, winner points, game-night visits, play time and last played.
- Added per-sport results, placements and personal records where the existing gameplay supplies a meaningful measurement.
- Race times prefer lower values; comparable high scores prefer higher values. Unsupported measurements are not invented.
- Statistics pages are controller-operated. Guest results do not become permanent profiles.

## Local Reports

- Each Game Night records structured match settings, participants, outcomes, point awards, interruptions and final standings.
- Added local JSON and readable text summaries, with controller export from final standings.
- Extended the existing bounded telemetry stream with Game Night IDs, device events, diagnostics and performance samples.
- Reports remain local. Nothing is automatically uploaded to ChatGPT or another service.
- Private Poker cards are excluded from telemetry.

## Fixes

- Fixed a stale session reference being polled during scene transitions.
- Fixed detached on-screen keyboard validation relying on a rendered TMP caret mesh.
- Fixed missing Unity UI components being mistaken for usable CanvasGroups.
- Fixed the original title canvas drawing over the new Game Night lobby.
- Corrected result projections that could treat Jenga's collapse loser or Goose's last-round leader as the overall champion.
- Preserved independent profile statistics when drivers share the same display name.

## Alpha Notes

Software-controller tests cover joining, keyboard ownership, ready gates, actual match completion, rematches, persistence and report export. Windows completed two actual matches and retained both profiles' results after executable relaunch. The corrected lobby and keyboard were inspected at720p. Native process exits still fail, and a separate visible Dance-Off run hit its deadline. These are not physical-controller, listening,1080p visual or human-playtest results. Exact observations and remaining work are recorded in the implementation report and acceptance ledger.
