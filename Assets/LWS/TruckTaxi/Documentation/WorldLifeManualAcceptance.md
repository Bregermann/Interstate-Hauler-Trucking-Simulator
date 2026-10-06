# World-life human acceptance checklist

Run the Windows development build and test a normal ride before and after the new features. Record visual issues and the exact location/vehicle state; automated PlayMode fixtures do not prove these interactions feel or look right.

- [ ] Park, exit with G or gamepad right-stick press, walk/look/sprint, re-enter; moving exit is refused and truck input returns. Check cockpit/exterior cameras and audio listeners.
- [ ] Enter a validation store; select two shelf products, remove one from the basket, checkout, verify wallet debit and inventory stacks. Attempt insufficient funds, then consume food and drink; inspect hunger/thirst changes.
- [ ] At a gas bay, set the parking brake, exit, take the nozzle, reach the filler, hold E/gamepad South, check gallons and charge, stop and return nozzle. Verify no free fuel, no overfill and no out-of-bay dispensing.
- [ ] Enter a truck stop and diner; sit, order/pay/eat, stand, leave. Verify UTS ambient pedestrians remain bounded and available where authored paths reach the facility.
- [ ] At a motel, check in/pay at the desk, use the bed, verify time/fatigue and room expiry. Try the bed before check-in. Sleep in cab for 2/4/8 hours with the brake set; reject unsafe or active-ride attempts.
- [ ] Use urinal and toilet, complete/cancel bladder and bowel timing, toggle simplified timing, use a portable toilet. Verify unique toilet count increments only on first successful use and survives save/load.
- [ ] Trigger optional panhandler give/decline and abstract Road Pep interaction near an existing UTS pedestrian; verify wallet/effect/comedown, cancellation, and no permanent modal input capture.
- [ ] Damage a front tire and a rear tire separately; verify flat handling and wheel/rim cascade. Damage brakes, steering, engine, transmission, radiator, fuel tank, suspension and headlights; inspect each on foot, repair by capability, confirm physical response returns. Repeat after save/load and roadside tow.
- [ ] Drive into a cone, trash can, mailbox, sign, fence, bench/cart, portable toilet, dumpster, sapling, large tree, pole, barrier and building where represented. Small objects should yield; structural objects should remain credible hard stops; debris should clean up.
- [ ] Check speed in MPH, fuel in US gallons, prices in dollars per gallon, distance in feet under 0.1 mile and one-decimal miles at/above it.
- [ ] Save, quit, restart and load both in-cab and after returning from on-foot mode. Confirm no duplicate truck, facility, input source, UI root, NPC population or save system.
