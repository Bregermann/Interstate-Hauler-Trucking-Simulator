# Release-safe diagnostic query — October 23, 2026

Q22-SKY-RELEASE-COMPILE-01 repairs an actual non-development player compiler failure exposed by Dead Air's ROOT Windows build. DerbyMirrors references SportsProbeInputIsolation.Active in normal runtime code; the property existed only under UNITY_EDITOR or DEVELOPMENT_BUILD, producing CS0117 for a production build.

SportsProbeInputIsolation now supplies a release-only `Active => false` branch. The existing Editor/development source is byte-identical after removing that added branch; production builds cannot activate diagnostic device muting. No mirror, camera, input, vehicle or scoring behavior was rewritten.

Validation: direct PE-metadata inspection of the actual ROOT non-development player assembly at `Library/Bee/PlayerScriptAssemblies/Assembly-CSharp.dll` found `LWS.TruckSports.SportsProbeInputIsolation.get_Active` with IL `16 2A` (`ldc.i4.0; ret`). Thus the previously missing API is present and returns false in real player compilation. The retained proof records the source and compiled-assembly hashes. The complete Dead Air executable build was still compiling shaders at this check; its final build and runtime acceptance belong to that independent task.

This source dependency is deliberately separate from the already tested/promoted Truck Sports development payload `14da5c0fb40271f7184f0d09a1650896400009a936cf2dced0e009b0d5ab8fad`. That frozen payload retains its original source archive and unchanged development implementation. The next Sports build will incorporate the additional release branch. No fresh Sports build, full release gameplay, hardware or listening test is claimed for this small repair.

The 925 protected unrelated pending files remain unchanged. Dead Air assets and its independently owned three-reference prefab metadata repair remain outside this commit.

- VENDOR ASSETS AUDITED: existing first-party diagnostic query and Derby mirror caller; Unity compile conditions.
- VENDOR ASSETS USED: existing Unity input/compile boundary; original NWH/mirror behavior retained.
- RELEVANT ASSETS NOT USED: no replacement vendor input or mirror implementation was needed.
- CUSTOM SYSTEMS CREATED: none; one constant query added to the production compile branch.
- VENDOR SOURCE MODIFIED: no.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: no.
