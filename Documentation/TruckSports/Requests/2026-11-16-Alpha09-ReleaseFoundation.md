# CODEX / SOL 6.1 HIGH — MASTER IMPLEMENTATION PROMPT
# INTERSTATE: HAULER — TRUCK SPORTS: PARTY!
## ALPHA 0.9.0 — RELEASE FOUNDATION
### GIT CLEANUP, COMPLETE BACKLOG INVENTORY, CONTROLLER-FIRST MULTIPLAYER, PERSISTENT PROFILES, SESSION STATISTICS, ANALYTICS & WINDOWS DELIVERY

**PRIORITY: HIGHEST**

**This prompt supersedes all older development prompts as the active execution plan.**

Do not resume an old task queue or development roadmap automatically.

We are deliberately changing development priorities because the previous workflow consumed excessive development time and Codex rate limits on repetitive validation, source-verification frameworks, test-harness maintenance, and prolonged attempts to perfect individual features.

Our immediate goal is to preserve and organize existing work, establish a clean GitHub checkpoint, inventory everything outstanding, and then build the complete controller-first Release Foundation.

**MANDATORY EXECUTION ORDER:**

1. **GIT CLEANUP FIRST:** Organize existing work, make logical commits, and push all appropriate project work to the existing remote repository.
2. **BACKLOG INVENTORY SECOND:** Present a complete list of every outstanding requirement from earlier versions and existing development records.
3. **ALPHA 0.9.0 THIRD:** Immediately implement the Release Foundation before any existing unfinished backlog work.
4. **WINDOWS DELIVERY:** Produce the playable build and report actual verification results.
5. **PRESERVE FUTURE WORK:** Keep all other requirements for later versions without beginning those expansions.

Do not ask for another approval between these stages unless an actual external authorization, unresolved destructive operation, or access limitation makes proceeding unsafe or impossible.

---

# 1. MANDATORY FIRST PHASE — COMPLETE GIT CLEANUP, ORGANIZATION, COMMIT AND PUSH

**BEFORE STARTING THE BACKLOG INVENTORY OR MODIFYING GAMEPLAY CODE, CLEAN UP THE EXISTING GIT WORKSPACE.**

This is a direct authorization to organize, stage, commit, and push the existing legitimate project work.

The purpose is to get the repository into a clean, understandable, recoverable state before starting Alpha 0.9.0.

This is not permission to delete unfinished work, discard changes, or rewrite existing history.

## 1.1 Inspect the existing Git state

Start with a lightweight repository inspection.

Determine:

- Current working directory and repository root.
- Current branch and HEAD.
- Configured remotes.
- Upstream tracking branch.
- Working-tree status.
- Staged changes.
- Unstaged changes.
- Untracked files.
- Existing ignored files.
- Existing commits not yet pushed.
- Existing local branches with relevant unfinished work.
- Submodules, if present.
- Git LFS configuration, if already in use.
- Relevant project-specific `.gitignore` rules.

Use ordinary targeted Git commands.

Do not begin an exhaustive historical audit of every commit.

Do not repeatedly hash the entire project.

Do not create another preservation/provenance framework.

Identify what needs to be saved and how to commit it safely.

## 1.2 Preserve all legitimate existing work

**Do not lose anything valuable.**

Preserve all existing:

- Gameplay scripts.
- Unity scenes.
- Prefabs.
- Materials.
- Textures and models.
- Audio and VFX.
- Sport assets.
- Custom sport logos.
- Vehicle configurations.
- Input settings.
- AI implementations.
- UI systems.
- Sports registration data.
- Save and persistence code.
- Existing Alpha 0.8.0 Dragon expansion work.
- Existing bug fixes.
- Development documentation.
- Acceptance ledger.
- Roadmap records.
- Outstanding task information.
- Relevant test scripts.
- Reproducible build configuration.

Do not delete or reset partially implemented features simply because they are unfinished.

Do not throw away changes because they have not passed every acceptance test.

Unfinished but meaningful work should be preserved in clearly described commits.

Where incomplete functionality could disrupt the stable game, preserve it on the appropriate development branch rather than overwriting the known-good release.

## 1.3 Clean up the repository structure

Organize the repository sensibly.

Where beneficial and safe:

- Put relevant documentation in its established documentation directories.
- Consolidate duplicate temporary planning documents.
- Keep current acceptance and roadmap records clearly identified.
- Organize legitimate test scripts alongside their relevant systems.
- Keep build scripts in an appropriate tools/build directory.
- Remove accidental temporary copies from Git tracking when they are provably disposable and safely preserved elsewhere.
- Correct inappropriate tracking of generated Unity caches.
- Improve `.gitignore` where necessary.

Do not create an entirely new directory structure when the existing one is already functional.

**Unity asset moves require special care.**

Do not casually move scenes, prefabs, scripts, or assets in ways that break serialized references.

Any necessary Unity asset relocation must preserve the associated `.meta` files, GUIDs, references, package compatibility, and intended asset paths.

Use Unity-aware moves where appropriate.

Prefer leaving functional production assets in place over reorganizing them purely for cosmetic reasons.

Do not rewrite game systems simply to make the repository look cleaner.

## 1.4 Keep generated clutter out of Git

Identify and exclude appropriate disposable files such as:

