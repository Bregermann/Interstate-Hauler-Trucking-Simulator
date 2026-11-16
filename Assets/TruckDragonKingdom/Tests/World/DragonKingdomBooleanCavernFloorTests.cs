#if UNITY_EDITOR && VISTA
using System;
using NUnit.Framework;

public sealed class DragonKingdomBooleanCavernFloorTests
{
    static T Read<T>(object value, string name) => (T)value.GetType().GetField(name).GetValue(value);
    [TestCase("signed-plane")]
    [TestCase("different-void-values")]
    [TestCase("idempotence")]
    [TestCase("outside-footprint")]
    [TestCase("outside-upper-band")]
    [TestCase("below-lower-bound")]
    [TestCase("original-solid")]
    public void NativeSignedFloorUsesExactWorldPlaneAndPreservesOutsidePayload(string mode)
    {
        var authoring = Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor", true);
        object proof = authoring.GetMethod("ProbeBooleanFloorControl").Invoke(null, new object[] { mode });
        Assert.That(Read<bool>(proof, "originalInputExact"), Is.True);
        Assert.That(Read<bool>(proof, "repeatPayloadExact"), Is.True);
        Assert.That(Read<bool>(proof, "outsidePayloadExact"), Is.True);
        var values = Read<float[]>(proof, "values"); var worldY = Read<float[]>(proof, "worldY"); var inside = Read<bool[]>(proof, "inside");
        for (int i = 0; i < values.Length; i++)
        {
            if (mode == "original-solid") Assert.That(values[i], Is.EqualTo(-80), "Already-solid field must not be made empty by the floor intersection.");
            else if (inside[i]) Assert.That(values[i], Is.EqualTo(worldY[i] - 105), "Both sides must use the same exact 105m world plane, independent of previous positive density.");
            else Assert.That(values[i], Is.EqualTo(30));
        }
        if (mode == "outside-footprint" || mode == "outside-upper-band" || mode == "below-lower-bound")
            Assert.That(Read<int>(proof, "outsideSamples"), Is.GreaterThan(0));
        if (mode == "original-solid" || mode == "outside-upper-band") Assert.That(Read<int>(proof, "modifiedSamples"), Is.Zero);
        if (mode == "signed-plane")
        {
            Assert.That(values[0], Is.EqualTo(-.5f));
            Assert.That(values[4], Is.Zero);
            Assert.That(values[8], Is.EqualTo(.5f));
            float crossing = worldY[0] + (worldY[8] - worldY[0]) * -values[0] / (values[8] - values[0]);
            Assert.That(crossing, Is.EqualTo(105), "Linear marching interpolation must produce the authored plane, not a tolerance-adjusted floor.");
        }
    }
}
#endif
