#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;

public sealed class SportsTextEditBufferTests
{
    static Type Buffer => Type.GetType("LWS.TruckSports.SportsTextEditBuffer, Assembly-CSharp", true);
    static object New(string value, int limit) => Activator.CreateInstance(Buffer, value, limit);
    static object Call(object value, string method, params object[] args) => Buffer.GetMethod(method).Invoke(value, args);
    static string Text(object value) => (string)Buffer.GetProperty("Text").GetValue(value);
    [Test] public void DraftNeverChangesOriginalAndCancelDiscardsAllEdits()
    {
        var value = New("Driver", 40); Call(value, "Insert", " 2"); Call(value, "Backspace");
        Assert.That(Text(value), Is.EqualTo("Driver "));
        Assert.That(Buffer.GetProperty("Original").GetValue(value), Is.EqualTo("Driver"));
        Assert.That(Call(value, "Cancel"), Is.EqualTo("Driver"));
        Assert.That(Call(value, "Insert", "wrong"), Is.EqualTo(false));
    }
    [Test] public void ClearAllowsReplacementAndCommitClosesExactlyOnce()
    {
        var value = New("Old", 8); Call(value, "Clear"); Call(value, "Insert", "New 123");
        Assert.That(Call(value, "Commit"), Is.EqualTo("New 123"));
        Assert.That(Call(value, "Clear"), Is.EqualTo(false));
        Assert.Throws<TargetInvocationException>(() => Call(value, "Commit"));
    }
    [Test] public void LimitRejectsWholeInsertionWithoutTruncatingOrDamagingSurrogates()
    {
        var value = New("123", 4);
        Assert.That(Call(value, "Insert", "\U0001F69A"), Is.EqualTo(false));
        Assert.That(Text(value), Is.EqualTo("123"));
        Assert.That(Call(value, "Insert", "4"), Is.EqualTo(true));
        Assert.That(Call(value, "Insert", "5"), Is.EqualTo(false));
    }
    [TestCase("\n")][TestCase("\t")][TestCase("\u0000")]
    public void ControlsAndUnpairedSurrogatesCannotEnterTheDraft(string invalid)
    { var value = New("Keep", 40); Assert.That(Call(value, "Insert", invalid), Is.EqualTo(false)); Assert.That(Text(value), Is.EqualTo("Keep")); }
    [Test] public void UnpairedUtf16SurrogatesCannotEnterTheDraft()
    { foreach (char c in new[] { (char)0xD800, (char)0xDC00 }) { var value = New("Keep", 40); Assert.That(Call(value, "Insert", new string(c, 1)), Is.EqualTo(false)); Assert.That(Text(value), Is.EqualTo("Keep")); } }
    [Test] public void BackspaceRemovesAnEntireCombiningCharacterOrSupplementaryCharacter()
    {
        var value = New("Ae\u0301\U0001F69A", 40); Call(value, "Backspace"); Assert.That(Text(value), Is.EqualTo("Ae\u0301"));
        Call(value, "Backspace"); Assert.That(Text(value), Is.EqualTo("A"));
    }
    [Test] public void RejectedValidatedReplacementLeavesTheOriginalDraftUntouched()
    { var value = New("Keep", 4); Assert.That(Call(value, "ReplaceDraft", "Too long"), Is.EqualTo(false)); Assert.That(Text(value), Is.EqualTo("Keep")); }
}
#endif