- Unity `Library` caches.
- Temporary Unity import data.
- `Temp` directories.
- `Obj` directories.
- Generated intermediate build files.
- Disposable screenshots.
- Repeated temporary validation project copies.
- Redundant compiler outputs.
- Massive temporary diagnostic logs.
- Obsolete generated proof archives.
- Temporary downloaded packages.
- Local machine-specific caches.

Do not automatically delete all logs or evidence.

If a diagnostic record is needed to explain a remaining bug or preserve a meaningful test result, retain the relevant information.

Prefer a concise permanent report plus necessary reproducible tests over committing thousands of redundant evidence files.

Keep large local evidence archives outside Git when appropriate.

Do not accidentally remove important assets merely because they are large.

Do not use `git clean -fdx`, destructive directory wipes, or other broad cleanup operations.

## 1.5 Check for secrets and inappropriate files

Before committing, inspect the proposed staged changes for:

- API keys.
- Passwords.
- Personal access tokens.
- Credentials.
- Private configuration.
- Machine-specific sensitive paths.
- Other secrets that should not enter Git history.

Do not print secret values into reports.

Do not commit proprietary third-party asset source files when their licensing or repository handling prohibits doing so.

Preserve legitimate owned/licensed project assets using the existing authorized repository arrangements.

Do not upload unrelated personal files or private data.

If an asset requires Git LFS, use the existing LFS configuration or an appropriate compatible configuration.

Do not migrate the entire repository to a new storage system without a concrete need.

## 1.6 Create logical, clean commits

Do not dump everything into one giant unintelligible commit if the existing changes can be separated safely.

Group related changes into meaningful commits.

Suggested categories include:

**Commit A — Dragon sports gameplay**
- Relevant Dragon Riders, Hoard Heist, Egg Delivery, and Escape work.

**Commit B — AI, vehicle physics, and gameplay fixes**
- Relevant functional corrections.

**Commit C — Scenes, assets, and presentation**
- Scenery, models, materials, logos, and UI assets.

**Commit D — Testing and development tooling**
- Legitimate reusable validation scripts and supporting utilities.

**Commit E — Documentation and acceptance records**
- Backlog, implementation notes, test results, and roadmap records.

These are illustrative groupings, not a requirement to create five commits when the actual changes call for a different structure.

Preserve dependency order.

Do not split changes into commits that unnecessarily break compilation between commits.

Use accurate, descriptive commit messages.

Examples:

- `feat(truck-sports): preserve alpha 0.8 dragon gameplay`
- `fix(truck-sports): improve AI navigation and match lifecycle`
- `art(truck-sports): integrate dragon kingdom presentation`
- `chore(truck-sports): organize validation tools and ignore generated files`
- `docs(truck-sports): reconcile implementation and acceptance records`

Commit unfinished work accurately; do not falsely describe unverified functionality as complete.

Stage changes deliberately.

Inspect staged diffs before committing.

Avoid broad staging that accidentally includes generated files or secrets.

## 1.7 Preserve existing history

Absolutely do not:

- Force-push.
- Delete remote branches.
- Rewrite published commit history.
- Perform destructive hard resets.
- Discard unresolved merge conflicts.
- Delete meaningful local branches.
- Rewrite unrelated projects' commits.
- Squash existing shared history merely for cosmetic cleanliness.
- Alter unrelated users' work.

Prefer normal additive commits.

If a remote branch has advanced, fetch and assess the divergence before integrating.

Use a safe merge or another appropriate non-destructive reconciliation strategy.

Do not blindly rebase branches containing shared or uncommitted work.

## 1.8 Push the existing work to GitHub

After organizing and committing the legitimate existing changes:

**PUSH THE WORK TO THE CORRECT EXISTING GITHUB REPOSITORY.**

Use the configured remote and appropriate development branch.

Do not accidentally create a second unrelated repository.

Do not push development work directly over a protected stable release branch if the repository's current workflow requires a feature/development branch.

If needed, create a clearly named working branch such as:

`develop/truck-sports-alpha-0.9.0-foundation`

Use the existing branch if that is already the proper development location.

Set upstream tracking when appropriate.

Push the resulting commits normally.

If the configured remote rejects the push, investigate the specific reason without repeatedly retrying blindly.

If authentication, permissions, protected-branch policy, or a genuine remote conflict prevents the push, preserve all local commits and report the blocker accurately.

Do not claim a successful backup merely because a local commit exists.

Do not resort to force-pushes.

## 1.9 Verify the Git cleanup succeeded

Perform one concise final verification.

Confirm:

- Legitimate work has been committed.
- Relevant commits are present locally.
- The correct remote and branch were used.
- The push succeeded, if remote access is available.
- The pushed branch contains the expected commits.
- No important uncommitted project changes were accidentally omitted.
- Remaining untracked files are intentionally excluded, ignored, or clearly documented.
- Any known preexisting unfinished work is preserved.

Do not run extensive game-world acceptance tests just to verify Git cleanup.

A targeted Git verification is sufficient.

## 1.10 Git cleanup deliverable

Before proceeding, provide a concise report:

### GIT CLEANUP COMPLETE

Include:

- Repository.
- Branch.
- Remote.
- Commits created.
- Major groups of work preserved.
- Files intentionally excluded from Git.
- Any outstanding legitimate local changes.
- Push status.
- Final commit SHA.
- Any cleanup issues requiring future attention.

**This must be the first completed deliverable.**

