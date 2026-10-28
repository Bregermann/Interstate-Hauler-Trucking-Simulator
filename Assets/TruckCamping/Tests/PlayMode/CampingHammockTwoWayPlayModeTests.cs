#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using static WildernessNativeScope;

public sealed class CampingHammockTwoWayPlayModeTests
{
    [Serializable] sealed class WheelRow { public string name, surface; public bool grounded, deck, ramp; public float load, radius; }
    [Serializable] sealed class ToeRow { public string ramp, terrain; public Vector3 geometricTop, actualQuery; public bool actualRampHit, actualTerrainHit; public float rampY, terrainY, stepAboveTerrain; }
    [Serializable] sealed class Row
    {
        public string status = "CONTROLLED_ASSIGNED_KEYBOARD_GEOMETRY_NOT_HUMAN_OR_WINDOWS_ACCEPTANCE", phase;
        public float real, clock, speed, deckCenterY, deckTopY, deckBottomY, emptyDeckCenterY, relativeSag;
        public Vector3 truckPosition, localTruck, platformVelocity, destination;
        public Vector2 requested, pulse;
        public int rootId, deckWheelCount, rampWheelCount, groundWheelCount;
        public int[] attachedBodyIds;
        public Vector3[] deckCorners;
        public float[] springLengths;
        public WheelRow[] wheels;
        public ToeRow[] actualOuterToes;
    }
    sealed class Observation
    {
        public WildernessNativeScope scope;
        public Component truck, hammock;
        public Rigidbody body, platform;
        public BoxCollider deck;
        public Collider[] ramps;
        public string output;
        public float emptyY;
        public Vector2 requested, pulse;
        public Row Sample(string phase, Vector3 destination)
        {
            var corners = new Vector3[8];
            for (int i = 0; i < 8; i++) corners[i] = deck.transform.TransformPoint(deck.center + Vector3.Scale(deck.size * .5f,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
            var wheels = Components(Get(truck, "wheels")).Select(w =>
            {
                var hit = (Collider)Get(w, "HitCollider"); bool grounded = (bool)Get(w, "IsGrounded");
                return new WheelRow { name = w.name, surface = hit == null ? "NONE" : hit.name, grounded = grounded,
                    deck = grounded && hit == deck, ramp = grounded && hit != null && ramps.Contains(hit), load = (float)Get(w, "Load"), radius = (float)Get(Get(w, "wheel"), "radius") };
            }).ToArray();
            var toes = new List<ToeRow>();
            foreach(var rampShape in ramps.OfType<BoxCollider>())
            {
                float side = Mathf.Sign(rampShape.transform.localPosition.z);
                foreach(float x in new[]{-1f,-.5f,0,.5f,1f})
                {
                    Vector3 top = rampShape.transform.TransformPoint(rampShape.center + Vector3.Scale(rampShape.size * .5f, new Vector3(x,1,side)));
                    // Stay .01m inside the box edge; exact boundary ray queries
                    // are numerically ambiguous and are not clearance proof.
                    Vector3 query = Vector3.Lerp(top, rampShape.transform.TransformPoint(rampShape.center + Vector3.Scale(rampShape.size * .5f,new Vector3(0,1,0))), .003f);
                    var ray = new Ray(query + Vector3.up * 10, Vector3.down);
                    bool rampHit = rampShape.Raycast(ray,out var rh,20);
                    var terrainHits = Physics.RaycastAll(ray,20,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(h=>h.collider is TerrainCollider).OrderBy(h=>h.distance).ToArray();
                    bool terrainHit = terrainHits.Length>0;
                    toes.Add(new ToeRow { ramp=rampShape.name, terrain=terrainHit?terrainHits[0].collider.name:"NONE", geometricTop=top, actualQuery=query,
                        actualRampHit=rampHit, actualTerrainHit=terrainHit, rampY=rampHit?rh.point.y:0, terrainY=terrainHit?terrainHits[0].point.y:0,
                        stepAboveTerrain=rampHit&&terrainHit?rh.point.y-terrainHits[0].point.y:0 });
                }
            }
            var row = new Row { phase = phase, real = Time.realtimeSinceStartup, clock = (float)Get(scope.match, "WildernessSeconds"),
                speed = body.linearVelocity.magnitude, deckCenterY = platform.worldCenterOfMass.y, deckTopY = corners.Max(v => v.y), deckBottomY = corners.Min(v => v.y),
                emptyDeckCenterY = emptyY, relativeSag = emptyY - platform.worldCenterOfMass.y, truckPosition = body.position,
                localTruck = hammock.transform.InverseTransformPoint(body.position), platformVelocity = platform.linearVelocity, destination = destination,
                requested = requested, pulse = pulse, rootId = body.GetInstanceID(), attachedBodyIds = truck.GetComponentsInChildren<Rigidbody>(true).Select(b => b.GetInstanceID()).OrderBy(i => i).ToArray(),
                deckWheelCount = wheels.Count(w => w.deck), rampWheelCount = wheels.Count(w => w.ramp), groundWheelCount = wheels.Count(w => w.grounded && !w.deck && !w.ramp),
                deckCorners = corners, wheels = wheels, actualOuterToes = toes.ToArray(),
                springLengths = platform.GetComponents<SpringJoint>().Select(s => Vector3.Distance(s.transform.TransformPoint(s.anchor), s.connectedBody == null ? s.connectedAnchor : s.connectedBody.transform.TransformPoint(s.connectedAnchor))).ToArray() };
            File.AppendAllText(output, JsonUtility.ToJson(row) + "\n", new System.Text.UTF8Encoding(false)); return row;
        }
        public int DeckWheels() => Components(Get(truck, "wheels")).Count(w => (bool)Get(w, "IsGrounded") && (Collider)Get(w, "HitCollider") == deck);
        public int RampWheels() => Components(Get(truck, "wheels")).Count(w => (bool)Get(w, "IsGrounded") && (Collider)Get(w, "HitCollider") != null && ramps.Contains((Collider)Get(w, "HitCollider")));
        public int GroundWheels() => Components(Get(truck, "wheels")).Count(w =>
        {
            var hit = (Collider)Get(w, "HitCollider");
            return (bool)Get(w, "IsGrounded") && hit != null && hit != deck && !ramps.Contains(hit);
        });
    }
    static Vector3 At(Component hammock, float localZ, Component world)
    {
        var point = hammock.transform.TransformPoint(new Vector3(0, 0, localZ)); point.y = (float)Call(world, "Height", point); return point;
    }
    // This test driver presses only the participant's real configured keys.
    // Direct aiming isolates the physical seam from NavMesh arrival gates. It
    // changes no input/controller settings, rigidbody pose, force or velocity.
    static IEnumerator Drive(Observation o, Vector3 goal, Func<bool> actualReached, string phase, float realLimit,
        WildernessHammockGeometryObservation.CaptureScope geometry)
    {
        float end = Time.realtimeSinceStartup + realLimit, next = 0;
        var player = ((Array)Get(Get(o.scope.match, "setup"), "players")).GetValue(0);
        while (!actualReached() && Time.realtimeSinceStartup < end)
        {
            var delta = Vector3.ProjectOnPlane(goal - o.body.position, Vector3.up);
            var local = Quaternion.Inverse(o.body.rotation) * delta;
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float desired = delta.magnitude < .7f ? 0 : delta.magnitude < 25 || Mathf.Abs(angle) > 35 ? 1.4f : 3;
            float speed = Vector3.Dot(o.body.linearVelocity, o.truck.transform.forward);
            o.requested = new Vector2(Mathf.Clamp(angle / 28, -1, 1), Mathf.Clamp((desired - speed) * .35f, -.6f, .7f));
            o.pulse = WildernessDriveObservation.KeyboardPulse(o.requested, Time.time / .2f);
            var keys = new List<Key>();
            if (o.pulse.x > 0) keys.Add((Key)Get(player, "right")); else if (o.pulse.x < 0) keys.Add((Key)Get(player, "left"));
            if (o.pulse.y > 0) keys.Add((Key)Get(player, "forward")); else if (o.pulse.y < 0) keys.Add((Key)Get(player, "back"));
            o.scope.Keys(keys.ToArray());
            if (Time.realtimeSinceStartup >= next) { next = Time.realtimeSinceStartup + .25f; o.Sample(phase, goal); }
            yield return null;
        }
        o.scope.Keys(); var final = o.Sample(phase + "-stop-or-deadline", goal);
        if (!actualReached()) geometry.Capture("original-drive-assertion-failed");
        Assert.That(actualReached(), Is.True, phase + " must satisfy real wheel/physical-envelope conditions; no nine-metre navigation arrival substitute. Last row " + JsonUtility.ToJson(final));
    }
    [UnityTest, Timeout(230000)]
    public IEnumerator AssignedKeyboardPhysicallyEntersLoadedSpringDeckAndExitsBothSidesWithoutPoseWrites()
    {
        using (var s = new WildernessNativeScope("camp-hammock-two-way"))
        {
            yield return s.Load("TruckCamping", "LWS.TruckCamping.CampingMatch"); s.Configure(1);
            Assert.That((bool)Call(s.match, "StartMatch"), Is.True); Time.timeScale = 3; yield return s.Advance(3);
            var explorer = Components(Get(s.match, "explorers"))[0]; var truck = (Component)Get(explorer, "truck"); var world = (Component)Get(s.match, "world");
            var body = (Rigidbody)Get(truck, "body"); var hammock = Components(Get(world, "hammocks")).OrderBy(h => (h.transform.position - body.position).sqrMagnitude).First();
            var deck = (BoxCollider)Get(hammock, "deck"); var platform = (Rigidbody)Get(hammock, "platform");
            var ramps = hammock.GetComponentsInChildren<Collider>(true).Where(c => c.name == "Actual gently sloped hammock approach").ToArray();
            Assert.That(ramps.Length, Is.EqualTo(2)); Assert.That(platform.mass, Is.EqualTo(35000)); Assert.That(platform.isKinematic, Is.False);
            string identity = PhysicsIdentity(truck), id = Guid.NewGuid().ToString("N");
            string output = Path.Combine(Path.GetDirectoryName(s.progress), "two-way-" + id + ".jsonl");
            var o = new Observation { scope = s, truck = truck, body = body, hammock = hammock, deck = deck, platform = platform, ramps = ramps, output = output, emptyY = platform.worldCenterOfMass.y };
            float outer = ramps.SelectMany(r => new[] { r.bounds.min, r.bounds.max }).Max(v => Mathf.Abs(hammock.transform.InverseTransformPoint(v).z));
            Assert.That(outer, Is.InRange(13, 32.1f), "Actual approved extended ramp footprint, not the superseded six-metre approach.");
            foreach(var ramp in ramps)Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,ramp.transform.localEulerAngles.x)),Is.LessThanOrEqualTo(12),"Actual physical approach retains the twelve-degree ceiling.");
            Vector3 near = At(hammock, outer + 20, world), far = At(hammock, -outer - 20, world);
            o.Sample("unloaded-after-original-start", near);
            Debug.Log("WILDERNESS_HAMMOCK_TWO_WAY " + output);
            yield return WildernessDriveObservation.Run(s, explorer, near, 45, "two-way-ground-runup");
            using (var geometry = new WildernessHammockGeometryObservation.CaptureScope(s, explorer, ((Transform)Get(hammock, "approach")).position,
                Path.Combine(Path.GetDirectoryName(s.progress), "two-way-geometry-" + id + ".jsonl")))
            {
                geometry.Capture("begin");
                for (int traversal = 0; traversal < 2; traversal++)
                {
                    int exitSide = traversal == 0 ? -1 : 1;
                    float firstContactZ = float.NaN;
                    Vector3 center = ((Transform)Get(hammock, "approach")).position;
                    yield return Drive(o, center, () =>
                    {
                        var local = hammock.transform.InverseTransformPoint(body.position);
                        if (float.IsNaN(firstContactZ) && o.DeckWheels() > 0) { firstContactZ = local.z; o.Sample("first-native-deck-contact-" + traversal, center); }
                        return Mathf.Abs(local.x) < 1.5f && Mathf.Abs(local.z) < 2 && o.DeckWheels() >= 2 && (bool)Call(hammock, "Supports", truck);
                    }, "entry-" + (traversal == 0 ? "positive-z" : "negative-z"), 40, geometry);
                    Assert.That(firstContactZ * -exitSide, Is.GreaterThan(4), "Actual first deck wheel contact must occur from the intended end, not a lateral or same-end shortcut.");
                    var loaded = o.Sample("loaded-entry-" + traversal, center);
                    Assert.That(loaded.deckWheelCount, Is.GreaterThanOrEqualTo(2)); Assert.That(loaded.deckTopY, Is.Not.NaN);
                    yield return s.Advance(2); o.Sample("loaded-held-two-sim-seconds-" + traversal, center);
                    Assert.That((bool)Call(hammock, "Supports", truck), Is.True, "Original two-wheel low-speed hammock rule must remain true under the actual loaded deck.");
                    Vector3 exit = exitSide < 0 ? far : near;
                    yield return Drive(o, exit, () =>
                    {
                        float localZ = hammock.transform.InverseTransformPoint(body.position).z;
                        return exitSide * localZ > outer + 4 && o.DeckWheels() == 0 && o.RampWheels() == 0 && o.GroundWheels() >= 2 && body.linearVelocity.magnitude < 1.2f;
                    }, "exit-" + (exitSide < 0 ? "negative-z" : "positive-z"), 35, geometry);
                    yield return s.Advance(2); o.Sample("unloaded-after-exit-" + traversal, exit);
                    Assert.That(PhysicsIdentity(truck), Is.EqualTo(identity), "Same original body/controllers through both real traversals.");
                    Assert.That(platform.mass, Is.EqualTo(35000)); Assert.That(platform.GetComponents<SpringJoint>().Length, Is.EqualTo(4));
                }
                geometry.Capture("begin");
            }
            s.Capture("camp-hammock-two-way-returned-ground", 1920, 1080);
            TestContext.WriteLine("Controlled real assigned keyboard: original spawn only, both-side physical entry, loaded original two-wheel rest, ordinary exit beyond ramp/chassis envelope. Actual deck/spring sag sampled; no human, hardware or Windows claim.");
        }
    }
}
#endif
