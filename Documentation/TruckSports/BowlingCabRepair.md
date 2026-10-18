# Bowling cab and model presentation repair

Actual Windows camera validation exposed two concrete defects. The original Inside tiny cab primitive roots remained under all four cameras after the canonical Taxi interiors were added. The shared upgrade only replaced Driver seat interior, so the old primitives were not removed or included in the new interior-renderer visibility list. They appeared over the correct cab and leaked into the new AI spectator view.

Native scene repair removed exactly four obsolete roots, retained the canonical Taxi interiors, and updated saved renderer references. Regeneration now removes the obsolete roots too; normal build validation rejects their return. The existing scene was edited narrowly through native Unity APIs, not regenerated.

The same images showed detached tyres. Native mesh bounds confirmed that the Cab geometry was4.48m ahead of its authoritative chassis and later lanes had additional sideways offsets up to5.61m. The placeholder tyre cylinders remained near the actual physics body. The owned truck mesh group is now normalized in body-local coordinates to its original1.25m visual length; actual owned Whl HD FL/FR/RL/RR meshes use NWH's public rotating visual containers. Extra Wheel visual cylinders are removed. After repair every Cab center measures approximately(0,.34,.37) relative to its chassis.

The authoring repair checks that the recorded mass, collider size/center/material and wheel radius/width/mass/suspension/damper/friction-preset values are unchanged. Native saved-scene validation passes. This repair does not alter lane handling, impulses, back-wall behavior, gutters, scoring or pinsetter timing. The broader Goddess Bowling environment/back-wall/gutter/replay requirements remain separate pending work.

Files: BowlingCabRepairAuthoring.cs, BowlingAuthoring.cs, SportsCabUpgradeAuthoring.cs, saved TruckBowling.unity; AI spectator presentation uses the existing BowlingCabCamera. Human cab controls, actual steering/free look, GPS art and original Taxi assets are retained.

A controlled native preview shows attached, aligned wheels during actual AI approaches and pin impacts; all sixteen visual centers remain within0.000002m of their native rotating-container center. This is explicitly a controlled Editor render, not a Windows-window claim. The spectator deck angle stays inside the side frame.

Windows visual and applicable physical regression acceptance passed for these saved-scene edits; see the current receipt below. Superseded candidates and measured failure evidence are retained in project-local Logs.

VENDOR ASSETS AUDITED: installed NWH wheel visual-container API, canonical Taxi cab adapter, Heat/TMP setup/HUD and existing source truck meshes.

VENDOR ASSETS USED: NWH owned GR3D truck/wheel meshes and FindOrSpawnVisualContainers/rotatingContainer; original Taxi interior and steering adapter; existing native cameras.

RELEVANT ASSETS NOT USED: no new model package, vehicle physics, UI framework or custom wheel simulation is needed.

CUSTOM SYSTEMS CREATED: narrow additive scene repair/validation and body-local visual normalization; no replacement vendor framework.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

## Accepted Windows milestone

All 21 strict Windows suites passed for managed assembly `ef8a78590f6119af62340482d3b2c16fcc49be010f1e87e0cfa435fb19ea7b28`. See [version-bound acceptance and limitations](PartyCheeseImpactValidation.md). The 20-mode camera suite passed 2,467 checks plus separate preference-write/read fixtures. Applicable physical, rematch and full-match regressions passed; human/hardware validation remains manual. Earlier pending paragraphs record pre-acceptance history.
