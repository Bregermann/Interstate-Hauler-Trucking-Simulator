# Truck Hockey

Playable 1v1 mode in Truck Sports. Scene: Assets/TruckHockey/Scenes/TruckHockey.unity. Start from Assets/TruckSports/Scenes/TruckSports.unity for the collection menu, or open this scene directly in Unity.

Drive trucks into the puck continuously. Play keyboard or assigned-pad versus AI, shared keyboard, two assigned pads, or keyboard versus pad. Choose distinct team colors, names, Easy/Normal/Hard AI and 2/3/5-minute regulation. Default is three minutes. Hockey has simultaneous play; Billiards retains its separate hot-seat mode.

P1 WASD / Space brake / R recover; P2 arrows / Right Shift brake / Enter recover. Assigned pad: left stick, RT gas, LT reverse, South brake, North recover, Start pause. Esc pauses. Recovery resets only that truck in its own half, with a two-second cooldown. Release controls after a pause or reassignment before driving again.

A whole-puck crossing through the mouth counts once for the attacking team, including own goals. A crossing at or before the authoritative regulation deadline counts. Pauses, goals and faceoffs do not consume regulation time. A tie enters sudden-death overtime. Rematch preserves setup and swaps ends. Invalid puck recovery announces a faceoff and preserves score/time; an accessible stationary puck is left alone.

Rules: Scripts/HockeyRules.cs. Runtime authority: Scripts/HockeyMatch.cs. Physical driving: Scripts/HockeyTruck.cs. Contact/containment: Scripts/HockeyPuck.cs. Same-controller AI: Scripts/HockeyAI.cs. Saved-scene authoring: Editor/HockeyAuthoring.cs. Tuning is serialized on these components and Materials/* contact.asset. Vendor geometry is render-only; the dynamic rigidbody and one truck box collider provide hockey contact.

See ../TruckSports/README.md for settings, build commands and vendor audit; ../TruckSports/VALIDATION.md for actual evidence and limitations; ../TruckSports/CHECKPOINT.md for delivery status. Native input, physical controllers/rumble, listening and sustained unassisted matches need human playtesting.