Keep the cleanup bounded and practical.

Do not spend the entire development session reorganizing historical files.

Once the work is safely committed and pushed, immediately proceed to the backlog inventory.

If pushing is blocked by permissions or authentication, clearly report that condition. Do not delete or modify local work in response. A safe read-only inventory may continue, but do not begin risky production changes on the assumption that an unsuccessful push provided a backup.

---

# 2. OFFICIAL VERSION ROADMAP — REPLACES ALL PREVIOUS VERSION ASSIGNMENTS

The definitive roadmap is:

| Version | Name | Scope |
|---|---|---|
| Alpha 0.8.0 | Existing Dragon Expansion | Preserve completed work and record unfinished requirements |
| Alpha 0.9.0 | Release Foundation | Complete controller-first multiplayer and game-night infrastructure |
| Alpha 0.10.0 | Hitches, Horrors & High Water | 22 new sports, previously called Alpha 0.9.0 |
| Alpha 0.11.0 | Tricks, Tartan & Total Chaos | 28 new sports, previously called Alpha 0.10.0 |

The original 0.9.0 becomes 0.10.0.

The original 0.10.0 becomes 0.11.0.

The Release Foundation is the **only active feature-development milestone** for this assignment.

Preserve all other approved requirements.

Do not begin either later expansion during this task.

Do not create separate party-pack products yet.

---

# 3. MANDATORY SECOND PHASE — COMPLETE EXISTING BACKLOG INVENTORY

**AFTER GIT CLEANUP, BUT BEFORE IMPLEMENTING ALPHA 0.9.0, PRESENT A COMPLETE LIST OF ALL EXISTING OUTSTANDING WORK.**

This is an explicit user request.

I want to know everything that Codex still considers unfinished across the project's historical backlog.

The inventory is for visibility, not permission to restart old work.

## 3.1 Sources to inspect

Inspect relevant:

- Authoritative Truck Sports acceptance ledger.
- Existing backlog and roadmap.
- Alpha 0.8.0 implementation records.
- Earlier versions' incomplete requirements.
- Previous development handoff documents.
- Known bug reports.
- Existing feature requests.
- Previous accepted design requirements.
- Current implementation status.
- Relevant Git commits.
- Current unfinished development checkpoints.
- Previously deferred user requests.

Use available project documentation rather than reconstructing the entire historical conversation unnecessarily.

Do not run extensive Unity Play Mode tests merely to generate the inventory.

## 3.2 Include every outstanding requirement

Inventory all unique unresolved items involving:

- Existing sports.
- AI and gameplay.
- Vehicle physics.
- Multiplayer.
- Cameras.
- User interface.
- Controls.
- Scoring.
- Menus.
- Logos and presentation.
- Weather.
- Environments.
- Save/load.
- Assets.
- Build packaging.
- Runtime bugs.
- Technical integration.
- Human playtest requirements.
- Unverified implementation.
- Previously committed unfinished work.

Include unfinished Dragon expansion requirements.

Do not arbitrarily limit the list to the newest or most visible tasks.

Do not reopen already verified work without evidence of an actual problem.

## 3.3 Required inventory fields

For every item provide:

1. Requirement ID.
2. Original version or system.
3. Sport or feature.
4. Outstanding work.
5. Implementation status.
6. Relevant file/scene where available.
7. Existing evidence.
8. Whether it directly blocks Alpha 0.9.0.
9. Recommended later milestone.

Use the established statuses:

- MISSING
- PARTIAL
- IMPLEMENTED BUT UNVERIFIED
- VERIFIED
- BLOCKED

For planning purposes also distinguish deferred requirements, superseded execution instructions, and items whose status is genuinely unknown.

Do not mark an actual gameplay feature superseded merely because the original prompt is old.

## 3.4 Deliver the complete inventory

Produce:

**TRUCK SPORTS — COMPLETE PRE-0.9.0 BACKLOG INVENTORY**

Include all unique outstanding requirements that can be established from the available project records, grouped by version and system, with totals by status.

Preserve this report inside the existing project documentation structure.

Clearly identify missing source records or unknown statuses.

**AFTER PRESENTING THE BACKLOG, DO NOT BEGIN WORKING THROUGH IT.**

Proceed directly into Alpha 0.9.0.

No additional approval is required.

---

# 4. SUPERSEDE OLD PROMPTS AND DEVELOPMENT ORDERS

All older prompts are historical references rather than current execution instructions.

They may contain valid:

- Requirements.
- Designs.
- Technical details.
- Existing implementation evidence.
- Feature descriptions.
- Development decisions.

Preserve those.

However, old task ordering is no longer authoritative.

Do not:

- Resume an old "finish the backlog" command.
- Continue Alpha 0.8.0's endless validation.
- Prioritize Hoard Heist over the Release Foundation.
- Resume scenery and terrain polish.
- Implement the future 22-sport expansion.
- Implement the future 28-sport expansion.
- Recreate old validation frameworks.
- Revert completed work to an older checkpoint unnecessarily.

Only address an existing backlog defect during 0.9.0 if it directly blocks the Release Foundation's core workflow or causes a serious regression.

Keep such fixes narrowly scoped.

---

# 5. DEVELOPMENT EFFICIENCY — PREVENT WASTED RATE LIMIT

These rules are mandatory.

## 5.1 No endless validation chains

Do not repeatedly run:

