# October 22 — Fare Fight preservation acceptance

HPR-8-6 is verified in its automated preservation scope. No Fare Fight scene or gameplay changes were needed. The original city district, passenger state authority, NWH drivers, traffic/crowd, GPS and mode scripts match the accepted Sky Tow checkpoint after normalizing Git checkout line endings only.

Fresh exact Windows payload `1363d34f2710ddeecebf347361979da22780e1270a5a8d4d7c4b2dd9fa6b8327` passed **46 checks, 0 errors** in two independent processes:

- `FarePreservationFinalWindows`: 16 checks, PID 71216, 523.94 real seconds. Normal AI, recorded seed 616736539, complete first-to-five match through actual NWH driving and physical roadside pickups/deliveries. Equal initial road distances and non-overlapping truck hulls, pause, default five-point result, active UTS traffic/Taxi crowd, one listener, rematch and menu cleanup passed. Three-times simulation is explicitly configured; this is not a GPU benchmark.
- `FarePreservationDevicesWindows`: 30 checks, PID 65724, 17.57 real seconds. Software-injected keyboard, whole pad and independent left/right sticks actually drive their four assigned NWH trucks. Actual Taxi cab/chase switching, independent CNP route/camera/objective bindings, whole/shared-controller conflict rejection, disconnect freeze, neutral-before-reconnect gating, rematch ownership and scene cleanup passed. A separately posed ordinary Taxi pedestrian fixture then used an actual NWH bumper impact to activate the existing jointed ragdoll without awarding delivery points. It is not a complete four-human match or physical-device test.

The full AI match recorded longest two-metre pockets of 7.767578, 6.981491, 10.83057 and 9.048828 simulated seconds, with seven announced recoveries. The old intermittent immobility remains unreproduced in four recent recorded-seed full matches. **HPR-8-4 remains PARTIAL.** Neither a zero-RPM root cause nor a new engine fix is claimed. Existing telemetry captures body, input, wheel and powertrain state if a long pocket recurs.

Retained 720p/1080p town frames are controlled captures; four-human cab images are actual Windows screenshots. Current human driving/impact feel, physical controllers/wheels, subjective listening and severe-weather/dense-world profiling remain unperformed. Earlier native five-test acceptance stays in its original evidence; this unchanged-gameplay milestone does not claim new native tests.

The combined build remains `Builds/TruckSports/TruckSports.exe`, 60 scenes and 546 files, on the accepted camera-repair payload. No rebuild or new promotion was needed. All 925 unrelated pending files remain protected by the scoped commit guard.

VENDOR ASSETS AUDITED: existing NWH tractors, original UTS/Taxi crowd and traffic, Compass Navigator Pro GPS, Pixel Crushers integration, shared Heat/TMP UI and installed Weather Maker bindings.

VENDOR ASSETS USED: existing NWH physical driving/collision, Taxi jointed ragdoll, UTS ordinary traffic, CNP routes and participant-specific markers, original saved UI/cameras/weather adapters.

RELEVANT ASSETS NOT USED: no replacement navigation, traffic, vehicle, passenger, input or weather framework was required.

CUSTOM SYSTEMS CREATED: none; exact preservation comparison, existing explicit Windows probes and evidence/ledger reconciliation only.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
