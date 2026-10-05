using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiCompanionLifecycle")]
    public sealed class TruckTaxiCompanionLifecyclePlayModeTests
    {
        private readonly List<Object> owned = new List<Object>();
        private TruckTaxiBootstrap host;
        private TruckTaxiRoadsideCompanionLoop loop;
        private TruckTaxiCustomContentRunner runner;
        private TruckTaxiPassengerActor actor;
        private TruckTaxiCustomInteractionProfile profile;
        private Renderer originallyHidden;
        private GameObject truck;
        private int started, completed, cancelled, beats;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static void Field(object owner, string name, object value) => owner.GetType().GetField(name, Flags).SetValue(owner, value);
        private static void Property(object owner, string name, object value) => owner.GetType().GetProperty(name, Flags).SetValue(owner, value);
        private static void Phase(TruckTaxiRoadsideCompanionLoop value, string name) =>
            Field(value, "phase", Enum.Parse(typeof(TruckTaxiRoadsideCompanionLoop).GetNestedType("Phase", BindingFlags.NonPublic), name));
        private T Keep<T>(T value) where T : Object { owned.Add(value); return value; }
        private GameObject Box(string name, Vector3 position, Vector3 scale)
        {
            var value = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube)); value.name = name;
            value.transform.position = position; value.transform.localScale = scale; return value;
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Inactive bootstrap prevents its scene startup. All configuration below is transient.
            var owner = Keep(new GameObject("companion test bootstrap")); owner.SetActive(false);
            host = owner.AddComponent<TruckTaxiBootstrap>(); host.enabled = false;
            var persistence = owner.AddComponent<TruckTaxiVibinPersistence>(); persistence.enabled = false;
            // Readiness-only persistence fixture; no save registry or disk operations are started.
            Field(persistence, "participant", new TruckTaxiVibinSaveParticipant(null, null, null));
            Field(persistence, "profileReady", true); Property(persistence, "RunProfileId", "test.lifecycle.run");
            Property(host, "Persistence", persistence); Property(host, "Ready", true); Field(host, "startupPending", false);
            Assert.IsTrue(host.CanResume);
            host.configuration = Keep(ScriptableObject.CreateInstance<TruckTaxiConfiguration>());
            var session = new TruckTaxiSession(host.configuration, Array.Empty<TruckTaxiRideLocation>(), 27);
            Property(session, "State", TruckTaxiState.Available); Property(host, "Session", session);
            Property(host, "GPS", owner.AddComponent<TruckTaxiGPSAdapter>());
            truck = Box("fixture truck", Vector3.up, new Vector3(3, 2, 7));
            var body = truck.AddComponent<Rigidbody>(); body.isKinematic = true;
            var player = truck.AddComponent<LwsPlayerTruck>(); Property(host, "Player", player);
            foreach (var behaviour in truck.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            var controls = truck.AddComponent<LwsTruckControlController>(); controls.enabled = false;
            Field(controls, "_state", new LwsTruckControlState { parkingBrakeOn = true, hornActive = true });
            foreach (var behaviour in truck.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            var rig = truck.AddComponent<TruckTaxiCabOccupantRig>();
            rig.Configure(false, new[] { new TruckTaxiCabOccupantRig.Anchor {
                id = TruckTaxiCabAnchorId.SleeperRight, mountPath = "", localPosition = new Vector3(0, .5f, -.3f),
                secondaryOccupantSeat = true, allowedOccupants = TruckTaxiCabOccupantKind.Companion } });
            var driver = truck.AddComponent<TruckTaxiDriverPresentation>(); driver.enabled = false;
            var driverVisual = Box("driver visual", new Vector3(-1, 1, 0), Vector3.one * .3f);
            driverVisual.transform.SetParent(truck.transform, true); Object.Destroy(driverVisual.GetComponent<Collider>());
            Field(driver, "driver", driverVisual);
            var ground = Box("walkable floor", new Vector3(0, -.1f, 0), new Vector3(100, .2f, 100));
            ground.AddComponent<TruckTaxiSurface>();
            var needs = owner.AddComponent<TruckTaxiDriverNeedsCoordinator>();
            Property(needs, "State", new TruckTaxiDriverNeedsState(Keep(ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>()), 27));
            Property(host, "DriverNeeds", needs);
            var runnerObject = Keep(new GameObject("neutral runner"));
            runner = runnerObject.AddComponent<TruckTaxiCustomContentRunner>(); runner.Initialize(host);
            Property(host, "CustomContent", runner);
            runner.interactionStarted.AddListener(_ => started++);
            runner.interactionCompleted.AddListener(_ => completed++);
            runner.interactionCancelled.AddListener(_ => cancelled++);
            runner.interactionBeat.AddListener(_ => beats++);
            var loopObject = Keep(new GameObject("companion lifecycle"));
            loop = loopObject.AddComponent<TruckTaxiRoadsideCompanionLoop>();
            loop.enabled = false; // The fixture advances only the lifecycle steps under test.
            Property(host, "Companions", loop);
            var motion = loopObject.AddComponent<TruckTaxiPrivateEventVehicleMotion>(); motion.Initialize(truck.transform);
            Field(loop, "host", host); Field(loop, "truckBody", body); Field(loop, "controls", controls);
            Field(loop, "cabRig", rig); Field(loop, "motion", motion);
            Field(loop, "nextPopulationRefresh", float.PositiveInfinity);
            Field(loop, "nextMaterialization", float.PositiveInfinity);
            loop.RegisterBlock("fixture-block", new Bounds(Vector3.zero, Vector3.one * 40));
            var companions = (IList)typeof(TruckTaxiRoadsideCompanionLoop).GetField("companions", Flags).GetValue(loop);
            var companion = companions[0];
            var actorObject = Box("companion visual", new Vector3(5, 1, 0), Vector3.one * .5f);
            var hiddenPart = Box("originally hidden detail", Vector3.zero, Vector3.one * .1f);
            hiddenPart.transform.SetParent(actorObject.transform, false);
            Object.Destroy(hiddenPart.GetComponent<Collider>());
            originallyHidden = hiddenPart.GetComponent<Renderer>(); originallyHidden.enabled = false;
            actor = actorObject.AddComponent<TruckTaxiPassengerActor>(); actor.Bind(null);
            Assert.IsTrue(rig.TrySeatSecondary(actor.transform, TruckTaxiSeatType.CompanionPassenger, actor.StandingWorldScale));
            actor.CompleteSeating();
            var stop = Keep(new GameObject("fixture stop")).AddComponent<TruckTaxiStopObjectivePoint>();
            stop.stableId = "fixture-stop"; stop.radius = 30;
            Field(companion, "actor", actorObject); Field(companion, "destination", stop);
            Field(loop, "active", companion); Phase(loop, "ToPrivateStop");
            var cube = Box("harmless presentation cube", Vector3.one * 10000, Vector3.one * .2f);
            Object.Destroy(cube.GetComponent<Collider>());
            var presentation = Keep(ScriptableObject.CreateInstance<TruckTaxiCustomPresentationProfile>());
            presentation.stableId = "test.cube"; presentation.presentationPrefab = cube;
            presentation.duration = .15f; presentation.lockInput = false;
            var wave = Keep(new AnimationClip { name = "test harmless wave", legacy = false });
            wave.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(
                new Keyframe(0, 0), new Keyframe(.075f, 15), new Keyframe(.15f, 0)));
            presentation.passengerAnimation = wave;
            var beep = Keep(AudioClip.Create("test beep", 2205, 1, 44100, false));
            var samples = new float[2205];
            for (int i = 0; i < samples.Length; i++) samples[i] = .04f * Mathf.Sin(i * 2 * Mathf.PI * 440 / 44100);
            beep.SetData(samples, 0);
            var audio = Keep(ScriptableObject.CreateInstance<TruckTaxiCustomReactionAudioProfile>());
            audio.stableId = "test.beep"; audio.startClips = new[] { beep }; audio.completionClips = new[] { beep };
            profile = Keep(ScriptableObject.CreateInstance<TruckTaxiCustomInteractionProfile>());
            profile.stableId = "test.neutral-companion";
            profile.startPresentation = profile.loopPresentation = profile.completionPresentation = presentation;
            profile.startAudioProfile = profile.completionAudioProfile = audio;
            host.configuration.customCompanionInteraction = profile;
            Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (loop != null) loop.Cancel();
            if (runner != null) runner.Cancel();
            if (host?.Persistence != null) Field(host.Persistence, "participant", null);
            if (host?.DriverNeeds != null) Property(host.DriverNeeds, "State", null);
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.Destroy(owned[i]);
            owned.Clear();
            started = completed = cancelled = beats = 0;
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealCompanionEventPlaysNeutralProfileAndPreservesAsyncCompletion()
        {
            Assert.IsTrue(loop.Interact());
            Assert.AreEqual(1, started);
            Assert.IsTrue(runner.IsPlaying);
            Assert.IsTrue(runner.UsesEventInputHold);
            Assert.IsFalse(runner.OwnsInputHold);
            Assert.Greater(runner.ClipsPlayed, 0);
            Assert.Greater(runner.PresentationsPlayed, 0);
            Assert.IsFalse(actor.GetComponent<Renderer>().enabled, "Proxy playback must hide the source renderer.");
            Assert.IsFalse(originallyHidden.enabled);
            Assert.IsNotNull(truck.transform.Find("companion visual Custom Presentation Proxy"));
            loop.Tick(1.1f); Assert.GreaterOrEqual(beats, 1);
            loop.Tick(7f);
            Assert.AreEqual(1, completed);
            Assert.AreEqual(0, cancelled);
            Assert.IsTrue(runner.IsPlaying, "Finish must not cancel the completion phase.");
            Assert.IsTrue(actor.transform.IsChildOf(truck.transform), "Keep source seated until proxy/audio cleanup.");
            Assert.IsFalse(actor.GetComponent<Renderer>().enabled, "Completion proxy remains active until its end phase finishes.");
            float deadline = Time.realtimeSinceStartup + 3;
            while (runner.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(runner.IsPlaying);
            loop.Tick(.01f);
            Assert.IsFalse(loop.IsBusy);
            Assert.IsNull(actor.transform.parent);
            Assert.IsTrue(actor.gameObject.activeSelf);
            Assert.IsTrue(actor.GetComponent<Renderer>().enabled);
            Assert.IsFalse(originallyHidden.enabled, "Never enable a renderer that began disabled.");
            Assert.AreEqual(0, cancelled);
        }

        [UnityTest]
        public IEnumerator StandalonePreviewOwnsInputHoldWithoutCancellingAuxiliaryRide()
        {
            Phase(loop, "AuxiliaryRide");
            var preview = Keep(ScriptableObject.CreateInstance<TruckTaxiCustomPresentationProfile>());
            preview.stableId = "test.neutral-locked-preview";
            preview.duration = .1f;
            preview.lockInput = true;
            preview.presentationPrefab = profile.startPresentation.presentationPrefab;
            Assert.IsTrue(loop.IsBusy);
            Assert.IsFalse(host.Paused);

            runner.Preview(preview, null);
            Assert.IsTrue(runner.IsPlaying);
            Assert.IsTrue(runner.OwnsInputHold);
            Assert.IsFalse(runner.UsesEventInputHold);
            Assert.IsTrue(host.Paused);

            runner.Cancel();
            Assert.IsFalse(runner.IsPlaying);
            Assert.IsFalse(runner.OwnsInputHold);
            Assert.IsFalse(runner.UsesEventInputHold);
            Assert.IsFalse(host.Paused);
            Assert.IsTrue(loop.IsBusy);
            Assert.IsTrue(loop.HasOnboardCompanion);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConfigurationVariantOverrideStartsActualEventThenFallsBackToGlobal()
        {
            var companions = (IList)typeof(TruckTaxiRoadsideCompanionLoop).GetField("companions", Flags).GetValue(loop);
            var companion = companions[0];
            Field(companion, "variant", 0);
            loop.variantInteractionProfiles = Array.Empty<TruckTaxiCustomInteractionProfile>();
            var variantProfile = Keep(ScriptableObject.CreateInstance<TruckTaxiCustomInteractionProfile>());
            variantProfile.stableId = "test.neutral-companion.variant-zero";
            variantProfile.startPresentation = profile.startPresentation;
            variantProfile.startAudioProfile = profile.startAudioProfile;
            host.configuration.customCompanionVariantInteractions = new[] { variantProfile };
            var startedIds = new List<string>();
            runner.interactionStarted.AddListener(id => startedIds.Add(id));

            Assert.AreNotEqual(profile.stableId, variantProfile.stableId);
            Assert.IsTrue(loop.Interact());
            CollectionAssert.AreEqual(new[] { variantProfile.stableId }, startedIds);
            Assert.IsTrue(runner.IsPlaying);
            loop.Cancel();
            Assert.IsFalse(runner.IsPlaying);
            yield return null;

            host.configuration.customCompanionVariantInteractions = Array.Empty<TruckTaxiCustomInteractionProfile>();
            actor.PrepareForSeat();
            Assert.IsTrue(truck.GetComponent<TruckTaxiCabOccupantRig>().TrySeatSecondary(
                actor.transform, TruckTaxiSeatType.CompanionPassenger, actor.StandingWorldScale));
            actor.CompleteSeating();
            Field(loop, "active", companion); Phase(loop, "ToPrivateStop");
            Assert.AreSame(profile, host.configuration.customCompanionInteraction);
            Assert.IsTrue(loop.Interact());
            CollectionAssert.AreEqual(new[] { variantProfile.stableId, profile.stableId }, startedIds);
            Assert.AreEqual(2, started);
            Assert.IsTrue(runner.IsPlaying);
            loop.Cancel();
            Assert.IsFalse(runner.IsPlaying);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CancelRestoresVisibleWorldActorAndStopsNeutralPlayback()
        {
            Assert.IsTrue(loop.Interact());
            Assert.IsFalse(actor.GetComponent<Renderer>().enabled);
            loop.Cancel();
            Assert.AreEqual(1, cancelled);
            Assert.AreEqual(0, completed);
            Assert.IsFalse(runner.IsPlaying);
            Assert.IsFalse(loop.IsBusy);
            Assert.IsNull(actor.transform.parent);
            Assert.AreEqual(actor.StandingWorldScale, actor.transform.localScale);
            Assert.IsTrue(actor.gameObject.activeSelf);
            Assert.IsTrue(actor.GetComponent<Renderer>().enabled);
            Assert.IsFalse(originallyHidden.enabled);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NoAssignedProfileRetainsVisibleFallbackParticipant()
        {
            host.configuration.customCompanionInteraction = null;
            Assert.IsTrue(loop.Interact());
            Assert.AreEqual(0, started);
            Assert.IsTrue(actor.gameObject.activeInHierarchy);
            Assert.IsTrue(loop.HasOnboardCompanion);
            loop.Cancel();
            Assert.IsTrue(actor.GetComponent<Renderer>().enabled);
            Assert.IsFalse(originallyHidden.enabled);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CubeOnlyProfileKeepsParticipantVisibleAndHonorsRendererOnlyRocking()
        {
            profile.startPresentation.passengerAnimation = null;
            profile.startPresentation.useTruckRocking = true;
            Vector3 position = truck.transform.position;
            var body = truck.GetComponent<Rigidbody>();
            var collider = truck.GetComponent<Collider>();
            bool enabled = collider.enabled; bool kinematic = body.isKinematic;
            Assert.IsTrue(loop.Interact());
            Assert.IsTrue(actor.GetComponent<Renderer>().enabled, "Truck rocking must not snapshot the participant.");
            Assert.IsTrue(loop.GetComponent<TruckTaxiPrivateEventVehicleMotion>().IsActive);
            Assert.AreEqual(runner.FadeAlpha, loop.FadeAlpha);
            Assert.AreEqual(position, truck.transform.position);
            Assert.AreEqual(enabled, collider.enabled);
            Assert.AreEqual(kinematic, body.isKinematic);
            loop.Cancel();
            Assert.IsFalse(loop.GetComponent<TruckTaxiPrivateEventVehicleMotion>().IsActive);
            Assert.IsTrue(actor.GetComponent<Renderer>().enabled);
            Assert.IsFalse(originallyHidden.enabled);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NearbyServiceMaterializesItsMinimumWithoutAllMapActors()
        {
            loop.RegisterService("nearby-service", new Vector3(0, 0, 25), 8);
            loop.RegisterService("unloaded-service", new Vector3(1000, 0, 0), 18, true);
            loop.SetRegionAvailability(point => point.x < 500);
            Field(loop, "nextMaterialization", 0f);
            typeof(TruckTaxiRoadsideCompanionLoop).GetMethod("MaterializeNearby", Flags).Invoke(loop, null);
            Assert.AreEqual(10, loop.LogicalPopulationCount);
            Assert.AreEqual(4, loop.MaterializedCount, "One seated companion plus three available service companions.");
            var companions = (IList)typeof(TruckTaxiRoadsideCompanionLoop).GetField("companions", Flags).GetValue(loop);
            var positions = new List<Vector3>();
            for (int i = 1; i <= 3; i++)
            {
                var go = (GameObject)companions[i].GetType().GetField("actor").GetValue(companions[i]);
                Assert.IsNotNull(go);
                Assert.Greater(Vector3.Distance(go.transform.position, new Vector3(0, 0, 25)), 9);
                foreach (var position in positions) Assert.GreaterOrEqual(Vector3.Distance(go.transform.position, position), 3);
                positions.Add(go.transform.position);
            }
            yield return null;
        }

        private object PrepareWaitingCompanion()
        {
            var companions = (IList)typeof(TruckTaxiRoadsideCompanionLoop).GetField("companions", Flags).GetValue(loop);
            var companion = companions[0];
            actor.PrepareForWorld(truck.transform);
            Assert.IsTrue(actor.ShowInWorld(Vector3.right * 5, truck.transform));
            Field(companion, "destination", null);
            Field(loop, "active", null); Field(loop, "nearby", companion); Phase(loop, "Waiting");
            return companion;
        }

        [UnityTest]
        public IEnumerator FareOnboardCanPickUpSecondaryWithoutReplacingFareOrNavigation()
        {
            PrepareWaitingCompanion();
            var fare = Keep(ScriptableObject.CreateInstance<PassengerProfile>()); fare.passengerId = "test.fare";
            var destination = Keep(new GameObject("fare destination")).AddComponent<TruckTaxiRideLocation>();
            destination.locationId = "test.fare.destination"; destination.transform.position = Vector3.forward * 40;
            Property(host.Session, "Passenger", fare); Property(host.Session, "Destination", destination);
            Property(host.Session, "State", TruckTaxiState.DrivingToDestination);
            host.GPS.SetRideDestination(destination);
            long lease = host.GPS.ActiveLeaseToken;
            var fareVisual = Box("primary fare", new Vector3(1.2f, 2, 0), Vector3.one * .5f);
            fareVisual.AddComponent<TruckTaxiPassengerActor>().Bind(fare);
            fareVisual.transform.SetParent(truck.transform, true);
            Vector3 farePosition = fareVisual.transform.position;
            Assert.IsTrue(loop.CanInteract);
            Assert.IsTrue(loop.TryHornPickup());
            float deadline = Time.realtimeSinceStartup + 6;
            while (!loop.HasOnboardCompanion && loop.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(loop.HasOnboardCompanion);
            Assert.AreSame(fare, host.Session.Passenger);
            Assert.AreSame(destination, host.Session.Destination);
            Assert.AreEqual(destination.locationId, host.GPS.TargetId);
            Assert.AreEqual(lease, host.GPS.ActiveLeaseToken);
            Assert.IsFalse(host.Session.OffersSuppressed);
            Assert.AreEqual(farePosition, fareVisual.transform.position);
            Assert.AreNotEqual(farePosition, actor.transform.position);
            loop.Cancel();
            Assert.AreEqual(lease, host.GPS.ActiveLeaseToken);
        }

        [UnityTest]
        public IEnumerator MissingSecondaryCapabilityReportsNoRoom()
        {
            PrepareWaitingCompanion();
            truck.GetComponent<TruckTaxiCabOccupantRig>().Configure(false, Array.Empty<TruckTaxiCabOccupantRig.Anchor>());
            Property(host.Session, "State", TruckTaxiState.DrivingToDestination);
            Assert.AreEqual("NO ROOM FOR ANOTHER PASSENGER", loop.Prompt);
            Assert.IsFalse(loop.TryHornPickup());
            Assert.AreEqual("NO ROOM FOR ANOTHER PASSENGER", loop.Feedback);
            Assert.IsFalse(loop.IsBusy);
            Assert.IsNull(actor.transform.parent);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PassengerTimelineRunsThroughCompanionEventAndCleansItsProxy()
        {
            var timeline = Keep(ScriptableObject.CreateInstance<TimelineAsset>());
            var track = Keep(timeline.CreateTrack<AnimationTrack>("Passenger"));
            var clip = track.CreateClip(profile.startPresentation.passengerAnimation);
            Keep(clip.asset);
            clip.start = 0; clip.duration = .15;
            profile.startPresentation.timeline = timeline;
            profile.startPresentation.passengerAnimation = null;
            Vector3 position = truck.transform.position;
            Quaternion rotation = truck.transform.rotation;
            var body = truck.GetComponent<Rigidbody>();
            var collider = truck.GetComponent<Collider>();
            var constraints = body.constraints;
            bool kinematic = body.isKinematic, collisionEnabled = collider.enabled;

            Assert.IsTrue(loop.Interact());
            var director = truck.GetComponentInChildren<PlayableDirector>();
            Assert.IsNotNull(director);
            Assert.AreSame(timeline, director.playableAsset);
            Assert.IsTrue(director.playableGraph.IsValid());
            Assert.Greater(director.playableGraph.GetOutputCount(), 0);
            var proxy = truck.transform.Find("companion visual Custom Presentation Proxy");
            Assert.IsNotNull(proxy);
            Assert.AreSame(proxy.GetComponentInChildren<Animator>(), director.GetGenericBinding(track));
            Assert.IsEmpty(proxy.GetComponentsInChildren<Rigidbody>(true));
            Assert.IsEmpty(proxy.GetComponentsInChildren<Collider>(true));
            director.time = 0;
            director.Evaluate();
            Quaternion restRotation = proxy.localRotation;
            director.time = .075;
            director.Evaluate();
            Assert.Greater(Quaternion.Angle(restRotation, proxy.localRotation), 1f,
                "The Passenger track must evaluate the harmless wave on the proxy.");
            Assert.IsFalse(actor.GetComponent<Renderer>().enabled);
            Assert.AreEqual(position, truck.transform.position);
            Assert.AreEqual(rotation, truck.transform.rotation);
            Assert.AreEqual(constraints, body.constraints);
            Assert.AreEqual(kinematic, body.isKinematic);
            Assert.AreEqual(collisionEnabled, collider.enabled);

            loop.Tick(8f);
            Assert.AreEqual(1, completed);
            Assert.IsTrue(runner.IsPlaying);
            float deadline = Time.realtimeSinceStartup + 3;
            while (runner.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(runner.IsPlaying);
            loop.Tick(.01f);
            yield return null;
            Assert.IsEmpty(truck.GetComponentsInChildren<PlayableDirector>(true));
            Assert.IsNull(truck.transform.Find("companion visual Custom Presentation Proxy"));
            Assert.IsTrue(actor.GetComponent<Renderer>().enabled);
            Assert.IsFalse(originallyHidden.enabled);
            Assert.AreEqual(0, cancelled);
            Assert.AreEqual(position, truck.transform.position);
            Assert.AreEqual(rotation, truck.transform.rotation);
        }
    }
}