- Project-wide hash scans.
- Multi-gigabyte unchanged-file comparisons.
- Full validation-project rebuilds.
- Repeated source-provenance reconstruction.
- Tests of tests of tests.
- Excessive compiler-origin checks.
- Unnecessary world-geometry revalidation.
- Redundant historical evidence generation.
- Repeated identical Play Mode tests.
- Unnecessary screenshot captures.

Use targeted Git diffs, compilation, and appropriate functional tests.

Reuse previously passing evidence when the relevant code and data remain unchanged.

## 5.2 Bounded corrective attempts

When a test or implementation fails:

1. Determine the real cause.
2. Correct the smallest relevant area.
3. Retry the focused test.

After two unsuccessful corrective attempts for the same underlying problem, change strategy.

Do not continue repeating the same validation chain indefinitely.

For noncritical issues, record the problem and continue.

For critical gameplay blockers, isolate the system and pursue the simplest reliable correction.

Do not falsely certify unresolved blockers.

## 5.3 Testing hierarchy

**Level 1 — Fast checks**
- Compile relevant code.
- Inspect console errors.
- Run focused unit tests.

**Level 2 — Functional checks**
- Exercise affected UI/gameplay behavior.
- Verify appropriate state changes.

**Level 3 — Integration checks**
- Complete representative multiplayer session flows.
- Verify persistence and transitions.

**Level 4 — Windows release smoke test**
- Build executable.
- Launch it.
- Exercise a short representative game-night session.
- Verify return to menu and subsequent sport launch.

Do not run expensive release-level validation after every small change.

## 5.4 No new validation empires

Do not create additional generalized:

- Source authority systems.
- Evidence lineage frameworks.
- Universal validation orchestrators.
- Parallel acceptance ledgers.
- Massive audit archives.
- Historical verification reconstruction scripts.

Use the existing infrastructure and concise, meaningful evidence.

## 5.5 Implementation takes priority

Aim for approximately 80–90% of effort on actual implementation, integration, and gameplay fixes.

Keep routine testing and documentation proportionate.

These percentages are guidance, not permission to ignore real failures.

If validation begins consuming more work than implementing the actual feature, reassess.

## 5.6 Honest results

Never mark something verified because code compiled.

Never claim physical controller testing if no controllers were available.

Never claim the Windows build passed if it was not launched.

Never mark a sport fully functional when match progression is broken.

Keep the acceptance ledger accurate.

**Eliminate redundant verification, not accountability.**

---

# 6. THE ACTUAL ALPHA 0.9.0 GOAL

I want to invite friends over on a Friday night, turn on the computer, launch Truck Sports: Party!, and have everything behave like a polished console party game.

The complete flow:

**Main Menu → Join → Profiles → Ready Up → Sport Selection → Match → Results → Points → Lobby → Next Sport**

Players should be able to repeat this as long as they want.

No physical keyboard or mouse should be required.

No controller reassignment should be necessary between sports.

Profiles and session points must persist through transitions.

---

# 7. AUTOMATIC CONTROLLER DETECTION AND PLAYER JOINING

Implement:

- Automatic compatible gamepad detection.
- Controllers connecting after launch.
- Xbox controller support.
- PlayStation controller support.
- Compatible Nintendo-style and generic PC gamepads.
- Wired and Bluetooth devices through supported Unity input APIs.
- Independent controller ownership.
- Four local human player slots.
- Joining with an unassigned controller.
- Proper disconnection detection.
- Reconnection without restarting.
- Intentional replacement-controller assignment.
- Stable ownership across scene changes.
- Optional keyboard/mouse support.
- Protection against accidentally joining with unrelated steering wheels or specialty peripherals.

Reuse the established Unity Input System.

Avoid incompatible replacement input systems.

## Joining flow

Display:

**Press A / Cross / Start to Join**

After joining:

- Assign a player slot.
- Assign a player color.
- Show profile selection.
- Maintain controller ownership.
- Allow cancellation or leaving before match start.

Do not let one controller accidentally operate multiple players.

---

# 8. PERSISTENT PLAYER PROFILES

Support:

- Creating profiles.
- Selecting existing profiles.
- Renaming profiles.
- Editing profiles.
- Deleting profiles with confirmation.
- Selecting avatars/icons.
- Preferred player colors.
- Guest profiles.
- Local persistent saves.

Each permanent profile contains:

- Stable unique ID.
- Display name.
- Avatar.
- Preferred color.
- Applicable customization.
- Lifetime statistics.
- Personal sport records.
- Creation date.
- Last played date.

Do not use display names as primary identifiers.

Support safe saves and compatible migration of existing records.

Do not require internet access, external accounts, or subscriptions.

Prevent profile corruption from interrupted writes.

---

# 9. FULL CONTROLLER-OPERATED ON-SCREEN KEYBOARD

Create an attractive on-screen keyboard.

Required functions:

- Letters.
- Numbers.
- Spaces.
- Backspace.
- Clear.
- Confirm.
- Cancel.
- Shift/capitalization.
- Directional controller navigation.
- Visible key focus.
- Appropriate character limits.
- Input validation.
- Correct focus restoration.
- Readable scaling.

Use it for creating and editing profile names and other essential text entry.

Physical keyboard input remains optional.

A controller user must never have to click a text field using a mouse.

---

# 10. CENTRAL MULTIPLAYER LOBBY

