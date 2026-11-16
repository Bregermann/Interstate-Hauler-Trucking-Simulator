# Analytic cavern interior rock anchors

V372's native analytic-lobe cave passed all11,946floor/roof/source checks, then shared finishing rejected required rock5. No saved finished probe or visual acceptance was produced. The failure receipt is `Logs/TruckSports/DragonKingdom/V372/rendered-evidence/shared-cavern-finish.json` (SHA242cc22fbb7f60230e45c90ed0027b597967f5ed22d549481d833dbe7e26e6b2).

The analytic lower lobes change peripheral floor support from the previous flat-cylinder recipe. Rock5's old west-edge footprint extends below the actual voxel-aligned reset edge X439.0625; rock7's old accepted southern crossover footprint reaches radius64.856m from its nearest lobe, beyond the analytic floor105m intersection radius59.573m. Broadening support/guardian/road rules would hide a real placement error.

Only with explicit `-tsDragonCavernAnalyticLobes`, required rocks5/6/7 now begin their existing bounded searches at(452,105,805),(458.5,105,751),(550,105,738). Their source prefabs remain RockFormation_01/02/03, with unchanged normalized heights5.8/7.1/4.5m, yaws188/235/282degrees, authored hierarchy/rig, materials and colliders. New receipt fields preserve each original hint and selected interior hint.

Measured rock5 footprint X448.864..455.136/Z801.485..808.515 is inside actual reset X439.0625..615.625/Z725..854.6875. Its farthest sampled radius45.111m is inside the lower-lobe floor105m radius59.573m. It leaves8.301m beyond the already expanded guardian envelope,7.485m beyond the conservative west-road margin and1.562m to the nearest recorded peer visual AABB.

Measured rock7 footprint X545.818..554.182/Z733.934..742.066 has maximum radius56.222m and clears the actual source graph, four original truck starts, treasure/perch anchors, expanded guardian and recorded peers. Its southern placement also preserves rock8's prior eastern space.

The initial remaining review used all2,674actual source graph samples and exact original prefab anchor positions. It retained rock6 based on its mathematically supported floor footprint and prior native collider placement; that preliminary decision was superseded by V373 actual behavior. Rock5 passed trial0 at the new anchor, but rock6's unchanged search failed31support/22road/3other-art/8additional-clearance trials. Those aggregate counters do not identify a particular offending collider. No saved finished cave or images resulted.

Rock6 now uses the measured west-south bank: footprint X454.720..462.280/Z747.210..754.790, maximum lower-lobe radius55.459m (inside the original57m dense required-column domain),1.157m beyond the already expanded guardian envelope, approximately1.004m to the nearest recorded peer visual AABB and1.451m to lantern8. A coherent sequential review includes the new6 before7 and the new7 before8; both7and8 retain clear source envelopes. Rock8 remains unchanged with radius47.527m. See `AnalyticRockFiveAnchorReady03/remaining-rock-envelope-review.json`, with source graph/prefab hashes and explicit conservative-AABB limitations. These remain planning measurements until the next actual native run.

All original64-trial FinishPlace searches, actual9-contact native floor support,105±0.3m floor requirement, guardian3m margin, road/start/treasure/perch protection and physical peer-overlap gates remain unchanged. No art is omitted, shrunk, substituted or reparented to bypass checks. Ordinary/default and prior custom-brush hints remain unchanged; only analytic rocks5/6/7 receive new hints. Planning measurements do not establish actual new contacts.

Source: `Assets/TruckDragonKingdom/Editor/DragonKingdomRegionalWorldIntegration.CavernFinish.cs` SHA99f8192d42046571ea1d7b6ccb0e05ed720ae2b99d77cd533ff9b5c9b2a908eb. Final Csc Editor/tests passed exit0(4.846s/0.953s). V374 actual native excavation and saved-render fixtures passed(121.558024s/30.215334s). Shared finishing completed all32parts/288floor contacts with zero placement failures; analytic rocks5/6/7 each passed trial0, source bytes and original graph/anchor/object poses were preserved. Direct review of all three images still rejects the cave appearance for heavy ribs/abrupt joins, so this placement verification does not establish a finished cave, human gameplay or production adoption. No Unity, scene/prefab, CLEAN, Git or canonical-world mutation was performed by this scoped source correction.

VENDOR ASSETS AUDITED: actual Polyart RockFormation01/03/02/04 source bounds, native analytic lobes/reset and existing placement/collision rules.

VENDOR ASSETS USED: original Polyart prefabs/materials/colliders and Digger native geometry/support.

RELEVANT ASSETS NOT USED: replacement rocks, primitive supports, modified physics or new spawning/placement frameworks were unnecessary.

CUSTOM SYSTEMS CREATED: three explicit analytic-profile anchor choices and measurement evidence only; existing placement authority reused.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.