# Development validation scope

Explicit development probes use private Validation. preference keys through SportsPrefs. Normal play forwards existing keys unchanged. This covers settings, rosters, records and probe backups, so the final normal-company executable can run older acceptance probes without another build. A native fixture verifies normal writes, isolated writes, prefixed reads and isolated deletion. This is a test adapter around existing PlayerPrefs storage, not a new save framework.

Only explicit listed probes mute native Input System devices inside their own process and enable background execution. Injected devices are test sources. Normal startup does neither. Teardown restores local device state and background policy. These checks are not physical-controller or human playtesting.

Evidence goes beneath Logs/TruckSports. The final executable and required companions remain in the single Builds/TruckSports folder. Cab-steering acceptance exits its own test player automatically. User test builds are not closed.

Bog's sixty-second four-view workload uses normal simulation speed, a bounded uniform reservoir across the full interval and actual camera-render completion counts. Editor batch mode can execute physics without normal Game View rendering; its sub-millisecond sample is not a player/GPU benchmark. Windows acceptance must demonstrate automatic four-view rendering before timing results are reported.

Build through LWS.TruckSports.Editor.SportsBuilder.Build:21 explicit scenes, TruckSports startup,19 sports and the Keepaway camera variant. Exact host product/build-settings restoration and a broader12-file host snapshot are verified separately. Generated builds, logs, caches and credentials stay out of Git.