Create a polished four-player lobby.

Each occupied slot shows:

- Player number.
- Profile name.
- Avatar/icon.
- Assigned controller.
- Player color.
- Ready state.
- Current game-night points.

Unused slots invite players to join.

## Ready-up

- Each player operates their own ready control.
- Each player can ready and unready.
- Readiness is clearly presented.
- Matches cannot accidentally start with required participants unready.
- Readiness resets appropriately between matches.

## Player 1 host authority

Player 1 controls:

- Sport selection.
- Match settings.
- Starting the match.
- Returning to lobby.
- Ending game night.

Other players retain control of their own profile and readiness.

Handle host disconnection/departure clearly.

---

# 11. POLISHED SPORT SELECTION

Improve the sport-selection interface to support a growing catalog.

Provide:

- Custom sport logos.
- Attractive previews.
- Descriptions.
- Player-count information.
- Available modes.
- Category filters.
- Controller navigation.
- Selection animations.
- Sound effects.
- Clear focus states.
- Readable controls.

Reuse the existing sport registration architecture.

Preserve working artwork.

Use owned/appropriately licensed assets wherever suitable.

Do not create arbitrary placeholder thumbnails when finished assets are available.

Do not divide sports into future party packs yet.

---

# 12. MATCH CONFIGURATION

Player 1 should configure supported match options using their controller.

Include as applicable:

- Human participants.
- AI competitors.
- AI difficulty.
- Teams.
- Round count.
- Match duration.
- Arena/course.
- Weather.
- Time of day.
- Sport-specific settings.

Preserve established configurations, including up to four humans and larger AI-inclusive competitor fields where supported.

Do not advertise unsupported settings.

Provide sensible defaults for quick game-night starts.

---

# 13. COMPLETE CONTROLLER-ONLY NAVIGATION

Every player-facing function must work through a controller.

This includes:

- Main menu.
- Joining.
- Profiles.
- On-screen keyboard.
- Lobby.
- Ready-up.
- Sport selection.
- Match configuration.
- Gameplay.
- Pause.
- Settings.
- Sport-specific inventories/customization.
- Results.
- Statistics.
- Rematches.
- Return to lobby.
- Ending game night.
- Telemetry export.

Maintain consistent confirm, back, and pause controls.

Preserve working sport-specific input schemes.

Do not rebuild functioning gameplay controls unnecessarily.

---

# 14. SEAMLESS SPORT-TO-SPORT FLOW

The entire collection must behave as one connected party game.

Preserve across transitions:

- Profiles.
- Controller assignments.
- Player colors.
- Session points.
- Game-night identity.
- Applicable settings.

Sport-specific state must reset appropriately.

Avoid:

- Duplicate persistent managers.
- Lost controller ownership.
- Broken scene references.
- Duplicate event subscriptions.
- Leaked temporary objects.
- Missing match results.
- Incorrect winner assignment.

Use existing sport lifecycle adapters where possible.

The standard flow is:

**Lobby → Select Sport → Configure → Play → Results → Lobby**

Support rematches and continuing to another sport without restarting.

---

# 15. GAME-NIGHT WINNER POINTS

Implement simple session-wide points.

**DO NOT CREATE A PLAYER-FACING CHECKLIST OF SPORTS PLAYED.**

Default:

- Each completed sport determines the actual winning participant(s).
- Each winning human receives one game-night point.
- Ties follow appropriate rules.
- Session standings update after every match.
- AI competitors can win an individual match but do not receive permanent human-profile point awards.

Do not transfer large sport-specific scores directly into session points.

The host can continue game night indefinitely.

At session end, show final standings and celebrate the overall winner.

---

# 16. PERMANENT PLAYER STATISTICS

Every persistent profile needs a usable statistics screen.

Track:

- Total matches.
- Wins.
- Losses.
- Draws.
- Win percentage.
- Lifetime winner points.
- Game-night sessions.
- Placements.
- Personal records.
- Last played.
- Per-sport statistics.

Support sport-specific fields such as:

- Racing times.
- Lap records.
- Finishing positions.
- Shooting accuracy.
- Goals and successful scores.
- Knockouts.
- Combat hits.
- Throwing distances.
- High scores.
- Combo records.

Only record statistics actually supported by gameplay.

The UI must be fully controller-operated and easy to read.

---

# 17. BACKEND GAME-NIGHT TELEMETRY

**Every game-night session must produce useful structured information for later ChatGPT analysis.**

Required session data:

- Session ID.
- Version/build.
- Start/end.
- Duration.
- Human profiles.
- AI configurations.
- Controller events.
- Sports selected.
- Match settings.
- Match timestamps.
- Final scores.
- Winners.
- Session-point awards.
- Final standings.
- Interrupted matches.
- Errors and abnormal states.

## Per-sport events

Support:

- Objectives.
- Scoring events.
- Failures.
- Collisions.
- Knockouts.
- Completion time.
- Accuracy.
- Player actions.
- AI performance.
- Restarts.
- Repeated problems.
- Other sport-defined telemetry.

Use an extensible API.

Avoid excessive per-frame logging.

## Technical metrics

Capture bounded:

- Average FPS.
- Frame-time spikes.
- Low-performance periods.
- Loading times.
- Exceptions.
- Significant warnings.

Do not create logging-induced stutters.

## Required outputs

Generate:

