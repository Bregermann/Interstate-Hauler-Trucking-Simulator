using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-250)]
    [DisallowMultipleComponent]
    public sealed class DeadAirStoryDirector : MonoBehaviour
    {
        [SerializeField] private bool logTriggerEvents = true;

        private readonly Dictionary<string, DeadAirTriggerEvent> _firedTriggers = new Dictionary<string, DeadAirTriggerEvent>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DeadAirChoiceOutcome> _choiceOutcomes = new Dictionary<string, DeadAirChoiceOutcome>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _runtimeLog = new List<string>();

        public event Action<DeadAirTriggerEvent> TriggerActivated;
        public event Action<string, DeadAirChoiceOutcome> ChoiceCommitted;
        public IReadOnlyList<string> RuntimeLog => _runtimeLog;
        public int TriggeredBeatCount => _firedTriggers.Count;
        public string CurrentBeatId { get; private set; } = string.Empty;

        public void ResetRun()
        {
            _firedTriggers.Clear();
            _choiceOutcomes.Clear();
            _runtimeLog.Clear();
            CurrentBeatId = string.Empty;
            foreach (DeadAirTriggerZone trigger in FindObjectsByType<DeadAirTriggerZone>(FindObjectsSortMode.None))
            {
                trigger.ResetRuntimeState();
            }
        }

        public bool HasTriggered(string beatId)
        {
            return !string.IsNullOrWhiteSpace(beatId) && _firedTriggers.ContainsKey(beatId);
        }

        public bool TryGetChoice(string choiceId, out DeadAirChoiceOutcome outcome)
        {
            return _choiceOutcomes.TryGetValue(choiceId ?? string.Empty, out outcome);
        }

        public bool HandleTrigger(DeadAirTriggerEvent triggerEvent)
        {
            if (string.IsNullOrWhiteSpace(triggerEvent.beatId))
            {
                AddLog("Rejected trigger with no Beat ID.");
                return false;
            }

            CurrentBeatId = triggerEvent.beatId;
            _firedTriggers[triggerEvent.beatId] = triggerEvent;
            AddLog($"{triggerEvent.category}: {triggerEvent.beatId} - {triggerEvent.displayName}");
            TriggerActivated?.Invoke(triggerEvent);
            return true;
        }

        public bool CommitChoice(string choiceId, DeadAirChoiceOutcome outcome)
        {
            if (string.IsNullOrWhiteSpace(choiceId))
            {
                AddLog("Rejected choice commit with no choice ID.");
                return false;
            }

            if (_choiceOutcomes.TryGetValue(choiceId, out DeadAirChoiceOutcome existing) && existing != outcome)
            {
                AddLog($"Choice {choiceId} already committed as {existing}; ignored {outcome}.");
                return false;
            }

            _choiceOutcomes[choiceId] = outcome;
            AddLog($"Choice {choiceId}: {outcome}");
            ChoiceCommitted?.Invoke(choiceId, outcome);
            return true;
        }

        private void AddLog(string message)
        {
            string line = $"[{Time.time:0.0}] {message}";
            _runtimeLog.Add(line);
            while (_runtimeLog.Count > 32)
            {
                _runtimeLog.RemoveAt(0);
            }

            if (logTriggerEvents)
            {
                Debug.Log($"[Dead Air] {message}", this);
            }
        }
    }
}
