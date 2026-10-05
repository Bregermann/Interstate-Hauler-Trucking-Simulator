# Custom Content Tests Handoff

## Stable Scope

Added only `Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiCustomContentTests.cs` and this handoff. No production, vendor, serialized asset, or existing test changes. No Unity launch, build, or commit. Preserve all unrelated integration changes; Unity owns test-file metadata import.

Removed `NonParallelizable` after the parent reported CS0246 with the installed NUnit version. The fixture restores its PlayerPrefs setting and Unity random state, and destroys its in-memory assets and objects after each case.

## Coverage

- 84 cases using neutral `water-bottle` hand-over data: exact ordinal event/item matching, occurrence replay rejection, completion idempotence, per-ride history reset, and callback transaction safety.
- Null and empty objective item filters are authored before assignment, then completed through signals and a real session item transaction. Event-only capabilities accept any registered item for the exact event; nonempty item filters remain strict.
- Runner registration uses passenger-authored definitions, skips null assets and ineligible or unavailable inventory, rechecks live inventory at assignment, caps targets to available occurrences, and unregisters exhausted actions. The fixture injects dependencies into an inactive bootstrap; it does not start the game or invoke full player presentation.
- Atomic rejection and successful consumption, callback ordering, reentrant/cloned once-per-ride rejection, repeatable rewards, overflow rejection, and generic consumption without ordinary drink side effects.
- Explicit authored gender, age threshold, all-required case-insensitive tags, invalid definitions/objectives, presentation validation including inactive collider children, and null/empty profile/audio assets.

## Parent Validation

Parent ran the final integrated fixture in Unity 6000.4.10f1: **84/84 EditMode tests passed**. Report: `Logs/TruckTaxi-CustomContent-EditMode.json`. See `TruckTaxi_Custom_Content.md` for the separate 17 passing PlayMode cases.

In Unity Test Runner, select EditMode and run `LWS.TruckTaxi.Tests.EditMode.TruckTaxiCustomContentTests`. The same fully qualified name can be used as the parent's `-testFilter` with `-testPlatform EditMode`.

Local verification was a read-only Roslyn syntax parse: zero syntax errors, 10 direct tests plus 71 parameterized cases, zero remaining `NonParallelizable` attributes. This is not a Unity compilation or test pass. The parent's earlier compile found no other new compiler error before this final update; rerun compilation and all 81 cases against the integrated Session/runner changes.

Session-only tests explicitly register capability pairs to isolate event and transaction semantics. Separate runner tests cover runtime eligibility and capacity. Persistence, Timeline playback, visible presentation, vendor integration, and PlayMode behavior are outside this test-only scope.

## Vendor Disclosure

- VENDOR ASSETS AUDITED: vendor-first responsibility rule and existing LWS APIs/fixtures; no new commercial asset capability needed for these semantic tests.
- VENDOR ASSETS USED: none directly; existing Unity EditMode/NUnit test infrastructure reused.
- RELEVANT ASSETS NOT USED: Pixel Crushers, presentation, vehicle, and world assets are not needed for isolated LWS session tests.
- CUSTOM SYSTEMS CREATED: none; one test fixture and this handoff only.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