1. JSON session report.
2. Relevant per-match information.
3. Human-readable session summary.
4. Optional CSV summaries.
5. Versioned schemas.

Save locally under an appropriate persistent data directory.

Support controller-based report export/finalization.

Reports must be suitable for manual upload into ChatGPT.

Do not claim automatic transmission to ChatGPT without an authorized working integration.

Backend recording of which matches were played is required for analytics; this does not authorize a player-facing completion checklist.

---

# 18. PROFESSIONAL MENU AND UI QUALITY

The entire game should look like a polished commercial couch multiplayer title.

Use:

- Clean visual hierarchy.
- Readable fonts.
- Proper spacing.
- Attractive backgrounds.
- Clear player colors.
- Existing custom logos.
- Smooth transitions.
- Appropriate animations.
- Sound effects.
- Readable instructional prompts.
- Visible controller focus.
- Screen-safe layouts.

Avoid:

- Default-looking Unity UI.
- Placeholder panels.
- Tiny text.
- Cropped buttons.
- Broken navigation.
- Confusing submenus.
- Mouse-only controls.
- Lost selection focus.
- Inconsistent visual presentation.

Host-owned menus must not respond to conflicting inputs from every player's controller.

Player-owned controls must remain independently operable.

---

# 19. PAUSE, SETTINGS, AND ACCESSIBILITY

Preserve the existing settings infrastructure.

Support controller navigation for:

- Audio.
- Display.
- Resolution.
- Graphics.
- Input settings.
- Gameplay preferences.
- AI difficulty.
- Weather.
- Time of day.
- Sport-specific settings.
- Readable UI scaling where supported.

Do not duplicate functioning settings systems.

Preserve user preferences between sessions.

Pause should behave consistently with each sport's defined rules.

---

# 20. EXISTING SPORTS INTEGRATION

Do not rewrite every existing sport.

Integrate the Release Foundation with the current catalog.

Each sport should expose, directly or through an adapter:

- Stable sport ID.
- Display information.
- Supported player counts.
- Available modes.
- Launch behavior.
- Match lifecycle.
- Results.
- Actual winner.
- Statistics.
- Telemetry.
- Cleanup.

First make the complete foundation work with one reliable existing sport.

Then integrate the remaining available sports.

If an existing sport has a real critical defect, record it accurately.

Do not silently remove or conceal sports.

Do not spend the entire milestone repairing unrelated unfinished gameplay from earlier versions.

---

# 21. MODULARITY FOR FUTURE PARTY PACKS

Eventually Truck Sports will contain a very large catalog of minigames.

We intend to curate these into separate commercial collections later.

Preserve:

- Stable sport IDs.
- Independent sport scenes.
- Shared input infrastructure.
- Shared player profiles.
- Shared session scoring.
- Shared statistics.
- Shared telemetry.
- Shared UI.
- Configurable sport registration.

Avoid dependencies between unrelated sports.

Do not divide the game into separate packs during this milestone.

---

# 22. REQUIRED IMPLEMENTATION SEQUENCE

After Git cleanup and backlog inventory:

## Phase A — Foundation audit

- Inspect reusable input systems.
- Inspect player/session infrastructure.
- Inspect menus and sport registration.
- Establish safe implementation boundaries.

Do not launch historical full-project validation.

## Phase B — Controller joining and profiles

- Detection.
- Independent joining.
- Reconnection.
- Profiles.
- On-screen keyboard.
- Persistence.

## Phase C — Lobby and ready-up

- Four-player lobby.
- Player ownership.
- Host controls.
- Ready/unready.
- Player status display.

## Phase D — Sport launch and return

- Sport selection.
- Match settings.
- Launch.
- Results.
- Winner detection.
- Return to lobby.
- Rematch.

At this point, establish an actual end-to-end playable loop.

## Phase E — Points and statistics

- Winner points.
- Session standings.
- Permanent records.
- Statistics interface.
- Session ending.

## Phase F — Telemetry

- Gameplay events.
- Session report.
- Performance data.
- JSON export.
- Readable summary.

## Phase G — UI polish and catalog integration

- Final menu presentation.
- Controller focus.
- Existing sport compatibility.
- Consistent navigation.
- Remove player-facing placeholders.

## Phase H — Windows delivery

- Compile.
- Correct real blockers.
- Build.
- Launch executable.
- Exercise representative multiplayer flow.
- Verify persistence.
- Verify reporting.
- Update ledger.
- Deliver build.

Make logical Git commits as development progresses.

Push appropriate completed milestone work to the established remote development branch.

Do not perform giant cleanup or history-reorganization passes again after Phase 1 unless an actual problem requires it.

---

# 23. REQUIRED END-TO-END PLAYER EXPERIENCE TEST

Perform a representative game-night test.

1. Launch Windows game.
2. Join with Controller 1.
3. Join with Controller 2.
4. Create a profile using the controller-operated keyboard.
5. Select an existing profile for another player.
6. Ready both players.
7. Use Player 1 to select a sport.
8. Configure and start the match.
9. Confirm correct independent inputs.
10. Complete the match.
11. Show the actual winner.
12. Award the correct session point.
13. Return to lobby.
14. Preserve profiles, controllers, colors, and points.
15. Launch and finish a second distinct sport.
16. End game night.
17. Display standings.
18. Export session telemetry.
19. Close the executable.
20. Relaunch and confirm permanent profile/stat persistence.

