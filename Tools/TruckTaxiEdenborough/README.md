# Edenborough authoring tools

Project-owned coordination for the saved Truck Taxi Edenborough scene and actual Windows player at Builds/TruckTaxiEdenborough/TruckTaxi.exe. Current requirement-by-requirement acceptance state and remaining manual coverage are in Checkpoint.md. Evidence contains native player screenshots, isolated restart assertions, build hashes, source-prefab placements and imported mesh usage. Source diagnostics alone are not runtime proof.

Editor commands: Truck Taxi/Edenborough/Create Reviewed Art Palette; Build Fresh Dedicated Scene; Validate Loaded Dedicated Scene; Use Validated Edenborough As Primary World; Build Windows.

Build Fresh refuses to replace an existing dedicated scene and requires inspected art categories. It never invokes the old DemoCity generator. Old saves retain their original scene.

Read-only package inspection: bundled Python InspectUnityPackage.py inspect SOURCE.unitypackage INDEX.json. After selecting exact paths, select INDEX.json SELECTION.json STAGED.unitypackage produces a dependency-closed art subset and manifest. Framework/script dependencies, unresolved GUIDs and source overwrites are rejected. Staging is not Unity import.

Focused checks: bundled Python -m unittest -v test_package_inspector.py; Tools/TruckTaxiEdenborough/Test-SourceDiagnostics.ps1. The latter reuses the existing Roslyn checker with the installed Mathematics reference missing from the stale cached Editor response file.
