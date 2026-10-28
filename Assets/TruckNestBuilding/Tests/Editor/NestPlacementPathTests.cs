using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class NestPlacementPathTests
{
    [TestCase(-.2407f, 3.4578f, 12.9939f, 32.2f)]
    [TestCase(-4.984f, 3.206f, 9.877f, 27.8f)]
    public void MeasuredFirstPickupsCrossDeckOnlyAfterRaisingWholeMaterialAboveItsSurface(float x, float y, float z, float socketZ)
    {
        var method = Type.GetType("LWS.TruckNestBuilding.NestBuilder, Assembly-CSharp", true).GetMethod("PlacementTarget", BindingFlags.Public | BindingFlags.Static);
        var entry = new Vector3(x, y, z); var socket = new Vector3(0, 6.3f, socketZ);
        Vector3 Target(int stage) => (Vector3)method.Invoke(null, new object[] { stage, entry, socket, 6f });
        var lift = Target(0); var traverse = Target(1); var lower = Target(2);
        Assert.That(lift.x, Is.EqualTo(entry.x)); Assert.That(lift.z, Is.EqualTo(entry.z));
        Assert.That(lift.y, Is.GreaterThan(10)); Assert.That(traverse.y, Is.EqualTo(lift.y)); Assert.That(lower, Is.EqualTo(socket + Vector3.up * .07f));
        // A conservative three-metre horizontal material envelope includes every
        // orientation of the actual6m branch. No horizontal crossing occurs low.
        for (int segment = 0; segment < 3; segment++)
        {
            var from = segment == 0 ? entry : segment == 1 ? lift : traverse;
            var to = segment == 0 ? lift : segment == 1 ? traverse : lower;
            for (int i = 0; i <= 100; i++)
            {
                var p = Vector3.Lerp(from, to, i / 100f);
                bool horizontalCrossing = segment == 1 && p.z + 3 >= 24;
                if (horizontalCrossing) Assert.That(p.y - .28f, Is.GreaterThan(6), "The entire held material clears the actual platform top before crossing the lip.");
                if (segment == 2) { Assert.That(p.x, Is.EqualTo(socket.x)); Assert.That(p.z, Is.EqualTo(socket.z)); }
            }
        }
    }
}
