#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckBowling;
namespace LWS.TruckSports
{
    // Explicit acceptance fixture only. Normal gameplay never adds devices.
    public static class SportsExpansionCameraAcceptance
    {
        public static IEnumerator Run(SportsEventSession match, SportsDrivingViews views, Action<bool, string> check, string folder)
        {
            var pads = new Gamepad[4];
            try
            {
                for (int i = 0; i < 4; i++) pads[i] = InputSystem.AddDevice<Gamepad>("Injected expansion player " + i);
                for (int humans = 1; humans <= 4; humans++)
                {
                    match.SetupScreen(); match.setup.count = Mathf.Min(match.MaximumPlayers, humans + 2); match.NormalizeSetup(); match.AllBots();
                    for (int i = 0; i < humans; i++) { match.setup.players[i].source = BowlingSource.WholeController; match.setup.players[i].pad = pads[i]; InputSystem.QueueStateEvent(pads[i], new GamepadState()); }
                    SportsCameraOptions.Set(false); yield return null;
                    bool started = match.StartMatch(); check(started, humans + " software-controller humans plus AI start through setup");
                    if (!started) yield break;
                    views.Select(2); yield return new WaitForSecondsRealtime(.2f);
                    bool correct = match.cameras.Length == humans;
                    for (int view = 0; view < humans; view++)
                    {
                        var rect = match.cameras[view].rect;
                        correct &= match.cameras[view].enabled && views.rally.owners[view] == view && rect.xMin >= 0 && rect.yMin >= 0 && rect.xMax <= 1 && rect.yMax <= 1;
                        for (int previous = 0; previous < view; previous++) correct &= !rect.Overlaps(match.cameras[previous].rect);
                    }
                    check(correct, humans + " human-only cab viewports have distinct correct owners and bounded nonoverlapping rectangles");
                    InputSystem.QueueStateEvent(pads[0], new GamepadState { rightTrigger = .75f }); yield return new WaitForSecondsRealtime(.12f);
                    bool isolated = match.commands[0].y > .5f;
                    for (int i = 1; i < humans; i++) isolated &= match.commands[i] == Vector2.zero;
                    check(isolated, humans + "-human input ownership isolates injected accelerator");
                    InputSystem.QueueStateEvent(pads[0], new GamepadState());
                    if (humans == 4)
                    { yield return SportsWindowCapture.Save(1280, 720, folder + "/four-cabs-720.png"); yield return SportsWindowCapture.Save(1920, 1080, folder + "/four-cabs-1080.png"); }
                    SportsCameraOptions.Set(true); views.Layout(); yield return null;
                    bool aiPolicy = match.cameras.Length == Mathf.Min(4, match.setup.count);
                    for (int i = 0; i < humans; i++) aiPolicy &= views.rally.owners[i] == i;
                    for (int i = 0; i < match.setup.count; i++) aiPolicy &= views.rally.trucks[i].gameObject.activeSelf;
                    check(aiPolicy, "Show AI Cameras fills only supported views after humans and leaves all trucks active");
                    match.SetupScreen();
                }
                match.setup.players[4].source = BowlingSource.Keyboard;
                check(!match.ValidateSources(out string humanLimitReason) && humanLimitReason.Contains("four"), "unsupported fifth human is rejected with a clear local-player limit");
                match.setup.count = 4; match.AllBots(); SportsCameraOptions.Set(false); check(match.StartMatch(), "AI-only spectator rematch starts"); views.Select(1); yield return null;
                check(views.NeedsArena && views.rally.broadcast.view.enabled && match.cameras.Length == 1, "AI-only with AI cameras OFF has a live broadcast instead of a blank screen");
                SportsCameraOptions.Set(true); views.Layout(); yield return null;
                check(match.cameras.Length == 4, "AI-only with AI cameras ON has four correctly allocated views"); match.SetupScreen();
            }
            finally
            {
                foreach (var pad in pads) if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            }
        }
    }
}
#endif
