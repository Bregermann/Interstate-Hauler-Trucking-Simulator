# Controller text entry — RF09-KEYBOARD-001

Uses existing Heat buttons, broadcast plates, TMP validation, Input System and DOTween focus animation. No new profile database, input backend, menu navigation system or vendor source change.

## Shared lobby integration

```csharp
SportsControllerTextField.Bind(nameField); // once after creating a dynamic field
// Or BindUnder(lobbyTransform) once for a controlled shared UI hierarchy.
SportsControllerKeyboard.Open(nameField, enteringPlayersPad);
// Detached creation: nothing is saved if the user cancels.
SportsControllerKeyboard.Open(nameField, enteringPlayersPad, "", committedName => CreateProfile(committedName), "NEW DRIVER");
```

`Open` returns false if another modal is active, controls are still gated, the target is hidden/read-only/noninteractive, or the explicitly supplied pad is missing. Its complete signature is `Open(TMP_InputField, Gamepad, string initial = null, Action<string> onCommit = null, string title = "EDIT NAME")`. C# callers can supply two arguments; reflection callers must supply all five. Explicit owners may be nonhost players.

- `OwnerPad`: controller that owns the current modal.
- `InputBlocked`: true throughout editing and the closing neutral-release guard. The lobby should suspend its own raw per-pad shortcuts while true.
- `Closed(TMP_InputField, bool confirmed)` event: commit/cancel notification at the start of closing. Wait for `InputBlocked` to clear before restoring new menu focus or accepting another action. Never interpret cancellation as profile creation.
- `CancelFor(field)` / `Cancel()`: discard a draft safely.
- `MenuPadAllowed`: optional `Predicate<Gamepad>` used only by `SubmittingPad()`/automatic field opening. The party runtime may install its host/entry policy. Explicit `Open(field, pad)` does not restrict a nonhost owner.

The keyboard disables `EventSystem.sendNavigationEvents` temporarily and handles only its opening pad's key actions; existing pointer actions remain enabled. When applying a host-only UI action-device filter, retain Mouse/Keyboard in that filter if optional pointer/keyboard UI is desired. The modal does not replace the action asset or change its device filter.

Normal field edits emit the existing `onValueChanged` and `onEndEdit` callbacks once on confirmation, using `SetTextWithoutNotify` beforehand. The draft never updates the original field or profile per keystroke. An explicit creation callback replaces these field callbacks. Cancel, source-field disable, focus loss and owner disconnect do not commit. Keyboard letters, Space, Backspace, Enter/Escape and pointer key clicks are supported alongside the owner pad.

Existing shared sport and Your Crew fields register once on scene load, including inactive pages. `SportsMenuView.Start` also binds dynamically instantiated shared pages; the existing UI name-field builder adds the adapter. Your Crew's controller NEW path now waits for keyboard confirmation before creating a profile. Physical mouse NEW retains its existing flow.

## Checks and limits

Final cached Main compilation passes (exit0, 5.367s); the two focused NUnit source files compile (exit0, 0.634s). Both use retained references, not CLEAN or an Editor process. All14 actual standalone edit-buffer checks pass for detached edits, limits, Unicode deletion, invalid controls, cancel and commit-once behavior. Disposable Unity tests cover controller opening/ownership, cancel/focus, commit-once/release gating, creation callback, disconnect and native TMP validation. Unity/Windows execution, rendered keyboard scaling and real-controller verification remain unrun while the earlier build is active.

The keyboard edits at the end of the draft; it does not offer cursor/selection editing. Password fields mask the draft preview; existing shared essential fields are participant names. No match sessions, navigation/lobby state, registry or statistics code is changed.
