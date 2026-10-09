# Truck Taxi Continuous Acceptance

Status: NOT EXECUTED AS ONE CONTINUOUS RUN. No human acceptance or feature freeze is claimed.

Use an isolated ASTRA_SANDBOX_TEST profile. Record executable hash, run ID, date, tester, resolution, device, elapsed time and evidence paths. Do not use or corrupt normal player saves. Historical restart/unit/rendered results are separate evidence, not checkmarks for this sequence.

## Uninterrupted Sequence

1. [ ] Launch.
2. [ ] Load/create Career.
3. [ ] Start shift.
4. [ ] Get a local fare.
5. [ ] Decline several offers and verify spam control.
6. [ ] Receive an intercity offer in the expected mix.
7. [ ] Pick up the passenger.
8. [ ] Pick up a companion during the fare.
9. [ ] Verify passenger reaction.
10. [ ] Complete/trigger the companion bonus objective.
11. [ ] Encounter a rival.
12. [ ] Steal a rival passenger.
13. [ ] Deliver the stolen passenger.
14. [ ] Verify completion report and payout.
15. [ ] Verify route restoration.
16. [ ] Fuel without a parking-brake requirement.
17. [ ] Enter a store.
18. [ ] Buy labeled shelf products.
19. [ ] Use inventory.
20. [ ] Eat and drink.
21. [ ] Test maximum hunger/thirst and dream effects.
22. [ ] Use a bathroom.
23. [ ] Use a porta-potty.
24. [ ] Sleep in the truck.
25. [ ] Sleep in a motel.
26. [ ] Use the companion motel option.
27. [ ] Drive in rain.
28. [ ] Drive in snow.
29. [ ] Inspect snow tracks.
30. [ ] Smash breakaway scenery.
31. [ ] Throw the explosive jug.
32. [ ] Damage a tire.
33. [ ] Exit the truck.
34. [ ] Verify the seated driver is not duplicated.
35. [ ] Inspect the tire.
36. [ ] Repair the tire.
37. [ ] Re-enter the truck.
38. [ ] Run the highway/city race with 20 rivals.
39. [ ] Trigger a hurricane.
40. [ ] Open the map and verify hazard geography.
41. [ ] Save manually.
42. [ ] Exit to the menu.
43. [ ] Load the manual save.
44. [ ] Save again.
45. [ ] Quit the executable.
46. [ ] Relaunch.
47. [ ] Continue.
48. [ ] Verify stats, Career, inventory, fuel and damage.
49. [ ] Continue playing normally.

## Companion Regression Detail

- [ ] Private-stop GPS target and world circle both visible.
- [ ] Circle radius equals gameplay radius.
- [ ] Parking-brake prompt appears inside the private circle.
- [ ] Eject Passenger HUD button and semantic input work.
- [ ] Companion ejection physics observed.
- [ ] Private route, marker and circle clear after ejection.
- [ ] Ride Request suppression releases after ejection.

## One-Hour Soak

The opt-in standalone restart harness has a separate `soak` mode. It requires an existing isolated seeded run, validates drivetrain and road approach, and never teleports to make progress pass. Unsafe approach, origin-independent discontinuity, stalled motion, missing focus or recovery fail explicitly. A driving-only soak does not cover the 49-step sequence above.

- [ ] Sixty continuous minutes actually measured with the game visible and focused.
- [ ] Ordered loop progress, no recovery/teleport disguised as movement.
- [ ] Record frame-time mean/P95/max, memory growth, allocation spikes and scene/traffic counts.
- [ ] No growing exception/log spam, duplicate player/GPS/controller objects or leaked UI.
- [ ] Repeated services, map, weather, passenger and inventory use stays responsive.
- [ ] End-of-run save/quit/relaunch/Continue remains functional.

## Current Content Limits

- Current tractor retains its two authored compatible passenger slots. The reusable maximum of eight does not fabricate six extra physical seats.
- Taxi authored scenes currently contain no LWS trailer identity and no configured trailer spawn; optional moral-choice trailer gameplay still needs safe physical validation content.
- Roadside profiles need appropriate prefab pools and reviewed road/bay/entrance exclusions before explicit vendor generation. Empty profiles are not proof of denser scenery.
- Upgrade catalog currently enables actual engine, brake and fuel adapters; unavailable truck models/cosmetic assets are not sold as functioning content.
- Attached-trailer Taxi save/load remains guarded. No trailer-world restore acceptance is claimed.
- Voices, Chatterbox and source voice samples remain untouched.

