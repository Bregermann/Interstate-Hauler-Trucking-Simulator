using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-210)]
    [DisallowMultipleComponent]
    public sealed class DeadAirAudioDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class AudioStep
        {
            public DeadAirAudioChannel channel = DeadAirAudioChannel.Dispatcher;
            public AudioClip clip;
            public string speaker = "DISPATCH";
            [TextArea] public string subtitle;
            public float preDelaySeconds;
            public float postDelaySeconds = 0.1f;
            [Range(0f, 1f)] public float volume = 1f;
            public bool staticSquelch;
        }

        [Serializable]
        public sealed class AudioSequence
        {
            public string sequenceId;
            public bool interruptCurrent;
            public bool queueIfBusy = true;
            public List<AudioStep> steps = new List<AudioStep>();
        }

        private readonly Dictionary<DeadAirAudioChannel, AudioSource> _sources = new Dictionary<DeadAirAudioChannel, AudioSource>();
        private readonly Queue<AudioSequence> _queue = new Queue<AudioSequence>();
        private Coroutine _playRoutine;

        public event Action<string, string, DeadAirAudioChannel> SubtitleChanged;
        public event Action SubtitleCleared;
        public bool IsPlaying => _playRoutine != null;
        public string CurrentSequenceId { get; private set; } = string.Empty;
        public string LastWarning { get; private set; } = string.Empty;

        private void Awake()
        {
            EnsureSources();
        }

        public void Play(AudioSequence sequence)
        {
            if (sequence == null)
            {
                return;
            }

            EnsureSources();
            if (sequence.interruptCurrent)
            {
                StopAll();
            }

            if (_playRoutine != null)
            {
                if (sequence.queueIfBusy)
                {
                    _queue.Enqueue(sequence);
                }

                return;
            }

            _playRoutine = StartCoroutine(PlayRoutine(sequence));
        }

        public void PlaySubtitleOnly(string speaker, string subtitle, DeadAirAudioChannel channel = DeadAirAudioChannel.Dispatcher, float seconds = 3f)
        {
            var sequence = new AudioSequence
            {
                sequenceId = $"subtitle.{Time.time:0.00}",
                interruptCurrent = false,
                queueIfBusy = true
            };
            sequence.steps.Add(new AudioStep
            {
                speaker = speaker,
                subtitle = subtitle,
                channel = channel,
                postDelaySeconds = seconds
            });
            Play(sequence);
        }

        public void StopAll()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            _queue.Clear();
            foreach (AudioSource source in _sources.Values)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }

            CurrentSequenceId = string.Empty;
            SubtitleCleared?.Invoke();
        }

        private IEnumerator PlayRoutine(AudioSequence sequence)
        {
            CurrentSequenceId = sequence.sequenceId;
            for (int i = 0; i < sequence.steps.Count; i++)
            {
                AudioStep step = sequence.steps[i];
                if (step == null)
                {
                    continue;
                }

                if (step.preDelaySeconds > 0f)
                {
                    yield return new WaitForSeconds(step.preDelaySeconds);
                }

                SubtitleChanged?.Invoke(step.speaker, step.subtitle, step.channel);
                if (step.clip != null && _sources.TryGetValue(step.channel, out AudioSource source))
                {
                    source.volume = Mathf.Clamp01(step.volume);
                    source.clip = step.clip;
                    source.Play();
                    yield return new WaitForSeconds(step.clip.length);
                }
                else if (step.clip == null && !string.IsNullOrWhiteSpace(step.subtitle))
                {
                    LastWarning = $"Audio step in {sequence.sequenceId} has no clip; subtitle-only fallback used.";
                }

                if (step.staticSquelch)
                {
                    yield return new WaitForSeconds(0.08f);
                }

                if (step.postDelaySeconds > 0f)
                {
                    yield return new WaitForSeconds(step.postDelaySeconds);
                }
            }

            SubtitleCleared?.Invoke();
            CurrentSequenceId = string.Empty;
            _playRoutine = null;

            if (_queue.Count > 0)
            {
                _playRoutine = StartCoroutine(PlayRoutine(_queue.Dequeue()));
            }
        }

        private void EnsureSources()
        {
            foreach (DeadAirAudioChannel channel in Enum.GetValues(typeof(DeadAirAudioChannel)))
            {
                if (_sources.ContainsKey(channel) && _sources[channel] != null)
                {
                    continue;
                }

                GameObject sourceObject = new GameObject($"Dead Air {channel} Audio");
                sourceObject.transform.SetParent(transform, false);
                AudioSource source = sourceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = channel == DeadAirAudioChannel.World ? 1f : 0f;
                _sources[channel] = source;
            }
        }
    }
}
