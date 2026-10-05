using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiCustomContentRunner : MonoBehaviour
    {
        public UnityEvent<string> interactionStarted = new UnityEvent<string>();
        public UnityEvent<string> interactionBeat = new UnityEvent<string>();
        public UnityEvent<string> interactionCompleted = new UnityEvent<string>();
        public UnityEvent<string> interactionCancelled = new UnityEvent<string>();
        public event Action<string, TruckTaxiCustomPhase> PhaseChanged;
        public bool IsPlaying { get; private set; }
        public string Status { get; private set; } = "No custom content assigned.";
        public float FadeAlpha { get; private set; }
        public int ClipsPlayed { get; private set; }
        public int PresentationsPlayed { get; private set; }
        public bool RequestsTruckRocking => presentation != null && presentation.useTruckRocking;
        public TruckTaxiCustomPhase CurrentPhase => phase;
        public bool OwnsInputHold => ownsPause;
        public bool UsesEventInputHold => IsPlaying && externalInputHold;
        private TruckTaxiBootstrap host;
        private TruckTaxiCustomInteractionProfile profile;
        private TruckTaxiCustomPresentationProfile presentation;
        private TruckTaxiCustomReactionAudioProfile audioProfile;
        private TruckTaxiCustomPhase phase;
        private Transform driver, passenger;
        private TruckTaxiCustomVisualProxy driverProxy, passengerProxy;
        private GameObject prefab;
        private PlayableDirector director;
        private AudioSource voice, accent;
        private float phaseTime, nextAudio;
        private bool ownsPause, wasPaused;
        private bool externalInputHold;
        private int revision;
        private TruckTaxiCustomInteractionProfile temporaryProfile;
        private float standaloneEndAt;
        private readonly HashSet<(string eventId, string itemId)> registeredActions = new HashSet<(string, string)>();
        private readonly Dictionary<AudioClip[], Queue<AudioClip>> bags = new Dictionary<AudioClip[], Queue<AudioClip>>();
        private readonly Dictionary<AudioClip[], AudioClip> previous = new Dictionary<AudioClip[], AudioClip>();
        private readonly Dictionary<TruckTaxiCustomInteractionProfile, TruckTaxiCustomPresentationProfile> previousVariants = new Dictionary<TruckTaxiCustomInteractionProfile, TruckTaxiCustomPresentationProfile>();
        public void Initialize(TruckTaxiBootstrap owner)
        {
            host = owner;
            if (voice == null) voice = CreateVoice("Custom reaction voice");
            if (accent == null) accent = CreateVoice("Custom reaction accent");
            if (host?.Session != null) host.Session.CustomObjectiveTargetLimit = ObjectiveTargetLimit;
        }
        public void RefreshObjectiveCapabilities()
        {
            if (host?.Session == null) return;
            var session = host.Session;
            foreach (var pair in registeredActions) session.Capabilities.RegisterCustomItemAction(pair.eventId, pair.itemId, false);
            registeredActions.Clear();
            foreach (var definition in session.Passenger?.customItemInteractions ?? Array.Empty<TruckTaxiCustomItemInteractionDefinition>())
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.customEventId) || session.CustomInteractionBlockReason(definition, host.DriverNeeds?.State) != null) continue;
                var pair = (definition.customEventId, definition.requiredItemStableId);
                registeredActions.Add(pair);
                session.Capabilities.RegisterCustomItemAction(pair.customEventId, pair.requiredItemStableId);
            }
        }
        private int ObjectiveTargetLimit(PassengerRequestDefinition objective)
        {
            if (host?.Session == null || host.DriverNeeds?.State == null) return 0;
            int capacity = 0;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var action in host.Session.Passenger?.customItemInteractions ?? Array.Empty<TruckTaxiCustomItemInteractionDefinition>())
            {
                if (action == null || action.customEventId != objective.requiredCustomEventId ||
                    !string.IsNullOrEmpty(objective.requiredItemStableId) && action.requiredItemStableId != objective.requiredItemStableId ||
                    host.Session.CustomInteractionBlockReason(action, host.DriverNeeds.State) != null || !seen.Add(action.stableId)) continue;
                var item = TruckTaxiNeedsItems.FindByStableId(action.requiredItemStableId);
                int available = action.oncePerRide ? 1 : action.consumeQuantity > 0 ? host.DriverNeeds.State.Count(item.Item) / action.consumeQuantity : 10000;
                // Conservative: shared inventory must not be counted once for each definition.
                capacity = Math.Max(capacity, available);
            }
            return capacity;
        }
        private AudioSource CreateVoice(string label)
        {
            var child = new GameObject(label); child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.spatialBlend = 0; host?.Audio?.Route(source, TruckTaxiAudioCategory.Voices);
            return source;
        }
        public void Begin(TruckTaxiCustomInteractionProfile value, Transform driverVisual, Transform passengerVisual, bool inputHeldByEvent = false)
        {
            if (!isActiveAndEnabled) return;
            int requested = ++revision;
            CancelCurrent();
            if (requested != revision || value == null || !isActiveAndEnabled) return;
            if (voice == null) Initialize(host);
            profile = value; driver = driverVisual; passenger = passengerVisual;
            externalInputHold = inputHeldByEvent;
            IsPlaying = true; SetPhase(TruckTaxiCustomPhase.Started);
        }
        public void Beat()
        {
            if (!IsPlaying || phase == TruckTaxiCustomPhase.Completed) return;
            int requested = revision;
            string id = profile.stableId;
            Notify(() => interactionBeat.Invoke(id));
            Notify(() => PhaseChanged?.Invoke(id, TruckTaxiCustomPhase.Beat));
            if (requested == revision && IsPlaying && audioProfile != null) PlayAudio(audioProfile, TruckTaxiCustomPhase.Beat, true);
        }
        public void Complete()
        {
            if (IsPlaying && phase != TruckTaxiCustomPhase.Completed) SetPhase(TruckTaxiCustomPhase.Completed);
        }
        public bool TryExecuteItem(TruckTaxiCustomItemInteractionDefinition definition, out string reason)
        {
            reason = !isActiveAndEnabled ? "Content playback is disabled." : IsPlaying ? "Wait for the current presentation." : host?.Session == null ? "Session unavailable." :
                host.Player == null ? "Player truck unavailable." : null;
            if (reason != null) return false;
            if (!host.Session.TryExecuteCustomItemInteraction(definition, host.DriverNeeds?.State, out reason)) return false;
            var value = ScriptableObject.CreateInstance<TruckTaxiCustomInteractionProfile>();
            value.hideFlags = HideFlags.DontSave; value.stableId = definition.stableId;
            value.startPresentation = definition.presentationProfile; value.startAudioProfile = definition.audioProfile;
            Begin(value, host.Player.GetComponent<TruckTaxiDriverPresentation>()?.Occupant, host.Passengers?.PrimaryActor?.transform);
            OwnTemporaryProfile(value, StandaloneDuration(definition.presentationProfile, 1));
            return true;
        }
        public void Preview(TruckTaxiCustomPresentationProfile visual, TruckTaxiCustomReactionAudioProfile audio)
        {
            if (!isActiveAndEnabled) return;
            var value = ScriptableObject.CreateInstance<TruckTaxiCustomInteractionProfile>();
            value.hideFlags = HideFlags.DontSave; value.stableId = "custom.preview";
            value.startPresentation = visual; value.startAudioProfile = audio;
            Begin(value, host?.Player?.GetComponent<TruckTaxiDriverPresentation>()?.Occupant, host?.Passengers?.PrimaryActor?.transform);
            OwnTemporaryProfile(value, StandaloneDuration(visual, 3));
        }
        private void OwnTemporaryProfile(TruckTaxiCustomInteractionProfile value, float duration)
        {
            if (!IsPlaying || profile != value) { Destroy(value); return; }
            temporaryProfile = value;
            standaloneEndAt = Time.unscaledTime + duration;
        }
        private static float StandaloneDuration(TruckTaxiCustomPresentationProfile visual, float fallback) =>
            visual != null && float.IsFinite(visual.duration) ? Mathf.Max(.1f, visual.duration) : fallback;
        public void Cancel()
        {
            ++revision;
            CancelCurrent();
        }
        private void CancelCurrent()
        {
            bool cancelled = IsPlaying && phase != TruckTaxiCustomPhase.Completed;
            string id = profile != null ? profile.stableId : null;
            Finish();
            if (cancelled)
            {
                Notify(() => interactionCancelled.Invoke(id));
                Notify(() => PhaseChanged?.Invoke(id, TruckTaxiCustomPhase.Cancelled));
            }
        }
        private void SetPhase(TruckTaxiCustomPhase next)
        {
            ClearPresentation(); voice.Stop(); accent.Stop();
            phase = next; phaseTime = 0;
            presentation = next == TruckTaxiCustomPhase.Started ? profile.startPresentation :
                next == TruckTaxiCustomPhase.Completed ? profile.completionPresentation : SelectVariant();
            audioProfile = next == TruckTaxiCustomPhase.Started ? profile.startAudioProfile :
                next == TruckTaxiCustomPhase.Completed ? profile.completionAudioProfile : profile.loopAudioProfile;
            if (next == TruckTaxiCustomPhase.Started && profile.optionalCustomInteractionProfile != null)
            {
                if (presentation == null) presentation = profile.optionalCustomInteractionProfile.presentationProfile;
                if (audioProfile == null) audioProfile = profile.optionalCustomInteractionProfile.audioProfile;
            }
            if (audioProfile == null && presentation != null) audioProfile = presentation.audio;
            Status = profile.stableId + ": " + next;
            try { TryPresent(presentation); }
            catch (Exception exception) { ClearPresentation(); presentation = null; Debug.LogException(exception, this); }
            PlayAudio(audioProfile, next);
            string id = profile.stableId;
            if (next == TruckTaxiCustomPhase.Started) Notify(() => interactionStarted.Invoke(id));
            if (next == TruckTaxiCustomPhase.Completed) Notify(() => interactionCompleted.Invoke(id));
            Notify(() => PhaseChanged?.Invoke(id, next));
        }
        private TruckTaxiCustomPresentationProfile SelectVariant()
        {
            previousVariants.TryGetValue(profile, out var last);
            float total = 0; var unique = new HashSet<TruckTaxiCustomPresentationProfile>();
            foreach (var item in profile.variants ?? Array.Empty<TruckTaxiCustomPresentationProfile>())
                if (item != null && float.IsFinite(item.weight) && item.weight > 0) unique.Add(item);
            foreach (var item in unique)
                if (unique.Count < 2 || item != last) total += item.weight;
            float roll = UnityEngine.Random.value * total;
            foreach (var item in unique)
                if (unique.Count < 2 || item != last)
                { roll -= item.weight; if (roll <= 0) { previousVariants[profile] = item; return item; } }
            return profile.loopPresentation;
        }
        private void TryPresent(TruckTaxiCustomPresentationProfile value)
        {
            if (value == null) return;
            foreach (var problem in value.Validate())
            {
                Status = problem; Debug.LogWarning("Custom presentation skipped: " + problem, this);
                presentation = null;
                return;
            }
            var parent = host?.Player != null ? host.Player.transform : transform;
            if (value.driverAnimation != null || value.timeline != null) driverProxy = TruckTaxiCustomVisualProxy.Create(driver, parent, value.timeline == null ? value.driverAnimation : null);
            if (value.passengerAnimation != null || value.timeline != null) passengerProxy = TruckTaxiCustomVisualProxy.Create(passenger, parent, value.timeline == null ? value.passengerAnimation : null);
            PlaceAtAnchor(driverProxy?.Root.transform, value.driverAnchorId);
            PlaceAtAnchor(passengerProxy?.Root.transform, value.passengerAnchorId);
            if (value.presentationPrefab != null)
            {
                prefab = Instantiate(value.presentationPrefab, parent, false);
                foreach (var animator in prefab.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
                if (passenger != null) prefab.transform.SetPositionAndRotation(passenger.position, passenger.rotation);
                else if (driver != null) prefab.transform.SetPositionAndRotation(driver.position, driver.rotation);
                PlaceAtAnchor(prefab.transform, passenger != null ? value.passengerAnchorId : value.driverAnchorId);
            }
            if (value.timeline != null)
            {
                var target = new GameObject("Custom presentation timeline"); target.transform.SetParent(parent, false);
                director = target.AddComponent<PlayableDirector>(); director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                director.playableAsset = value.timeline;
                foreach (var output in value.timeline.outputs)
                    director.SetGenericBinding(output.sourceObject, output.streamName == "Driver" ? driverProxy?.Animator : passengerProxy?.Animator);
                director.Play();
            }
            // Existing companion events own their own parking/input lifecycle. Standalone content uses Taxi pause.
            if (value.lockInput && host != null && !externalInputHold && !ownsPause)
            { wasPaused = host.Paused; ownsPause = true; host.SetPaused(true); }
            PresentationsPlayed++;
        }
        private void PlaceAtAnchor(Transform target, string id)
        {
            if (target == null || string.IsNullOrEmpty(id) || host?.Player == null) return;
            var rig = host.Player.GetComponent<TruckTaxiCabOccupantRig>();
            if (rig == null || !Enum.TryParse<TruckTaxiCabAnchorId>(id, out var wanted))
            { Debug.LogWarning("Custom presentation anchor unavailable: " + id + ". Keeping the current visual pose.", this); return; }
            foreach (var anchor in rig.Anchors)
            {
                if (anchor.id != wanted) continue;
                var mount = string.IsNullOrEmpty(anchor.mountPath) ? rig.transform : rig.transform.Find(anchor.mountPath);
                if (mount == null) break;
                target.SetPositionAndRotation(mount.TransformPoint(anchor.localPosition), mount.rotation * Quaternion.Euler(anchor.localEulerAngles));
                return;
            }
            Debug.LogWarning("Custom presentation anchor unavailable: " + id + ". Keeping the current visual pose.", this);
        }
        public AudioClip ChooseClip(AudioClip[] clips, bool shuffle)
        {
            if (clips == null || clips.Length == 0) return null;
            if (!shuffle)
            {
                var valid = new List<AudioClip>(); foreach (var clip in clips) if (clip != null) valid.Add(clip);
                return valid.Count == 0 ? null : valid[UnityEngine.Random.Range(0, valid.Count)];
            }
            if (!bags.TryGetValue(clips, out var bag) || bag.Count == 0)
            {
                var valid = new List<AudioClip>(); foreach (var clip in clips) if (clip != null && !valid.Contains(clip)) valid.Add(clip);
                for (int i = valid.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var temp = valid[i]; valid[i] = valid[j]; valid[j] = temp; }
                if (valid.Count > 1 && previous.TryGetValue(clips, out var last) && valid[0] == last)
                { var temp = valid[0]; valid[0] = valid[1]; valid[1] = temp; }
                bag = new Queue<AudioClip>(valid); bags[clips] = bag;
            }
            if (bag.Count == 0) return null;
            var result = bag.Dequeue(); previous[clips] = result; return result;
        }
        private void PlayAudio(TruckTaxiCustomReactionAudioProfile value, TruckTaxiCustomPhase eventPhase, bool isAccent = false)
        {
            if (value == null || voice == null || isAccent && Time.unscaledTime < nextAudio) return;
            var source = isAccent && value.allowOverlap ? accent : voice;
            if (source.isPlaying || !value.allowOverlap && (voice.isPlaying || accent.isPlaying)) return;
            var clip = ChooseClip(value.Clips(eventPhase, isAccent), value.shuffleWithoutImmediateRepeat);
            if (clip == null) return;
            source.outputAudioMixerGroup = null;
            if (value.mixerGroup != null) source.outputAudioMixerGroup = value.mixerGroup;
            else host?.Audio?.Route(source, TruckTaxiAudioCategory.Voices);
            float lowPitch = float.IsFinite(value.pitchRange.x) ? Mathf.Clamp(value.pitchRange.x, .5f, 2) : 1;
            float highPitch = float.IsFinite(value.pitchRange.y) ? Mathf.Clamp(value.pitchRange.y, .5f, 2) : 1;
            source.pitch = UnityEngine.Random.Range(Mathf.Min(lowPitch, highPitch), Mathf.Max(lowPitch, highPitch));
            source.clip = clip; UpdateVolume(source, value); source.Play(); ClipsPlayed++;
            float minimum = float.IsFinite(value.minimumInterval) ? Mathf.Max(0, value.minimumInterval) : 1.5f;
            float maximum = float.IsFinite(value.maximumInterval) ? Mathf.Max(minimum, value.maximumInterval) : minimum;
            nextAudio = Time.unscaledTime + UnityEngine.Random.Range(minimum, maximum);
        }
        private void UpdateVolume(AudioSource source, TruckTaxiCustomReactionAudioProfile value)
        {
            bool inside = host?.Player?.NwhAdapter?.VehicleController?.CameraInsideVehicle == true;
            float gain = inside ? value.insideCabGain : value.exteriorGain;
            source.volume = (float.IsFinite(value.volume) ? Mathf.Clamp01(value.volume) : .7f) * (float.IsFinite(gain) ? Mathf.Clamp01(gain) : 1);
        }
        private void Update()
        {
            if (!IsPlaying) return;
            if (temporaryProfile != null && phase != TruckTaxiCustomPhase.Completed && Time.unscaledTime >= standaloneEndAt && !voice.isPlaying) Complete();
            phaseTime += Time.unscaledDeltaTime;
            float duration = presentation != null ? Mathf.Max(.05f, presentation.duration) : .1f;
            if (audioProfile != null) { UpdateVolume(voice, audioProfile); UpdateVolume(accent, audioProfile); }
            FadeAlpha = presentation == null ? 0 : .75f * Mathf.Max(
                presentation.fadeIn > 0 ? 1 - Mathf.Clamp01(phaseTime / presentation.fadeIn) : 0,
                phase != TruckTaxiCustomPhase.Beat && presentation.fadeOut > 0 ? Mathf.Clamp01((phaseTime - duration + presentation.fadeOut) / presentation.fadeOut) : 0);
            if (presentation != null && !presentation.allowMovement && host?.Player != null)
            {
                var body = host.Player.GetComponent<Rigidbody>();
                if (body != null && body.linearVelocity.sqrMagnitude > 1) { Cancel(); return; }
            }
            if (phase == TruckTaxiCustomPhase.Started && phaseTime >= duration && !voice.isPlaying) SetPhase(TruckTaxiCustomPhase.Beat);
            else if (phase == TruckTaxiCustomPhase.Completed && phaseTime >= duration && !voice.isPlaying && !accent.isPlaying) Finish();
            else if (phase == TruckTaxiCustomPhase.Beat && Time.unscaledTime >= nextAudio) PlayAudio(audioProfile, phase);
        }
        private void ClearPresentation()
        {
            if (director != null) { director.Stop(); Destroy(director.gameObject); director = null; }
            driverProxy?.Dispose(); passengerProxy?.Dispose(); driverProxy = passengerProxy = null;
            if (prefab != null) { prefab.SetActive(false); Destroy(prefab); prefab = null; }
        }
        private void Finish()
        {
            ClearPresentation(); if (voice != null) voice.Stop(); if (accent != null) accent.Stop();
            IsPlaying = false; FadeAlpha = 0; profile = null; presentation = null;
            audioProfile = null; driver = passenger = null; externalInputHold = false;
            if (temporaryProfile != null) { Destroy(temporaryProfile); temporaryProfile = null; }
            if (ownsPause && host != null) host.SetPaused(wasPaused);
            ownsPause = false;
        }
        private void OnDisable() => Cancel();
        private void Notify(Action notification)
        {
            try { notification(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
        private void OnDestroy()
        {
            Cancel();
            if (host?.Session == null) return;
            if (host.Session.CustomObjectiveTargetLimit == ObjectiveTargetLimit) host.Session.CustomObjectiveTargetLimit = null;
            foreach (var pair in registeredActions) host.Session.Capabilities.RegisterCustomItemAction(pair.eventId, pair.itemId, false);
        }
    }
}