Where practical, exercise three- and four-controller configurations.

If physical controllers are unavailable, use suitable simulated input and mark hardware testing unverified.

Do not invent physical testing results.

This representative player-facing test matters more than hundreds of redundant source-integrity checks.

---

# 24. AUTHORITATIVE ACCEPTANCE LEDGER

Maintain the existing authoritative acceptance ledger.

Do not create a competing backlog.

Each requirement must use:

- MISSING
- PARTIAL
- IMPLEMENTED BUT UNVERIFIED
- VERIFIED
- BLOCKED

For each item record:

- Requirement ID.
- Actual implementation.
- Status.
- Relevant test.
- Evidence.
- Remaining issue.

Do not mark a requirement verified simply because code compiled.

Do not endlessly reopen previously verified requirements without a relevant regression.

Keep the new Alpha 0.9.0 execution ledger distinct from the preserved older backlog while maintaining a single authoritative source of truth.

---

# 25. HARD STOPS AGAINST WASTED DEVELOPMENT TIME

Stop and reassess immediately if:

- A test is repaired repeatedly without improving gameplay.
- A minor menu change triggers giant asset verification.
- An unchanged project is repeatedly hashed.
- Previously accepted Dragon world geometry is being unnecessarily revalidated.
- An agent begins another source authority framework.
- A noncritical visual issue delays the playable multiplayer loop.
- Validation infrastructure becomes more complex than the feature it tests.
- The same validation failure has already received two corrective attempts.
- Multiple processes modify the same Unity project simultaneously.
- Optional polish blocks the core Release Foundation.
- Extensive work produces no meaningful player-facing improvement.

When this happens:

1. Preserve work.
2. Record the issue.
3. Identify the smallest productive correction.
4. Continue toward the playable deliverable.

Do not restart the old 0.8.0 pipeline.

Do not launch another massive validation framework.

Critical data loss, crashes, startup failures, profile corruption, and broken match progression must still be addressed or explicitly reported.

---

# 26. FUTURE VERSION 0.10.0 — PRESERVE ONLY

## HITCHES, HORRORS & HIGH WATER

The original 22-sport Alpha 0.9.0 expansion becomes Alpha 0.10.0.

Preserve these approved sports:

1. Truck Canoeing: Paddle Pals
2. Truck Kayaking: Whitewater Wipeout
3. Graveyard Shift: The Last Stop
4. Truck Harvest: Scarecrow Season
5. Dead End: The Last Headlight
6. Blackwater: Something Below
7. Trailerblazers: Rocket Launch
8. Jackknife Highwire
9. Hitch & Run
10. Trailer Trebuchet
11. Hitch Handoff
12. Unhitched!
13. Cargo Crisis
14. Cursed Cargo
15. Domino Derby
16. Trailer Plinko
17. Jackknife on Ice
18. Road Train Wreck
19. Hitch Ring Toss
20. Zero-G Hitch-Up
21. Skyhook Sprint
22. Ice Road Reckoning

Preserve all previously approved mechanics, modes, presentation, and acceptance requirements.

Do not begin implementing them.

---

# 27. FUTURE VERSION 0.11.0 — PRESERVE ONLY

## TRICKS, TARTAN & TOTAL CHAOS

The original 28-sport Alpha 0.10.0 expansion becomes Alpha 0.11.0.

Preserve:

1. Pro Hauler — Truck Trick Championship
2. Turf Trucks — Paint Wars
3. Rolling Masterpiece — Truck Art Studio
4. Shred Haulers — Truck Snowboarding
5. Alpine Anarchy — Truck Skiing
6. Big Rig Waves — Truck Surfing
7. Vertical Madness — Truck Rock Climbing
8. Quarter-Mile Mayhem — Truck Drag Racing
9. Wings of Steel — Truck Hang Gliding
10. Truck Slap Fighting
11. Truck Boxing
12. Booty Slapping Championship
13. Truck Cheerleading
14. Truck Bikini Contest
15. Hot Dog Eating Championship
16. Wet T-Shirt Contest
17. Truck High Diving
18. Truck Javelin Throw
19. Truck Hurdle Racing
20. Truck Discus Throw
21. Truck Shot Put
22. Truck Pole Vault
23. Caber Toss — Timber Tippers
24. Scottish Hammer Throw
25. Weight for Height
26. Weight for Distance
27. Sheaf Toss
28. Truck Drink Chugging Championship

Preserve all previously approved features and distinctive gameplay requirements.

Do not start implementing these sports during the Release Foundation milestone.

---

# 28. GIT WORKFLOW DURING ALPHA 0.9.0

After the initial cleanup and successful push, continue using clean source control.

Create meaningful local commits when functionality reaches stable checkpoints.

Appropriate examples:

- `feat(truck-sports): add controller-based player joining`
- `feat(truck-sports): implement persistent player profiles`
- `feat(truck-sports): add controller-operated keyboard`
- `feat(truck-sports): implement multiplayer ready lobby`
- `feat(truck-sports): integrate continuous sport selection and match flow`
- `feat(truck-sports): add game-night scoring and statistics`
- `feat(truck-sports): add structured session analytics`
- `ui(truck-sports): polish controller-first menus`
- `build(truck-sports): prepare alpha 0.9 windows release`

These are examples; use commits appropriate to actual completed work.

