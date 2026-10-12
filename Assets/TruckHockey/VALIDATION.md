# Truck Hockey validation

See ../TruckSports/VALIDATION.md for the actual Unity build, saved-scene/rule checks, physical puck goals/contacts, AI driving, input and lifecycle evidence, measurements, repairs and native/hardware limitations. Hockey integration tests never inject score events. Controls and source map: README.md.

Final collection player passed 161 checks at 1280x720 and 161 at 1920x1080 (Logs/TruckSports/Smoke-11-720 and Smoke-11-1080). Actual truck-pin contacts verify both own-truck recovery without false faceoff and announced emergency recovery with score/time preserved. The pin timeout is shortened in fixtures; production default remains ten seconds. Physical gamepads, native gameplay and audio listening are unverified.
