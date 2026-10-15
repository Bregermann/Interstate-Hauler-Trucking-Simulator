# Truck Sports delivery location and build policy

User instruction, 2026-10-15: keep Truck Sports work and outputs inside the main project at F:/Unity Projects/Interstate-Hauler-Trucking-Simulator. Do not deliver multiple numbered or sport-specific validation builds. The only current combined deliverable is Builds/TruckSports/TruckSports.exe with its required accompanying files.

Use the main project for future Sports work after coordinating exclusive Editor/source access with other active tasks. Do not create another C: checkout by default. Keep any strictly necessary transient validation payload inside this project, reuse it where practical and remove it after validation. Keep evidence under ignored Logs and selected report evidence under Documentation/TruckSports. Present the finished build when the requested update is done.

Never close or replace a build the user is currently testing. If its files are locked, retain that running copy temporarily; clean it after natural exit. Preserve unrelated Taxi builds, host settings and vendor work. Use SportsBuilder.Build with its explicit scene list and scoped setting restoration; never regenerate older scenes merely to deliver a build.

The completed eleven-sport source was synchronized from the published Sports branch into the main project without overwriting divergent work. The separate Sports Git branch remains the reviewed publication branch; do not push unrelated root history. The temporary C: managed worktree may be archived after its running test player exits and needed ignored evidence has been preserved.
