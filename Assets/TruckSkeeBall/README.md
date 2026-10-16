# Truck Skee-Ball

Your truck is the ball. Nine launches each; drive the actual curved EasyRoads ramp and enter physical open rings worth 10/20/30/40/50/100. The whole authoritative chassis must clear the same hole. Outside/rim impacts and misses score zero. One numbered delivery awards once. Tied leaders receive equal extra launches after every participant completes nine. Solo practice keeps a personal best.

Up to four simultaneous equivalent lanes, or up to eight sequential hot-seat participants. Names, colors, AI difficulty and explicit sources persist through rematches. Separate controllers, keyboard clusters and opposite sticks on one controller follow the collection ownership policy. Shared hot-seat replacement updates all sequential owners of the old device; simultaneous sources remain exclusive.

Accelerate/steer; reverse brakes before reversing. Physical ramp contact determines launch; no scripted scoring trajectory or extra launch impulse. Limited horizontal aftertouch: release forward/back after launch to arm range correction. First target contact disables aftertouch and wheels. Cab view defaults for humans; fresh ACTION toggles chase. Whole-pad right stick or one owned RMB mouse looks; shared sticks retain useful fixed views. The actual Taxi steering wheel follows the owning NWH wheel angles. Replays are recorded trajectories: left/right angle, up/down speed, fresh ACTION skip. Each lane continues independently.

Authoring: `LWS.TruckSkeeBall.Editor.SkeeAuthoring.Create`, saved scene `Assets/TruckSkeeBall/Scenes/TruckSkeeBall.unity`. Combined explicit-scene builder includes it without modifying the host build configuration. Do not use the broad CreateValidateBuild to preserve existing scene edits.

## Assets and authority

NWH WheelController owns wheel physics; EasyRoads owns curved road geometry; canonical Truck Taxi interior and steering presentation; Cinemachine views; Heat/TMP/DOTween shared UI; existing Bowling maple texture and truck models. LWS owns ring crossing, delivery/rules/AI, limited aftertouch, local input ownership and actual-motion replay coordination. Vendor sources unchanged; no duplicate vendor physics or UI framework.

## Validation status

See VALIDATION.md. Native simulation and injected inputs passed; the queued final production Windows build, sustained rendered performance and human/controller feel checks remain pending. This source stage is not a delivered Windows build.
