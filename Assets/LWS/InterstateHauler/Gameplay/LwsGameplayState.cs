using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsGameplayState
    {
        Initializing,
        LoadingWorld,
        FreeDrive,
        Paused,
        Transitioning,
        RecoveryError,
        AtDepot,
        JobSelection,
        TrailerPickup,
        HaulActive,
        Delivery,
        DeliveryResults
    }

    public readonly struct LwsGameplayStateChangedEvent
    {
        public LwsGameplayStateChangedEvent(
            LwsGameplayState previousState,
            LwsGameplayState currentState,
            LwsGameplayState returnState,
            string reason,
            DateTime enteredAtUtc)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            ReturnState = returnState;
            Reason = reason ?? string.Empty;
            EnteredAtUtc = enteredAtUtc;
        }

        public LwsGameplayState PreviousState { get; }
        public LwsGameplayState CurrentState { get; }
        public LwsGameplayState ReturnState { get; }
        public string Reason { get; }
        public DateTime EnteredAtUtc { get; }
        public bool AllowsDrivingInput => LwsGameplayStateRules.AllowsDrivingInput(CurrentState);
    }

    public readonly struct LwsGameplayStateTransitionResult
    {
        public LwsGameplayStateTransitionResult(
            bool succeeded,
            bool changed,
            LwsGameplayState from,
            LwsGameplayState to,
            string reason,
            string message)
        {
            Succeeded = succeeded;
            Changed = changed;
            From = from;
            To = to;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public bool Changed { get; }
        public LwsGameplayState From { get; }
        public LwsGameplayState To { get; }
        public string Reason { get; }
        public string Message { get; }

        public static LwsGameplayStateTransitionResult Success(
            bool changed,
            LwsGameplayState from,
            LwsGameplayState to,
            string reason,
            string message)
        {
            return new LwsGameplayStateTransitionResult(true, changed, from, to, reason, message);
        }

        public static LwsGameplayStateTransitionResult Rejected(
            LwsGameplayState from,
            LwsGameplayState to,
            string reason,
            string message)
        {
            return new LwsGameplayStateTransitionResult(false, false, from, to, reason, message);
        }
    }

    public interface ILwsGameplayStateService : ILwsService
    {
        LwsGameplayState CurrentState { get; }
        LwsGameplayState PreviousState { get; }
        LwsGameplayState PauseReturnState { get; }
        string LastTransitionReason { get; }
        DateTime StateEnteredAtUtc { get; }
        TimeSpan StateAge { get; }
        bool AllowsDrivingInput { get; }
        event Action<LwsGameplayStateChangedEvent> StateChanged;
        LwsGameplayStateTransitionResult TryTransitionTo(LwsGameplayState targetState, string reason);
        LwsGameplayStateTransitionResult Pause(string reason);
        LwsGameplayStateTransitionResult Resume(string reason);
        LwsGameplayStateTransitionResult EnterLoadingWorld(string reason);
        LwsGameplayStateTransitionResult EnterFreeDrive(string reason);
        LwsGameplayStateTransitionResult EnterRecoveryError(string reason);
        bool CanTransitionTo(LwsGameplayState targetState, out string rejectionReason);
    }

    public static class LwsGameplayStateRules
    {
        public static bool AllowsDrivingInput(LwsGameplayState state)
        {
            switch (state)
            {
                case LwsGameplayState.FreeDrive:
                case LwsGameplayState.TrailerPickup:
                case LwsGameplayState.HaulActive:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsCurrentRuntimeState(LwsGameplayState state)
        {
            return state == LwsGameplayState.Initializing ||
                   state == LwsGameplayState.LoadingWorld ||
                   state == LwsGameplayState.FreeDrive ||
                   state == LwsGameplayState.Paused ||
                   state == LwsGameplayState.Transitioning ||
                   state == LwsGameplayState.RecoveryError;
        }

        public static bool IsFutureReservedState(LwsGameplayState state)
        {
            return state == LwsGameplayState.AtDepot ||
                   state == LwsGameplayState.JobSelection ||
                   state == LwsGameplayState.TrailerPickup ||
                   state == LwsGameplayState.HaulActive ||
                   state == LwsGameplayState.Delivery ||
                   state == LwsGameplayState.DeliveryResults;
        }

        public static bool IsPauseReturnState(LwsGameplayState state)
        {
            return state == LwsGameplayState.FreeDrive ||
                   IsFutureReservedState(state);
        }
    }

    public sealed class LwsGameplayStateService : ILwsGameplayStateService
    {
        public const string ServiceIdentifier = "lws.gameplay.state";

        private LwsGameplayState _currentState = LwsGameplayState.Initializing;
        private LwsGameplayState _previousState = LwsGameplayState.Initializing;
        private LwsGameplayState _pauseReturnState = LwsGameplayState.FreeDrive;
        private string _lastTransitionReason = "Gameplay state service created.";
        private DateTime _stateEnteredAtUtc = DateTime.UtcNow;

        public string ServiceId => ServiceIdentifier;
        public LwsGameplayState CurrentState => _currentState;
        public LwsGameplayState PreviousState => _previousState;
        public LwsGameplayState PauseReturnState => _pauseReturnState;
        public string LastTransitionReason => _lastTransitionReason;
        public DateTime StateEnteredAtUtc => _stateEnteredAtUtc;
        public TimeSpan StateAge => DateTime.UtcNow - _stateEnteredAtUtc;
        public bool AllowsDrivingInput => LwsGameplayStateRules.AllowsDrivingInput(_currentState);

        public event Action<LwsGameplayStateChangedEvent> StateChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            _currentState = LwsGameplayState.Initializing;
            _previousState = LwsGameplayState.Initializing;
            _pauseReturnState = LwsGameplayState.FreeDrive;
            _lastTransitionReason = "LWS gameplay state service initialized.";
            _stateEnteredAtUtc = DateTime.UtcNow;
            return LwsServiceResult.Success("LWS gameplay state service initialized in Initializing state.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            StateChanged = null;
            _currentState = LwsGameplayState.Initializing;
            _previousState = LwsGameplayState.Initializing;
            _pauseReturnState = LwsGameplayState.FreeDrive;
            _lastTransitionReason = "LWS gameplay state service shut down.";
            _stateEnteredAtUtc = DateTime.UtcNow;
            return LwsServiceResult.Success("LWS gameplay state service shut down.");
        }

        public LwsGameplayStateTransitionResult Pause(string reason)
        {
            if (LwsGameplayStateRules.IsPauseReturnState(_currentState))
            {
                _pauseReturnState = _currentState;
            }

            return TryTransitionTo(LwsGameplayState.Paused, reason);
        }

        public LwsGameplayStateTransitionResult Resume(string reason)
        {
            if (_currentState != LwsGameplayState.Paused)
            {
                return LwsGameplayStateTransitionResult.Rejected(
                    _currentState,
                    _pauseReturnState,
                    NormalizeReason(reason),
                    "Resume is only valid from Paused.");
            }

            return TryTransitionTo(_pauseReturnState, reason);
        }

        public LwsGameplayStateTransitionResult EnterLoadingWorld(string reason)
        {
            return TryTransitionTo(LwsGameplayState.LoadingWorld, reason);
        }

        public LwsGameplayStateTransitionResult EnterFreeDrive(string reason)
        {
            return TryTransitionTo(LwsGameplayState.FreeDrive, reason);
        }

        public LwsGameplayStateTransitionResult EnterRecoveryError(string reason)
        {
            return TryTransitionTo(LwsGameplayState.RecoveryError, reason);
        }

        public LwsGameplayStateTransitionResult TryTransitionTo(LwsGameplayState targetState, string reason)
        {
            string normalizedReason = NormalizeReason(reason);
            if (_currentState == targetState)
            {
                _lastTransitionReason = normalizedReason;
                return LwsGameplayStateTransitionResult.Success(
                    false,
                    _currentState,
                    targetState,
                    normalizedReason,
                    $"Gameplay state already {targetState}.");
            }

            if (!CanTransitionTo(targetState, out string rejectionReason))
            {
                string message = $"Rejected gameplay state transition {_currentState} -> {targetState}: {rejectionReason}";
                Debug.LogWarning($"{message} Reason: {normalizedReason}");
                return LwsGameplayStateTransitionResult.Rejected(_currentState, targetState, normalizedReason, message);
            }

            LwsGameplayState from = _currentState;
            _previousState = from;
            _currentState = targetState;
            _lastTransitionReason = normalizedReason;
            _stateEnteredAtUtc = DateTime.UtcNow;

            if (LwsGameplayStateRules.IsPauseReturnState(targetState))
            {
                _pauseReturnState = targetState;
            }

            var changedEvent = new LwsGameplayStateChangedEvent(
                from,
                targetState,
                _pauseReturnState,
                normalizedReason,
                _stateEnteredAtUtc);
            StateChanged?.Invoke(changedEvent);

            return LwsGameplayStateTransitionResult.Success(
                true,
                from,
                targetState,
                normalizedReason,
                $"Gameplay state transitioned {from} -> {targetState}.");
        }

        public bool CanTransitionTo(LwsGameplayState targetState, out string rejectionReason)
        {
            if (!Enum.IsDefined(typeof(LwsGameplayState), targetState))
            {
                rejectionReason = "Target state is not part of the canonical LWS gameplay vocabulary.";
                return false;
            }

            if (_currentState == targetState)
            {
                rejectionReason = string.Empty;
                return true;
            }

            switch (_currentState)
            {
                case LwsGameplayState.Initializing:
                    return Allow(
                        targetState == LwsGameplayState.FreeDrive ||
                        targetState == LwsGameplayState.LoadingWorld ||
                        targetState == LwsGameplayState.RecoveryError,
                        "Initializing may only enter FreeDrive, LoadingWorld, or RecoveryError.",
                        out rejectionReason);

                case LwsGameplayState.FreeDrive:
                    return Allow(
                        targetState == LwsGameplayState.Paused ||
                        targetState == LwsGameplayState.LoadingWorld ||
                        targetState == LwsGameplayState.Transitioning ||
                        LwsGameplayStateRules.IsFutureReservedState(targetState),
                        "FreeDrive may pause, load, transition, or enter a future reserved gameplay state.",
                        out rejectionReason);

                case LwsGameplayState.Paused:
                    return Allow(
                        targetState == LwsGameplayState.LoadingWorld ||
                        LwsGameplayStateRules.IsPauseReturnState(targetState),
                        "Paused may load or resume to the remembered safe gameplay state.",
                        out rejectionReason);

                case LwsGameplayState.LoadingWorld:
                    return Allow(
                        targetState == LwsGameplayState.FreeDrive ||
                        targetState == LwsGameplayState.RecoveryError,
                        "LoadingWorld may complete to FreeDrive or fail to RecoveryError.",
                        out rejectionReason);

                case LwsGameplayState.Transitioning:
                    return Allow(
                        targetState == LwsGameplayState.FreeDrive ||
                        targetState == LwsGameplayState.RecoveryError ||
                        LwsGameplayStateRules.IsFutureReservedState(targetState),
                        "Transitioning may complete to a gameplay state or fail to RecoveryError.",
                        out rejectionReason);

                case LwsGameplayState.RecoveryError:
                    return Allow(
                        targetState == LwsGameplayState.LoadingWorld ||
                        LwsGameplayStateRules.IsPauseReturnState(targetState),
                        "RecoveryError may retry loading or return to a safe gameplay state.",
                        out rejectionReason);

                default:
                    return Allow(
                        targetState == LwsGameplayState.Paused ||
                        targetState == LwsGameplayState.LoadingWorld ||
                        targetState == LwsGameplayState.Transitioning ||
                        targetState == LwsGameplayState.FreeDrive ||
                        LwsGameplayStateRules.IsFutureReservedState(targetState),
                        "Future reserved gameplay states may pause, load, transition, free drive, or move between future reserved states.",
                        out rejectionReason);
            }
        }

        private static bool Allow(bool allowed, string rejectionMessage, out string rejectionReason)
        {
            rejectionReason = allowed ? string.Empty : rejectionMessage;
            return allowed;
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "No reason supplied." : reason.Trim();
        }
    }
}