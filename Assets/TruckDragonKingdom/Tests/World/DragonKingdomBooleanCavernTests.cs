#if UNITY_EDITOR && VISTA
using System;
using System.Reflection;
using NUnit.Framework;

public sealed class DragonKingdomBooleanCavernTests
{
    static Type Authoring => Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor", true);
    static T Read<T>(object value, string field) => (T)value.GetType().GetField(field).GetValue(value);

    [TestCase("inside-depth")]
    [TestCase("negative-band")]
    [TestCase("outside-band")]
    [TestCase("idempotence")]
    [TestCase("union-order")]
    [TestCase("native-ellipsoid")]
    [TestCase("native-holes")]
    public void NativeScheduledBooleanJobRetainsSignedFieldAndVendorPayloadSemantics(string mode)
    {
        object proof = Authoring.GetMethod("ProbeBooleanUnionControl").Invoke(null, new object[] { mode });
        Assert.That(Read<bool>(proof, "originalInputExact"), Is.True);
        Assert.That(Read<bool>(proof, "repeatPayloadExact"), Is.True, "Repeated union must preserve all voxel payload bits.");
        var values = Read<float[]>(proof, "values");
        Assert.That(values.Length, Is.EqualTo(64));
        switch (mode)
        {
            case "inside-depth":
                Assert.That(values, Is.All.EqualTo(1), "Native terrain depth must not amplify authored inside field.");
                Assert.That(Read<int>(proof, "interiorChanges"), Is.EqualTo(64));
                break;
            case "negative-band":
                Assert.That(values, Is.All.EqualTo(-1), "Negative exterior samples must replace deep untouched density too.");
                Assert.That(Read<int>(proof, "exteriorChanges"), Is.EqualTo(64));
                break;
            case "outside-band":
                Assert.That(Read<bool>(proof, "outsidePayloadExact"), Is.True);
                Assert.That(Read<int>(proof, "interiorChanges") + Read<int>(proof, "exteriorChanges"), Is.Zero);
                break;
            case "idempotence":
                for (int i = 0; i < values.Length; i++) Assert.That(values[i], Is.EqualTo(i % 2 == 0 ? -1 : 1));
                Assert.That(values[1] / (values[1] - values[0]), Is.EqualTo(.5f), "Crossing remains depth-independent with both signed endpoints.");
                break;
            case "union-order":
                Assert.That(Read<bool>(proof, "orderPayloadExact"), Is.True);
                break;
            case "native-ellipsoid":
                Assert.That(values[1 * 16 + 1 * 4 + 1], Is.EqualTo(2));
                Assert.That(values[2 * 16 + 1 * 4 + 1], Is.EqualTo(1));
                Assert.That(values[1 * 16 + 2 * 4 + 1], Is.EqualTo(0));
                Assert.That(values[1 * 16 + 3 * 4 + 1], Is.EqualTo(-2));
                break;
            case "native-holes":
                Assert.That(Read<int>(proof, "newHoles"), Is.GreaterThan(0));
                Assert.That(Read<int>(proof, "markedHoles"), Is.GreaterThan(0));
                break;
        }
    }
}
#endif
