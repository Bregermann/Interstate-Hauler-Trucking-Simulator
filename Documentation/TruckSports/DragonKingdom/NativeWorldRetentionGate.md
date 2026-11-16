# Full native world result required before retention

V187's authoring receipt reports `PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW`, but the actual NUnit world case FAILED on an unhandled RiverModeler initialization exception: a68,211-vertex combined mesh exceeded the UInt16 limit of65,535. The separate read-only identity-plan case passed. Neither that pass nor the later receipt flags authorize retaining candidate17.

For `--scope candidate --candidate RoundedIntegrated...`, the preservation tool now requires exactly one case with this exact fullname and `result="Passed"` in that run's actual `tests.xml`:

`DragonKingdomRoundedWorldIntegrationTests.MeasuredRoundedVistaAndRealDiggerPreserveAcceptedSourcesAndPassCompleteWorldGates`

Missing/skipped/failed/inconclusive/duplicate cases are rejected. A different passing test or a mixture of one passed and one failed duplicate cannot authorize copying. The run's `rendered-evidence/regional-world-<candidate>.json` must also match the selected candidate's complete Integration Receipt exactly, so a prior world's pass cannot authorize a different or stale candidate. Both checks run before the existing Digger/Finish/coast asset guards and before any ROOT copy/backup writes. All existing source, geometry, GUID, exact-output and concurrency checks remain.

Eight regression cases include an extracted actual V187 failed-world/passed-plan pair with its original XML SHA256 and actual failure message, synthetic mixed batches, missing XML/cases, skipped/inconclusive/duplicate cases, wrong candidate evidence, rejection despite all receipt flags being true, and successful complete retention paths for valid14/16 examples. The recorded excerpt omits unrelated output/stack text; it is a unit regression fixture, not a substitute native run. The original local V187 receipt is preserved unchanged and its hash is compared when present.

The complete115-case tooling suite passes in4.792 s. This guard does not repair RiverModeler, regenerate candidate17 or imply the world/visual/course acceptance of a future candidate. Parent and world owner handle the vendor public-API lifecycle correction and fresh18 run. Passing weather remains frozen.

VENDOR ASSETS AUDITED: actual RiverModeler exception and Unity NUnit result/authoring receipt lifecycle.

VENDOR ASSETS USED: actual native NUnit status and existing vendor-generated world evidence.

RELEVANT ASSETS NOT USED: unrelated passing cases cannot authorize this world.

CUSTOM SYSTEMS CREATED: retention authorization guard and regression fixtures.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
