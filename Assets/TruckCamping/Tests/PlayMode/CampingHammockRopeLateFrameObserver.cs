#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static WildernessNativeScope;

// Reads AFTER default-order CampHammock.LateUpdate in the SAME rendered frame.
// No manual callback, transform/physics write, sync or simulation freeze.
[DefaultExecutionOrder(30000)]
public sealed class CampingHammockRopeLateFrameObserver : MonoBehaviour
{
    public sealed class Statistics
    {
        public int frames, endpoints, movingFrames;
        public float maxStartError, maxEndError, maxDeckDistance, maxLength, maxAnchorMovement;
        public bool physicsExact = true;
        public string failure = "";
    }
    [Serializable] sealed class Endpoint
    {
        public int hammock, rope, bodyId;
        public Vector3 fixedAnchor, actualStart, springAnchor, actualEnd, platformPosition;
        public Quaternion platformRotation;
        public float startError, endError, deckDistance, length;
    }
    [Serializable] sealed class Frame
    {
        public string phase, status = "ACTUAL_LATEUPDATE_30000_AFTER_ORIGINAL_ROPE_WRITER_0";
        public int frame, endpointCount;
        public float time, real, maxStartError, maxEndError;
        public bool physicsExact;
        public Endpoint[] endpoints;
    }
    readonly Dictionary<string, Statistics> phases = new Dictionary<string, Statistics>();
    Component[] hammocks;
    string[] originalPhysics;
    Func<Component, string> physicsIdentity;
    string phase, output;
    Vector3[] previousAnchors;
    public void Initialize(Component[] hammocks, string[] physics, Func<Component, string> identity, string output)
    {
        this.hammocks = hammocks; originalPhysics = physics; physicsIdentity = identity; this.output = output;
    }
    public void BeginPhase(string name)
    {
        phase = name; phases.Add(name, new Statistics()); previousAnchors = null;
    }
    public Statistics Observed(string name) => phases[name];
    void LateUpdate()
    {
        if (hammocks == null || string.IsNullOrEmpty(phase)) return;
        var stats = phases[phase];
        try
        {
            var values = new List<Endpoint>();
            for (int h = 0; h < hammocks.Length; h++)
            {
                var hammock = hammocks[h]; var body = (Rigidbody)Get(hammock, "platform"); var deck = (BoxCollider)Get(hammock, "deck");
                var springs = body.GetComponents<SpringJoint>(); var anchors = (Transform[])Get(hammock, "anchors"); var ropes = (LineRenderer[])Get(hammock, "ropes");
                if (springs.Length != 4 || anchors.Length != 4 || ropes.Length != 4) throw new InvalidOperationException("Original four spring/anchor/rope arrays required.");
                stats.physicsExact &= physicsIdentity(hammock) == originalPhysics[h];
                for (int i = 0; i < 4; i++)
                {
                    var expected = body.transform.TransformPoint(springs[i].anchor);
                    var start = ropes[i].GetPosition(0); var end = ropes[i].GetPosition(2);
                    values.Add(new Endpoint { hammock = h, rope = i, bodyId = body.GetInstanceID(), fixedAnchor = anchors[i].position,
                        actualStart = start, springAnchor = expected, actualEnd = end, platformPosition = body.position, platformRotation = body.rotation,
                        startError = Vector3.Distance(start, anchors[i].position), endError = Vector3.Distance(end, expected),
                        deckDistance = Vector3.Distance(deck.ClosestPoint(expected), expected), length = Vector3.Distance(start, end) });
                }
            }
            float movement = previousAnchors == null ? 0 : values.Select((v, i) => Vector3.Distance(v.springAnchor, previousAnchors[i])).Max();
            if (movement > 0) stats.movingFrames++;
            stats.maxAnchorMovement = Mathf.Max(stats.maxAnchorMovement, movement); previousAnchors = values.Select(v => v.springAnchor).ToArray();
            stats.frames++; stats.endpoints += values.Count;
            stats.maxStartError = Mathf.Max(stats.maxStartError, values.Max(v => v.startError));
            stats.maxEndError = Mathf.Max(stats.maxEndError, values.Max(v => v.endError));
            stats.maxDeckDistance = Mathf.Max(stats.maxDeckDistance, values.Max(v => v.deckDistance));
            stats.maxLength = Mathf.Max(stats.maxLength, values.Max(v => v.length));
            var row = new Frame { phase = phase, frame = Time.frameCount, time = Time.time, real = Time.realtimeSinceStartup,
                endpointCount = values.Count, maxStartError = stats.maxStartError, maxEndError = stats.maxEndError, physicsExact = stats.physicsExact, endpoints = values.ToArray() };
            File.AppendAllText(output, JsonUtility.ToJson(row) + "\n", new System.Text.UTF8Encoding(false));
        }
        catch (Exception error) { stats.failure = error.ToString(); }
    }
}
#endif
