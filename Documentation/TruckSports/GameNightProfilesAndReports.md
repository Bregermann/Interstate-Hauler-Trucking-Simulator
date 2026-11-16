# Game-night profiles, statistics and reports

Alpha0.9 implementation guide. The acceptance ledger records verification separately. Common `SportsEventSession` and all seven legacy session owners are integrated; their targeted evidence is in the implementation checkpoint. A new Windows0.9build has not yet been delivered.

## Controller flow

1. Choose Play, then press A/Cross or Start/Menu to join a lobby seat.
2. Left/right chooses a saved driver; A confirms. X creates a driver and Y chooses a temporary guest. The controller that opens the on-screen keyboard owns that edit. Start confirms; B cancels without saving a draft.
3. After choosing a driver, A readies/unreadies that player. X renames; Y opens profile statistics. Up changes the preferred color and Down cycles the existing driver icons.
4. Once everyone is ready, Player1 presses Start/Menu to choose the next sport. Player1 owns shared sport selection and setup menus.
5. Changing match options clears readiness. Player1 can press Y/F6 in setup to switch crew play and AI spectating. Each participating human presses Start/Menu to ready, then Player1 activates the sport's Start button. Humans beyond a sport's supported capacity spectate; the setup banner identifies them.
6. Results can return to the lobby or begin a rematch. Rematch returns to setup for fresh readiness. An abandoned or restarted match earns no winner point.
7. An unassigned replacement controller presses Start in the lobby. When multiple controllers are missing, choose the driver explicitly. A connected participant can take over hosting with Start if the host is disconnected. Identities and points remain with their drivers.
8. Player1's View/Select button opens the end-night confirmation. The final screen shows session standings. X saves/exports the session report; the screen displays its actual file location.

Keyboard/mouse remains optional. Controller-owned players ready with their own controllers. Existing per-sport gameplay and wheel bindings retain their original owners.

## Persistent statistics

From the lobby, press Y on a selected saved driver. Left/right pages through lifetime statistics and all recorded sport/variant records. B returns. X opens deletion confirmation; deleting a profile preserves historical match records.

The existing `SportsParticipantRegistry` uses stable IDs, permits duplicate display names, and retains its original preference keys and backups. Schema4 adds statistics without fabricating historical results. Older text-only match history is retained; it is not reverse-parsed to invent wins or records.

Statistics distinguish competitive outcomes from practice and cooperative activity. Only actual participating humans update saved profile statistics. Guest points remain within the current night. AI can win a sport but does not receive human-profile statistics or permanent winner points.

Session points award one per winning human, including applicable shared winners; raw sport scores are not session points. Completed match IDs deduplicate persistent awards. Records use actual typed result measurements, with separate variants and units; unknown measurement direction produces no invented personal best. Only supported statistics are recorded.

## Local reports

Normal reports live below `Application.persistentDataPath/TruckSports/Sessions`:

- `game-night-<session-id>.json`: versioned structured report.
- `game-night-<session-id>.txt`: readable summary.
- Previous copies use `.bak`; writes replace each file through a temporary file.

The existing event stream lives below `TruckSports/Telemetry` as JSONL. Reports include its actual path. No data is automatically uploaded or transmitted to ChatGPT. The files can be shared manually.

Reports contain the session/version/timestamps, selected human identities, actual AI/control configuration, match settings, official scores/winners, human point awards, interruptions, final standings and bounded diagnostic/performance summaries. The stream retains score/outcome/device/error/performance events. `SportsSessionTelemetry.GameplayEvent` is an extensible sport-event entry point; it does not award points.

The report retains512match summaries and512recent events. Older details remain in the stream, with explicit omission counts. Performance is sampled rather than logged every frame. Poker reports contain public final results, not private card contents.

Performance-sample records additionally carry `simulationTimeScale` and `applicationFocused`; these optional fields describe samples, not other event kinds. Focus loss and common-session pause/resume have explicit events. This preserves the existing focus policy while making unexpected waits easier to distinguish from intentional pauses. The additions remain compatible with schema1 readers that ignore unknown fields.

Automated preferences, reports and telemetry are isolated below a validation scope. Software-controller tests are not physical-controller, listening or human-playtest acceptance.

## Ownership and vendor accounting

- VENDOR ASSETS AUDITED: Heat/uGUI, TMP validation, Input System device/UI APIs, DOTween, existing profile/preferences and sport result adapters.
- VENDOR ASSETS USED: those existing UI/input/animation components; NWH and sport rules remain unchanged authorities.
- RELEVANT ASSETS NOT USED: Pixel Crushers career saves are not substituted for the established independent Sports preferences. World/weather/traffic systems are unaffected.
- CUSTOM SYSTEMS CREATED: game-night semantics, typed result/statistics projections and telemetry report summaries.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
