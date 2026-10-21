#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FieldGoalHumanPlaytestRegressionTests
{
    static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
    static Type MatchType => Type.GetType("LWS.TruckFieldGoal.FieldGoalMatch, Assembly-CSharp", true);
    static Bounds Rendered(Transform root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true); Assert.That(renderers, Is.Not.Empty);
        var b = renderers[0].bounds; foreach (var renderer in renderers) b.Encapsulate(renderer.bounds); return b;
    }
    static IEnumerator Load()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
    }

    [UnityTest]
    public IEnumerator HolderIsGroundedImmediatelyBesideTheBallAndFacesTheUprights()
    {
        yield return Load();
        var match = (Component)UnityEngine.Object.FindObjectsByType(MatchType, FindObjectsSortMode.None).Single();
        var holder = match.transform.Find("Stationary football holder truck"); Assert.That(holder, Is.Not.Null);
        var model = holder.Cast<Transform>().Single(t => t.name.Contains("NWH truck model"));
        var b = Rendered(model); var ball = (Component)Field(match, "football");
        Assert.That(Vector3.Dot(holder.forward, Vector3.forward), Is.GreaterThan(.95f), "Holder must face downfield rather than sideways.");
        Assert.That(b.min.y, Is.InRange(.01f, .12f));
        Assert.That(b.center.x - ball.transform.position.x, Is.InRange(1.4f, 2.3f), "Use actual model bounds, not the imported model's off-center pivot.");
        Assert.That(Mathf.Abs(b.center.z - ball.transform.position.z), Is.LessThan(1.2f));
        Assert.That(b.size.z, Is.InRange(2.7f, 3.3f));
        Assert.That(holder.GetComponentsInChildren<Collider>(true), Is.Empty, "The presentation holder must not obstruct the run-up or football.");
    }

    [UnityTest]
    public IEnumerator ConfirmedScoreCelebratesOnceWithoutGivingTheHolderAnyPhysicsAuthority()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "FieldHolderRegression" }))
        {
            yield return Load();
            var match = (Component)UnityEngine.Object.FindObjectsByType(MatchType, FindObjectsSortMode.None).Single();
            var presentationType = Type.GetType("LWS.TruckFieldGoal.FieldGoalHolderPresentation, Assembly-CSharp");
            Assert.That(presentationType, Is.Not.Null, "Holder needs a score-confirmed presentation, not a scoring animation event.");
            var presentation = match.GetComponent(presentationType); Assert.That(presentation, Is.Not.Null);
            Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            var holder = (Transform)Field(presentation, "holder"); var home = holder.localPosition;
            // Explicit presentation fixture. Actual bumper kicks and scoring are
            // verified separately; this does not pretend to be an earned goal.
            Call(match, "Resolve", true, "PRESENTATION FIXTURE", Vector3.zero);
            Assert.That((int)presentationType.GetProperty("Celebrations").GetValue(presentation), Is.EqualTo(1));
            yield return new WaitForSeconds(.18f);
            Assert.That((holder.localPosition - home).magnitude, Is.GreaterThan(.025f));
            Assert.That(holder.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            Assert.That(holder.GetComponentsInChildren<Collider>(true), Is.Empty);
            Call(match, "Resolve", true, "LATE CALLBACK", Vector3.zero);
            Assert.That((int)presentationType.GetProperty("Celebrations").GetValue(presentation), Is.EqualTo(1));
            Call(match, "SetupScreen"); yield return null;
            Assert.That(holder.localPosition, Is.EqualTo(home));
        }
    }

    [UnityTest]
    public IEnumerator AudioDiagnosticUsesAnIsolatedListenerAndRestoresTheOriginal()
    {
        yield return Load();
        var listener = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Single(a => a.isActiveAndEnabled);
        var captureType = Type.GetType("LWS.TruckFieldGoal.FieldGoalAudioCapture, Assembly-CSharp", true);
        var capture = (Component)captureType.GetMethod("Install").Invoke(null, new object[] { listener });
        Assert.That(capture.GetComponents<AudioSource>(), Is.Empty);
        Assert.That(listener.enabled, Is.False);
        Assert.That(capture.GetComponent<AudioListener>().enabled, Is.True);
        Assert.That(capture.transform.position, Is.EqualTo(listener.transform.position));
        Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a => a.isActiveAndEnabled), Is.EqualTo(1));
        UnityEngine.Object.Destroy(capture.gameObject); yield return null;
        Assert.That(listener.enabled, Is.True);
        Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a => a.isActiveAndEnabled), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator OneConfirmedGoalHasOneCrowdAudioOwnerAndPostContactsAreDebounced()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "FieldAudioOwnership" }))
        {
            yield return Load();
            var match = (Component)UnityEngine.Object.FindObjectsByType(MatchType, FindObjectsSortMode.None).Single();
            Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            var feedback = Field(match, "feedback"); var played = feedback.GetType().GetProperty("Played");
            var clips = (AudioClip[])Field(feedback, "clips"); int kickIndex = (int)Field(match, "kickSoundIndex");
            Assert.That(kickIndex, Is.InRange(0, clips.Length - 1)); Assert.That(clips[kickIndex], Is.Not.Null);
            Assert.That(clips[kickIndex].name, Is.EqualTo("SuspensionThump"), "Saved sport configuration must use the dedicated physical kick clip.");
            int before = (int)played.GetValue(feedback);
            Call(match, "Resolve", true, "EXPLICIT AUDIO OWNERSHIP FIXTURE", Vector3.zero);
            Assert.That((int)played.GetValue(feedback), Is.EqualTo(before), "The existing crowd response must not be doubled by the shared feedback pool at another pitch.");
            for (int i = 0; i < 6; i++) Call(match, "PostHit", 8f);
            Assert.That((int)played.GetValue(feedback), Is.EqualTo(before), "Late post callbacks after a locked score must not overlap the result cheer.");
            Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            before = (int)played.GetValue(feedback);
            for (int i = 0; i < 6; i++) Call(match, "PostHit", 8f);
            Assert.That((int)played.GetValue(feedback), Is.EqualTo(before + 1), "Compound post callbacks in one instant must create one clang, not a buzz.");
            yield return new WaitForSeconds(.16f); Call(match, "PostHit", 8f);
            Assert.That((int)played.GetValue(feedback), Is.EqualTo(before + 2), "A later genuine rebound still has sound.");
            Call(match, "SetupScreen");
        }
    }
}
#endif
