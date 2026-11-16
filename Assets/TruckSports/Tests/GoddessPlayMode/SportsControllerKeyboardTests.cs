#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

// Small disposable UI tests: no sport scene, gameplay state or saved profile is changed.
public sealed class SportsControllerKeyboardTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object o, string name) => o.GetType().GetField(name, Any) is FieldInfo f ? f.GetValue(o) : o.GetType().GetProperty(name, Any).GetValue(o);
    static void Set(object o, string name, object value) => o.GetType().GetProperty(name, Any).SetValue(o, value);
    static object Invoke(object o, string name, params object[] args) => o.GetType().GetMethod(name, Any).Invoke(o, args);
    static object KeyboardCall(string name, params object[] args) => Sports("SportsControllerKeyboard").GetMethod(name).Invoke(null, args);
    static bool Open => (bool)Sports("SportsControllerKeyboard").GetProperty("IsOpen").GetValue(null);
    static bool Blocked => (bool)Sports("SportsControllerKeyboard").GetProperty("InputBlocked").GetValue(null);
    static object Modal => Sports("SportsControllerKeyboard").GetField("current", Any).GetValue(null);
    GameObject root, fieldObject;
    Component field;
    EventSystem events, previous;
    bool previousEnabled;
    Gamepad owner, other;
    int values, commits;
    [UnitySetUp] public IEnumerator Setup()
    {
        values = commits = 0;
        previous = EventSystem.current; previousEnabled = previous != null && previous.enabled; if (previous != null) previous.enabled = false;
        root = new GameObject("Disposable keyboard UI", typeof(RectTransform), typeof(Canvas));
        var eventRoot = new GameObject("Disposable event system", typeof(EventSystem)); eventRoot.transform.SetParent(root.transform); events = eventRoot.GetComponent<EventSystem>();
        var menu = (Behaviour)root.AddComponent(Sports("SportsMenuView")); menu.enabled = false;
        fieldObject = new GameObject("Name", typeof(RectTransform)); fieldObject.transform.SetParent(root.transform);
        field = fieldObject.AddComponent(Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro", true)); Set(field, "characterLimit", 12); Invoke(field, "SetTextWithoutNotify", "Old");
        ((UnityEvent<string>)Get(field, "onValueChanged")).AddListener(_ => values++); ((UnityEvent<string>)Get(field, "onEndEdit")).AddListener(_ => commits++);
        Sports("SportsControllerTextField").GetMethod("BindUnder").Invoke(null, new object[] { root.transform });
        Sports("SportsControllerTextField").GetMethod("BindUnder").Invoke(null, new object[] { root.transform });
        Assert.That(fieldObject.GetComponents(Sports("SportsControllerTextField")).Length, Is.EqualTo(1));
        owner = InputSystem.AddDevice<Gamepad>("Keyboard owner"); other = InputSystem.AddDevice<Gamepad>("Unrelated controller");
        events.SetSelectedGameObject(fieldObject); yield return null;
    }
    static IEnumerator Pad(Gamepad p, GamepadState state)
    { InputSystem.QueueStateEvent(p, state); yield return null; yield return null; }
    IEnumerator StartEdit()
    {
        yield return Pad(owner, new GamepadState().WithButton(GamepadButton.South)); Assert.That(Open, Is.True, "Selected shared input must open without a mouse.");
        yield return Pad(owner, new GamepadState()); yield return null;
    }
    [UnityTest] public IEnumerator OwnerOnlyTypingCancelAndFocusRestoreDoNotCommit()
    {
        Sports("SportsControllerKeyboard").GetProperty("MenuPadAllowed").SetValue(null, new Predicate<Gamepad>(p => p == owner));
        yield return Pad(other, new GamepadState().WithButton(GamepadButton.South)); Assert.That(Open, Is.False);
        yield return Pad(other, new GamepadState()); yield return StartEdit();
        Assert.That(Sports("SportsControllerKeyboard").GetProperty("OwnerPad").GetValue(null), Is.SameAs(owner));
        yield return Pad(other, new GamepadState().WithButton(GamepadButton.South));
        Assert.That(Get(Get(Modal, "draft"), "Text"), Is.EqualTo("Old"));
        yield return Pad(other, new GamepadState());
        yield return Pad(owner, new GamepadState().WithButton(GamepadButton.South));
        Assert.That(Get(Get(Modal, "draft"), "Text"), Is.EqualTo("Old1")); Assert.That(Get(field, "text"), Is.EqualTo("Old")); Assert.That(commits, Is.Zero); Assert.That(values, Is.Zero);
        yield return Pad(owner, new GamepadState()); yield return Pad(owner, new GamepadState().WithButton(GamepadButton.East));
        Assert.That(Open, Is.False); Assert.That(Get(field, "text"), Is.EqualTo("Old")); Assert.That(commits, Is.Zero);
        yield return Pad(owner, new GamepadState()); yield return null; yield return null;
        Assert.That(events.currentSelectedGameObject, Is.SameAs(fieldObject)); Assert.That(events.sendNavigationEvents, Is.True);
    }
    [UnityTest] public IEnumerator ConfirmCommitsOnceAndHeldSubmitCannotLeakToTheUnderlyingMenu()
    {
        yield return StartEdit(); yield return Pad(owner, new GamepadState().WithButton(GamepadButton.South)); yield return Pad(owner, new GamepadState());
        yield return Pad(owner, new GamepadState().WithButton(GamepadButton.Start));
        Assert.That(Open, Is.False); Assert.That(Get(field, "text"), Is.EqualTo("Old1")); Assert.That(commits, Is.EqualTo(1)); Assert.That(values, Is.EqualTo(1)); Assert.That(Blocked, Is.True);
        yield return null; yield return null; Assert.That(commits, Is.EqualTo(1));
        yield return Pad(owner, new GamepadState()); yield return null; yield return null; Assert.That(Open, Is.False); Assert.That(commits, Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator ExplicitCreateCallbackReceivesOnlyConfirmedDraft()
    {
        int creates = 0; string result = null;
        Assert.That(KeyboardCall("Open", field, owner, "", new Action<string>(v => { creates++; result = v; }), "NEW DRIVER"), Is.EqualTo(true));
        yield return null; yield return null; yield return null;
        Invoke(Modal, "Insert", "New"); Assert.That(creates, Is.Zero); Assert.That(Get(field, "text"), Is.EqualTo("Old"));
        yield return Pad(owner, new GamepadState().WithButton(GamepadButton.Start)); Assert.That(creates, Is.EqualTo(1)); Assert.That(result, Is.EqualTo("New")); Assert.That(commits, Is.Zero); Assert.That(Get(field, "text"), Is.EqualTo("Old"));
    }
    [UnityTest] public IEnumerator OwnerDisconnectCancelsTheDraftWithoutAProfileCommit()
    {
        yield return StartEdit(); Invoke(Modal, "Insert", " New"); InputSystem.RemoveDevice(owner); yield return null; yield return null;
        Assert.That(Open, Is.False); Assert.That(Get(field, "text"), Is.EqualTo("Old")); Assert.That(commits, Is.Zero); Assert.That(values, Is.Zero);
    }
    [Test] public void DetachedTmpValidationUsesTheActualFieldRulesWithoutItsSaveCallbacks()
    {
        var validationRoot = new GameObject("Detached validator", typeof(RectTransform)); validationRoot.SetActive(false);
        try
        {
            var kind = field.GetType().GetProperty("characterValidation").PropertyType; Set(field, "characterValidation", Enum.Parse(kind, "Integer"));
            var validator = validationRoot.AddComponent(Sports("SportsKeyboardValidation")); Invoke(validator, "Configure", field);
            Assert.That(Invoke(validator, "AppendValidated", "2", "a3"), Is.EqualTo("23")); Assert.That(Get(field, "text"), Is.EqualTo("Old")); Assert.That(commits, Is.Zero); Assert.That(values, Is.Zero);
        }
        finally { UnityEngine.Object.DestroyImmediate(validationRoot); }
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        KeyboardCall("Cancel"); if (owner != null && owner.added) InputSystem.QueueStateEvent(owner, new GamepadState()); if (other != null && other.added) InputSystem.QueueStateEvent(other, new GamepadState());
        yield return null; yield return null; yield return null;
        if (Modal is Component live) UnityEngine.Object.Destroy(live.gameObject);
        if (root != null) UnityEngine.Object.Destroy(root);
        if (owner != null && owner.added) InputSystem.RemoveDevice(owner); if (other != null && other.added) InputSystem.RemoveDevice(other);
        Sports("SportsControllerKeyboard").GetProperty("MenuPadAllowed").SetValue(null, null);
        yield return null; if (previous != null) previous.enabled = previousEnabled;
    }
}
#endif