Push milestone work to the appropriate remote branch when safely integrated.

Do not push generated caches, secrets, or enormous disposable logs.

Do not force-push.

Do not merge into protected release branches unnecessarily.

Do not alter unrelated projects.

---

# 29. REQUIRED FINAL DELIVERABLES

### Deliverable 1 — Git Cleanup Report

Provide:

- Repository/remote.
- Branch.
- Logical commits created.
- Preserved work.
- Push result.
- Final commit SHA.
- Deliberately excluded generated files.
- Any real outstanding Git issues.

### Deliverable 2 — Complete Existing Backlog Inventory

Present all outstanding pre-0.9.0 requirements before development begins.

Include statuses, versions, and relevant evidence.

### Deliverable 3 — Playable Alpha 0.9.0 Windows Build

Provide the actual executable path.

Report whether the build was successfully launched and tested.

### Deliverable 4 — Complete Controller-First Party Experience

Include:

- Controller detection.
- Independent joining.
- Profiles.
- On-screen keyboard.
- Lobby.
- Ready-up.
- Player 1 sport selection.
- Match configuration.
- Gameplay.
- Results.
- Return to lobby.
- Rematches.

### Deliverable 5 — Session Points and Persistent Statistics

Award winner points, maintain current session standings, and preserve permanent profile statistics.

### Deliverable 6 — Backend Session Reports

Generate real, usable reports suitable for ChatGPT analysis.

Provide actual output directory and example report structure.

### Deliverable 7 — Updated Acceptance Ledger

Record verified, incomplete, blocked, and unverified requirements accurately.

### Deliverable 8 — Concise Implementation Report

Include:

- Actual completed features.
- Tests performed.
- Remaining problems.
- Known limitations.
- Build path.
- Git commit/push status.
- Outstanding Release Foundation items.

### Deliverable 9 — Classic World of Warcraft-Style Patch Notes

Produce entertaining, substantial player-facing release notes titled:

**Truck Sports: Party! — Alpha 0.9.0: Release Foundation**

Describe only features actually implemented and delivered.

Do not describe future sports as shipped.

---

# 30. DEFINITION OF SUCCESS

When this milestone is finished, I should be able to:

- Launch the game.
- Hand controllers to friends.
- Have them join effortlessly.
- Select or create player profiles.
- Type names with controllers.
- Ready up.
- Let Player 1 select the sport.
- Play a competition.
- See the winner.
- Award session points.
- Return to the lobby.
- Select another sport.
- Continue all evening.
- End with final standings.
- Review permanent player statistics.
- Export useful gameplay information.
- Bring that report into ChatGPT for analysis.

The UI should be polished and intuitive.

The game should be controller-first and couch-friendly.

This foundation must support our existing catalog and prepare us for 50 additional sports across Alpha 0.10.0 and Alpha 0.11.0.

Do not add Mario Party-style board progression.

Do not add a player-facing checklist of completed sports.

Do not remove approved backlog requirements.

Do not replace real gameplay with fabricated results.

Do not repeat meaningless validation work.

---

# 31. FINAL EXECUTION DIRECTIVE

**FIRST — GIT CLEANUP**

Inspect the Git workspace, preserve all legitimate existing work, organize changes appropriately, create logical commits, and push them to the correct existing GitHub remote.

Verify the resulting repository state and present a concise cleanup report.

Do not delete unfinished work, force-push, or overwrite stable releases.

**SECOND — COMPLETE BACKLOG INVENTORY**

Provide the full outstanding backlog from existing project records.

Include unfinished earlier-version requirements and accurate implementation statuses.

Present the inventory before implementing new production code.

**THIRD — IGNORE OLD EXECUTION ORDERS**

Treat previous prompts as historical requirement references.

Do not resume an old backlog or validation pipeline.

**FOURTH — BUILD ALPHA 0.9.0**

Implement the entire Release Foundation:

- Automatic controller detection.
- Four-player joining.
- Persistent profiles.
- On-screen keyboard.
- Ready-up.
- Host-controlled sport selection.
- Complete controller navigation.
- Seamless sport transitions.
- Winner points.
- Persistent statistics.
- Structured session telemetry.
- Polished menus.
- Working Windows build.

**FIFTH — TEST EFFICIENTLY**

Use focused tests and a complete representative game-night smoke test.

Do not waste rate limit on recursive QA or repeated multi-gigabyte verification.

**SIXTH — DELIVER AND PUSH**

Produce the working Windows build, update the ledger, create logical milestone commits, and push the completed source to the correct development branch.

Report the actual results honestly.

**SEVENTH — PRESERVE FUTURE DEVELOPMENT**

Leave remaining earlier-version backlog items documented for later.

Preserve Alpha 0.10.0 and Alpha 0.11.0 as the next planned content expansions.

Do not automatically begin them.

---

**THE ENTIRE PURPOSE OF THIS MILESTONE IS TO GET TRUCK SPORTS INTO A STATE WHERE I CAN INVITE PEOPLE OVER, HAND THEM CONTROLLERS, AND IMMEDIATELY START PLAYING.**

Stop treating endless technical validation as the primary deliverable.

Preserve the work.

Clean up Git.

Commit and push.

Show me the full outstanding backlog.

Then implement the entire Release Foundation before anything else.

**BEGIN WITH THE GIT CLEANUP NOW.**
