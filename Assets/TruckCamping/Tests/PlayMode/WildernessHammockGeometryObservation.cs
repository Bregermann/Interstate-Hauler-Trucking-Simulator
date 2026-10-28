#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static WildernessNativeScope;

// Test-only observers: native collision callbacks and current geometry only.
// No pose, velocity, collider, spring, input or navigation setting is written.
public sealed class WildernessHammockGeometryObservation : MonoBehaviour
{
    [Serializable] public sealed class NativeContact
    {
        public string callback, other, thisCollider, otherCollider;
        public int frame, count;
        public float time, separation;
        public Vector3 point, normal, pairImpulse, pairRelativeVelocity;
    }
    readonly Dictionary<int, NativeContact[]> pairs = new Dictionary<int, NativeContact[]>();
    void OnCollisionEnter(Collision collision) { Observe(collision, "Enter"); }
    void OnCollisionStay(Collision collision) { Observe(collision, "Stay"); }
    void OnCollisionExit(Collision collision) { pairs.Remove(collision.collider.GetInstanceID()); }
    void Observe(Collision collision, string callback)
    {
        var contacts = new ContactPoint[collision.contactCount];
        int count = collision.GetContacts(contacts);
        pairs[collision.collider.GetInstanceID()] = contacts.Take(count).Select(c => new NativeContact
        {
            callback = callback, other = PathOf(collision.collider.transform),
            thisCollider = c.thisCollider == null ? "NULL" : PathOf(c.thisCollider.transform),
            otherCollider = c.otherCollider == null ? "NULL" : PathOf(c.otherCollider.transform),
            frame = Time.frameCount, time = Time.fixedTime, count = count,
            point = c.point, normal = c.normal, separation = c.separation,
            pairImpulse = collision.impulse, pairRelativeVelocity = collision.relativeVelocity
        }).ToArray();
    }
    static string PathOf(Transform t)
    {
        string value = t.name;
        while (t.parent != null) { t = t.parent; value = t.name + "/" + value; }
        return value;
    }
    [Serializable] sealed class Shape
    {
        public string path, type;
        public int id, bodyId;
        public bool enabled, trigger, exactBoxCorners;
        public Vector3 position, euler, scale, boundsCenter, boundsSize, center, size;
        public Quaternion rotation;
        public Vector3[] corners;
    }
    [Serializable] sealed class Body
    {
        public string path;
        public int id;
        public bool kinematic, sleeping, collisions;
        public float mass;
        public Vector3 position, velocity, angularVelocity, centerOfMass;
        public Quaternion rotation;
    }
    [Serializable] sealed class Spring
    {
        public Vector3 localAnchor, connectedAnchor, worldAnchor, worldConnectedAnchor;
        public int connectedBody;
        public float length, minDistance, maxDistance, spring, damper, tolerance;
        public bool autoConfigure;
    }
    [Serializable] sealed class Pair
    {
        public string a, b;
        public bool penetrating;
        public Vector3 direction;
        public float depth;
    }
    [Serializable] sealed class Row
    {
        public string status = "MEASUREMENT_ONLY_ORIGINAL_KEYBOARD_ASSERTIONS_UNCHANGED", stage, hammock;
        public string contactsMeaning = "Native Enter/Stay pairs retained until Exit; timestamps expose sleeping/stale callbacks. Pair impulse repeated per point; do not sum points. Begin has no callback history.";
        public float clock, real;
        public int frame;
        public Vector3 destination, approach;
        public Shape deck, hull;
        public Shape[] ramps, truckShapes;
        public Body platformBody, truckBody;
        public Spring[] springs;
        public Pair[] actualPenetrations;
        public NativeContact[] deckContacts, truckContacts;
    }
    static Vector3[] Corners(Vector3 center, Vector3 size, Transform transform)
    {
        var result = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 local = center + Vector3.Scale(size * .5f, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            result[i] = transform == null ? local : transform.TransformPoint(local);
        }
        return result;
    }
    static Shape Describe(Collider c)
    {
        var box = c as BoxCollider; var bounds = c.bounds;
        return new Shape { path = PathOf(c.transform), type = c.GetType().FullName,
            id = c.GetInstanceID(), bodyId = c.attachedRigidbody == null ? 0 : c.attachedRigidbody.GetInstanceID(),
            enabled = c.enabled, trigger = c.isTrigger, exactBoxCorners = box != null,
            position = c.transform.position, rotation = c.transform.rotation, euler = c.transform.eulerAngles, scale = c.transform.lossyScale,
            boundsCenter = bounds.center, boundsSize = bounds.size,
            center = box == null ? Vector3.zero : box.center, size = box == null ? Vector3.zero : box.size,
            corners = box == null ? Corners(bounds.center, bounds.size, null) : Corners(box.center, box.size, box.transform) };
    }
    static Body Describe(Rigidbody b) => new Body { path = PathOf(b.transform), id = b.GetInstanceID(),
        kinematic = b.isKinematic, sleeping = b.IsSleeping(), collisions = b.detectCollisions, mass = b.mass,
        position = b.position, rotation = b.rotation, velocity = b.linearVelocity, angularVelocity = b.angularVelocity, centerOfMass = b.worldCenterOfMass };
    static Pair Describe(Collider a, Collider b)
    {
        bool overlaps = Physics.ComputePenetration(a, a.transform.position, a.transform.rotation,
            b, b.transform.position, b.transform.rotation, out var direction, out var depth);
        return new Pair { a = PathOf(a.transform), b = PathOf(b.transform), penetrating = overlaps, direction = direction, depth = depth };
    }
    public sealed class CaptureScope : IDisposable
    {
        readonly WildernessNativeScope scope;
        readonly Component truck, hammock;
        readonly BoxCollider deck;
        readonly Collider hull;
        readonly Collider[] ramps;
        readonly Rigidbody truckBody, platform;
        readonly Vector3 destination;
        readonly string output;
        readonly WildernessHammockGeometryObservation truckTap, deckTap;
        public CaptureScope(WildernessNativeScope scope, Component explorer, Vector3 destination, string output)
        {
            this.scope = scope; this.destination = destination; this.output = output;
            truck = (Component)Get(explorer, "truck"); truckBody = (Rigidbody)Get(truck, "body"); hull = (Collider)Get(truck, "shape");
            var world = (Component)Get(scope.match, "world");
            hammock = Components(Get(world, "hammocks")).Where(h => h != null)
                .OrderBy(h => (((Transform)Get(h, "approach")).position - destination).sqrMagnitude).First();
            deck = (BoxCollider)Get(hammock, "deck"); platform = (Rigidbody)Get(hammock, "platform");
            ramps = hammock.GetComponentsInChildren<Collider>(true).Where(c => c.name == "Actual gently sloped hammock approach").ToArray();
            truckTap = truckBody.gameObject.AddComponent<WildernessHammockGeometryObservation>();
            deckTap = platform.gameObject.AddComponent<WildernessHammockGeometryObservation>();
            Debug.Log("WILDERNESS_HAMMOCK_GEOMETRY " + output);
        }
        public void Capture(string stage)
        {
            if (stage != "begin" && stage != "original-drive-assertion-failed") return;
            try
            {
                var pairs = ramps.Select(r => Describe(deck, r)).ToList(); pairs.Add(Describe(hull, deck));
                var row = new Row { stage = stage, clock = (float)Get(scope.match, "WildernessSeconds"), real = Time.realtimeSinceStartup,
                    frame = Time.frameCount, hammock = PathOf(hammock.transform), destination = destination,
                    approach = ((Transform)Get(hammock, "approach")).position, deck = Describe(deck), hull = Describe(hull),
                    ramps = ramps.Select(Describe).ToArray(), truckShapes = truckBody.GetComponentsInChildren<Collider>(true).Select(Describe).ToArray(),
                    platformBody = Describe(platform), truckBody = Describe(truckBody), actualPenetrations = pairs.ToArray(),
                    springs = platform.GetComponents<SpringJoint>().Select(s => new Spring { localAnchor = s.anchor, connectedAnchor = s.connectedAnchor,
                        worldAnchor = s.transform.TransformPoint(s.anchor), worldConnectedAnchor = s.connectedBody == null ? s.connectedAnchor : s.connectedBody.transform.TransformPoint(s.connectedAnchor),
                        connectedBody = s.connectedBody == null ? 0 : s.connectedBody.GetInstanceID(),
                        length = Vector3.Distance(s.transform.TransformPoint(s.anchor), s.connectedBody == null ? s.connectedAnchor : s.connectedBody.transform.TransformPoint(s.connectedAnchor)),
                        minDistance = s.minDistance, maxDistance = s.maxDistance, spring = s.spring, damper = s.damper, tolerance = s.tolerance, autoConfigure = s.autoConfigureConnectedAnchor }).ToArray(),
                    truckContacts = truckTap.pairs.Values.SelectMany(v => v).ToArray(), deckContacts = deckTap.pairs.Values.SelectMany(v => v).ToArray() };
                File.AppendAllText(output, JsonUtility.ToJson(row) + "\n", new System.Text.UTF8Encoding(false));
            }
            catch (Exception error) { Debug.LogWarning("Read-only hammock geometry capture failed: " + error); }
        }
        public void Dispose()
        {
            if (truckTap != null) UnityEngine.Object.Destroy(truckTap);
            if (deckTap != null) UnityEngine.Object.Destroy(deckTap);
        }
    }
}
#endif
