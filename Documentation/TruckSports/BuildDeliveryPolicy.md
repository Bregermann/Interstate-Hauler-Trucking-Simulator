# Truck Sports delivery location and build policy
# Truck Sports build delivery

Keep source, validation projects, tools, evidence and temporary work inside the Interstate: Hauler project on F:. Do not create C: worktrees or alternate user-facing build collections.

Deliver one canonical finished Windows folder: `Builds/TruckSports/`, with `TruckSports.exe` and all companion files. Use the explicit Sports-only scene list through `SportsBuilder.Build`; normal builds validate saved scenes and do not regenerate them. Temporary product/player-setting overrides must be restored on success or failure.

Validate changes in the owned isolated Editor before building. Produce a new combined executable when the completed changes are ready for player validation. Do not produce a separate build for every small fixture or leave numerous validation-build folders. Label a candidate QA when release acceptance still fails; identify the actual assembly/manifest and tests for that binary.

Do not ask the user to close games they are testing or stop those processes. Preserve unrelated Taxi, host-project, vendor and imported asset work. Build output, caches, logs, installers and crash dumps are excluded from Git unless an established explicit tracking rule says otherwise.
User instruction, 2026-10-15: keep Truck Sports work and outputs inside the main project at F:/Unity Projects/Interstate-Hauler-Trucking-Simulator. Do not deliver multiple numbered or sport-specific validation builds. The only current combined deliverable is Builds/TruckSports/TruckSports.exe with its required accompanying files.

Use the main project for future Sports work after coordinating exclusive Editor/source access with other active tasks. Do not create another C: checkout by default. Keep any strictly necessary transient validation payload inside this project, reuse it where practical and remove it after validation. Keep evidence under ignored Logs and selected report evidence under Documentation/TruckSports. Present the finished build when the requested update is done.

Never close or replace a build the user is currently testing. If its files are locked, retain that running copy temporarily; clean it after natural exit. Preserve unrelated Taxi builds, host settings and vendor work. Use SportsBuilder.Build with its explicit scene list and scoped setting restoration; never regenerate older scenes merely to deliver a build.

The completed eleven-sport source was synchronized from the published Sports branch into the main project without overwriting divergent work. The separate Sports Git branch remains the reviewed publication branch; do not push unrelated root history. The temporary C: managed worktree may be archived after its running test player exits and needed ignored evidence has been preserved.
