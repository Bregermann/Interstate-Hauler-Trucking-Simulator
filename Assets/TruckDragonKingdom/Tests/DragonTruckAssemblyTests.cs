#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonTruckAssemblyTests
{
    readonly List<GameObject> objects = new List<GameObject>();
    static Type Runtime(string name) => Type.GetType("LWS.TruckDragonKingdom." + name + ", Assembly-CSharp", true);
    static void Set(object instance, string field, object value) => instance.GetType().GetField(field).SetValue(instance, value);
    static object Call(object instance, string method, params object[] args) => instance.GetType().GetMethod(method).Invoke(instance, args);
    GameObject New(string name) { var g = new GameObject(name); objects.Add(g); return g; }
    Component Truck(out Rigidbody main, out Rigidbody child, out Collider mainCollider, out Collider childCollider)
    {
        var root = New("Unmodified multibody truck fixture"); root.SetActive(false);
        main = root.AddComponent<Rigidbody>(); main.useGravity = false; main.mass = 1200;
        main.constraints = RigidbodyConstraints.FreezeRotationX; main.interpolation = RigidbodyInterpolation.Interpolate;
        mainCollider = root.AddComponent<BoxCollider>();
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube); visual.name = "Cab"; visual.transform.SetParent(root.transform, false);
        UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
        var secondary = New("Actual secondary assembly body"); secondary.transform.SetParent(root.transform, false);
        secondary.transform.localPosition = new Vector3(0, 1, -2);
        child = secondary.AddComponent<Rigidbody>(); child.isKinematic = true; child.useGravity = true; child.mass = 37;
        childCollider = secondary.AddComponent<BoxCollider>();
        var type = Type.GetType("LWS.TruckSports.SportsEventTruck, Assembly-CSharp", true);
        var truck = root.AddComponent(type);
        Set(truck, "body", main); Set(truck, "shape", mainCollider); Set(truck, "visuals", visual.transform);
        Set(truck, "paint", visual.GetComponent<Renderer>());
        Set(truck, "wheels", Array.CreateInstance(type.GetField("wheels").FieldType.GetElementType(), 0));
        root.SetActive(true); ((Behaviour)truck).enabled = false;
        return truck;
    }
    [TearDown] public void Cleanup() { foreach (var item in objects) if (item != null) UnityEngine.Object.DestroyImmediate(item); objects.Clear(); }

    [Test] public void CarryRestoresEveryBodyFlagAndExistingCollisionIgnores()
    {
        var truck = Truck(out var main, out var child, out var own, out var extra);
        Call(truck, "Power", true);
        var other = New("Dragon collider").AddComponent<BoxCollider>(); other.transform.position = Vector3.right * 30;
        Physics.IgnoreCollision(own, other, true);
        var carry = Activator.CreateInstance(Runtime("DragonTruckCarryBinding"), new object[] { truck, new Pose(Vector3.zero, Quaternion.identity), new Collider[] { other } });
        Assert.That(main.isKinematic && child.isKinematic, Is.True);
        Assert.That((bool)truck.GetType().GetProperty("Powered").GetValue(truck), Is.False);
        Assert.That(Physics.GetIgnoreCollision(extra, other), Is.True);
        Call(carry, "Release", Vector3.forward * 100, Vector3.up * 20, false);
        Assert.That(main.isKinematic, Is.False); Assert.That(main.useGravity, Is.False);
        Assert.That(main.constraints, Is.EqualTo(RigidbodyConstraints.FreezeRotationX));
        Assert.That(main.interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
        Assert.That(child.isKinematic && child.useGravity, Is.True);
        Assert.That(main.mass, Is.EqualTo(1200)); Assert.That(child.mass, Is.EqualTo(37));
        Assert.That(main.linearVelocity.magnitude, Is.EqualTo(85).Within(.001f));
        Assert.That(main.angularVelocity.magnitude, Is.LessThanOrEqualTo(3.001f));
        Assert.That(Physics.GetIgnoreCollision(own, other), Is.True);
        Assert.That(Physics.GetIgnoreCollision(extra, other), Is.False);
        Assert.That((bool)truck.GetType().GetProperty("Powered").GetValue(truck), Is.True);
        main.linearVelocity = Vector3.right * 7;
        Call(carry, "Release", Vector3.zero, Vector3.zero, true);
        Assert.That(main.linearVelocity, Is.EqualTo(Vector3.right * 7), "Repeated release must not overwrite a resumed truck.");
    }

    [Test] public void ResetReleaseClearsMotionAndDoesNotEnableAnOriginallyParkedTruck()
    {
        var truck = Truck(out var main, out _, out _, out _);
        var carry = Activator.CreateInstance(Runtime("DragonTruckCarryBinding"), new object[] { truck, new Pose(Vector3.zero, Quaternion.identity), null });
        Call(carry, "Release", Vector3.forward * 20, Vector3.up, true);
        Assert.That(main.linearVelocity, Is.EqualTo(Vector3.zero));
        Assert.That((bool)truck.GetType().GetProperty("Powered").GetValue(truck), Is.False);
    }

    [Test] public void EnteringBoatTriggerWithoutRealWheelSupportCannotSecureOrExtract()
    {
        var truck = Truck(out _, out _, out _, out _);
        var boat = New("Solid evacuation deck"); boat.SetActive(false);
        var hull = boat.AddComponent<Rigidbody>(); hull.isKinematic = true;
        var deck = boat.AddComponent<BoxCollider>(); deck.size = new Vector3(24, 1, 52);
        var zone = New("Parking zone"); zone.transform.SetParent(boat.transform, false);
        var parking = zone.AddComponent<BoxCollider>(); parking.isTrigger = true; parking.size = new Vector3(22, 10, 46);
        var destination = New("Real world departure waypoint").transform; destination.position = Vector3.forward * 50;
        var vessel = boat.AddComponent(Runtime("DragonEvacuationVessel"));
        Set(vessel, "hull", hull); Set(vessel, "parkingZone", parking); Set(vessel, "supportingDeck", new Collider[] { deck });
        Set(vessel, "departureWaypoints", new[] { destination }); boat.SetActive(true);
        for (int i = 0; i < 100; i++) Assert.That((bool)Call(vessel, "ObserveBoarding", truck, .02f, 1f, .7f), Is.False);
        Assert.That((bool)Call(vessel, "RequestDeparture"), Is.False);
        Assert.That((bool)Call(vessel, "HasExtracted", truck, 12f), Is.False);
        Assert.That((int)vessel.GetType().GetProperty("SecuredCount").GetValue(vessel), Is.Zero);
    }
}
#endif
