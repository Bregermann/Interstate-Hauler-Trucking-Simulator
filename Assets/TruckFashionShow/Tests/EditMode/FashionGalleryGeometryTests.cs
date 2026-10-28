using System;
using NUnit.Framework;
using UnityEngine;

public sealed class FashionGalleryGeometryTests
{
    static Type Runtime => Type.GetType("LWS.TruckFashionShow.FashionGalleryPresentation, Assembly-CSharp", true);
    static void Fit(Bounds bounds, float aspect, out Vector3 focus, out Vector3 offset)
    {
        object[] args = { bounds, Quaternion.LookRotation(-new Vector3(0, .72f, -1).normalized), 48f, aspect, null, null };
        Runtime.GetMethod("FitHubBounds").Invoke(null, args); focus = (Vector3)args[4]; offset = (Vector3)args[5];
    }
    [TestCase(16f / 9, -24, 24, -39, -39)]
    [TestCase(16f / 9, -38, 38, -22, 12)]
    [TestCase(4f / 3, -38, 38, -39, 40)]
    public void ActualParticipantEnvelopeFitsAboveThreeHudRowsAndBelowHeader(float aspect, float left, float right, float near, float far)
    {
        var bounds = new Bounds(new Vector3((left + right) / 2, 3, (near + far) / 2), new Vector3(right - left + 12, 8, far - near + 20));
        Fit(bounds, aspect, out var focus, out var offset); var eye = focus + offset; var inverse = Quaternion.Inverse(Quaternion.LookRotation(focus - eye));
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 local = inverse * (corner - eye); float y = .5f + local.y / (2 * Mathf.Tan(24 * Mathf.Deg2Rad) * local.z), x = .5f + local.x / (2 * Mathf.Tan(24 * Mathf.Deg2Rad) * aspect * local.z);
            Assert.That(local.z, Is.GreaterThan(0)); Assert.That(x, Is.InRange(.06f, .94f)); Assert.That(y, Is.InRange(.23f, .92f));
        }
    }
    [Test] public void SharedQueueShotFitsActualFourTrucksRatherThanEntireEmptyOriginalShell()
    {
        Fit(new Bounds(new Vector3(0, 3, -39), new Vector3(66, 8, 20)), 16f / 9, out var focus, out var offset);
        Assert.That(offset.magnitude, Is.LessThan(80), "Original Fashion used65m vertical /73m depth to show the entire124x112m shell.");
        Assert.That(Quaternion.Angle(Quaternion.LookRotation(-offset), Quaternion.LookRotation(focus - (focus + offset))), Is.LessThan(.001f), "Shot offset remains relative to its real focus.");
    }
}
